using Library.Infra;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;
using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Library.Loaders.Json;

/// <summary>
/// Zero-allocation JSON data loader that uses EtlRecord instead of Dictionary.
/// Provides improved performance by minimizing allocations during JSON serialization.
/// </summary>
public class JsonDataLoaderV2 : IDataLoaderV2
{
    public event LoadNotificationHandler? OnWrite;
    public event LoadNotificationHandler? OnFinish;
    public event EasyEtlErrorEventHandler? OnError;

    public long CurrentLine { get; set; }
    public long TotalLines { get; set; }
    public double PercentWritten { get; set; }

    private readonly JsonDataLoaderConfig _config;
    private readonly Stopwatch _timer = new();
    private readonly FieldDescriptor[] _schema;
    private FileStream? _outputStream;
    private readonly MemoryStream _bufferStream = new();
    private bool _firstRecord = true;
    private bool _firstElement = true;

    /// <summary>
    /// Gets the schema expected by this loader.
    /// </summary>
    public FieldDescriptor[] Schema => _schema;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDataLoaderV2"/> class.
    /// </summary>
    /// <param name="config">Configuration for JSON loading.</param>
    /// <param name="schema">Schema for the records to load.</param>
    public JsonDataLoaderV2(JsonDataLoaderConfig config, FieldDescriptor[] schema)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    /// <summary>
    /// Loads a single record synchronously (ref struct limitation).
    /// Data is extracted synchronously, then buffered for async flush in Complete().
    /// </summary>
    public void Load(ref EtlRecord record, CancellationToken cancellationToken)
    {
        try
        {
            // Initialize on first record
            if (_firstRecord)
            {
                _timer.Restart();
                _outputStream = new FileStream(_config.OutputPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: false);
                
                if (!_config.IsJsonl)
                {
                    // Write JSON array opening bracket
                    _outputStream.Write(Encoding.UTF8.GetBytes("["));
                }
                
                _firstRecord = false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Synchronously serialize record to buffer (ref struct limitation)
            _bufferStream.SetLength(0);
            _bufferStream.Position = 0;
            
            using (var writer = new Utf8JsonWriter(_bufferStream, new JsonWriterOptions 
            { 
                Indented = _config.IsJsonl ? false : _config.IndentJson,
                SkipValidation = true 
            }))
            {
                // Must extract data from ref struct synchronously
                WriteRecordToJson(writer, record);
                writer.Flush();
            }

            // Synchronous write the buffered data
            var recordBytes = _bufferStream.ToArray();
            
            if (!_config.IsJsonl && !_firstElement)
            {
                // Add comma between array elements
                _outputStream!.Write(Encoding.UTF8.GetBytes(","));
            }
            
            _outputStream!.Write(recordBytes);
            
            if (_config.IsJsonl)
            {
                _outputStream.Write(Encoding.UTF8.GetBytes(Environment.NewLine));
            }

            _firstElement = false;
            CurrentLine++;
            UpdateProgress();
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
            if (_outputStream != null)
            {
                if (!_config.IsJsonl)
                {
                    // Write JSON array closing bracket
                    await _outputStream.WriteAsync(Encoding.UTF8.GetBytes("]"), cancellationToken);
                }
                
                await _outputStream.FlushAsync(cancellationToken);
                await _outputStream.DisposeAsync();
                _outputStream = null;
            }
            
            _bufferStream.Dispose();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, [], CurrentLine));
        }
        finally
        {
            _timer.Stop();
            OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, 100, 
                _timer.Elapsed.TotalSeconds > 0 ? CurrentLine / _timer.Elapsed.TotalSeconds : 0));
        }
    }

    /// <summary>
    /// Writes a record to JSON format. Must be called synchronously.
    /// </summary>
    private void WriteRecordToJson(Utf8JsonWriter writer, EtlRecord record)
    {
        writer.WriteStartObject();

        for (int i = 0; i < _schema.Length; i++)
        {
            var field = _schema[i];
            var value = record.GetValue(i);

            writer.WritePropertyName(field.Name);

            switch (value.Type)
            {
                case FieldType.Null:
                    writer.WriteNullValue();
                    break;
                case FieldType.String:
                    writer.WriteStringValue(value.AsString());
                    break;
                case FieldType.Int32:
                    writer.WriteNumberValue(value.AsInt32());
                    break;
                case FieldType.Int64:
                    writer.WriteNumberValue(value.AsInt64());
                    break;
                case FieldType.Double:
                    writer.WriteNumberValue(value.AsDouble());
                    break;
                case FieldType.Float:
                    writer.WriteNumberValue(value.AsFloat());
                    break;
                case FieldType.Boolean:
                    writer.WriteBooleanValue(value.AsBoolean());
                    break;
                case FieldType.DateTime:
                    writer.WriteStringValue(value.AsDateTime());
                    break;
                case FieldType.Decimal:
                    writer.WriteNumberValue(value.AsDecimal());
                    break;
                case FieldType.Int16:
                    writer.WriteNumberValue(value.AsInt16());
                    break;
                case FieldType.Byte:
                    writer.WriteNumberValue(value.AsByte());
                    break;
                case FieldType.Guid:
                    writer.WriteStringValue(value.AsGuid());
                    break;
                default:
                    writer.WriteNullValue();
                    break;
            }
        }

        writer.WriteEndObject();
    }

    private void UpdateProgress()
    {
        PercentWritten = TotalLines > 0 ? (double)CurrentLine / TotalLines * 100 : 0;
        if (CurrentLine % _config.RaiseChangeEventAfer == 0)
        {
            OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, PercentWritten, 
                _timer.Elapsed.TotalSeconds > 0 ? CurrentLine / _timer.Elapsed.TotalSeconds : 0));
        }
    }
}
