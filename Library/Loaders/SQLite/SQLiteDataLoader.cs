using Library.Infra;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace Library.Loaders.SQLite
{
    public class SQLiteDataLoader(DatabaseDataLoaderConfig config) : IDataLoader
    {
        private readonly DatabaseDataLoaderConfig _config = config;
        private readonly Stopwatch _timer = new();

        public event LoadNotificationHandler? OnWrite;
        public event LoadNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long CurrentLine { get; set; }
        public long TotalLines { get; set; }
        public double PercentWritten { get; set; }


        public async Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)
        {
            _timer.Restart();
            await using var connection = new SqliteConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            using var transaction = connection.BeginTransaction();
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            await foreach (var record in data)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    command.CommandText = BuildInsertCommand(record);
                    command.Parameters.Clear();

                    foreach (var kvp in record)
                    {
                        command.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                    }

                    await command.ExecuteNonQueryAsync(cancellationToken);
                    CurrentLine++;
                    PercentWritten = (double)CurrentLine / TotalLines * 100;
                    OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, PercentWritten, CurrentLine / _timer.Elapsed.TotalSeconds));
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, record, CurrentLine));
                    break;
                }
            }

            await transaction.CommitAsync(cancellationToken);
            _timer.Stop();

            OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, 100, CurrentLine / _timer.Elapsed.TotalSeconds));
        }

        private string BuildInsertCommand(Dictionary<string, object?> record)
        {
            var columnNames = string.Join(", ", record.Keys);
            var paramNames = string.Join(", ", record.Keys.Select(k => "@" + k));
            return $"INSERT INTO {_config.TableName} ({columnNames}) VALUES ({paramNames})";
        }
    }
}
