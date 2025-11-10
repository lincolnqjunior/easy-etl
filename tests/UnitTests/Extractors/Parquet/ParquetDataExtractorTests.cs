using Library.Extractors.Parquet;
using Library.Infra.Config;
using Xunit;

namespace Tests.Extractors.Parquet
{
    public class ParquetDataExtractorTests : IDisposable
    {
        private readonly string _testDirectory;

        public ParquetDataExtractorTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"parquet_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ParquetDataExtractor(null!));
        }

        [Fact]
        public void Constructor_WithValidConfig_ShouldInitialize()
        {
            // Arrange
            var config = new ParquetDataExtractorConfig
            {
                Directory = _testDirectory,
                Mask = "*.parquet"
            };

            // Act
            var extractor = new ParquetDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
            Assert.Equal(0, extractor.LineNumber);
            Assert.Equal(0, extractor.BytesRead);
            Assert.Equal(0, extractor.PercentRead);
        }

        [Fact]
        public void Properties_ShouldBeInitializedToZero()
        {
            // Arrange
            var config = new ParquetDataExtractorConfig
            {
                Directory = _testDirectory
            };
            var extractor = new ParquetDataExtractor(config);

            // Assert
            Assert.Equal(0, extractor.TotalLines);
            Assert.Equal(0, extractor.LineNumber);
            Assert.Equal(0, extractor.BytesRead);
            Assert.Equal(0.0, extractor.PercentRead);
            Assert.Equal(0, extractor.FileSize);
        }

        [Fact]
        public void Config_DefaultMask_ShouldBeParquet()
        {
            // Arrange & Act
            var config = new ParquetDataExtractorConfig
            {
                Directory = _testDirectory
            };

            // Assert
            Assert.Equal("*.parquet", config.Mask);
        }

        [Fact]
        public void Config_CanSetCustomMask()
        {
            // Arrange & Act
            var config = new ParquetDataExtractorConfig
            {
                Directory = _testDirectory,
                Mask = "*.parq"
            };

            // Assert
            Assert.Equal("*.parq", config.Mask);
        }

        [Fact]
        public void Config_CanSetDirectory()
        {
            // Arrange & Act
            var config = new ParquetDataExtractorConfig
            {
                Directory = "/test/path"
            };

            // Assert
            Assert.Equal("/test/path", config.Directory);
        }

        [Fact]
        public void Config_ColumnsListShouldNotBeNull()
        {
            // Arrange & Act
            var config = new ParquetDataExtractorConfig();

            // Assert
            Assert.NotNull(config.Columns);
            Assert.Empty(config.Columns);
        }

        [Fact]
        public void OnRead_Event_CanBeSubscribed()
        {
            // Arrange
            var config = new ParquetDataExtractorConfig { Directory = _testDirectory };
            var extractor = new ParquetDataExtractor(config);
            bool eventCalled = false;

            // Act
            extractor.OnRead += (args) => { eventCalled = true; };

            // Assert - event subscription doesn't throw
            Assert.False(eventCalled); // Event hasn't been triggered yet
        }

        [Fact]
        public void OnFinish_Event_CanBeSubscribed()
        {
            // Arrange
            var config = new ParquetDataExtractorConfig { Directory = _testDirectory };
            var extractor = new ParquetDataExtractor(config);
            bool eventCalled = false;

            // Act
            extractor.OnFinish += (args) => { eventCalled = true; };

            // Assert - event subscription doesn't throw
            Assert.False(eventCalled); // Event hasn't been triggered yet
        }

        [Fact]
        public void OnError_Event_CanBeSubscribed()
        {
            // Arrange
            var config = new ParquetDataExtractorConfig { Directory = _testDirectory };
            var extractor = new ParquetDataExtractor(config);
            bool eventCalled = false;

            // Act
            extractor.OnError += (args) => { eventCalled = true; };

            // Assert - event subscription doesn't throw
            Assert.False(eventCalled); // Event hasn't been triggered yet
        }
    }
}
