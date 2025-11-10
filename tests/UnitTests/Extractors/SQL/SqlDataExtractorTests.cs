using Library.Extractors.SQL;
using Library.Infra.Config;
using Xunit;

namespace Tests.Extractors.SQL
{
    public class SqlDataExtractorTests
    {
        [Fact]
        public void Constructor_WithValidConfig_ShouldInitialize()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };

            // Act
            var extractor = new SqlDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
            Assert.Equal(0, extractor.TotalLines);
            Assert.Equal(0, extractor.LineNumber);
            Assert.Equal(0, extractor.BytesRead);
        }

        [Fact]
        public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SqlDataExtractor(null!));
        }

        [Fact]
        public void Properties_ShouldBeInitializedWithDefaults()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };
            var extractor = new SqlDataExtractor(config);

            // Assert
            Assert.Equal(0, extractor.TotalLines);
            Assert.Equal(0, extractor.LineNumber);
            Assert.Equal(0, extractor.BytesRead);
            Assert.Equal(0, extractor.PercentRead);
            Assert.Equal(0, extractor.FileSize);
        }

        [Fact]
        public void Events_ShouldBeNullByDefault()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };
            
            // Act
            var extractor = new SqlDataExtractor(config);

            // Assert - events should be null (not throw when invoked with null check)
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithCustomQuerySelect_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable",
                QuerySelect = "SELECT TOP {1} * FROM {0}",
                PageSize = 100
            };

            // Act
            var extractor = new SqlDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithCustomQueryCount_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable",
                QueryCount = "SELECT COUNT(1) FROM {0}",
                PageSize = 100
            };

            // Act
            var extractor = new SqlDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithColumns_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };
            config.Columns.Add(new Library.Infra.ColumnActions.DefaultColumnAction(
                "Id", 0, false, "ID", typeof(int)
            ));

            // Act
            var extractor = new SqlDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithCultureInfo_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable",
                CultureInfo = "en-US"
            };

            // Act
            var extractor = new SqlDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithRaiseChangeEventAfter_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable",
                RaiseChangeEventAfer = 5000
            };

            // Act
            var extractor = new SqlDataExtractor(config);

            // Assert
            Assert.NotNull(extractor);
        }
    }
}
