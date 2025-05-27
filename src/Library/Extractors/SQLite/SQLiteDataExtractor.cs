using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.EventArgs;
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

        public async Task Extract(RowAction processRow, CancellationToken cancellationToken)
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
                    await using var connection = new SqliteConnection(_config.ConnectionString); // Switched to await using
                    await connection.OpenAsync(cancellationToken); // Used async version

                    long offset = currentPage * _config.PageSize;
                    var selectQuery = string.Format(_config.QuerySelect, _config.TableName, _config.PageSize, offset);

                    await using var command = new SqliteCommand(selectQuery, connection); // Switched to await using
                    await using var reader = await command.ExecuteReaderAsync(cancellationToken); // Used async version

                    if (!reader.HasRows)
                    {
                        break;
                    }

                    while (await reader.ReadAsync(cancellationToken)) // Used async version
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        rowData.Clear();
                        foreach (var action in actions.Values)
                        {
                            var columnIndex = reader.GetOrdinal(action.Name); // GetOrdinal is sync
                            if (await reader.IsDBNullAsync(columnIndex, cancellationToken)) // Used async version
                            {
                                rowData[action.OutputName ?? action.Name] = null;
                            }
                            else
                            {
                                var value = reader.GetValue(columnIndex); // GetValue is sync
                                rowData[action.OutputName ?? action.Name] = action.ExecuteAction(value);
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
            if (LineNumber % _config.RaiseChangeEventAfer == 0)
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
