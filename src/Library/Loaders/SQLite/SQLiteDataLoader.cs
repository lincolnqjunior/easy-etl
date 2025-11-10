using Library.Infra.Config;
using Library.Infra.EventArgs;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;

namespace Library.Loaders.SQLite
{
    public class SqliteDataLoader(DatabaseDataLoaderConfig config) : IDataLoader
    {
        private readonly DatabaseDataLoaderConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly Stopwatch _timer = new();
        private bool firstRecord = true;

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
            var transaction = connection.BeginTransaction();

            var command = connection.CreateCommand();
            command.Transaction = transaction;

            try
            {
                await foreach (var record in data.WithCancellation(cancellationToken))
                {
                    if (firstRecord)
                    {
                        command = PrepareInsertCommand(connection, record);
                        firstRecord = false;
                    }

                    int parameterIndex = 0;
                    foreach (var kvp in record)
                    {
                        command.Parameters[parameterIndex].Value = kvp.Value ?? DBNull.Value;
                        parameterIndex++;
                    }

                    command.ExecuteNonQuery();

                    CurrentLine++;

                    if (CurrentLine % _config.BatchSize == 0)
                    {
                        transaction.Commit();
                        transaction = connection.BeginTransaction();
                        command.Transaction = transaction;
                    }

                    if (CurrentLine % _config.RaiseChangeEventAfer == 0)
                    {
                        PercentWritten = (double)CurrentLine / TotalLines * 100;
                        OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, PercentWritten, CurrentLine / _timer.Elapsed.TotalSeconds));
                    }
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback(); 
                OnError?.Invoke(new ErrorNotificationEventArgs(Infra.EtlType.Load, ex, [], CurrentLine));
            }            
            finally
            {
                transaction.Dispose();
                _timer.Stop();
                OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, 100, CurrentLine / _timer.Elapsed.TotalSeconds));
            }
        }

        private SqliteCommand PrepareInsertCommand(SqliteConnection connection, Dictionary<string, object?> sampleRecord)
        {
            var command = connection.CreateCommand();

            var columnNames = string.Join(", ", sampleRecord.Keys.Select(k => $"\"{k}\""));
            var paramNames = string.Join(", ", sampleRecord.Keys.Select(k => $"@{k.Replace(" ", string.Empty)}"));
            command.CommandText = $"INSERT INTO {_config.TableName} ({columnNames}) VALUES ({paramNames})";

            foreach (var key in sampleRecord.Keys)
            {
                command.Parameters.Add(new SqliteParameter($"@{key.Replace(" ", string.Empty)}", DbType.Object));
            }

            return command;
        }
    }
}
