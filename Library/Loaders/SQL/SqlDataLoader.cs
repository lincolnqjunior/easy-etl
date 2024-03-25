using Library.Infra;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Library.Loaders.Sql
{
    public class SqlDataLoader(DatabaseDataLoaderConfig config) : IDataLoader
    {
        private readonly DatabaseDataLoaderConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly Stopwatch _timer = new Stopwatch();

        public event LoadNotificationHandler? OnWrite;
        public event LoadNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long CurrentLine { get; set; }
        public long TotalLines { get; set; }
        public double PercentWritten { get; set; }

        public async Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)
        {
            _timer.Restart();
            DataTable dataTable = new(_config.TableName);

            using var bulkCopy = new SqlBulkCopy(_config.ConnectionString, SqlBulkCopyOptions.TableLock)
            {
                DestinationTableName = _config.TableName,
                BatchSize = (int)_config.BatchSize,
                NotifyAfter = _config.NotifyAfter,
            };

            bulkCopy.SqlRowsCopied += (sender, e) =>
            {                
                PercentWritten = CalculatePercentWritten();
                OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, PercentWritten, CalculateSpeed()));
            };

            try
            {
                await foreach (var record in data.WithCancellation(cancellationToken))
                {
                    if (dataTable.Columns.Count == 0)
                    {
                        foreach (var kvp in record)
                        {
                            var outputName = $"{kvp.Key.Replace(" ", string.Empty)}";
                            var column = dataTable.Columns.Add(outputName, kvp.Value?.GetType() ?? typeof(string));
                            bulkCopy.ColumnMappings.Add(column.ColumnName, outputName);
                        }
                    }

                    DataRow row = dataTable.NewRow();
                    foreach (var kvp in record)
                    {
                        var outputName = $"{kvp.Key.Replace(" ", string.Empty)}";
                        row[outputName] = kvp.Value ?? DBNull.Value;
                    }

                    dataTable.Rows.Add(row);

                    if (dataTable.Rows.Count >= _config.BatchSize)
                    {
                        await ExecuteBulk(dataTable, bulkCopy, cancellationToken);
                    }
                }

                ExecuteBulk(dataTable, bulkCopy, cancellationToken);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, new Dictionary<string, object?>(), CurrentLine));
            }

            _timer.Stop();
            OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, 100, CalculateSpeed()));
        }

        private async Task ExecuteBulk(DataTable dataTable, SqlBulkCopy bulkCopy, CancellationToken cancellationToken)
        {
            await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
            CurrentLine += dataTable.Rows.Count;
            dataTable.Clear();
        }

        private double CalculatePercentWritten()
        {
            return (TotalLines > 0) ?
                (double)CurrentLine / TotalLines * 100 :
                0;
        }

        private double CalculateSpeed()
        {
            return (_timer.Elapsed.TotalSeconds > 0) ?
                CurrentLine / _timer.Elapsed.TotalSeconds :
                0;
        }
    }
}
