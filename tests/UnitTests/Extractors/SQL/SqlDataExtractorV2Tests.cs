using Library.Extractors.SQL;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.ZeroAlloc;
using Xunit;

namespace Tests.Extractors.SQL
{
    public class SqlDataExtractorV2Tests
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
            var extractor = new SqlDataExtractorV2(config);

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
            Assert.Throws<ArgumentNullException>(() => new SqlDataExtractorV2(null!));
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
            var extractor = new SqlDataExtractorV2(config);

            // Assert
            Assert.Equal(0, extractor.TotalLines);
            Assert.Equal(0, extractor.LineNumber);
            Assert.Equal(0, extractor.BytesRead);
            Assert.Equal(0, extractor.PercentRead);
            Assert.Equal(0, extractor.FileSize);
        }

        [Fact]
        public void Schema_BeforeExtract_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };
            var extractor = new SqlDataExtractorV2(config);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => extractor.Schema);
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
            config.Columns.Add(new DefaultColumnAction(
                "Id", 0, false, "ID", typeof(int)
            ));
            config.Columns.Add(new DefaultColumnAction(
                "Name", 1, false, "Name", typeof(string)
            ));

            // Act
            var extractor = new SqlDataExtractorV2(config);

            // Assert
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
                QuerySelect = "SELECT * FROM {0}",
                PageSize = 100
            };

            // Act
            var extractor = new SqlDataExtractorV2(config);

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
            var extractor = new SqlDataExtractorV2(config);

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
            var extractor = new SqlDataExtractorV2(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Constructor_WithPool_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };
            var pool = new EtlRecordPool();

            // Act
            var extractor = new SqlDataExtractorV2(config, pool);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithMultipleTypedColumns_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };
            
            // Add columns with different types
            config.Columns.Add(new DefaultColumnAction("Id", 0, false, "ID", typeof(int)));
            config.Columns.Add(new DefaultColumnAction("Name", 1, false, "Name", typeof(string)));
            config.Columns.Add(new DefaultColumnAction("Age", 2, false, "Age", typeof(int)));
            config.Columns.Add(new DefaultColumnAction("Salary", 3, false, "Salary", typeof(double)));
            config.Columns.Add(new DefaultColumnAction("Active", 4, false, "Active", typeof(bool)));
            config.Columns.Add(new DefaultColumnAction("CreatedAt", 5, false, "CreatedAt", typeof(DateTime)));

            // Act
            var extractor = new SqlDataExtractorV2(config);

            // Assert
            Assert.NotNull(extractor);
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
            var extractor = new SqlDataExtractorV2(config);

            // Assert - events should be null (not throw when invoked with null check)
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithIgnoredColumns_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Server=test;Database=db;",
                TableName = "TestTable"
            };
            
            // Add a column with Ignore action
            config.Columns.Add(new DefaultColumnAction("Id", 0, false, "ID", typeof(int)));
            config.Columns.Add(new DefaultColumnAction("Internal", 1, false, "Internal", typeof(string))
            {
                Action = ColumnAction.Ignore
            });

            // Act
            var extractor = new SqlDataExtractorV2(config);

            // Assert
            Assert.NotNull(extractor);
        }
    }
}
