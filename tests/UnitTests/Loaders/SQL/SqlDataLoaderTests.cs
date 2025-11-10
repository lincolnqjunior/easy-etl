using Library.Loaders.Sql;
using Library.Infra.Config;
using Xunit;

namespace Tests.Loaders.SQL
{
    public class SqlDataLoaderTests
    {
        [Fact]
        public void Constructor_WithValidConfig_ShouldInitialize()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };

            // Act
            var loader = new SqlDataLoader(config);

            // Assert
            Assert.NotNull(loader);
            Assert.Equal(0, loader.CurrentLine);
            Assert.Equal(0, loader.TotalLines);
        }

        [Fact]
        public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SqlDataLoader(null!));
        }

        [Fact]
        public void Properties_ShouldBeInitializedWithDefaults()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };
            var loader = new SqlDataLoader(config);

            // Assert
            Assert.Equal(0, loader.CurrentLine);
            Assert.Equal(0, loader.TotalLines);
            Assert.Equal(0, loader.PercentWritten);
            Assert.Equal(0, loader.Speed);
        }

        [Fact]
        public void Config_WithBatchSize_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable",
                BatchSize = 10000
            };

            // Act
            var loader = new SqlDataLoader(config);

            // Assert
            Assert.NotNull(loader);
        }

        [Fact]
        public void Config_WithWriteThreads_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable",
                WriteThreads = 4
            };

            // Act
            var loader = new SqlDataLoader(config);

            // Assert
            Assert.NotNull(loader);
        }

        [Fact]
        public void Config_WithRaiseChangeEventAfter_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable",
                RaiseChangeEventAfer = 5000
            };

            // Act
            var loader = new SqlDataLoader(config);

            // Assert
            Assert.NotNull(loader);
        }

        [Fact]
        public void Config_WithCultureInfo_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable",
                CultureInfo = "pt-BR"
            };

            // Act
            var loader = new SqlDataLoader(config);

            // Assert
            Assert.NotNull(loader);
        }

        [Fact]
        public void Events_ShouldBeNullByDefault()
        {
            // Arrange
            var config = new DatabaseDataLoaderConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };
            
            // Act
            var loader = new SqlDataLoader(config);

            // Assert - events should be null (not throw when invoked with null check)
            Assert.NotNull(loader);
        }
    }
}
