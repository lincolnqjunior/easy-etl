using Library.Infra.Config;
using Library.Loaders.Sql;
using Library.Loaders.SQLite;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Tests.Integration
{
    [Trait("Category", "IntegrationTest")]
    public class SqliteDataLoaderTests
    {
        private static readonly CultureInfo provider = CultureInfo.InvariantCulture;
        private readonly DatabaseDataLoaderConfig _config = new()
        {
            ConnectionString = "Data Source = F:\\Tests.sqlite",
            BatchSize = 5,
            WriteThreads = 1,
            RaiseChangeEventAfer = 1,
            TableName = "SqliteDataLoaderTests"
        };

        public DatabaseDataLoaderConfig Config => _config;

        public SqliteDataLoaderTests()
        {
            InitializeDatabase(_config.ConnectionString);
        }

        private static void InitializeDatabase(string connectionString)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DROP TABLE IF EXISTS SqliteDataLoaderTests;
                CREATE TABLE IF NOT EXISTS SqliteDataLoaderTests (
                    [Index] INTEGER PRIMARY KEY,
                    [CustomerId] TEXT,
                    [FirstName] TEXT,
                    [LastName] TEXT,
                    [Company] TEXT,
                    [City] TEXT,
                    [Country] TEXT,
                    [Phone1] TEXT,
                    [Salary] REAL,
                    [Email] TEXT,
                    [SubscriptionDate] TEXT,
                    [Website] TEXT
                )";
            command.ExecuteNonQuery();
        }

        private static List<Dictionary<string, object?>> GenerateTestData()
        {
            return
            [
                new() {
                    { "Index", 1 },
                    { "CustomerId", Guid.NewGuid() },
                    { "FirstName", "Kgkx" },
                    { "LastName", "Dzgqabksdl" },
                    { "Company", "Yxnogrgujptrve" },
                    { "City", "Twslkth" },
                    { "Country", "Dysepwbvq" },
                    { "Phone1", "939-559-2574" },
                    { "Salary", 10911.52 },
                    { "Email", "kgkx60@test.com" },
                    { "SubscriptionDate", DateTime.Parse("10/16/2022 12:00:00 AM", provider) },
                    { "Website", "http://example.com/" }
                },
                new() {
                    { "Index", 2 },
                    { "CustomerId", Guid.NewGuid() },
                    { "FirstName", "Zkvl" },
                    { "LastName", "Lpafz" },
                    { "Company", "Iuvjqfh" },
                    { "City", "Cqagho" },
                    { "Country", "Myttz" },
                    { "Phone1", "672-936-1080" },
                    { "Salary", 11998.08 },
                    { "Email", "zkvl57@example.com" },
                    { "SubscriptionDate", DateTime.Parse("8/29/2021 12:00:00 AM", provider) },
                    { "Website", "http://example.com/" }
                },
                new() {
                    { "Index", 3 },
                    { "CustomerId", Guid.NewGuid() },
                    { "FirstName", "Yhvwiqg" },
                    { "LastName", "Ngfquio" },
                    { "Company", "Gqwslvgbozrao" },
                    { "City", "Klsgajbs" },
                    { "Country", "Jnvwpgco" },
                    { "Phone1", "195-124-5840" },
                    { "Salary", 4892.02 },
                    { "Email", "yhvwiqg23@demo.com" },
                    { "SubscriptionDate", DateTime.Parse("8/14/2021 12:00:00 AM", provider) },
                    { "Website", "http://example.com/" }
                },
                new() {
                    { "Index", 4 },
                    { "CustomerId", Guid.NewGuid() },
                    { "FirstName", "Xhvvojbs" },
                    { "LastName", "Bnrrmn" },
                    { "Company", "Mxmqixrcg" },
                    { "City", "Tuxmkl" },
                    { "Country", "Slsdyzck" },
                    { "Phone1", "272-994-6292" },
                    { "Salary", 3832.7 },
                    { "Email", "xhvvojbs57@example.com" },
                    { "SubscriptionDate", DateTime.Parse("1/7/2024 12:00:00 AM", provider) },
                    { "Website", "http://example.com/" }
                },
                new() {
                    { "Index", 5 },
                    { "CustomerId", Guid.NewGuid() },
                    { "FirstName", "Aidpq" },
                    { "LastName", "Ixfewkefg" },
                    { "Company", "Dqjgswvbcjquqfk" },
                    { "City", "Abdrfakkik" },
                    { "Country", "Kbqiglzu" },
                    { "Phone1", "513-927-9268" },
                    { "Salary", 8582.46 },
                    { "Email", "aidpq25@demo.com" },
                    { "SubscriptionDate", DateTime.Parse("3/31/2024 12:00:00 AM", provider) },
                    { "Website", "http://example.com/" }
                }
            ];
        }

        private static bool DataIsInDatabase(DatabaseDataLoaderConfig config, List<Dictionary<string, object?>> testData)
        {
            using var connection = new SqliteConnection(config.ConnectionString);
            connection.Open();

            foreach (var expectedRow in testData)
            {
                var query = $"SELECT * FROM {config.TableName} WHERE [Index] = @Index";
                using var command = new SqliteCommand(query, connection);
                command.Parameters.AddWithValue("@Index", expectedRow["Index"]);

                using var reader = command.ExecuteReader();
                if (!reader.Read()) return false;

                foreach (var key in expectedRow.Keys)
                {
                    var expectedValue = expectedRow[key];
                    object? actualValue = reader[key];

                    if (expectedValue is int) actualValue = reader.GetInt32(reader.GetOrdinal(key));
                    else if (expectedValue is double) actualValue = reader.GetDouble(reader.GetOrdinal(key));
                    else if (expectedValue is string) actualValue = reader.GetString(reader.GetOrdinal(key));
                    else if (expectedValue is DateTime) actualValue = reader.GetDateTime(reader.GetOrdinal(key));
                    else if (expectedValue is Guid) actualValue = reader.GetGuid(reader.GetOrdinal(key));                    

                    if (expectedValue != null && !expectedValue.Equals(actualValue)) return false;
                }
            }

            return true;
        }

        [Fact]
        public async Task Load_BasicData_Success()
        {
            // Arrange        
            var loader = new SqliteDataLoader(Config);
            var data = GenerateTestData();

            int writeEventCount = 0;
            int finishEventCount = 0;
            loader.OnWrite += (args) => { writeEventCount++; };
            loader.OnFinish += _ => finishEventCount++;

            // Act
            await loader.Load(data.ToAsyncEnumerable(), CancellationToken.None);

            // Assert
            Assert.True(DataIsInDatabase(Config, data), "Not all data was correctly loaded into the database.");
            Assert.True(writeEventCount > 0, "The OnWrite event was not fired indicating the load may not have completed.");
            Assert.True(finishEventCount > 0, "The OnFinish event was not fired indicating the load may not have completed.");
        }

        [Fact]
        public async Task Load_FinalWrite_WithRemainingRows_Success()
        {
            // Arrange
            var loader = new SqliteDataLoader(Config with { BatchSize = 3 });
            var data = GenerateTestData();

            int finishEventCount = 0;
            loader.OnFinish += args =>
            {
                Assert.True(args.WritePercentage == 100, "The write percentage was not 100%.");
                finishEventCount++;
            };

            // Act
            await loader.Load(data.ToAsyncEnumerable(), CancellationToken.None);

            // Assert
            Assert.True(DataIsInDatabase(Config, data), "Not all data was correctly loaded into the database.");
            Assert.True(finishEventCount > 0, "The OnFinish event was not fired indicating the load may not have completed.");
        }

        [Fact]
        public async Task Load_WithError_DispatchesErrorEvent()
        {
            // Arrange        
            var loader = new SqliteDataLoader(Config with { TableName = "WrongTableName" });
            var data = GenerateTestData();

            bool errorEventFired = false;
            Exception? exception = null;

            loader.OnError += (args) =>
            {
                exception = args.Exception;
                errorEventFired = true;
            };

            // Act            
            await loader.Load(data.ToAsyncEnumerable(), CancellationToken.None);

            //Assert
            Assert.True(exception is SqliteException, "The thrown exception was not correct.");
            Assert.True(errorEventFired, "The OnError event was not fired indicating that the exception was not raised");
        }
    }
}
