using Library.Infra;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Channels;
using System.Xml;

namespace Library.Loaders.Sql
{
    public class SqlDataLoader(DatabaseDataLoaderConfig config) : IDataLoader
    {
        private readonly DatabaseDataLoaderConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly Stopwatch _timer = new();
        private readonly object _lock = new();

        public event LoadNotificationHandler? OnWrite;
        public event LoadNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long CurrentLine { get; set; }
        public long TotalLines { get; set; }
        public double PercentWritten { get; set; }

        public async Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)
        {
            _timer.Restart();
            var channel = Channel.CreateBounded<Dictionary<string, object?>>(new BoundedChannelOptions(5) { SingleReader = false, SingleWriter = true });

            var tasks = new List<Task>();
            for (int i = 0; i < _config.WriteThreads; i++)
            {
                tasks.Add(Task.Run(() => ProcessRecordsAsync(channel.Reader, cancellationToken), cancellationToken));
            }

            try
            {
                await foreach (var record in data.WithCancellation(cancellationToken))
                {
                    await channel.Writer.WriteAsync(record, cancellationToken);
                }

                channel.Writer.Complete();

                // Aguarda todas as tarefas de consumidor serem completadas
                await Task.WhenAll(tasks);

                _timer.Stop();
                OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, 100, CalculateSpeed()));
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, new Dictionary<string, object?>(), CurrentLine));
            }
        }

        private async Task ProcessRecordsAsync(ChannelReader<Dictionary<string, object?>> reader, CancellationToken cancellationToken)
        {
            DataTable dataTable = new(_config.TableName);

            using var bulkCopy = new SqlBulkCopy(_config.ConnectionString, SqlBulkCopyOptions.TableLock)
            {
                DestinationTableName = _config.TableName,
                BatchSize = (int)_config.BatchSize,
                NotifyAfter = _config.NotifyAfter,
                EnableStreaming = true
            };

            bulkCopy.SqlRowsCopied += (sender, e) =>
            {
                lock (_lock) { CurrentLine += e.RowsCopied; }
                OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, CalculatePercentWritten(), CalculateSpeed()));
            };

            try
            {
                var dataReader = new AsyncEnumerableDataReader(reader.ReadAllAsync(cancellationToken));

                while (await dataReader.ReadAsync())
                {
                    if (dataTable.Columns.Count == 0)
                    {
                        for (int i = 0; i < dataReader.FieldCount; i++)
                        {
                            var outputName = dataReader.GetName(i);
                            var fieldType = dataReader.GetFieldType(i);
                            var columnType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
                            var column = dataTable.Columns.Add(outputName, columnType);
                            bulkCopy.ColumnMappings.Add(column.ColumnName, outputName);
                        }
                    }

                    DataRow row = dataTable.NewRow();
                    for (int i = 0; i < dataReader.FieldCount; i++)
                    {
                        row[i] = dataReader.GetValue(i) ?? DBNull.Value;
                    }

                    dataTable.Rows.Add(row);

                    if (CurrentLine % _config.NotifyAfter == 0)
                    {
                        OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, CalculatePercentWritten(), CalculateSpeed()));
                    }

                    if (dataTable.Rows.Count >= _config.BatchSize)
                    {
                        await ExecuteBulk(dataTable, bulkCopy, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, new Dictionary<string, object?>(), CurrentLine));
                throw;
            }

            // Do a final write with any remaining rows
            if (dataTable.Rows.Count > 0)
            {
                lock (_lock) { CurrentLine += dataTable.Rows.Count; }
                await ExecuteBulk(dataTable, bulkCopy, cancellationToken);
            }
        }

        private static async Task ExecuteBulk(DataTable dataTable, SqlBulkCopy bulkCopy, CancellationToken cancellationToken)
        {
            await bulkCopy.WriteToServerAsync(dataTable, cancellationToken);
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
