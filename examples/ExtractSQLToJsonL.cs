using Humanizer;
using Library;
using Library.Extractors.SQL;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Loaders.Json;
using Newtonsoft.Json;
using Spectre.Console;
using System.Diagnostics;

namespace Playground
{
    public class ExtractSQLToJsonL
    {
        public async Task Execute()
        {
            AnsiConsole.Clear();

            var _timer = new Stopwatch();

            var layout = new Layout("Root")
                    .SplitRows(new Layout("Extractor"), new Layout("Transformer"), new Layout("Loader"), new Layout("Global"));

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ColumnActionConverter());

            var extractorConfig = JsonConvert.DeserializeObject<DatabaseDataExtractorConfig>(ExtractorConfig(), settings) ?? throw new InvalidDataException("DatabaseDataExtractorConfig");            
            extractorConfig.RaiseChangeEventAfer = 10_000;
			extractorConfig.PageSize = 10_000;

            var extractor = new SqlDataExtractor(extractorConfig);

            var loaderConfig = JsonConvert.DeserializeObject<JsonDataLoaderConfig>(LoaderConfig()) ?? throw new InvalidDataException("JsonDataLoaderConfig");
            loaderConfig.RaiseChangeEventAfer = 10_000;
            var loader = new JsonDataLoader(loaderConfig);
			
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

                        var table = new Table().AddColumns("Status", "Total de linhas", "Linhas lidas", "% Completo", "Velocidade (linhas x segundo)", "Término");
                        table.AddRow(progress.Status.ToString(), progress.TotalLines.ToString("N0"), progress.CurrentLine.ToString("N0"), progress.PercentComplete.ToString("N2"), progress.Speed.ToString("N2"), status.Value.EstimatedTimeToEnd.Humanize(1));
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

                    var table = new Table().AddColumns("Status", "Total de linhas", "Linhas lidas", "% Completo", "Velocidade (linhas x segundo)", "Término");
                    table.AddRow(progress.Status.ToString(), progress.TotalLines.ToString("N0"), progress.CurrentLine.ToString("N0"), progress.PercentComplete.ToString("N2"), progress.Speed.ToString("N2"), status.Value.EstimatedTimeToEnd.Humanize(1));
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

        private static string ExtractorConfig()
        {
            return @"{
				""ConnectionString"": ""Server=(localdb)\\Playground;Integrated Security=true;AttachDbFileName=C:\\Users\\linco\\AppData\\Local\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\Playground\\Playground.mdf"",
				""TableName"": ""ExtractCsvToSQTable"",
				""QuerySelect"": ""SELECT * FROM {0}"",
				""RaiseChangeEventAfer"": 10000,
				""PageSize"": 10000,
				""Columns"": [
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""Index"",
						""Position"": 0,
						""IsHeader"": false,
						""OutputName"": ""Index"",
						""OutputType"": ""System.Int32""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""Customer Id"",
						""Position"": 1,
						""IsHeader"": false,
						""OutputName"": ""CustomerId"",
						""OutputType"": ""System.Guid""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""First Name"",
						""Position"": 2,
						""IsHeader"": false,
						""OutputName"": ""FirstName"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""Last Name"",
						""Position"": 3,
						""IsHeader"": false,
						""OutputName"": ""LastName"",
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
						""OutputName"": ""Phone1"",
						""OutputType"": ""System.String""
					},
					{
						""Type"": ""DefaultColumnAction"",
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
						""Type"": ""DefaultColumnAction"",
						""OutputName"": ""Subscription Date"",
						""Position"": 10,
						""IsHeader"": false,
						""OutputName"": ""SubscriptionDate"",
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

        private static string LoaderConfig()
        {
            return @"
			{	
				""RaiseChangeEventAfer"": 10000,
				""OutputPath"": ""F:\\huge_easy_etl.jsonl"",
				""IndentJson"": false,
				""IsJsonl"": true
			}";
        }
    }
}
