using Library.Extractors;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Infra.Helpers;
using Library.Infra.ZeroAlloc;
using nietras.SeparatedValues;
using System.Diagnostics;

namespace Library.Extractors.Csv;

/// <summary>
/// Zero-allocation CSV data extractor that uses EtlRecord instead of Dictionary.
/// Provides significant performance improvements by eliminating allocations in the hot path.
/// </summary>
public class CsvDataExtractorV2 : IDataExtractorV2
{
    public event ReadNotification? OnRead;
    public event ReadNotification? OnFinish;
    public event EasyEtlErrorEventHandler? OnError;

    public long TotalLines { get; set; }
    public int LineNumber { get; set; }
    public long BytesRead { get; set; }
    public double PercentRead { get; set; }
    public long FileSize { get; set; }

    private readonly CsvDataExtractorConfig _config;
    private readonly EtlRecordPool _pool;
    private readonly Stopwatch _timer = new();
    private bool _first = true;
    private FieldDescriptor[]? _schema;

    /// <summary>
    /// Gets the schema for the records produced by this extractor.
    /// Schema is built from the configuration columns.
    /// </summary>
    public FieldDescriptor[] Schema => _schema ?? throw new InvalidOperationException("Schema not initialized. Call Extract first.");

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvDataExtractorV2"/> class.
    /// </summary>
    /// <param name="config">Configuration for CSV extraction.</param>
    /// <param name="pool">Optional pool for buffer management.</param>
    public CsvDataExtractorV2(CsvDataExtractorConfig config, EtlRecordPool? pool = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _pool = pool ?? new EtlRecordPool();
    }

    /// <summary>
    /// Extracts data from CSV file using zero-allocation EtlRecord.
    /// </summary>
    /// <param name="processRecord">Action to process each record.</param>
    public void Extract(RecordAction processRecord)
    {
        try
        {
            Init(_config.FilePath);

            // Build schema from configuration
            BuildSchema();

            // Rent buffer once for all records
            var bufferSize = EtlRecordPool.CalculateBufferSize(_schema!);
            var buffer = _pool.RentBuffer(bufferSize);

            try
            {
                using var reader = Sep.New(_config.Delimiter).Reader().FromFile(_config.FilePath);
                
                // Configure column actions
                var actions = _config.Columns
                    .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
                    .OrderBy(x => x.Position)
                    .ToList();

                foreach (var line in reader)
                {
                    // Skip header if specified
                    if (_config.HasHeader && _first)
                    {
                        _first = false;
                        continue;
                    }

                    // Create record with buffer
                    var record = new EtlRecord(buffer.AsSpan(), _schema!);

                    // Parse each column into the record
                    for (int i = 0; i < actions.Count; i++)
                    {
                        var action = actions[i];
                        var columnValue = line[action.Position];
                        BytesRead += System.Text.Encoding.Unicode.GetByteCount(columnValue.Span);

                        // Parse and set value directly in record (zero-alloc)
                        SetRecordValue(ref record, i, columnValue.Span, action.OutputType);
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
    }

    /// <summary>
    /// Builds the schema from configuration columns.
    /// </summary>
    private void BuildSchema()
    {
        var columns = _config.Columns
            .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
            .OrderBy(x => x.Position)
            .ToList();

        var schemaFields = new List<(string Name, FieldType Type)>();

        foreach (var column in columns)
        {
            var fieldType = MapTypeToFieldType(column.OutputType);
            schemaFields.Add((column.OutputName ?? column.Name, fieldType));
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
    /// Parses and sets a value in the record without allocations.
    /// </summary>
    private static void SetRecordValue(ref EtlRecord record, int index, ReadOnlySpan<char> valueSpan, Type outputType)
    {
        // Handle empty values
        if (valueSpan.IsEmpty || valueSpan.IsWhiteSpace())
        {
            record.SetValue(index, FieldValue.Null());
            return;
        }

        var fieldValue = outputType switch
        {
            Type t when t == typeof(string) => FieldValue.FromString(valueSpan.ToString()),
            Type t when t == typeof(int) => FieldValue.FromInt32(int.Parse(valueSpan)),
            Type t when t == typeof(long) => FieldValue.FromInt64(long.Parse(valueSpan)),
            Type t when t == typeof(double) => FieldValue.FromDouble(double.Parse(valueSpan)),
            Type t when t == typeof(float) => FieldValue.FromFloat(float.Parse(valueSpan)),
            Type t when t == typeof(decimal) => FieldValue.FromDecimal(decimal.Parse(valueSpan)),
            Type t when t == typeof(DateTime) => FieldValue.FromDateTime(DateTime.Parse(valueSpan)),
            Type t when t == typeof(bool) => FieldValue.FromBoolean(bool.Parse(valueSpan)),
            Type t when t == typeof(short) => FieldValue.FromInt16(short.Parse(valueSpan)),
            Type t when t == typeof(byte) => FieldValue.FromByte(byte.Parse(valueSpan)),
            Type t when t == typeof(Guid) => FieldValue.FromGuid(Guid.Parse(valueSpan)),
            _ => throw new NotSupportedException($"The type {outputType.Name} is not supported.")
        };

        record.SetValue(index, fieldValue);
    }

    /// <summary>
    /// Validates the file and sets initial properties.
    /// </summary>
    private void Init(string filePath)
    {
        _timer.Restart();

        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("File not found", filePath);

        FileSize = fileInfo.Length;
        TotalLines = fileInfo.CountLines().Result;
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
            PercentRead = (double)BytesRead / FileSize * 100;
            var speed = TotalLines / _timer.Elapsed.TotalSeconds;

            OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
        }
    }

    /// <summary>
    /// Notifies subscribers that extraction has completed.
    /// </summary>
    private void NotifyFinish()
    {
        _timer.Stop();

        TotalLines = LineNumber;
        BytesRead = FileSize;
        PercentRead = 100;
        var speed = TotalLines / _timer.Elapsed.TotalSeconds;

        OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
    }
}
