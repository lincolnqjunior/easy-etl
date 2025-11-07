using Library.Extractors;
using Library.Infra;
using Library.Infra.Adapters;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;

namespace Tests.Infra.Adapters;

/// <summary>
/// Tests for ExtractorV1ToV2Adapter.
/// Ensures backward compatibility between V1 and V2 interfaces.
/// </summary>
public class ExtractorV1ToV2AdapterTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitialize()
    {
        // Arrange
        var mockExtractor = new MockExtractor();
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String)
        );

        // Act
        var adapter = new ExtractorV1ToV2Adapter(mockExtractor, schema);

        // Assert
        Assert.NotNull(adapter);
        Assert.NotNull(adapter.Schema);
        Assert.Equal(2, adapter.Schema.Length);
    }

    [Fact]
    public void Constructor_WithNullExtractor_ShouldThrow()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(("Id", FieldType.Int32));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExtractorV1ToV2Adapter(null!, schema));
    }

    [Fact]
    public void Constructor_WithNullSchema_ShouldThrow()
    {
        // Arrange
        var mockExtractor = new MockExtractor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExtractorV1ToV2Adapter(mockExtractor, null!));
    }

    [Fact]
    public void Schema_ShouldReturnCorrectSchema()
    {
        // Arrange
        var mockExtractor = new MockExtractor();
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String),
            ("Age", FieldType.Int32)
        );
        var adapter = new ExtractorV1ToV2Adapter(mockExtractor, schema);

        // Act
        var resultSchema = adapter.Schema;

        // Assert
        Assert.Equal(3, resultSchema.Length);
        Assert.Equal("Id", resultSchema[0].Name);
        Assert.Equal("Name", resultSchema[1].Name);
        Assert.Equal("Age", resultSchema[2].Name);
    }

    [Fact]
    public void Extract_ShouldConvertDictionaryToRecord()
    {
        // Arrange
        var mockExtractor = new MockExtractor();
        mockExtractor.AddRow(new Dictionary<string, object?>
        {
            ["Id"] = 123,
            ["Name"] = "Test User",
            ["Age"] = 30
        });

        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String),
            ("Age", FieldType.Int32)
        );
        var adapter = new ExtractorV1ToV2Adapter(mockExtractor, schema);

        int callCount = 0;
        int? extractedId = null;
        string? extractedName = null;
        int? extractedAge = null;

        // Act
        adapter.Extract((ref EtlRecord record) =>
        {
            callCount++;
            extractedId = record.GetValue("Id").AsInt32();
            extractedName = record.GetValue("Name").AsString();
            extractedAge = record.GetValue("Age").AsInt32();
        });

        // Assert
        Assert.Equal(1, callCount);
        Assert.Equal(123, extractedId);
        Assert.Equal("Test User", extractedName);
        Assert.Equal(30, extractedAge);
    }

    [Fact]
    public void Extract_WithMultipleRows_ShouldProcessAll()
    {
        // Arrange
        var mockExtractor = new MockExtractor();
        mockExtractor.AddRow(new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "Alice" });
        mockExtractor.AddRow(new Dictionary<string, object?> { ["Id"] = 2, ["Name"] = "Bob" });
        mockExtractor.AddRow(new Dictionary<string, object?> { ["Id"] = 3, ["Name"] = "Charlie" });

        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String)
        );
        var adapter = new ExtractorV1ToV2Adapter(mockExtractor, schema);

        var processedIds = new List<int>();
        var processedNames = new List<string>();

        // Act
        adapter.Extract((ref EtlRecord record) =>
        {
            processedIds.Add(record.GetValue("Id").AsInt32());
            processedNames.Add(record.GetValue("Name").AsString()!);
        });

        // Assert
        Assert.Equal(3, processedIds.Count);
        Assert.Equal(new[] { 1, 2, 3 }, processedIds);
        Assert.Equal(new[] { "Alice", "Bob", "Charlie" }, processedNames);
    }

    [Fact]
    public void Properties_ShouldProxyToV1Extractor()
    {
        // Arrange
        var mockExtractor = new MockExtractor
        {
            TotalLines = 100,
            LineNumber = 50,
            BytesRead = 1024,
            PercentRead = 50.5,
            FileSize = 2048
        };
        var schema = EtlRecordPool.CreateSchema(("Id", FieldType.Int32));
        var adapter = new ExtractorV1ToV2Adapter(mockExtractor, schema);

        // Act & Assert
        Assert.Equal(100, adapter.TotalLines);
        Assert.Equal(50, adapter.LineNumber);
        Assert.Equal(1024, adapter.BytesRead);
        Assert.Equal(50.5, adapter.PercentRead);
        Assert.Equal(2048, adapter.FileSize);

        // Test setters
        adapter.TotalLines = 200;
        adapter.LineNumber = 75;
        adapter.BytesRead = 2048;
        adapter.PercentRead = 75.5;
        adapter.FileSize = 4096;

        Assert.Equal(200, mockExtractor.TotalLines);
        Assert.Equal(75, mockExtractor.LineNumber);
        Assert.Equal(2048, mockExtractor.BytesRead);
        Assert.Equal(75.5, mockExtractor.PercentRead);
        Assert.Equal(4096, mockExtractor.FileSize);
    }

    [Fact]
    public void Events_ShouldProxyToV1Extractor()
    {
        // Arrange
        var mockExtractor = new MockExtractor();
        var schema = EtlRecordPool.CreateSchema(("Id", FieldType.Int32));
        var adapter = new ExtractorV1ToV2Adapter(mockExtractor, schema);

        bool onReadCalled = false;
        bool onFinishCalled = false;
        bool onErrorCalled = false;

        adapter.OnRead += (args) => onReadCalled = true;
        adapter.OnFinish += (args) => onFinishCalled = true;
        adapter.OnError += (args) => onErrorCalled = true;

        // Act
        mockExtractor.TriggerOnRead();
        mockExtractor.TriggerOnFinish();
        mockExtractor.TriggerOnError();

        // Assert
        Assert.True(onReadCalled);
        Assert.True(onFinishCalled);
        Assert.True(onErrorCalled);
    }

    // Mock extractor for testing
    private class MockExtractor : IDataExtractor
    {
        private readonly List<Dictionary<string, object?>> _rows = new();

        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long TotalLines { get; set; }
        public int LineNumber { get; set; }
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }

        public void AddRow(Dictionary<string, object?> row)
        {
            _rows.Add(row);
        }

        public void Extract(RowAction processRow)
        {
            foreach (var row in _rows)
            {
                var rowCopy = new Dictionary<string, object?>(row);
                processRow(ref rowCopy);
            }
        }

        public void TriggerOnRead()
        {
            OnRead?.Invoke(new ExtractNotificationEventArgs(0, 0, 0, 0, 0, 0));
        }

        public void TriggerOnFinish()
        {
            OnFinish?.Invoke(new ExtractNotificationEventArgs(0, 0, 0, 0, 0, 0));
        }

        public void TriggerOnError()
        {
            OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, new Exception("Test"), new Dictionary<string, object?>(), 0));
        }
    }
}
