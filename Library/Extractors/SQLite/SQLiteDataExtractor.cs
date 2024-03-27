using Library.Infra;
using Library.Infra.ColumnActions;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Globalization;

namespace Library.Extractors
{
    public class SQLiteDataExtractor : IDataExtractor
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long TotalLines { get; set; }
        public int LineNumber { get; set; }
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }

        private readonly DatabaseDataExtractorConfig _config;
        private readonly Stopwatch _timer = new();
        private CultureInfo _cultureInfo;

        public SQLiteDataExtractor(DatabaseDataExtractorConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cultureInfo = new CultureInfo(_config.CultureInfo);
        }

        private void Init()
        {
            _timer.Restart();

            using var connection = new SqliteConnection(_config.ConnectionString);
            connection.Open();

            var countQuery = string.Format(_config.QueryCount, _config.TableName);
            using var commandCount = new SqliteCommand(countQuery, connection);
            TotalLines = Convert.ToInt64(commandCount.ExecuteScalar());
        }

        public void Extract(RowAction processRow)
        {
            var rowData = new Dictionary<string, object?>();

            try
            {
                Init();

                var actions = _config.Columns
                    .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
                    .ToDictionary(x => x.Name, x => x);

                int currentPage = 0;

                while (true)
                {
                    using var connection = new SqliteConnection(_config.ConnectionString);
                    connection.Open();

                    long offset = currentPage * _config.PageSize;
                    var selectQuery = string.Format(_config.QuerySelect, _config.TableName, _config.PageSize, offset);

                    using var command = new SqliteCommand(selectQuery, connection);
                    using var reader = command.ExecuteReader();

                    if (!reader.HasRows)
                    {
                        break;
                    }

                    while (reader.Read())
                    {
                        rowData.Clear();
                        foreach (var action in actions.Values)
                        {
                            var columnIndex = reader.GetOrdinal(action.Name);
                            if (reader.IsDBNull(columnIndex))
                            {
                                rowData[action.OutputName] = null;
                            }
                            else
                            {
                                var value = reader.GetValue(columnIndex);
                                rowData[action.OutputName] = action.ExecuteAction(value);
                            }
                        }

                        processRow(ref rowData);
                        LineNumber++;
                        NotifyReadProgress();
                    }

                    currentPage++;
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

        private void NotifyReadProgress()
        {
            if (LineNumber % _config.NotifyAfter == 0)
            {
                PercentRead = (double)LineNumber / TotalLines * 100;
                var speed = LineNumber / _timer.Elapsed.TotalSeconds;
                OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, 0, 0, PercentRead, speed));
            }
        }

        private void NotifyFinish()
        {
            PercentRead = 100;
            var speed = LineNumber / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, 0, 0, PercentRead, speed));
        }
    }
}
