using Library.Infra;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Channels;

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
        public double Speed { get; set; }

        public async Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)
        {
            _timer.Restart();
            var channel = Channel.CreateBounded<Dictionary<string, object?>>(new BoundedChannelOptions(5) { SingleReader = false, SingleWriter = true });
            var tasks = new List<Task>();

            try
            {
                for (int i = 0; i < _config.WriteThreads; i++)
                {
                    tasks.Add(Task.Run(() => ProcessRecordsAsync(channel.Reader, cancellationToken), cancellationToken));
                }

                await foreach (var record in data.WithCancellation(cancellationToken))
                {
                    await channel.Writer.WriteAsync(record, cancellationToken);
                }

                channel.Writer.Complete();
                await Task.WhenAll(tasks);

                _timer.Stop();

                lock (_lock)
                {
                    PercentWritten = 100;
                    Speed = CalculateSpeed();
                }

                OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, PercentWritten, Speed));
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, [], CurrentLine));
            }
        }

        private async Task ProcessRecordsAsync(ChannelReader<Dictionary<string, object?>> reader, CancellationToken cancellationToken)
        {
            DataTable dataTable = new(_config.TableName);
            using var bulkCopy = new SqlBulkCopy(_config.ConnectionString, SqlBulkCopyOptions.TableLock)
            {
                DestinationTableName = _config.TableName,
                BatchSize = (int)_config.BatchSize,
                NotifyAfter = _config.RaiseChangeEventAfer,
                EnableStreaming = true
            };

            bulkCopy.SqlRowsCopied += (sender, e) =>
            {
                lock (_lock) { CurrentLine += e.RowsCopied; }
                NotifyChange();
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
                        var value = dataReader.GetValue(i);
                        if (value is DateTime dateTime)
                        {
                            if (dateTime == DateTime.MinValue) { row[i] = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc); }
                            else { row[i] = dateTime; }
                        }
                        else
                            row[i] = value ?? default;
                    }

                    dataTable.Rows.Add(row);

                    if (dataTable.Rows.Count >= _config.BatchSize)
                        await ExecuteBulk(dataTable, bulkCopy, cancellationToken);

                    if (CurrentLine % _config.RaiseChangeEventAfer == 0)
                        NotifyChange();
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, [], CurrentLine));
                throw;
            }

            // Do a final write with any remaining rows
            if (dataTable.Rows.Count > 0)
            {
                lock (_lock) { CurrentLine += dataTable.Rows.Count; }
                await ExecuteBulk(dataTable, bulkCopy, cancellationToken);
            }
        }

        private void NotifyChange()
        {
            lock (_lock)
            {
                PercentWritten = CalculatePercentWritten();
                Speed = CalculateSpeed();
            }

            OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, PercentWritten, Speed));
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
