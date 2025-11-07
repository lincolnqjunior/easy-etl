using Library.Extractors.Json;
using Library.Infra;
using Library.Infra.Config;
using Xunit;

namespace Tests.Extractors.Json
{
    public class JsonDataExtractorTests : IDisposable
    {
        private readonly string _testFilePath;

        public JsonDataExtractorTests()
        {
            _testFilePath = Path.GetTempFileName();
        }

        public void Dispose()
        {
            if (File.Exists(_testFilePath))
            {
                File.Delete(_testFilePath);
            }
        }

        [Fact]
        public void Constructor_WithValidConfig_ShouldInitialize()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "{\"id\":1,\"name\":\"test\"}");
            var config = new JsonDataExtractorConfig { FilePath = _testFilePath };

            // Act
            var extractor = new JsonDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
            Assert.Equal(1, extractor.TotalLines);
        }

        [Fact]
        public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JsonDataExtractor(null!));
        }

        [Fact]
        public void Constructor_WithNonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var config = new JsonDataExtractorConfig { FilePath = "nonexistent.jsonl" };

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => new JsonDataExtractor(config));
        }

        [Fact]
        public void Extract_WithValidJsonLFile_ShouldProcessAllLines()
        {
            // Arrange
            var jsonLines = new[]
            {
                "{\"id\":1,\"name\":\"Alice\"}",
                "{\"id\":2,\"name\":\"Bob\"}",
                "{\"id\":3,\"name\":\"Charlie\"}"
            };
            File.WriteAllLines(_testFilePath, jsonLines);
            
            var config = new JsonDataExtractorConfig { FilePath = _testFilePath };
            var extractor = new JsonDataExtractor(config);
            
            var extractedRows = new List<Dictionary<string, object?>>();

            // Act
            extractor.Extract((ref Dictionary<string, object?> row) =>
            {
                extractedRows.Add(new Dictionary<string, object?>(row));
            });

            // Assert
            Assert.Equal(3, extractedRows.Count);
            Assert.Equal(3, extractor.LineNumber);
        }

        [Fact]
        public void Extract_WithEmptyLines_ShouldSkipEmptyLines()
        {
            // Arrange
            var jsonLines = new[]
            {
                "{\"id\":1,\"name\":\"Alice\"}",
                "",
                "{\"id\":2,\"name\":\"Bob\"}",
                "   ",
                "{\"id\":3,\"name\":\"Charlie\"}"
            };
            File.WriteAllLines(_testFilePath, jsonLines);
            
            var config = new JsonDataExtractorConfig { FilePath = _testFilePath };
            var extractor = new JsonDataExtractor(config);
            
            var extractedRows = new List<Dictionary<string, object?>>();

            // Act
            extractor.Extract((ref Dictionary<string, object?> row) =>
            {
                extractedRows.Add(new Dictionary<string, object?>(row));
            });

            // Assert
            Assert.Equal(3, extractedRows.Count);
        }

        [Fact]
        public void Extract_ShouldTriggerOnFinishEvent()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "{\"id\":1,\"name\":\"test\"}");
            var config = new JsonDataExtractorConfig { FilePath = _testFilePath };
            var extractor = new JsonDataExtractor(config);
            
            bool finishTriggered = false;
            extractor.OnFinish += (args) => { finishTriggered = true; };

            // Act
            extractor.Extract((ref Dictionary<string, object?> row) => { });

            // Assert
            Assert.True(finishTriggered);
        }

        [Fact]
        public void Extract_WithComplexJsonObjects_ShouldParseCorrectly()
        {
            // Arrange
            var jsonLines = new[]
            {
                "{\"id\":1,\"name\":\"Alice\",\"age\":30,\"active\":true}",
                "{\"id\":2,\"name\":\"Bob\",\"age\":25,\"active\":false}"
            };
            File.WriteAllLines(_testFilePath, jsonLines);
            
            var config = new JsonDataExtractorConfig { FilePath = _testFilePath };
            var extractor = new JsonDataExtractor(config);
            
            var extractedRows = new List<Dictionary<string, object?>>();

            // Act
            extractor.Extract((ref Dictionary<string, object?> row) =>
            {
                extractedRows.Add(new Dictionary<string, object?>(row));
            });

            // Assert
            Assert.Equal(2, extractedRows.Count);
            Assert.True(extractedRows[0].ContainsKey("id"));
            Assert.True(extractedRows[0].ContainsKey("name"));
            Assert.True(extractedRows[0].ContainsKey("age"));
            Assert.True(extractedRows[0].ContainsKey("active"));
        }

        [Fact]
        public void Extract_WithInvalidJson_ShouldThrowException()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "{ invalid json }");
            var config = new JsonDataExtractorConfig { FilePath = _testFilePath };
            var extractor = new JsonDataExtractor(config);

            // Act & Assert
            Assert.ThrowsAny<Exception>(() =>
            {
                extractor.Extract((ref Dictionary<string, object?> row) => { });
            });
        }

        [Fact]
        public void Extract_ShouldUpdateBytesReadProperty()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "{\"id\":1,\"name\":\"test\"}");
            var config = new JsonDataExtractorConfig { FilePath = _testFilePath };
            var extractor = new JsonDataExtractor(config);

            // Act
            extractor.Extract((ref Dictionary<string, object?> row) => { });

            // Assert
            Assert.True(extractor.BytesRead > 0);
        }

        [Fact]
        public void Properties_ShouldBeInitializedCorrectly()
        {
            // Arrange
            File.WriteAllText(_testFilePath, "{\"id\":1}");
            var config = new JsonDataExtractorConfig { FilePath = _testFilePath };
            var extractor = new JsonDataExtractor(config);

            // Assert
            Assert.Equal(0, extractor.LineNumber);
            Assert.Equal(0, extractor.BytesRead);
            Assert.Equal(0, extractor.PercentRead);
            Assert.True(extractor.FileSize > 0);
            Assert.True(extractor.TotalLines > 0);
        }
    }
}
