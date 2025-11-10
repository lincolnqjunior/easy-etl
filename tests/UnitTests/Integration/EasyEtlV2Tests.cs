using Library;
using Library.Extractors;
using Library.Infra;
using Library.Infra.Adapters;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;
using Library.Loaders;
using Library.Transformers;

namespace Tests.Integration;

/// <summary>
/// Integration tests for EasyEtlV2 with zero-allocation pipeline.
/// </summary>
public class EasyEtlV2Tests
{
    [Fact]
    public async Task Execute_WithSimplePipeline_ShouldProcessRecords()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String)
        );

        var mockExtractor = new MockExtractorV2(schema);
        mockExtractor.AddRecord(1, "Alice");
        mockExtractor.AddRecord(2, "Bob");
        mockExtractor.AddRecord(3, "Charlie");

        var transformer = new BypassDataTransformerV2(schema);
        var loader = new MockLoaderV2(schema);

        var etl = new EasyEtlV2(mockExtractor, transformer, loader);

        // Act
        await etl.Execute();

        // Assert
        Assert.Equal(3, loader.LoadedRecords.Count);
        Assert.Equal(1, loader.LoadedRecords[0].Id);
        Assert.Equal("Alice", loader.LoadedRecords[0].Name);
        Assert.Equal(2, loader.LoadedRecords[1].Id);
        Assert.Equal("Bob", loader.LoadedRecords[1].Name);
        Assert.Equal(3, loader.LoadedRecords[2].Id);
        Assert.Equal("Charlie", loader.LoadedRecords[2].Name);
    }

    [Fact]
    public async Task Execute_WithBypassConstructor_ShouldWork()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Value", FieldType.Double)
        );

        var mockExtractor = new MockExtractorV2(schema);
        mockExtractor.AddRecord(1, 10.5);
        mockExtractor.AddRecord(2, 20.5);

        var loader = new MockLoaderV2(schema);

        var etl = new EasyEtlV2(mockExtractor, loader);

        // Act
        await etl.Execute();

        // Assert
        Assert.Equal(2, loader.LoadedRecords.Count);
    }

    [Fact]
    public void Constructor_WithIncompatibleSchemas_ShouldThrow()
    {
        // Arrange
        var extractorSchema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String),
            ("Extra", FieldType.Boolean) // Different length!
        );

        var loaderSchema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Value", FieldType.Double)
        );

        var mockExtractor = new MockExtractorV2(extractorSchema);
        var transformer = new BypassDataTransformerV2(extractorSchema);
        var loader = new MockLoaderV2(loaderSchema);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new EasyEtlV2(mockExtractor, transformer, loader));
    }

    // Mock Extractor V2
    private class MockExtractorV2 : IDataExtractorV2
    {
        private readonly List<(int Id, object Value)> _records = new();
        private readonly FieldDescriptor[] _schema;
        private readonly EtlRecordPool _pool = new();

        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long TotalLines { get; set; }
        public int LineNumber { get; set; }
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }

        public FieldDescriptor[] Schema => _schema;

        public MockExtractorV2(FieldDescriptor[] schema)
        {
            _schema = schema;
        }

        public void AddRecord(int id, object value)
        {
            _records.Add((id, value));
        }

        public void Extract(RecordAction processRecord)
        {
            var bufferSize = EtlRecordPool.CalculateBufferSize(_schema);
            var buffer = _pool.RentBuffer(bufferSize);

            try
            {
                foreach (var (id, value) in _records)
                {
                    var record = new EtlRecord(buffer, _schema);
                    record.SetValue(0, FieldValue.FromInt32(id));
                    
                    if (value is string str)
                        record.SetValue(1, FieldValue.FromString(str));
                    else if (value is double dbl)
                        record.SetValue(1, FieldValue.FromDouble(dbl));
                    
                    TotalLines++;
                    LineNumber++;
                    
                    processRecord(ref record);
                }
            }
            finally
            {
                _pool.ReturnBuffer(buffer);
            }
        }
    }

    // Mock Loader V2
    private class MockLoaderV2 : IDataLoaderV2
    {
        public event LoadNotificationHandler? OnWrite;
        public event LoadNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long CurrentLine { get; set; }
        public long TotalLines { get; set; }
        public double PercentWritten { get; set; }

        public FieldDescriptor[] Schema { get; }

        public List<(int Id, string? Name)> LoadedRecords { get; } = new();

        public MockLoaderV2(FieldDescriptor[] schema)
        {
            Schema = schema;
        }

        public void Load(ref EtlRecord record, CancellationToken cancellationToken)
        {
            var id = record.GetValue(0).AsInt32();
            var value = record.GetValue(1);
            
            string? name = value.Type == FieldType.String ? value.AsString() : value.AsDouble().ToString();
            
            LoadedRecords.Add((id, name));
            CurrentLine++;
        }

        public Task Complete(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
