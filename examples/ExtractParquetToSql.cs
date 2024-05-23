using Humanizer;
using Library;
using Library.Extractors.Parquet;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Loaders.Sql;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Playground
{
    public class ExtractParquetToSql
    {
        private const string EXTRACTOR_CONFIG = @"{
                ""Directory"": ""F:\\EasyEtl Files"",
				""RaiseChangeEventAfer"": 10000,
				""Columns"": [
                    {
                        ""OutputName"": ""BillingEventId"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""BillingAuditConsumptionId"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""ProductName"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""BillingFactName"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""BillingEventKey1Cd"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""BillingEventKey2Cd"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""BillingEventKey3Cd"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""BillingEventKey4Cd"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""BillingEventKey5Cd"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""BillingEventKey6Cd"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""Participante"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""Detentor"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""Titular"",
                        ""OutputType"": ""System.String""
                    },
	                {
                        ""OutputName"": ""Contratante"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""Credenciadora"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""Ufr"",
                        ""OutputType"": ""System.String""
                    },
	                {
                        ""OutputName"": ""IdContrato"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""Registradora"",
                        ""OutputType"": ""System.String""
                    },
	                {
                        ""OutputName"": ""TotalUrs"",
                        ""OutputType"": ""System.Decimal?""
                    },
                    {
                        ""OutputName"": ""Tipo"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""ReferenciaExterna"",
                        ""OutputType"": ""System.String""
                    },
	                {
                        ""OutputName"": ""Carteira"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""Arranjos"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""PEAC"",
                        ""OutputType"": ""System.String""
                    },
	                {
                        ""OutputName"": ""DataRegistro"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""AccountConsumptionVal"",
                        ""OutputName"": ""AccountPaymentNum"",
                        ""OutputType"": ""System.String""
                    },
                    {
                        ""OutputName"": ""ReferenceDt"",
                        ""OutputType"": ""System.DateTime""
                    }                
                ]
            }";
        private const string LOADER_CONFIG = @"
			{
				""ConnectionString"": ""Server=(localdb)\\Playground;Database=EasyETL"",
				""TableName"": ""ExtractParquetToSQL"",
                ""RaiseChangeEventAfer"": 10000,
                ""BatchSize"": 10000,
                ""WriteThreads"": 12 
			}";

        public async Task Execute()
        {
            var _timer = new Stopwatch();

            Console.SetWindowSize(110, 23);
            AnsiConsole.Clear();

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ColumnActionConverter());

            var extractorConfig = JsonConvert.DeserializeObject<ParquetDataExtractorConfig>(EXTRACTOR_CONFIG, settings) ?? throw new InvalidDataException("EXTRACTOR_CONFIG");
            var extractor = new ParquetDataExtractor(extractorConfig);

            var loaderConfig = JsonConvert.DeserializeObject<DatabaseDataLoaderConfig>(LOADER_CONFIG) ?? throw new InvalidDataException("LOADER_CONFIG");            
            var loader = new SqlDataLoader(loaderConfig);

            await CreateTable(loaderConfig);
            var etl = new EasyEtl(extractor, loader, 12);

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
                    RenderScreen(args, _timer);
                    lastUpdate = DateTime.Now.TimeOfDay;
                }
            };

            etl.OnComplete += (args) => { RenderScreen(args, _timer, true); };

            _timer.Restart();
            await etl.Execute();
            _timer.Stop();
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

                CREATE TABLE dbo.{table} (
                    [BillingEventId] NVARCHAR(MAX),
                    [BillingAuditConsumptionId] NVARCHAR(MAX),
                    [ProductName] NVARCHAR(MAX),
                    [BillingFactName] NVARCHAR(MAX),
                    [BillingEventKey1Cd] NVARCHAR(MAX),
                    [BillingEventKey2Cd] NVARCHAR(MAX),
                    [BillingEventKey3Cd] NVARCHAR(MAX),
                    [BillingEventKey4Cd] NVARCHAR(MAX),
                    [BillingEventKey5Cd] NVARCHAR(MAX),
                    [BillingEventKey6Cd] NVARCHAR(MAX),
                    [Participante] NVARCHAR(MAX),
                    [Detentor] NVARCHAR(MAX),
                    [Titular] NVARCHAR(MAX),
                    [Contratante] NVARCHAR(MAX),
                    [Credenciadora] NVARCHAR(MAX),
                    [Ufr] NVARCHAR(MAX),
                    [IdContrato] NVARCHAR(MAX),
                    [Registradora] NVARCHAR(MAX),
                    [TotalUrs] FLOAT,
                    [Tipo] NVARCHAR(MAX),
                    [ReferenciaExterna] NVARCHAR(MAX),
                    [Carteira] NVARCHAR(MAX),
                    [Arranjos] NVARCHAR(MAX),
                    [PEAC] NVARCHAR(MAX),
                    [DataRegistro] NVARCHAR(MAX),
                    [AccountPaymentNum] NVARCHAR(MAX),
                    [ReferenceDt] DATETIME);";
        }

        private void RenderScreen(EasyEtlNotificationEventArgs args, Stopwatch _timer, bool completed = false)
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
                    .AddColumns("Status", "Total", "Processed", "Progress (%)", "Speed (l x s)", "Estimated");

                table.AddRow($"{progress.Status}", $"{progress.TotalLines:N0}", $"{progress.CurrentLine:N0}",
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
    }
}
