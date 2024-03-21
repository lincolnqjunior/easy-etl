using Library.Infra.ColumnActions;
using Library.Readers;
using nietras.SeparatedValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Readers
{
    public class DefaultCsvFixture : IDisposable
    {
        readonly string _filePath;
        readonly FileReadConfig _config;

        public FileReadConfig Config => _config;
        public string FilePath => _filePath;

        public DefaultCsvFixture(int notifyAfter, int linesToGenerate)
        {
            _filePath = Path.GetTempFileName() + ".csv";
            _config = new FileReadConfig
            {
                HasHeader = true,
                Delimiter = ',',
                NotifyAfter = notifyAfter,
                ColumnsConfig =
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

            GenerateCsvFile(_filePath, linesToGenerate);
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

        public void Dispose() => new FileInfo(_filePath).Delete();
    }
}
