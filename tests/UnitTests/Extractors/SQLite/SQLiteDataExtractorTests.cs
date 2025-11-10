using Library.Extractors;
using Library.Infra.Config;
using Xunit;

namespace Tests.Extractors.SQLite
{
    public class SQLiteDataExtractorTests
    {
        [Fact]
        public void Constructor_WithValidConfig_ShouldInitialize()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };

            // Act
            var extractor = new SQLiteDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
            Assert.Equal(0, extractor.TotalLines);
            Assert.Equal(0, extractor.LineNumber);
        }

        [Fact]
        public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SQLiteDataExtractor(null!));
        }

        [Fact]
        public void Properties_ShouldBeInitializedWithDefaults()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };
            var extractor = new SQLiteDataExtractor(config);

            // Assert
            Assert.Equal(0, extractor.TotalLines);
            Assert.Equal(0, extractor.LineNumber);
            Assert.Equal(0, extractor.BytesRead);
            Assert.Equal(0, extractor.PercentRead);
        }

        [Fact]
        public void Config_WithCustomQuerySelect_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable",
                QuerySelect = "SELECT * FROM {0} LIMIT {1} OFFSET {2}",
                PageSize = 100
            };

            // Act
            var extractor = new SQLiteDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithCustomQueryCount_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable",
                QueryCount = "SELECT COUNT(*) FROM {0}",
                PageSize = 100
            };

            // Act
            var extractor = new SQLiteDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithColumns_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };
            config.Columns.Add(new Library.Infra.ColumnActions.DefaultColumnAction(
                "Id", 0, false, "ID", typeof(int)
            ));

            // Act
            var extractor = new SQLiteDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithCultureInfo_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable",
                CultureInfo = "en-US"
            };

            // Act
            var extractor = new SQLiteDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithPageSize_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable",
                PageSize = 5000
            };

            // Act
            var extractor = new SQLiteDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
        }
    }
}
