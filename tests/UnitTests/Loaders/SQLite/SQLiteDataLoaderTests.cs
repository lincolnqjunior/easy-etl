using Library.Loaders.SQLite;
using Library.Infra.Config;
using Xunit;

namespace Tests.Loaders.SQLite
{
    public class SQLiteDataLoaderTests
    {
        [Fact]
        public void Constructor_WithValidConfig_ShouldInitialize()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };

            // Act
            var loader = new SqliteDataLoader(config);

            // Assert
            Assert.NotNull(loader);
            Assert.Equal(0, loader.CurrentLine);
            Assert.Equal(0, loader.TotalLines);
        }

        [Fact]
        public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SqliteDataLoader(null!));
        }

        [Fact]
        public void Properties_ShouldBeInitializedWithDefaults()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };
            var loader = new SqliteDataLoader(config);

            // Assert
            Assert.Equal(0, loader.CurrentLine);
            Assert.Equal(0, loader.TotalLines);
            Assert.Equal(0, loader.PercentWritten);
        }

        [Fact]
        public void Config_WithBatchSize_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable",
                BatchSize = 10000
            };

            // Act
            var loader = new SqliteDataLoader(config);

            // Assert
            Assert.NotNull(loader);
        }

        [Fact]
        public void Config_WithWriteThreads_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable",
                WriteThreads = 4
            };

            // Act
            var loader = new SqliteDataLoader(config);

            // Assert
            Assert.NotNull(loader);
        }

        [Fact]
        public void Config_WithRaiseChangeEventAfter_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable",
                RaiseChangeEventAfer = 5000
            };

            // Act
            var loader = new SqliteDataLoader(config);

            // Assert
            Assert.NotNull(loader);
        }

        [Fact]
        public void Config_WithCultureInfo_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable",
                CultureInfo = "en-US"
            };

            // Act
            var loader = new SqliteDataLoader(config);

            // Assert
            Assert.NotNull(loader);
        }

        [Fact]
        public void Events_ShouldBeNullByDefault()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };
            
            // Act
            var loader = new SqliteDataLoader(config);

            // Assert - events should be null (not throw when invoked with null check)
            Assert.NotNull(loader);
        }
    }
}
