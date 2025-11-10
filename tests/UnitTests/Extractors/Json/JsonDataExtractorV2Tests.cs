using Library.Extractors.Json;
using Library.Infra.Config;
using Library.Infra.ZeroAlloc;

namespace Tests.Extractors.Json;

/// <summary>
/// Tests for JsonDataExtractorV2 - zero-allocation JSON/JSONL extractor.
/// </summary>
public class JsonDataExtractorV2Tests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testJsonPath;

    public JsonDataExtractorV2Tests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "JsonDataExtractorV2Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
        _testJsonPath = Path.Combine(_testDirectory, "test.jsonl");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Constructor_WithValidConfig_ShouldInitialize()
    {
        // Arrange
        CreateSimpleJsonLFile();
        var config = CreateBasicConfig();

        // Act
        var extractor = new JsonDataExtractorV2(config);

        // Assert
        Assert.NotNull(extractor);
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new JsonDataExtractorV2(null!));
    }

    [Fact]
    public void Constructor_WithMissingFile_ShouldThrow()
    {
        // Arrange
        var config = new JsonDataExtractorConfig
        {
            FilePath = "/non/existent/file.jsonl",
            RaiseChangeEventAfer = 1000
        };

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => new JsonDataExtractorV2(config));
    }

    [Fact]
    public void Extract_WithSimpleJsonL_ShouldProcessRecords()
    {
        // Arrange
        CreateSimpleJsonLFile();
        var config = CreateBasicConfig();
        var extractor = new JsonDataExtractorV2(config);

        var records = new List<(double Id, string Name)>();

        // Act
        extractor.Extract((ref EtlRecord record) =>
        {
            // JSON numbers are parsed as Double by default
            var id = record.GetValue("id").AsDouble();
            var name = record.GetValue("name").AsString();
            records.Add((id, name!));
        });

        // Assert
        Assert.Equal(3, records.Count);
        Assert.Equal((1.0, "Alice"), records[0]);
        Assert.Equal((2.0, "Bob"), records[1]);
        Assert.Equal((3.0, "Charlie"), records[2]);
    }

    [Fact]
    public void Extract_ShouldBuildCorrectSchema()
    {
        // Arrange
        CreateSimpleJsonLFile();
        var config = CreateBasicConfig();
        var extractor = new JsonDataExtractorV2(config);

        FieldDescriptor[]? capturedSchema = null;

        // Act
        extractor.Extract((ref EtlRecord record) =>
        {
            if (capturedSchema == null)
            {
                capturedSchema = extractor.Schema;
            }
        });

        // Assert
        Assert.NotNull(capturedSchema);
        Assert.Equal(2, capturedSchema!.Length);
        Assert.Contains(capturedSchema, f => f.Name == "id");
        Assert.Contains(capturedSchema, f => f.Name == "name");
    }

    [Fact]
    public void Extract_WithMultipleTypes_ShouldParseCorrectly()
    {
        // Arrange
        CreateMultiTypeJsonLFile();
        var config = CreateBasicConfig();
        var extractor = new JsonDataExtractorV2(config);

        var records = new List<(double Id, string Name, bool Active)>();

        // Act
        extractor.Extract((ref EtlRecord record) =>
        {
            // JSON numbers are typically parsed as Double
            var id = record.GetValue("id").AsDouble();
            var name = record.GetValue("name").AsString();
            var active = record.GetValue("active").AsBoolean();
            records.Add((id, name!, active));
        });

        // Assert
        Assert.Equal(2, records.Count);
        Assert.Equal((1.0, "Test1", true), records[0]);
        Assert.Equal((2.0, "Test2", false), records[1]);
    }

    [Fact]
    public void Extract_ShouldTrackMetrics()
    {
        // Arrange
        CreateSimpleJsonLFile();
        var config = CreateBasicConfig();
        var extractor = new JsonDataExtractorV2(config);

        // Act
        extractor.Extract((ref EtlRecord record) => { });

        // Assert
        Assert.Equal(3, extractor.LineNumber);
        Assert.True(extractor.BytesRead > 0);
        Assert.Equal(100, extractor.PercentRead);
    }

    [Fact]
    public void Extract_ShouldReuseBuffer()
    {
        // Arrange
        CreateSimpleJsonLFile();
        var config = CreateBasicConfig();
        var pool = new EtlRecordPool();
        var extractor = new JsonDataExtractorV2(config, pool);

        var recordCount = 0;

        // Act - All records should use the same buffer
        extractor.Extract((ref EtlRecord record) =>
        {
            recordCount++;
            // Verify we can read values (buffer is valid)
            var id = record.GetValue("id").AsDouble();
            Assert.True(id > 0);
        });

        // Assert
        Assert.Equal(3, recordCount);
    }

    [Fact]
    public void OnFinish_ShouldFireAfterCompletion()
    {
        // Arrange
        CreateSimpleJsonLFile();
        var config = CreateBasicConfig();
        var extractor = new JsonDataExtractorV2(config);

        bool onFinishCalled = false;
        long? finalLineCount = null;

        extractor.OnFinish += (args) =>
        {
            onFinishCalled = true;
            finalLineCount = args.LineCount;
        };

        // Act
        extractor.Extract((ref EtlRecord record) => { });

        // Assert
        Assert.True(onFinishCalled);
        Assert.Equal(3, finalLineCount);
    }

    // Helper methods

    private JsonDataExtractorConfig CreateBasicConfig()
    {
        return new JsonDataExtractorConfig
        {
            FilePath = _testJsonPath,
            RaiseChangeEventAfer = 1000
        };
    }

    private void CreateSimpleJsonLFile()
    {
        var lines = new[]
        {
            "{\"id\":1,\"name\":\"Alice\"}",
            "{\"id\":2,\"name\":\"Bob\"}",
            "{\"id\":3,\"name\":\"Charlie\"}"
        };
        File.WriteAllLines(_testJsonPath, lines);
    }

    private void CreateMultiTypeJsonLFile()
    {
        var lines = new[]
        {
            "{\"id\":1,\"name\":\"Test1\",\"active\":true}",
            "{\"id\":2,\"name\":\"Test2\",\"active\":false}"
        };
        File.WriteAllLines(_testJsonPath, lines);
    }
}
