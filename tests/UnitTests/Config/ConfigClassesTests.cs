using Library.Infra.Config;
using Xunit;

namespace Tests.Config
{
    public class ConfigClassesTests
    {
        [Fact]
        public void DatabaseDataExtractorConfig_DefaultValues_ShouldBeSet()
        {
            // Act
            var config = new DatabaseDataExtractorConfig();

            // Assert
            Assert.Equal(10_000, config.RaiseChangeEventAfer);
            Assert.Equal("pt-BR", config.CultureInfo);
            Assert.NotNull(config.Columns);
            Assert.Empty(config.Columns);
            Assert.Equal(1000, config.PageSize);
        }

        [Fact]
        public void DatabaseDataExtractorConfig_CanSetProperties()
        {
            // Arrange & Act
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db",
                TableName = "TestTable",
                PageSize = 5000,
                RaiseChangeEventAfer = 500,
                CultureInfo = "en-US"
            };

            // Assert
            Assert.Equal("Server=test;Database=db", config.ConnectionString);
            Assert.Equal("TestTable", config.TableName);
            Assert.Equal(5000, config.PageSize);
            Assert.Equal(500, config.RaiseChangeEventAfer);
            Assert.Equal("en-US", config.CultureInfo);
        }

        [Fact]
        public void DatabaseDataLoaderConfig_DefaultValues_ShouldBeSet()
        {
            // Act
            var config = new DatabaseDataLoaderConfig();

            // Assert
            Assert.Equal(10_000, config.RaiseChangeEventAfer);
            Assert.Equal("en-US", config.CultureInfo);
            Assert.Equal(50_000, config.BatchSize);
            Assert.Equal(2, config.WriteThreads);
        }

        [Fact]
        public void DatabaseDataLoaderConfig_CanSetProperties()
        {
            // Arrange & Act
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Server=test;Database=db",
                TableName = "TestTable",
                BatchSize = 10_000,
                WriteThreads = 4,
                RaiseChangeEventAfer = 2000,
                CultureInfo = "pt-BR"
            };

            // Assert
            Assert.Equal("Server=test;Database=db", config.ConnectionString);
            Assert.Equal("TestTable", config.TableName);
            Assert.Equal(10_000, config.BatchSize);
            Assert.Equal(4, config.WriteThreads);
            Assert.Equal(2000, config.RaiseChangeEventAfer);
            Assert.Equal("pt-BR", config.CultureInfo);
        }

        [Fact]
        public void JsonDataExtractorConfig_DefaultValues_ShouldBeSet()
        {
            // Act
            var config = new JsonDataExtractorConfig();

            // Assert
            Assert.Equal(10_000, config.RaiseChangeEventAfer);
        }

        [Fact]
        public void JsonDataExtractorConfig_CanSetProperties()
        {
            // Arrange & Act
            var config = new JsonDataExtractorConfig
            {
                FilePath = "test.jsonl",
                RaiseChangeEventAfer = 5000
            };

            // Assert
            Assert.Equal("test.jsonl", config.FilePath);
            Assert.Equal(5000, config.RaiseChangeEventAfer);
        }

        [Fact]
        public void ParquetDataExtractorConfig_DefaultValues_ShouldBeSet()
        {
            // Act
            var config = new ParquetDataExtractorConfig();

            // Assert
            Assert.Equal(10_000, config.RaiseChangeEventAfer);
            Assert.NotNull(config.Columns);
            Assert.Empty(config.Columns);
        }

        [Fact]
        public void ParquetDataExtractorConfig_CanSetProperties()
        {
            // Arrange & Act
            var config = new ParquetDataExtractorConfig
            {
                Directory = "/path/to/parquet",
                RaiseChangeEventAfer = 3000
            };

            // Assert
            Assert.Equal("/path/to/parquet", config.Directory);
            Assert.Equal(3000, config.RaiseChangeEventAfer);
        }
    }
}
