using Humanizer;
using Library;
using Library.Extractors;
using Library.Extractors.Csv;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Loaders;
using Library.Loaders.Sql;
using Newtonsoft.Json;
using nietras.SeparatedValues;
using Spectre.Console;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Playground
{
    public class ExtractCsvToSqlTable
    {
        public async Task Execute()
        {
            AnsiConsole.Clear();

            var filePath = "F:\\big_easy_etl.csv";
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) GenerateCsvFile(filePath, 1_000_000);

            var _timer = new Stopwatch();

            var layout = new Layout("Root")
                    .SplitRows(new Layout("Extractor"), new Layout("Transformer"), new Layout("Loader"), new Layout("Global"));

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ColumnActionConverter());

            var extractorConfig = JsonConvert.DeserializeObject<DataExtractorConfig>(ExtractorConfig(), settings) ?? throw new InvalidDataException("DataExtractorConfig");
            extractorConfig.FilePath = filePath;
            extractorConfig.NotifyAfter = 10_000;
            var extractor = new CsvDataExtractor(extractorConfig);            

            var loaderConfig = JsonConvert.DeserializeObject<DatabaseDataLoaderConfig>(LoaderConfig()) ?? throw new InvalidDataException("LoaderConfig");
            loaderConfig.NotifyAfter = 10_000;
            loaderConfig.BatchSize = 50_000;            

            var loader = new SqlDataLoader(loaderConfig);

            await CreateTable(loaderConfig);

            var etl = new EasyEtl(extractor, loader, 50);

            etl.OnError += (args) =>
            {
                AnsiConsole.WriteLine();
                AnsiConsole.WriteException(args.Exception);
            };

            TimeSpan lastUpdate = DateTime.Now.TimeOfDay;

            etl.OnChange += (args) =>
            {
                if (lastUpdate < DateTime.Now.AddMilliseconds(-1500).TimeOfDay)
                {
                    foreach (var status in args.Progress)
                    {
                        var progress = status.Value;

                        var table = new Table().AddColumns("Status", "Tamanho", "Total de linhas", "Linhas lidas", "% Completo", "Velocidade (linhas x segundo)", "Término");
                        table.AddRow(progress.Status.ToString(), fileInfo.Length.Bytes().Humanize(), progress.TotalLines.ToString("N0"), progress.CurrentLine.ToString("N0"), progress.PercentComplete.ToString("N2"), progress.Speed.ToString("N2"), status.Value.EstimatedTimeToEnd.Humanize(1));
                        FillTable(layout, status, table);

                        AnsiConsole.Clear();
                        AnsiConsole.Write(new Text($"Tempo em execução: {_timer.Elapsed.Humanize(2)}"));
                        AnsiConsole.WriteLine();
                        AnsiConsole.Write(layout);
                    }

                    lastUpdate = DateTime.Now.TimeOfDay;
                }
            };

            etl.OnComplete += (args) =>
            {
                foreach (var status in args.Progress)
                {
                    var progress = status.Value;

                    var table = new Table().AddColumns("Status", "Tamanho", "Total de linhas", "Linhas lidas", "% Completo", "Velocidade (linhas x segundo)", "Término");
                    table.AddRow(progress.Status.ToString(), fileInfo.Length.Bytes().Humanize(), progress.TotalLines.ToString("N0"), progress.CurrentLine.ToString("N0"), progress.PercentComplete.ToString("N2"), progress.Speed.ToString("N2"), status.Value.EstimatedTimeToEnd.Humanize(1));
                    FillTable(layout, status, table);

                    AnsiConsole.Clear();
                    AnsiConsole.Write(new Text($"Tempo em execução: {_timer.Elapsed.Humanize(2)}"));
                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(layout);
                }
            };

            _timer.Start();
            await etl.Execute();
            _timer.Stop();
        }

        private static void FillTable(Layout layout, KeyValuePair<EtlType, EtlDataProgress> status, Table table)
        {
            switch (status.Key)
            {
                case EtlType.Extract:
                    table.Title = new TableTitle("Extractor", new Style(Color.Green));					
                    layout["Extractor"].Update(table);
                    break;
                case EtlType.Transform:
                    table.Title = new TableTitle("Transformer", new Style(Color.Blue));
                    layout["Transformer"].Update(table);
                    break;
                case EtlType.Load:
                    table.Title = new TableTitle("Loader", new Style(Color.Yellow));
                    layout["Loader"].Update(table);
                    break;
                default:
                    table.Title = new TableTitle("Global", new Style(Color.White));
                    layout["Global"].Update(table);
                    break;
            }
        }

        private static void GenerateCsvFile(string filePath, int lines)
        {
            var rnd = new Random();

            using var writer = Sep.New(',').Writer().ToFile(filePath);

            for (int i = 0; i <= lines; i++)
            {
                var salary = rnd.NextDouble() * (15000.0 - 2500.0) + 2500.0;
                var subscriptionDate = DateTime.Now.AddDays(-rnd.Next(1000)).ToString("yyyy-MM-dd");

                using var writeRow = writer.NewRow();
                writeRow["Index"].Format(i);
                writeRow["Customer Id"].Format(Guid.NewGuid());
                writeRow["First Name"].Set("Name");
                writeRow["Last Name"].Set("LastName");
                writeRow["Company"].Set("Company");
                writeRow["City"].Set("City");
                writeRow["Country"].Set("Country");
                writeRow["Phone 1"].Set("Phone1");
                writeRow["Salary"].Format(salary);
                writeRow["Email"].Set("Email");
                writeRow["Subscription Date"].Set(subscriptionDate);
                writeRow["Website"].Set("http://example.com/");
            }
        }

        private static string ExtractorConfig()
        {
            return @"{
				""HasHeader"": true,
				""Delimiter"": "","",
				""NotifyAfter"": 10000,
				""ColumnsConfig"": [
					{
						""Type"": ""ParseColumnAction"",
						""ColumnName"": ""Index"",
						""Position"": 0,
						""IsHeader"": false,
						""OutputName"": ""Index"",
						""OutputType"": ""System.Int32""
					},
					{
						""Type"": ""ParseColumnAction"",
						""ColumnName"": ""Customer Id"",
						""Position"": 1,
						""IsHeader"": false,
						""OutputName"": ""Customer Id"",
						""OutputType"": ""System.Guid""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""ColumnName"": ""First Name"",
						""Position"": 2,
						""IsHeader"": false,
						""OutputName"": ""First Name"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""ColumnName"": ""Last Name"",
						""Position"": 3,
						""IsHeader"": false,
						""OutputName"": ""Last Name"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""ColumnName"": ""Company"",
						""Position"": 4,
						""IsHeader"": false,
						""OutputName"": ""Company"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""ColumnName"": ""City"",
						""Position"": 5,
						""IsHeader"": false,
						""OutputName"": ""City"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""ColumnName"": ""Country"",
						""Position"": 6,
						""IsHeader"": false,
						""OutputName"": ""Country"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""ColumnName"": ""Phone 1"",
						""Position"": 7,
						""IsHeader"": false,
						""OutputName"": ""Phone 1"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""ParseColumnAction"",
						""ColumnName"": ""Salary"",
						""Position"": 8,
						""IsHeader"": false,
						""OutputName"": ""Salary"",
						""OutputType"": ""System.Double""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""ColumnName"": ""Email"",
						""Position"": 9,
						""IsHeader"": false,
						""OutputName"": ""Email"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""ParseColumnAction"",
						""ColumnName"": ""Subscription Date"",
						""Position"": 10,
						""IsHeader"": false,
						""OutputName"": ""Subscription Date"",
						""OutputType"": ""System.DateTime""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""ColumnName"": ""Website"",
						""Position"": 11,
						""IsHeader"": false,
						""OutputName"": ""Website"",
						""OutputType"": ""System.String""
					}
				]
			}";
        }        

        private static string LoaderConfig()
        {
            return @"
			{
				""ConnectionString"": ""Server=(localdb)\\Playground;Integrated Security=true;AttachDbFileName=C:\\Users\\linco\\AppData\\Local\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\Playground\\Playground.mdf"",
				""TableName"": ""ExtractCsvToSQTable""
			}";
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
