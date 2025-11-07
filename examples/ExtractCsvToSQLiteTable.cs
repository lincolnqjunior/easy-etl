using Humanizer;
using Library;
using Library.Extractors.Csv;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Loaders.SQLite;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.Json.Serialization;
using nietras.SeparatedValues;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;

namespace Playground
{
    public class ExtractCsvToSQLiteTable
    {
        public async Task Execute()
        {
            Console.SetWindowSize(110, 23);
            AnsiConsole.Clear();

            var filePath = "F:\\big_easy_etl.csv";
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) GenerateCsvFile(filePath, 1_000_000);

            var _timer = new Stopwatch();

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ColumnActionConverter());

            var extractorConfig = JsonSerializer.Deserialize<CsvDataExtractorConfig>(ExtractorConfig(), options) ?? throw new InvalidDataException("CsvDataExtractorConfig");
            extractorConfig.FilePath = filePath;
            extractorConfig.RaiseChangeEventAfer = 10_000;
            var extractor = new CsvDataExtractor(extractorConfig);

            var loaderConfig = JsonSerializer.Deserialize<DatabaseDataLoaderConfig>(LoaderConfig()) ?? throw new InvalidDataException("LoaderConfig");
            loaderConfig.RaiseChangeEventAfer = 10_000;
            var loader = new SqliteDataLoader(loaderConfig);

            await CreateTable(loaderConfig);

            var etl = new EasyEtl(extractor, loader, 50);

            etl.OnError += (args) =>
            {
                AnsiConsole.Clear();
                AnsiConsole.WriteException(args.Exception);
            };

            TimeSpan lastUpdate = DateTime.Now.TimeOfDay;

            etl.OnChange += (args) =>
            {
                if (lastUpdate < DateTime.Now.AddMilliseconds(-1500).TimeOfDay)
                {
                    RenderScreen(args, fileInfo, _timer);
                    lastUpdate = DateTime.Now.TimeOfDay;
                }
            };

            etl.OnComplete += (args) => { RenderScreen(args, fileInfo, _timer, true); };

            _timer.Start();
            await etl.Execute();
            _timer.Stop();
        }

        private void RenderScreen(EasyEtlNotificationEventArgs args, FileInfo fileInfo, Stopwatch _timer, bool completed = false)
        {
            var rows = new List<IRenderable>();
            var chart = new BreakdownChart().FullSize();
            var tables = new Layout();
            var childs = new List<Layout>();
            var elapsed = new Markup($"[bold]Tempo em execução:[/] {_timer.Elapsed.Humanize(2)}", new Style(completed ? Color.Green : Color.White));

            foreach (var status in args.Progress.Where(x => x.Key != EtlType.Global))
            {
                var layout = new Layout();
                var progress = status.Value;

                var table = new Table()
                    .Expand()
                    .Border(TableBorder.HeavyEdge)
                    .AddColumns("Status", "Size", "Total", "Processed", "Progress (%)", "Speed (l x s)", "Estimated");

                table.AddRow($"{progress.Status}", fileInfo.Length.Bytes().Humanize(), $"{progress.TotalLines:N0}", $"{progress.CurrentLine:N0}",
                    $"{progress.PercentComplete:N1}", $"{progress.Speed:N2}", status.Value.EstimatedTimeToEnd.Humanize(1));

                FillTable(layout, status, table);

                var color = Color.White;

                switch (status.Key)
                {
                    case EtlType.Extract:
                        color = Color.Green;
                        break;
                    case EtlType.Transform:
                        color = Color.Blue;
                        break;
                    case EtlType.Load:
                        color = Color.Yellow;
                        break;
                    case EtlType.Global:
                    default:
                        break;
                }

                if (status.Key != EtlType.Global)
                    chart.AddItem(status.Key.ToString(), progress.CurrentLine, color);

                childs.Add(layout);
            }

            tables.SplitRows(childs.ToArray());
            rows.Add(new Rows(tables));
            rows.Add(new Rows(chart));            
            rows.Add(new Rows(elapsed));

            AnsiConsole.Clear();
            AnsiConsole.Write(new Rows(rows));
        }

        private static void FillTable(Layout layout, KeyValuePair<EtlType, EtlDataProgress> status, Table table)
        {
            switch (status.Key)
            {
                case EtlType.Extract:
                    table.Title = new TableTitle("Extractor", new Style(Color.Green));
                    break;
                case EtlType.Transform:
                    table.Title = new TableTitle("Transformer", new Style(Color.Blue));
                    break;
                case EtlType.Load:
                    table.Title = new TableTitle("Loader", new Style(Color.Yellow));
                    break;
                default:
                    table.Title = new TableTitle("Global", new Style(Color.White));
                    break;
            }

            layout.Update(Align.Center(table, VerticalAlignment.Middle));
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
				""RaiseChangeEventAfer"": 10000,
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
        }

        private static string TransformerConfig()
        {
            return @"
			{
			  ""RaiseChangeEventAfer"": 10000,  
			  ""Transformations"": [
				{
				  ""Condition"": ""true"",
				  ""Actions"": [
					{
					  ""FieldMappings"": {						
						""Country"": {
						  ""Value"": ""Brasil"",
						  ""IsDynamic"": false
						}
					  }
					}
				  ]
				}
			  ]
			}";
        }

        private static string LoaderConfig()
        {
            return @"
			{
				""ConnectionString"": ""Data Source=F:\\Playground.db"",
				""TableName"": ""ExtractCsvToSQLiteTable""
			}";
        }

        private async Task CreateTable(DatabaseDataLoaderConfig config)
        {
            await using var connection = new SqliteConnection(config.ConnectionString);
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = ResetTableSql(config.TableName);
            await command.ExecuteNonQueryAsync();
        }

        private static string ResetTableSql(string table)
        {
            return $@"
				DROP TABLE IF EXISTS {table};
				CREATE TABLE IF NOT EXISTS {table} (
					[Index] INTEGER PRIMARY KEY,
					[Customer Id] TEXT,
					[First Name] TEXT,
					[Last Name] TEXT,
					[Company] TEXT,
					[City] TEXT,
					[Country] TEXT,
					[Phone 1] TEXT,
					[Salary] REAL,
					[Email] TEXT,
					[Subscription Date] TEXT,
					[Website] TEXT
				);";
        }
    }
}
