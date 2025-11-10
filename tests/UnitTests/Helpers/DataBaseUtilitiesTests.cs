using Library.Infra.Config;
using Library.Infra.Exceptions;
using Library.Infra.Helpers;
using Xunit;

namespace Tests.Helpers
{
    public class DataBaseUtilitiesTests
    {
        private class MockDatabaseConfig : IDataBaseConfig
        {
            public string ConnectionString { get; set; } = string.Empty;
            public string TableName { get; set; } = string.Empty;
            public string CultureInfo { get; set; } = "en-US";
            public int RaiseChangeEventAfer { get; set; } = 1000;
        }

        [Fact]
        public void TruncateTable_WithNullConfig_ShouldThrowConfigException()
        {
            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => DataBaseUtilities.TruncateTable(null!));
            Assert.Equal("config", exception.ConfigName);
            Assert.Contains("should not be null", exception.Message);
        }

        [Fact]
        public async Task TruncateTableAsync_WithNullConfig_ShouldThrowConfigException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ConfigException>(() => DataBaseUtilities.TruncateTableAsync(null!));
            Assert.Equal("config", exception.ConfigName);
            Assert.Contains("should not be null", exception.Message);
        }

        [Fact]
        public void TruncateTable_WithEmptyConnectionString_ShouldThrowConfigException()
        {
            // Arrange
            var config = new MockDatabaseConfig
            {
                ConnectionString = "",
                TableName = "TestTable"
            };

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => DataBaseUtilities.TruncateTable(config));
            Assert.Equal("config", exception.ConfigName);
            Assert.Contains("ConnectionString", exception.Message);
            Assert.Contains("required", exception.Message);
        }

        [Fact]
        public async Task TruncateTableAsync_WithEmptyConnectionString_ShouldThrowConfigException()
        {
            // Arrange
            var config = new MockDatabaseConfig
            {
                ConnectionString = "",
                TableName = "TestTable"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ConfigException>(() => DataBaseUtilities.TruncateTableAsync(config));
            Assert.Equal("config", exception.ConfigName);
            Assert.Contains("ConnectionString", exception.Message);
            Assert.Contains("required", exception.Message);
        }

        [Fact]
        public void TruncateTable_WithEmptyTableName_ShouldThrowConfigException()
        {
            // Arrange
            var config = new MockDatabaseConfig
            {
                ConnectionString = "Server=localhost;Database=test;",
                TableName = ""
            };

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => DataBaseUtilities.TruncateTable(config));
            Assert.Equal("config", exception.ConfigName);
            Assert.Contains("TableName", exception.Message);
            Assert.Contains("required", exception.Message);
        }

        [Fact]
        public async Task TruncateTableAsync_WithEmptyTableName_ShouldThrowConfigException()
        {
            // Arrange
            var config = new MockDatabaseConfig
            {
                ConnectionString = "Server=localhost;Database=test;",
                TableName = ""
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ConfigException>(() => DataBaseUtilities.TruncateTableAsync(config));
            Assert.Equal("config", exception.ConfigName);
            Assert.Contains("TableName", exception.Message);
            Assert.Contains("required", exception.Message);
        }

        [Fact]
        public void TruncateTable_WithWhitespaceConnectionString_ShouldThrowConfigException()
        {
            // Arrange
            var config = new MockDatabaseConfig
            {
                ConnectionString = "   ",
                TableName = "TestTable"
            };

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => DataBaseUtilities.TruncateTable(config));
            Assert.Contains("ConnectionString", exception.Message);
        }

        [Fact]
        public void TruncateTable_WithWhitespaceTableName_ShouldThrowConfigException()
        {
            // Arrange
            var config = new MockDatabaseConfig
            {
                ConnectionString = "Server=localhost;Database=test;",
                TableName = "   "
            };

            // Act & Assert
            var exception = Assert.Throws<ConfigException>(() => DataBaseUtilities.TruncateTable(config));
            Assert.Contains("TableName", exception.Message);
        }
    }
}
