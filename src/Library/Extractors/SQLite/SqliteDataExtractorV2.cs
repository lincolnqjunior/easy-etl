using Library.Extractors;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;

namespace Library.Extractors.SQLite;

/// <summary>
/// Zero-allocation SQLite data extractor that uses EtlRecord instead of Dictionary.
/// Provides significant performance improvements by eliminating allocations in the hot path.
/// </summary>
public class SqliteDataExtractorV2 : IDataExtractorV2
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
    /// Initializes a new instance of the <see cref="SqliteDataExtractorV2"/> class.
    /// </summary>
    /// <param name="config">Configuration for SQLite extraction.</param>
    /// <param name="pool">Optional pool for buffer management.</param>
    public SqliteDataExtractorV2(DatabaseDataExtractorConfig config, EtlRecordPool? pool = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _pool = pool ?? new EtlRecordPool();
    }

    private void Init()
    {
        _timer.Restart();

        using var connection = new SqliteConnection(_config.ConnectionString);
        connection.Open();

        var countQuery = string.Format(_config.QueryCount, _config.TableName);
        using var commandCount = new SqliteCommand(countQuery, connection);
        TotalLines = Convert.ToInt64(commandCount.ExecuteScalar());

        // Build schema from configuration columns
        BuildSchema();
    }

    private void BuildSchema()
    {
        // Get non-header columns that are not ignored
        var columns = _config.Columns
            .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
            .ToList();

        // Build actions dictionary
        _actions = columns.ToDictionary(x => x.Name, x => x);

        // Create field definitions for schema
        var fields = columns.Select(col => (col.OutputName, MapTypeToFieldType(col.OutputType))).ToArray();
        _schema = EtlRecordPool.CreateSchema(fields);
    }

    private static FieldType MapTypeToFieldType(Type type)
    {
        if (type == typeof(int)) return FieldType.Int32;
        if (type == typeof(long)) return FieldType.Int64;
        if (type == typeof(double)) return FieldType.Double;
        if (type == typeof(float)) return FieldType.Float;
        if (type == typeof(bool)) return FieldType.Boolean;
        if (type == typeof(DateTime)) return FieldType.DateTime;
        if (type == typeof(string)) return FieldType.String;
        if (type == typeof(decimal)) return FieldType.Decimal;
        if (type == typeof(short)) return FieldType.Int16;
        if (type == typeof(byte)) return FieldType.Byte;
        if (type == typeof(Guid)) return FieldType.Guid;
        
        // Default to string for unknown types
        return FieldType.String;
    }

    public void Extract(RecordAction processRecord)
    {
        if (_schema == null || _actions == null)
        {
            Init();
        }

        // Rent a buffer for processing records
        var bufferSize = EtlRecordPool.CalculateBufferSize(_schema!);
        using var bufferContext = _pool.CreateBufferContext(bufferSize);

        try
        {
            int currentPage = 0;

            while (true)
            {
                using var connection = new SqliteConnection(_config.ConnectionString);
                connection.Open();

                long offset = currentPage * _config.PageSize;
                var selectQuery = string.Format(_config.QuerySelect, _config.TableName, _config.PageSize, offset);

                using var command = new SqliteCommand(selectQuery, connection);
                using var reader = command.ExecuteReader();

                if (!reader.HasRows)
                {
                    break;
                }

                while (reader.Read())
                {
                    // Create EtlRecord on the stack (zero allocation)
                    var record = new EtlRecord(bufferContext.AsSpan(), _schema!);

                    // Process each column
                    foreach (var action in _actions!.Values)
                    {
                        var columnIndex = reader.GetOrdinal(action.Name);
                        
                        if (reader.IsDBNull(columnIndex))
                        {
                            // Set field to null
                            var fieldIndex = Array.FindIndex(_schema!, f => f.Name == action.OutputName);
                            if (fieldIndex >= 0)
                            {
                                record.SetValue(fieldIndex, FieldValue.Null());
                            }
                        }
                        else
                        {
                            var value = reader.GetValue(columnIndex);
                            var processedValue = action.ExecuteAction(value);
                            SetFieldValue(ref record, action.OutputName ?? action.Name, processedValue);
                        }
                    }

                    processRecord(ref record);
                    LineNumber++;
                    NotifyReadProgress();
                }

                currentPage++;
            }

            NotifyFinish();
        }
        catch (Exception ex)
        {
            // For error reporting, create a dummy record (we can't capture the actual record due to ref struct limitations)
            OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, ex, new Dictionary<string, object?>(), LineNumber));
        }
        finally
        {
            _timer.Stop();
        }
    }

    private void SetFieldValue(ref EtlRecord record, string fieldName, object? value)
    {
        if (value == null)
        {
            var fieldIndex = Array.FindIndex(_schema!, f => f.Name == fieldName);
            if (fieldIndex >= 0)
            {
                record.SetValue(fieldIndex, FieldValue.Null());
            }
            return;
        }

        // Convert to FieldValue based on type
        FieldValue fieldValue = value switch
        {
            int i => FieldValue.FromInt32(i),
            long l => FieldValue.FromInt64(l),
            double d => FieldValue.FromDouble(d),
            float f => FieldValue.FromFloat(f),
            bool b => FieldValue.FromBoolean(b),
            DateTime dt => FieldValue.FromDateTime(dt),
            string s => FieldValue.FromString(s),
            decimal dec => FieldValue.FromDecimal(dec),
            short sh => FieldValue.FromInt16(sh),
            byte by => FieldValue.FromByte(by),
            Guid g => FieldValue.FromGuid(g),
            _ => FieldValue.FromString(value.ToString() ?? string.Empty)
        };

        record.SetValue(fieldName, fieldValue);
    }

    private void NotifyReadProgress()
    {
        if (LineNumber % _config.RaiseChangeEventAfer == 0)
        {
            PercentRead = (double)LineNumber / TotalLines * 100;
            var speed = LineNumber / _timer.Elapsed.TotalSeconds;
            OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, 0, 0, PercentRead, speed));
        }
    }

    private void NotifyFinish()
    {
        PercentRead = 100;
        var speed = LineNumber / _timer.Elapsed.TotalSeconds;
        OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, 0, 0, PercentRead, speed));
    }
}
