using Library.Extractors.SQL;
using Library.Infra.ColumnActions;
using Library.Infra.Config;

namespace Tests.Integration
{
    [Trait("Category", "IntegrationTest")]
    public class SqlDataExtractorTests
    {
        private readonly string _connectionString = "Server=(localdb)\\Playground;Database=Tests;Trusted_Connection=True;";

        [Fact]
        public void Extract_FetchesDataFromDatabase()
        {
            // // Arrange
            // var config = new DatabaseDataExtractorConfig
            // {
            //     ConnectionString = _connectionString,
            //     TableName = "SqlDataExtractorTests",
            //     QueryCount = "SELECT COUNT(*) FROM {0}",
            //     QuerySelect = "SELECT * FROM {0}",
            //     RaiseChangeEventAfer = 1,
            //     Columns =
            //     [
            //         new DefaultColumnAction("Index", 0, false, "Index", typeof(int)),
            //         new DefaultColumnAction("CustomerId", 1, false, "CustomerId", typeof(Guid)),
            //         new DefaultColumnAction("FirstName", 2, false, "FirstName", typeof(string)),
            //         new DefaultColumnAction("LastName", 3, false, "LastName", typeof(string)),
            //         new DefaultColumnAction("Company", 4, false, "Company", typeof(string)),
            //         new DefaultColumnAction("City", 5, false, "City", typeof(string)),
            //         new DefaultColumnAction("Country", 6, false, "Country", typeof(string)),
            //         new DefaultColumnAction("Phone1", 7, false, "Phone1", typeof(string)),
            //         new DefaultColumnAction("Salary", 8, false, "Salary", typeof(double)),
            //         new DefaultColumnAction("Email", 9, false, "Email", typeof(string)),
            //         new DefaultColumnAction("SubscriptionDate", 10, false, "SubscriptionDate", typeof(DateTime)),
            //         new DefaultColumnAction("Website", 11, false, "Website", typeof(string))
            //     ]
            // };

            // int processedRowCount = 0;
            // int OnReadCount = 0;
            // int OnFinishCount = 0;

            // var extractor = new SqlDataExtractor(config);
            // extractor.OnRead += (args) => { OnReadCount++; }; 
            // extractor.OnFinish += (args) => { OnFinishCount++; };           

            // // Act
            // extractor.Extract((ref Dictionary<string, object?> row) => { processedRowCount++; });            

            // // Assert
            // Assert.True(processedRowCount > 0, "Nenhuma linha foi processada.");
            // Assert.True(OnReadCount > 0, "Nenhum evento OnRead foi disparado.");
            // Assert.True(OnFinishCount > 0, "Nenhum evento OnFinish foi disparado.");
        }
    }
}
