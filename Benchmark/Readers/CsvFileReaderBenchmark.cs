using BenchmarkDotNet.Attributes;
using Library.Infra.ColumnActions;
using Library.Readers;
using nietras.SeparatedValues;
using System.Text;

namespace Benchmark.Readers
{
    [MemoryDiagnoser]
    public class CsvFileReaderBenchmark
    {
        private string _filePath;
        private FileReadConfig _config;

        [Params(1000)] //10000, 100000, 1000000)] 
        public int NumberOfLines { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _filePath = Path.GetTempFileName() + ".csv";
            _config = new FileReadConfig
            {
                HasHeader = true,
                Delimiter = ',',
                NotifyAfter = 100,
                ColumnsConfig =
                [
                    new ParseColumnAction("Index", 0, false, "Index", typeof(int)),
                    new DefaultColumnAction("Customer Id", 1, false, "Customer Id", typeof(string)),
                    new DefaultColumnAction("First Name", 2, false, "First Name", typeof(string)),
                    new DefaultColumnAction("Last Name", 3, false, "Last Name", typeof(string)),
                    new DefaultColumnAction("Company", 4, false, "Company", typeof(string)),
                    new DefaultColumnAction("City", 5, false, "City", typeof(string)),
                    new DefaultColumnAction("Country", 6, false, "Country", typeof(string)),
                    new DefaultColumnAction("Phone 1", 7, false, "Phone 1", typeof(string)),
                    new DefaultColumnAction("Phone 2", 8, false, "Phone 2", typeof(string)),
                    new DefaultColumnAction("Email", 9, false, "Email", typeof(string)),
                    new ParseColumnAction("Subscription Date", 10, false, "Subscription Date", typeof(DateTime)),
                    new DefaultColumnAction("Website", 11, false, "Website", typeof(string))
                ]
            };

            GenerateCsvFile(_filePath, NumberOfLines);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            File.Delete(_filePath);
        }

        private static void GenerateCsvFile(string filePath, int lines)
        {
            var rnd = new Random();
            var sb = new StringBuilder();
            sb.AppendLine("Index,Customer Id,First Name,Last Name,Company,City,Country,Phone 1,Phone 2,Email,Subscription Date,Website");

            for (int i = 1; i <= lines; i++)
            {
                sb.AppendLine($"{i},EB54EF1154C3A78,Name,LastName,Company,City,Country,Phone1,Phone2,Email,{DateTime.Now.AddDays(-rnd.Next(1000)).ToString("yyyy-MM-dd")},http://example.com/");
            }

            File.WriteAllText(filePath, sb.ToString());
        }

        [Benchmark]
        public void ReadFile_With_EasyETL()
        {
            var reader = new CsvFileReader(_config);
            reader.Read(_filePath, (ref Dictionary<string, object> row) =>
            {

            });
        }

        //[Benchmark]
        //public void ReadFile_With_CsvHelper()
        //{            
        //    using var textReader = new StringReader(File.ReadAllText(_filePath));
        //    using var reader = new CsvHelper.CsvReader(textReader, System.Globalization.CultureInfo.CurrentCulture);

        //    foreach (var _ in reader.GetRecords<dynamic>()) { }
        //}

        [Benchmark(Baseline = true)]
        public void ReadFile_With_Sep()
        {
            using var reader = Sep.Reader().FromText(_filePath);
            foreach (var _ in reader) { }
        }
    }
}
