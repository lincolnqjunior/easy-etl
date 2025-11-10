using Library.Extractors;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;

namespace Library.Infra.Adapters;

/// <summary>
/// Adapter that wraps a V1 extractor to work with V2 (EtlRecord) interface.
/// Provides backward compatibility while using zero-allocation structures internally.
/// </summary>
public class ExtractorV1ToV2Adapter : IDataExtractorV2
{
    private readonly IDataExtractor _v1Extractor;
    private readonly FieldDescriptor[] _schema;
    private readonly EtlRecordPool _pool;

    public event ReadNotification? OnRead
    {
        add => _v1Extractor.OnRead += value;
        remove => _v1Extractor.OnRead -= value;
    }

    public event ReadNotification? OnFinish
    {
        add => _v1Extractor.OnFinish += value;
        remove => _v1Extractor.OnFinish -= value;
    }

    public event EasyEtlErrorEventHandler OnError
    {
        add => _v1Extractor.OnError += value;
        remove => _v1Extractor.OnError -= value;
    }

    public long TotalLines
    {
        get => _v1Extractor.TotalLines;
        set => _v1Extractor.TotalLines = value;
    }

    public int LineNumber
    {
        get => _v1Extractor.LineNumber;
        set => _v1Extractor.LineNumber = value;
    }

    public long BytesRead
    {
        get => _v1Extractor.BytesRead;
        set => _v1Extractor.BytesRead = value;
    }

    public double PercentRead
    {
        get => _v1Extractor.PercentRead;
        set => _v1Extractor.PercentRead = value;
    }

    public long FileSize
    {
        get => _v1Extractor.FileSize;
        set => _v1Extractor.FileSize = value;
    }

    public FieldDescriptor[] Schema => _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractorV1ToV2Adapter"/> class.
    /// </summary>
    /// <param name="v1Extractor">The V1 extractor to wrap.</param>
    /// <param name="schema">The schema for the records (must match dictionary keys).</param>
    /// <param name="pool">Optional pool for buffer management. If null, creates a new pool.</param>
    public ExtractorV1ToV2Adapter(IDataExtractor v1Extractor, FieldDescriptor[] schema, EtlRecordPool? pool = null)
    {
        _v1Extractor = v1Extractor ?? throw new ArgumentNullException(nameof(v1Extractor));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _pool = pool ?? new EtlRecordPool();
    }

    /// <summary>
    /// Extracts data using V1 extractor and converts to V2 records.
    /// </summary>
    public void Extract(RecordAction processRecord)
    {
        var bufferSize = EtlRecordPool.CalculateBufferSize(_schema);
        var buffer = _pool.RentBuffer(bufferSize);

        try
        {
            _v1Extractor.Extract((ref Dictionary<string, object?> row) =>
            {
                // Convert Dictionary to EtlRecord
                var record = EtlRecord.FromDictionary(buffer.AsSpan(), _schema, row);
                
                // Process with V2 API
                processRecord(ref record);
            });
        }
        finally
        {
            _pool.ReturnBuffer(buffer);
        }
    }
}
