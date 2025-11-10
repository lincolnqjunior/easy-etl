using Library.Infra;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace Library.Loaders.Sql;

/// <summary>
/// Zero-allocation SQL Server data loader that uses EtlRecord instead of Dictionary.
/// Uses SqlBulkCopy for efficient batch insertions.
/// </summary>
public class SqlDataLoaderV2 : IDataLoaderV2
{
    private readonly DatabaseDataLoaderConfig _config;
    private readonly Stopwatch _timer = new();
    private readonly FieldDescriptor[] _schema;
    private readonly DataTable _dataTable;
    private SqlBulkCopy? _bulkCopy;

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
    /// Initializes a new instance of the <see cref="SqlDataLoaderV2"/> class.
    /// </summary>
    /// <param name="config">Configuration for SQL Server loading.</param>
    /// <param name="schema">Schema for the records to load.</param>
    public SqlDataLoaderV2(DatabaseDataLoaderConfig config, FieldDescriptor[] schema)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        
        // Initialize DataTable with schema
        _dataTable = new DataTable(_config.TableName);
        foreach (var field in _schema)
        {
            var columnType = FieldTypeToClrType(field.Type);
            _dataTable.Columns.Add(field.Name, columnType);
        }
    }

    /// <summary>
    /// Loads a single record by adding it to the batch buffer.
    /// </summary>
    public void Load(ref EtlRecord record, CancellationToken cancellationToken)
    {
        try
        {
            // Initialize on first record
            if (_bulkCopy == null)
            {
                _timer.Restart();
                _bulkCopy = new SqlBulkCopy(_config.ConnectionString, SqlBulkCopyOptions.TableLock)
                {
                    DestinationTableName = _config.TableName,
                    BatchSize = (int)_config.BatchSize,
                    NotifyAfter = _config.RaiseChangeEventAfer,
                    EnableStreaming = true
                };
                
                _bulkCopy.SqlRowsCopied += (sender, e) =>
                {
                    OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, PercentWritten, Speed));
                };
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Add row to DataTable buffer
            var row = _dataTable.NewRow();
            for (int i = 0; i < _schema.Length; i++)
            {
                var value = record.GetValue(i);
                row[_schema[i].Name] = FieldValueToObject(value);
            }
            _dataTable.Rows.Add(row);
            CurrentLine++;

            // Flush batch if needed
            if (_dataTable.Rows.Count >= _config.BatchSize)
            {
                FlushBatch();
            }

            PercentWritten = TotalLines > 0 ? (double)CurrentLine / TotalLines * 100 : 0;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, [], CurrentLine));
            throw;
        }
    }

    /// <summary>
    /// Completes the loading process by flushing remaining rows.
    /// </summary>
    public async Task Complete(CancellationToken cancellationToken)
    {
        try
        {
            // Flush any remaining rows
            if (_dataTable.Rows.Count > 0)
            {
                FlushBatch();
            }

            _bulkCopy?.Close();
            _bulkCopy = null;
            _dataTable.Dispose();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, [], CurrentLine));
        }
        finally
        {
            _timer.Stop();
            OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, 100, Speed));
        }
        
        await Task.CompletedTask;
    }

    private void FlushBatch()
    {
        if (_bulkCopy != null && _dataTable.Rows.Count > 0)
        {
            _bulkCopy.WriteToServer(_dataTable);
            _dataTable.Rows.Clear();
        }
    }

    private static Type FieldTypeToClrType(FieldType fieldType)
    {
        return fieldType switch
        {
            FieldType.Int32 => typeof(int),
            FieldType.Int64 => typeof(long),
            FieldType.Double => typeof(double),
            FieldType.Float => typeof(float),
            FieldType.Boolean => typeof(bool),
            FieldType.DateTime => typeof(DateTime),
            FieldType.Decimal => typeof(decimal),
            FieldType.Int16 => typeof(short),
            FieldType.Byte => typeof(byte),
            FieldType.Guid => typeof(Guid),
            FieldType.String => typeof(string),
            _ => typeof(object)
        };
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
