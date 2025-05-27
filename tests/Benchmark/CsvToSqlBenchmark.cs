using BenchmarkDotNet.Attributes;
using Library.Extractors.Csv;
using Library.Infra.ColumnActions;
using Library.Loaders.Sql;
using Library;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using Library.Infra.Config;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class CsvToSqlBenchmark
    {
        private EasyEtl etl;

        private const string EXTRACTOR_CONFIG = @"{
				""HasHeader"": true,
				""Delimiter"": "","",
				""RaiseChangeEventAfer"": 10000,
                ""FilePath"": ""F:\\big_easy_etl.csv"",
				""Columns"": [
					{
						""Type"": ""ParseColumnAction"",
						""OutputName"": ""Index"",
						""Position"": 0,
						""IsHeader"": false,
						""OutputName"": ""Index"",
						""OutputType"": ""System.Int32""
					},
					{
						""Type"": ""ParseColumnAction"",
						""OutputName"": ""Customer Id"",
						""Position"": 1,
						""IsHeader"": false,
						""OutputName"": ""Customer Id"",
						""OutputType"": ""System.Guid""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""First Name"",
						""Position"": 2,
						""IsHeader"": false,
						""OutputName"": ""First Name"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""Last Name"",
						""Position"": 3,
						""IsHeader"": false,
						""OutputName"": ""Last Name"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""Company"",
						""Position"": 4,
						""IsHeader"": false,
						""OutputName"": ""Company"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""City"",
						""Position"": 5,
						""IsHeader"": false,
						""OutputName"": ""City"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""Country"",
						""Position"": 6,
						""IsHeader"": false,
						""OutputName"": ""Country"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""Phone 1"",
						""Position"": 7,
						""IsHeader"": false,
						""OutputName"": ""Phone 1"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""ParseColumnAction"",
						""OutputName"": ""Salary"",
						""Position"": 8,
						""IsHeader"": false,
						""OutputName"": ""Salary"",
						""OutputType"": ""System.Double""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""Email"",
						""Position"": 9,
						""IsHeader"": false,
						""OutputName"": ""Email"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""ParseColumnAction"",
						""OutputName"": ""Subscription Date"",
						""Position"": 10,
						""IsHeader"": false,
						""OutputName"": ""Subscription Date"",
						""OutputType"": ""System.DateTime""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""Website"",
						""Position"": 11,
						""IsHeader"": false,
						""OutputName"": ""Website"",
						""OutputType"": ""System.String""
					}
				]
			}";

        private const string LOADER_CONFIG = @"
			{
				""ConnectionString"": ""Server=(localdb)\\Playground;Integrated Security=true;AttachDbFileName=C:\\Users\\linco\\AppData\\Local\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\Playground\\Playground.mdf"",
				""TableName"": ""ExtractCsvToSQTable""
			}";
        
		[GlobalSetup]
        public async Task Setup()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ColumnActionConverter());

            var extractorConfig = JsonConvert.DeserializeObject<CsvDataExtractorConfig>(EXTRACTOR_CONFIG, settings) ?? throw new InvalidDataException("CsvDataExtractorConfig");
            var extractor = new CsvDataExtractor(extractorConfig);

            var loaderConfig = JsonConvert.DeserializeObject<DatabaseDataLoaderConfig>(LOADER_CONFIG) ?? throw new InvalidDataException("LoaderConfig");

            var loader = new SqlDataLoader(loaderConfig);

            await CreateTable(loaderConfig);

            etl = new EasyEtl(extractor, loader, 50);
        }        

        [Benchmark]
        public async Task Execute()
        {
            await etl.Execute();
        }

        private async Task CreateTable(DatabaseDataLoaderConfig config)
        {
            await using var connection = new SqlConnection(config.ConnectionString);
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = ResetTableSql(config.TableName);
            await command.ExecuteNonQueryAsync();
        }

        private static string ResetTableSql(string table)
        {
            return $@"
				IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'{table}')
                    DROP TABLE {table};
                CREATE TABLE {table} (
                    [Index] INT,
                    [CustomerId] UNIQUEIDENTIFIER,
                    [FirstName] NVARCHAR(MAX),
                    [LastName] NVARCHAR(MAX),
                    [Company] NVARCHAR(MAX),
                    [City] NVARCHAR(MAX),
                    [Country] NVARCHAR(MAX),
                    [Phone1] NVARCHAR(MAX),
                    [Salary] FLOAT,
                    [Email] NVARCHAR(MAX),
                    [SubscriptionDate] DATETIME,
                    [Website] NVARCHAR(MAX)
                );";
        }

    }
}
