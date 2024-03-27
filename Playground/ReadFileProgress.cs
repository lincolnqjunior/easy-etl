using Humanizer;
using Library.Extractors;
using Library.Extractors.Csv;
using Library.Infra.ColumnActions;
using nietras.SeparatedValues;
using Spectre.Console;
using System.Diagnostics;

namespace Playground
{
    public class ReadFileProgress
    {
        private string _filePath = string.Empty;
        private DataExtractorConfig _config = new();
        private readonly Stopwatch _timer = new();
        private long _maxMemoryUsage = 0;

        public void Setup()
        {
            _filePath = Path.GetRandomFileName() + ".csv";
            _config = new DataExtractorConfig
            {
                FilePath = _filePath,
                HasHeader = true,
                Delimiter = ',',
                NotifyAfter = 100,
                Columns =
                [
                    new ParseColumnAction("Index", 0, false, "Index", typeof(int)),
                    new ParseColumnAction("Customer Id", 1, false, "Customer Id", typeof(Guid)),
                    new DefaultColumnAction("First Name", 2, false, "First Name", typeof(string)),
                    new DefaultColumnAction("Last Name", 3, false, "Last Name", typeof(string)),
                    new DefaultColumnAction("Company", 4, false, "Company", typeof(string)),
                    new DefaultColumnAction("City", 5, false, "City", typeof(string)),
                    new DefaultColumnAction("Country", 6, false, "Country", typeof(string)),
                    new DefaultColumnAction("Phone 1", 7, false, "Phone 1", typeof(string)),
                    new ParseColumnAction("Salary", 8, false, "Salary", typeof(double)),
                    new DefaultColumnAction("Email", 9, false, "Email", typeof(string)),
                    new ParseColumnAction("Subscription Date", 10, false, "Subscription Date", typeof(DateTime)),
                    new DefaultColumnAction("Website", 11, false, "Website", typeof(string))
                ]
            };

            GenerateCsvFile(_filePath, 1000000);            
            _timer.Start();
        }

        public void Cleanup()
        {
            File.Delete(_filePath);
        }

        public void Execute()
        {
            Setup();
            ReadFile();
            //AnsiConsole.Progress()
            //    .AutoRefresh(true)
            //    .AutoClear(false)
            //    .HideCompleted(false)
            //    .Columns([new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new ElapsedTimeColumn(), new RemainingTimeColumn(), new SpinnerColumn()])
            //    .Start(ctx => { ReadFile(ctx.AddTask("[green]Reading file...[/]")); });

            Cleanup();
        }

        public void ReadFile()
        {
            var process = Process.GetCurrentProcess();
            var reader = new CsvDataExtractor(_config);

            reader.OnRead += args =>
            {
                //task.Value(args.ReadPercentage);
                //task.Description($"[green]Reading file... {args.ReadPercentage:F2}% [/]");

                if (_maxMemoryUsage < process.WorkingSet64) _maxMemoryUsage = process.WorkingSet64;
                if (args.LineCount % 100000 == 0) AnsiConsole.MarkupLine($"[yellow]Reading line {reader.LineNumber:N0}[/]     [blue]Speed: {args.Speed:N2} lines/s[/]");
            };

            reader.OnFinish += args =>
            {
                _timer.Stop();

                //task.Value(args.ReadPercentage);
                //task.Description($"[green]Done reading file [/]");
                //task.StopTask();                

                AnsiConsole.MarkupLine($"[green]The process took {_timer.Elapsed.Humanize(2)} to complete[/]");
                AnsiConsole.MarkupLine($"[blue]Total lines read: {args.LineCount:N0}[/]");
                AnsiConsole.MarkupLine($"[green]Total file size: {args.FileSize.Bytes().Humanize()} [/]");
                AnsiConsole.MarkupLine($"[blue]Speed: {args.Speed:N2} lines/s[/]");
                AnsiConsole.MarkupLine($"[green]Max memory usage: {_maxMemoryUsage.Bytes().Humanize()} [/]");
            };

            reader.Extract((ref Dictionary<string, object?> row) =>
            {

            });
        }

        private static void GenerateCsvFile(string filePath, int lines)
        {
            var rnd = new Random();

            using var writer = Sep.New(',').Writer().ToFile(filePath);

            using (var writeRow = writer.NewRow())
            {
                writeRow["Index"].Set("Index");
                writeRow["Customer Id"].Set("Customer Id");
                writeRow["First Name"].Set("First Name");
                writeRow["Last Name"].Set("Last Name");
                writeRow["Company"].Set("Company");
                writeRow["City"].Set("City");
                writeRow["Country"].Set("Country");
                writeRow["Phone 1"].Set("Phone 1");
                writeRow["Salary"].Set("Salary");
                writeRow["Email"].Set("Email");
                writeRow["Subscription Date"].Set("Subscription Date");
                writeRow["Website"].Set("Website");
            }

            for (int i = 1; i <= lines; i++)
            {
                var salary = rnd.NextDouble() * (15000.0 - 2500.0) + 2500.0;
                var subscriptionDate = DateTime.Now.AddDays(-rnd.Next(1000)).ToString("yyyy-MM-dd");

                using (var writeRow = writer.NewRow())
                {
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
        }
    }
}
