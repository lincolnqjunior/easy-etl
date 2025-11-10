using Library.Infra;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;

namespace Library.Loaders.SQLite;

/// <summary>
/// Zero-allocation SQLite data loader that uses EtlRecord instead of Dictionary.
/// Uses batch transactions for efficient inserts.
/// </summary>
public class SqliteDataLoaderV2 : IDataLoaderV2
{
    private readonly DatabaseDataLoaderConfig _config;
    private readonly Stopwatch _timer = new();
    private readonly FieldDescriptor[] _schema;
    private SqliteConnection? _connection;
    private SqliteTransaction? _transaction;
    private SqliteCommand? _command;
    private bool _firstRecord = true;

    public event LoadNotificationHandler? OnWrite;
    public event LoadNotificationHandler? OnFinish;
    public event EasyEtlErrorEventHandler? OnError;

    public long CurrentLine { get; set; }
    public long TotalLines { get; set; }
    public double PercentWritten { get; set; }
    public double Speed => _timer.Elapsed.TotalSeconds > 0 ? CurrentLine / _timer.Elapsed.TotalSeconds : 0;

    /// <summary>
    /// Gets the schema expected by this loader.
    /// </summary>
    public FieldDescriptor[] Schema => _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteDataLoaderV2"/> class.
    /// </summary>
    /// <param name="config">Configuration for SQLite loading.</param>
    /// <param name="schema">Schema for the records to load.</param>
    public SqliteDataLoaderV2(DatabaseDataLoaderConfig config, FieldDescriptor[] schema)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    /// <summary>
    /// Loads a single record.
    /// </summary>
    public void Load(ref EtlRecord record, CancellationToken cancellationToken)
    {
        try
        {
            // Initialize on first record
            if (_firstRecord)
            {
                _timer.Restart();
                _connection = new SqliteConnection(_config.ConnectionString);
                _connection.Open();
                _transaction = _connection.BeginTransaction();
                _command = PrepareInsertCommand(_connection);
                _command.Transaction = _transaction;
                _firstRecord = false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Set parameter values from record
            for (int i = 0; i < _schema.Length; i++)
            {
                var value = record.GetValue(i);
                _command!.Parameters[i].Value = FieldValueToObject(value);
            }

            _command!.ExecuteNonQuery();
            CurrentLine++;

            // Commit batch if needed
            if (CurrentLine % _config.BatchSize == 0)
            {
                _transaction!.Commit();
                _transaction = _connection!.BeginTransaction();
                _command!.Transaction = _transaction;
            }

            if (CurrentLine % _config.RaiseChangeEventAfer == 0)
            {
                PercentWritten = TotalLines > 0 ? (double)CurrentLine / TotalLines * 100 : 0;
                OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, PercentWritten, Speed));
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, [], CurrentLine));
            throw;
        }
    }

    /// <summary>
    /// Completes the loading process.
    /// </summary>
    public async Task Complete(CancellationToken cancellationToken)
    {
        try
        {
            _transaction?.Commit();
        }
        catch (Exception ex)
        {
            _transaction?.Rollback();
            OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, [], CurrentLine));
        }
        finally
        {
            _transaction?.Dispose();
            _command?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _timer.Stop();
            OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, 100, Speed));
        }
        
        await Task.CompletedTask;
    }

    private SqliteCommand PrepareInsertCommand(SqliteConnection connection)
    {
        var command = connection.CreateCommand();

        var columnNames = string.Join(", ", _schema.Select(f => $"\"{f.Name}\""));
        var paramNames = string.Join(", ", _schema.Select(f => $"@{f.Name.Replace(" ", string.Empty)}"));
        command.CommandText = $"INSERT INTO {_config.TableName} ({columnNames}) VALUES ({paramNames})";

        foreach (var field in _schema)
        {
            command.Parameters.Add(new SqliteParameter($"@{field.Name.Replace(" ", string.Empty)}", DbType.Object));
        }

        return command;
    }

    private static object? FieldValueToObject(FieldValue value)
    {
        return value.Type switch
        {
            FieldType.Null => DBNull.Value,
            FieldType.Int32 => value.AsInt32(),
            FieldType.Int64 => value.AsInt64(),
            FieldType.Double => value.AsDouble(),
            FieldType.Float => value.AsFloat(),
            FieldType.Boolean => value.AsBoolean(),
            FieldType.DateTime => value.AsDateTime(),
            FieldType.Decimal => value.AsDecimal(),
            FieldType.Int16 => value.AsInt16(),
            FieldType.Byte => value.AsByte(),
            FieldType.Guid => value.AsGuid(),
            FieldType.String => value.AsString(),
            _ => DBNull.Value
        };
    }
}
