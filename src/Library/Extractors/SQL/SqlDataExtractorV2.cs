using Library.Extractors;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace Library.Extractors.SQL;

/// <summary>
/// Zero-allocation SQL Server data extractor that uses EtlRecord instead of Dictionary.
/// Provides significant performance improvements by eliminating allocations in the hot path.
/// </summary>
public class SqlDataExtractorV2 : IDataExtractorV2
{
    public event ReadNotification? OnRead;
    public event ReadNotification? OnFinish;
    public event EasyEtlErrorEventHandler? OnError;

    public long TotalLines { get; set; }
    public int LineNumber { get; set; }
    public long BytesRead { get; set; }
    public double PercentRead { get; set; }
    public long FileSize { get; set; }

    private readonly DatabaseDataExtractorConfig _config;
    private readonly EtlRecordPool _pool;
    private readonly Stopwatch _timer = new();
    private FieldDescriptor[]? _schema;
    private Dictionary<string, IColumnAction>? _actions;

    /// <summary>
    /// Gets the schema for the records produced by this extractor.
    /// Schema is built from the configuration columns after first query.
    /// </summary>
    public FieldDescriptor[] Schema => _schema ?? throw new InvalidOperationException("Schema not initialized. Call Extract first.");

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlDataExtractorV2"/> class.
    /// </summary>
    /// <param name="config">Configuration for SQL Server extraction.</param>
    /// <param name="pool">Optional pool for buffer management.</param>
    public SqlDataExtractorV2(DatabaseDataExtractorConfig config, EtlRecordPool? pool = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _pool = pool ?? new EtlRecordPool();
    }

    /// <summary>
    /// Extracts data from SQL Server using zero-allocation EtlRecord.
    /// </summary>
    /// <param name="processRecord">Action to process each record.</param>
    public void Extract(RecordAction processRecord)
    {
        try
        {
            InitAsync().Wait();

            // Build schema from configuration
            BuildSchema();

            // Rent buffer once for all records
            var bufferSize = EtlRecordPool.CalculateBufferSize(_schema!);
            var buffer = _pool.RentBuffer(bufferSize);

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                var selectQuery = string.Format(_config.QuerySelect, _config.TableName);
                using var command = new SqlCommand(selectQuery, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    // Create record with buffer
                    var record = new EtlRecord(buffer.AsSpan(), _schema!);

                    // Parse each column into the record
                    int fieldIndex = 0;
                    foreach (var action in _actions!.Values)
                    {
                        var columnIndex = reader.GetOrdinal(action.OutputName ?? action.Name);
                        
                        // Set value directly in record (zero-alloc)
                        if (reader.IsDBNull(columnIndex))
                        {
                            record.SetValue(fieldIndex, FieldValue.Null());
                        }
                        else
                        {
                            var value = reader.GetValue(columnIndex);
                            var transformedValue = action.ExecuteAction(value);
                            SetRecordValue(ref record, fieldIndex, transformedValue, action.OutputType);
                        }

                        fieldIndex++;
                    }

                    // Process record
                    processRecord(ref record);
                    LineNumber++;
                    NotifyReadProgress();
                }

                NotifyFinish();
            }
            finally
            {
                _pool.ReturnBuffer(buffer);
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, ex, new Dictionary<string, object?>(), LineNumber));
            throw;
        }
        finally
        {
            _timer.Stop();
        }
    }

    /// <summary>
    /// Builds the schema from configuration columns.
    /// </summary>
    private void BuildSchema()
    {
        _actions = _config.Columns
            .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
            .ToDictionary(x => x.Name, x => x);

        var schemaFields = new List<(string Name, FieldType Type)>();

        foreach (var action in _actions.Values)
        {
            var fieldType = MapTypeToFieldType(action.OutputType);
            schemaFields.Add((action.OutputName ?? action.Name, fieldType));
        }

        _schema = EtlRecordPool.CreateSchema(schemaFields.ToArray());
    }

    /// <summary>
    /// Maps .NET Type to FieldType enum.
    /// </summary>
    private static FieldType MapTypeToFieldType(Type type)
    {
        return type switch
        {
            Type t when t == typeof(string) => FieldType.String,
            Type t when t == typeof(int) => FieldType.Int32,
            Type t when t == typeof(long) => FieldType.Int64,
            Type t when t == typeof(double) => FieldType.Double,
            Type t when t == typeof(float) => FieldType.Float,
            Type t when t == typeof(decimal) => FieldType.Decimal,
            Type t when t == typeof(DateTime) => FieldType.DateTime,
            Type t when t == typeof(bool) => FieldType.Boolean,
            Type t when t == typeof(short) => FieldType.Int16,
            Type t when t == typeof(byte) => FieldType.Byte,
            Type t when t == typeof(Guid) => FieldType.Guid,
            _ => throw new NotSupportedException($"The type {type.Name} is not supported.")
        };
    }

    /// <summary>
    /// Sets a value in the record without allocations.
    /// </summary>
    private static void SetRecordValue(ref EtlRecord record, int index, object? value, Type outputType)
    {
        // Handle null values
        if (value == null)
        {
            record.SetValue(index, FieldValue.Null());
            return;
        }

        var fieldValue = outputType switch
        {
            Type t when t == typeof(string) => FieldValue.FromString((string)value),
            Type t when t == typeof(int) => FieldValue.FromInt32((int)value),
            Type t when t == typeof(long) => FieldValue.FromInt64((long)value),
            Type t when t == typeof(double) => FieldValue.FromDouble((double)value),
            Type t when t == typeof(float) => FieldValue.FromFloat((float)value),
            Type t when t == typeof(decimal) => FieldValue.FromDecimal((decimal)value),
            Type t when t == typeof(DateTime) => FieldValue.FromDateTime((DateTime)value),
            Type t when t == typeof(bool) => FieldValue.FromBoolean((bool)value),
            Type t when t == typeof(short) => FieldValue.FromInt16((short)value),
            Type t when t == typeof(byte) => FieldValue.FromByte((byte)value),
            Type t when t == typeof(Guid) => FieldValue.FromGuid((Guid)value),
            _ => throw new NotSupportedException($"The type {outputType.Name} is not supported.")
        };

        record.SetValue(index, fieldValue);
    }

    /// <summary>
    /// Initializes the data extraction process by calculating the total number of rows.
    /// </summary>
    private async Task InitAsync()
    {
        _timer.Restart();

        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var countQuery = string.Format(_config.QueryCount, _config.TableName);
        await using var commandCount = new SqlCommand(countQuery, connection);
        TotalLines = Convert.ToInt64(await commandCount.ExecuteScalarAsync());

        LineNumber = 0;
        BytesRead = 0;
    }

    /// <summary>
    /// Notifies subscribers of progress at configured intervals.
    /// </summary>
    private void NotifyReadProgress()
    {
        if (LineNumber % _config.RaiseChangeEventAfer == 0)
        {
            PercentRead = (double)LineNumber / TotalLines * 100;
            var speed = LineNumber / _timer.Elapsed.TotalSeconds;

            OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, BytesRead, FileSize, PercentRead, speed));
        }
    }

    /// <summary>
    /// Notifies subscribers that extraction has completed.
    /// </summary>
    private void NotifyFinish()
    {
        PercentRead = 100;
        var speed = LineNumber / _timer.Elapsed.TotalSeconds;
        OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, BytesRead, FileSize, PercentRead, speed));
    }

    /// <summary>
    /// Creates and returns a new SQL connection using the configured connection string.
    /// </summary>
    /// <returns>A new instance of SqlConnection.</returns>
    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_config.ConnectionString);
    }
}
