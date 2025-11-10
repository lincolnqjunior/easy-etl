using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;
using System.Collections.Concurrent;
using System.Diagnostics;
using ParquetNet = Parquet;

namespace Library.Extractors.Parquet;

/// <summary>
/// Zero-allocation Parquet data extractor that uses EtlRecord instead of Dictionary.
/// Provides significant performance improvements by eliminating allocations in the hot path.
/// </summary>
public class ParquetDataExtractorV2 : IDataExtractorV2
{
    public event ReadNotification? OnRead;
    public event ReadNotification? OnFinish;
    public event EasyEtlErrorEventHandler? OnError;

    public long TotalLines { get; set; }
    public int LineNumber { get; set; }
    public long BytesRead { get; set; }
    public double PercentRead { get; set; }
    public long FileSize { get; set; }

    private readonly ParquetDataExtractorConfig _config;
    private readonly EtlRecordPool _pool;
    private readonly Stopwatch _timer = new();
    private readonly object _lock = new();
    private readonly ConcurrentBag<long> _bytesRead = new();
    private FieldDescriptor[]? _schema;
    private string[] _files = [];
    private Dictionary<string, IColumnAction> _actions = [];
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Gets the schema for the records produced by this extractor.
    /// Schema is built from the configuration columns.
    /// </summary>
    public FieldDescriptor[] Schema => _schema ?? throw new InvalidOperationException("Schema not initialized. Call Extract first.");

    /// <summary>
    /// Initializes a new instance of the <see cref="ParquetDataExtractorV2"/> class.
    /// </summary>
    /// <param name="config">Configuration for Parquet extraction.</param>
    /// <param name="pool">Optional pool for buffer management.</param>
    public ParquetDataExtractorV2(ParquetDataExtractorConfig config, EtlRecordPool? pool = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _pool = pool ?? new EtlRecordPool();
    }

    /// <summary>
    /// Extracts data from Parquet files using zero-allocation EtlRecord.
    /// </summary>
    /// <param name="processRecord">Action to process each record.</param>
    public void Extract(RecordAction processRecord)
    {
        try
        {
            Init();

            // Build schema from configuration
            BuildSchema();

            var cancelToken = _cts.Token;
            var semaphore = new SemaphoreSlim(1);
            var tasks = new List<Task>();

            // Rent buffer once per file (reused for all records in that file)
            var bufferSize = EtlRecordPool.CalculateBufferSize(_schema!);

            foreach (var file in _files)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await semaphore.WaitAsync(cancelToken);
                        _cts.Token.ThrowIfCancellationRequested();

                        // Rent buffer for this file
                        var buffer = _pool.RentBuffer(bufferSize);
                        try
                        {
                            await ProcessFileAsync(file, buffer, processRecord);
                            _bytesRead.Add(new FileInfo(file).Length);
                            lock (_lock) { BytesRead = _bytesRead.Sum(); }
                        }
                        finally
                        {
                            _pool.ReturnBuffer(buffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        await _cts.CancelAsync();
                        OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, ex, [], LineNumber));
                        throw;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            Task.WaitAll([.. tasks]);

            _timer.Stop();
            PercentRead = 100;
            var finalSpeed = LineNumber / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, finalSpeed));
        }
        finally
        {
            _cts.Dispose();
        }
    }

    private async Task ProcessFileAsync(string file, byte[] buffer, RecordAction processRecord)
    {
        using Stream fileStream = File.OpenRead(file);
        using var reader = await ParquetNet.ParquetReader.CreateAsync(fileStream);

        List<ParquetColumnData> columnsList = [];
        for (int rowGroupIndex = 0; rowGroupIndex < reader.RowGroupCount; rowGroupIndex++)
        {
            var rowGroupReader = reader.OpenRowGroupReader(rowGroupIndex);
            long rowCount = rowGroupReader.RowCount;

            columnsList.Clear();
            foreach (var dataField in reader.Schema.DataFields)
            {
                if (_actions.TryGetValue(dataField.Name, out var columnAction))
                {
                    columnsList.Add(new ParquetColumnData
                    {
                        OutputName = columnAction.OutputName ?? columnAction.Name,
                        ColumnData = await rowGroupReader.ReadColumnAsync(dataField)
                    });
                }
            }

            // Process rows synchronously after async read
            ProcessRows(columnsList, rowCount, buffer, processRecord);
        }
    }

    private void ProcessRows(List<ParquetColumnData> columnsList, long rowCount, byte[] buffer, RecordAction processRecord)
    {
        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            lock (_lock) { LineNumber++; }

            // Create EtlRecord on stack (zero allocation)
            var record = new EtlRecord(buffer, _schema!);

            // Populate record from Parquet columns
            foreach (var column in columnsList)
            {
                var value = column.ColumnData.Data.GetValue(rowIndex);
                var fieldValue = ConvertToFieldValue(value);
                record.SetValue(column.OutputName, fieldValue);
            }

            processRecord(ref record);

            lock (_lock)
            {
                if (LineNumber % _config.RaiseChangeEventAfer == 0)
                {
                    PercentRead = (double)LineNumber / TotalLines * 100;
                    var speed = LineNumber / _timer.Elapsed.TotalSeconds;
                    OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
                }
            }
        }
    }

    private void Init()
    {
        _timer.Restart();

        _actions = _config.Columns
            .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
            .ToDictionary(x => x.Name, x => x);

        _files = Directory.GetFiles(_config.Directory, _config.Mask);

        foreach (var file in _files)
        {
            using Stream fileStream = File.OpenRead(file);
            using var reader = ParquetNet.ParquetReader.CreateAsync(fileStream).Result;

            TotalLines += reader.Metadata?.RowGroups.Sum(x => x.NumRows) ?? 0;
            FileSize += fileStream.Length;
        }
    }

    private void BuildSchema()
    {
        // Build schema from configuration columns
        var fields = new List<(string Name, FieldType Type)>();

        var orderedColumns = _config.Columns
            .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
            .OrderBy(x => x.Position)
            .ToList();

        foreach (var column in orderedColumns)
        {
            var fieldType = MapToFieldType(column.OutputType);
            var fieldName = column.OutputName ?? column.Name;
            fields.Add((fieldName, fieldType));
        }

        _schema = EtlRecordPool.CreateSchema([.. fields]);
    }

    private static FieldType MapToFieldType(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return underlyingType switch
        {
            Type t when t == typeof(int) => FieldType.Int32,
            Type t when t == typeof(long) => FieldType.Int64,
            Type t when t == typeof(double) => FieldType.Double,
            Type t when t == typeof(float) => FieldType.Float,
            Type t when t == typeof(decimal) => FieldType.Decimal,
            Type t when t == typeof(bool) => FieldType.Boolean,
            Type t when t == typeof(DateTime) => FieldType.DateTime,
            Type t when t == typeof(string) => FieldType.String,
            Type t when t == typeof(short) => FieldType.Int16,
            Type t when t == typeof(byte) => FieldType.Byte,
            Type t when t == typeof(Guid) => FieldType.Guid,
            _ => FieldType.String // Default to string for unknown types
        };
    }

    private static FieldValue ConvertToFieldValue(object? value)
    {
        if (value == null || value == DBNull.Value)
            return FieldValue.Null();

        return value switch
        {
            int i => FieldValue.FromInt32(i),
            long l => FieldValue.FromInt64(l),
            double d => FieldValue.FromDouble(d),
            float f => FieldValue.FromFloat(f),
            decimal dec => FieldValue.FromDecimal(dec),
            bool b => FieldValue.FromBoolean(b),
            DateTime dt => FieldValue.FromDateTime(dt),
            string s => FieldValue.FromString(s),
            short sh => FieldValue.FromInt16(sh),
            byte by => FieldValue.FromByte(by),
            Guid g => FieldValue.FromGuid(g),
            _ => FieldValue.FromString(value.ToString() ?? string.Empty)
        };
    }

    private struct ParquetColumnData
    {
        public ParquetNet.Data.DataColumn ColumnData { get; set; }
        public string OutputName { get; set; }
    }
}
