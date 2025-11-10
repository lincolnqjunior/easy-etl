using Library.Infra;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;
using nietras.SeparatedValues;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Library.Loaders.Csv;

/// <summary>
/// Zero-allocation CSV data loader that uses EtlRecord instead of Dictionary.
/// Provides improved performance by minimizing allocations during CSV writing.
/// </summary>
public class CsvDataLoaderV2 : IDataLoaderV2
{
    public event LoadNotificationHandler? OnWrite;
    public event LoadNotificationHandler? OnFinish;
    public event EasyEtlErrorEventHandler? OnError;

    public long CurrentLine { get; set; } = 0;
    public long TotalLines { get; set; } = 0;
    public double PercentWritten { get; set; } = 0;
    public double Speed => _timer.Elapsed.TotalSeconds > 0 ? CurrentLine / _timer.Elapsed.TotalSeconds : 0;

    private readonly CsvDataLoaderConfig _config;
    private readonly Stopwatch _timer = new();
    private readonly FieldDescriptor[] _schema;
    private SepWriter? _writer;

    /// <summary>
    /// Gets the schema expected by this loader.
    /// </summary>
    public FieldDescriptor[] Schema => _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvDataLoaderV2"/> class.
    /// </summary>
    /// <param name="config">Configuration for CSV loading.</param>
    /// <param name="schema">Schema for the records to load.</param>
    public CsvDataLoaderV2(CsvDataLoaderConfig config, FieldDescriptor[] schema)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    /// <summary>
    /// Loads a single record synchronously.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Load(ref EtlRecord record, CancellationToken cancellationToken)
    {
        try
        {
            // Initialize writer on first record
            if (_writer == null)
            {
                _timer.Restart();
                _writer = Sep.New(_config.Delimiter).Writer().ToFile(_config.OutputPath);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Write the row
            HandleRow(_writer, record);

            PercentWritten = TotalLines > 0 ? (double)CurrentLine / TotalLines * 100 : 0;

            if (CurrentLine % _config.RaiseChangeEventAfer == 0)
            {
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
            _writer?.Dispose();
            _writer = null;
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

    /// <summary>
    /// Handles writing a single row to CSV.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void HandleRow(SepWriter writer, EtlRecord record)
    {
        using var line = writer.NewRow();

        for (int i = 0; i < _schema.Length; i++)
        {
            var field = _schema[i];
            var value = record.GetValue(i);

            switch (value.Type)
            {
                case FieldType.Null:
                    line[field.Name].Set((string?)null);
                    break;
                case FieldType.String:
                    line[field.Name].Set(value.AsString());
                    break;
                case FieldType.Int32:
                    line[field.Name].Format(value.AsInt32());
                    break;
                case FieldType.Int64:
                    line[field.Name].Format(value.AsInt64());
                    break;
                case FieldType.Double:
                    line[field.Name].Format(value.AsDouble());
                    break;
                case FieldType.Float:
                    line[field.Name].Format(value.AsFloat());
                    break;
                case FieldType.Boolean:
                    line[field.Name].Set(value.AsBoolean().ToString());
                    break;
                case FieldType.DateTime:
                    line[field.Name].Format(value.AsDateTime());
                    break;
                case FieldType.Decimal:
                    line[field.Name].Format(value.AsDecimal());
                    break;
                case FieldType.Int16:
                    line[field.Name].Format(value.AsInt16());
                    break;
                case FieldType.Byte:
                    line[field.Name].Format(value.AsByte());
                    break;
                case FieldType.Guid:
                    line[field.Name].Format(value.AsGuid());
                    break;
                default:
                    line[field.Name].Set((string?)null);
                    break;
            }
        }

        CurrentLine++;
    }
}
