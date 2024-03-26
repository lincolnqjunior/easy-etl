using Library.Infra;
using Library.Infra.ColumnActions;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace Library.Extractors.SQL
{
    /// <summary>
    /// Initializes a new instance of the SqlDataExtractor.
    /// </summary>
    /// <param name="config">The configuration for data extraction.</param>
    public class SqlDataExtractor(DatabaseDataExtractorConfig config) : IDataExtractor
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long TotalLines { get; set; }
        public int LineNumber { get; set; }
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }

        private readonly DatabaseDataExtractorConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly Stopwatch _timer = new();

        private Dictionary<string, object?> rowData = [];
        private Dictionary<string, IColumnAction> actions = [];

        /// <summary>
        /// Initializes the data extraction process by calculating the total number of rows.
        /// </summary>
        private async Task InitAsync()
        {
            _timer.Restart();

            actions = _config.ColumnsConfig
                    .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
                    .ToDictionary(x => x.Name, x => x);

            await using var connection = CreateConnection();
            await connection.OpenAsync();

            var countQuery = string.Format(_config.QueryCount, _config.TableName);
            await using var commandCount = new SqlCommand(countQuery, connection);
            TotalLines = Convert.ToInt64(await commandCount.ExecuteScalarAsync());
        }

        /// <summary>
        /// Extracts the data row by row, processing each through the provided action.
        /// </summary>
        /// <param name="processRow">The action to process each row of data.</param>
        public void Extract(RowAction processRow)
        {
            try
            {
                InitAsync().Wait();


                using var connection = CreateConnection();
                connection.Open();

                var selectQuery = string.Format(_config.QuerySelect, _config.TableName);

                using var command = new SqlCommand(selectQuery, connection);
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    rowData.Clear();
                    foreach (var action in actions.Values)
                    {
                        var columnIndex = reader.GetOrdinal(action.OutputName ?? action.Name);
                        rowData[action.OutputName ?? action.Name] = reader.IsDBNull(columnIndex)
                            ? null
                            : action.ExecuteAction(reader.GetValue(columnIndex));
                    }

                    processRow(ref rowData);
                    LineNumber++;
                    NotifyReadProgress();
                }

                NotifyFinish();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, ex, rowData, LineNumber));
            }
            finally
            {
                _timer.Stop();
            }
        }

        /// <summary>
        /// Notifies the progress of reading data.
        /// </summary>
        private void NotifyReadProgress()
        {
            if (LineNumber % _config.NotifyAfter == 0)
            {
                PercentRead = (double)LineNumber / TotalLines * 100;
                var speed = LineNumber / _timer.Elapsed.TotalSeconds;
                OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, BytesRead, FileSize, PercentRead, speed));
            }
        }

        /// <summary>
        /// Notifies when the reading process has finished.
        /// </summary>
        private void NotifyFinish()
        {
            PercentRead = 100;
            var speed = LineNumber / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, BytesRead, FileSize, PercentRead, speed));
        }

        /// <summary>
        /// Creates and returns a new SQL connection using the configured connection string.
        /// </summary>
        /// <returns>A new instance of SqlConnection.</returns>
        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_config.ConnectionString);
        }
    }
}
