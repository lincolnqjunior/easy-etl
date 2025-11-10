using Library.Extractors.SQLite;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.ZeroAlloc;
using Xunit;

namespace Tests.Extractors.SQLite
{
    public class SqliteDataExtractorV2Tests
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
            var extractor = new SqliteDataExtractorV2(config);

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
            Assert.Throws<ArgumentNullException>(() => new SqliteDataExtractorV2(null!));
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
            var extractor = new SqliteDataExtractorV2(config);

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
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };
            var extractor = new SqliteDataExtractorV2(config);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => extractor.Schema);
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
            config.Columns.Add(new DefaultColumnAction("Id", 0, false, "ID", typeof(int)));
            config.Columns.Add(new DefaultColumnAction("Name", 1, false, "Name", typeof(string)));
            config.Columns.Add(new DefaultColumnAction("Age", 2, false, "Age", typeof(int)));

            // Act
            var extractor = new SqliteDataExtractorV2(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithMultipleTypedColumns_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };
            config.Columns.Add(new DefaultColumnAction("Id", 0, false, "ID", typeof(int)));
            config.Columns.Add(new DefaultColumnAction("Name", 1, false, "Name", typeof(string)));
            config.Columns.Add(new DefaultColumnAction("Salary", 2, false, "Salary", typeof(double)));
            config.Columns.Add(new DefaultColumnAction("Active", 3, false, "Active", typeof(bool)));
            config.Columns.Add(new DefaultColumnAction("CreatedAt", 4, false, "CreatedAt", typeof(DateTime)));

            // Act
            var extractor = new SqliteDataExtractorV2(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithCustomQueries_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable",
                QuerySelect = "SELECT * FROM {0} LIMIT {1} OFFSET {2}",
                QueryCount = "SELECT COUNT(1) FROM {0}"
            };
            config.Columns.Add(new DefaultColumnAction("Id", 0, false, "ID", typeof(int)));

            // Act
            var extractor = new SqliteDataExtractorV2(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Events_ShouldBeInitialized()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };
            var extractor = new SqliteDataExtractorV2(config);

            // Assert - Events should be accessible (not throw)
            Assert.NotNull(extractor);
            extractor.OnRead += (args) => { };
            extractor.OnFinish += (args) => { };
            extractor.OnError += (args) => { };
        }

        [Fact]
        public void Constructor_WithPool_ShouldAcceptPool()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };
            var pool = new EtlRecordPool();

            // Act
            var extractor = new SqliteDataExtractorV2(config, pool);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithIgnoredColumn_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable"
            };
            config.Columns.Add(new DefaultColumnAction("Id", 0, false, "ID", typeof(int)));
            var ignoredColumn = new DefaultColumnAction("Ignored", 1, false, "Ignored", typeof(string));
            ignoredColumn.Action = Library.Infra.ColumnActions.ColumnAction.Ignore;
            config.Columns.Add(ignoredColumn);
            config.Columns.Add(new DefaultColumnAction("Name", 2, false, "Name", typeof(string)));

            // Act
            var extractor = new SqliteDataExtractorV2(config);

            // Assert
            Assert.NotNull(extractor);
        }

        [Fact]
        public void Config_WithRaiseChangeEventAfter_ShouldAccept()
        {
            // Arrange
            var config = new DatabaseDataExtractorConfig
            {
                ConnectionString = "Data Source=:memory:",
                TableName = "TestTable",
                RaiseChangeEventAfer = 100
            };
            config.Columns.Add(new DefaultColumnAction("Id", 0, false, "ID", typeof(int)));

            // Act
            var extractor = new SqliteDataExtractorV2(config);

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
                CultureInfo = "pt-BR"
            };
            config.Columns.Add(new DefaultColumnAction("Id", 0, false, "ID", typeof(int)));

            // Act
            var extractor = new SqliteDataExtractorV2(config);

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
                PageSize = 1000
            };
            config.Columns.Add(new DefaultColumnAction("Id", 0, false, "ID", typeof(int)));

            // Act
            var extractor = new SqliteDataExtractorV2(config);

            // Assert
            Assert.NotNull(extractor);
        }
    }
}
