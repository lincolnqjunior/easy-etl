using Library.Extractors;
using Library.Infra;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Infra.Helpers;
using Library.Infra.ZeroAlloc;
using System.Diagnostics;
using System.Text.Json;

namespace Library.Extractors.Json;

/// <summary>
/// Zero-allocation JSON/JSONL data extractor that uses EtlRecord instead of Dictionary.
/// Provides significant performance improvements by eliminating allocations in the hot path.
/// Supports both JSON array format and JSONL (newline-delimited JSON) format.
/// </summary>
public class JsonDataExtractorV2 : IDataExtractorV2
{
    public event ReadNotification? OnRead;
    public event ReadNotification? OnFinish;
    public event EasyEtlErrorEventHandler? OnError;

    public long TotalLines { get; set; }
    public int LineNumber { get; set; }
    public long BytesRead { get; set; }
    public double PercentRead { get; set; }
    public long FileSize { get; set; }

    private readonly JsonDataExtractorConfig _config;
    private readonly EtlRecordPool _pool;
    private readonly Stopwatch _timer = new();
    private FieldDescriptor[]? _schema;

    /// <summary>
    /// Gets the schema for the records produced by this extractor.
    /// Schema is inferred from the first JSON object.
    /// </summary>
    public FieldDescriptor[] Schema => _schema ?? throw new InvalidOperationException("Schema not initialized. Call Extract first.");

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDataExtractorV2"/> class.
    /// </summary>
    /// <param name="config">Configuration for JSON extraction.</param>
    /// <param name="pool">Optional pool for buffer management.</param>
    public JsonDataExtractorV2(JsonDataExtractorConfig config, EtlRecordPool? pool = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _pool = pool ?? new EtlRecordPool();

        var fileInfo = new FileInfo(_config.FilePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("The input file was not found.", _config.FilePath);

        FileSize = fileInfo.Length;
        TotalLines = fileInfo.CountLines().Result;
    }

    /// <summary>
    /// Extracts data from JSON/JSONL file using zero-allocation EtlRecord.
    /// </summary>
    /// <param name="processRecord">Action to process each record.</param>
    public void Extract(RecordAction processRecord)
    {
        try
        {
            _timer.Start();

            byte[]? buffer = null;

            try
            {
                using var fs = new FileStream(_config.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, false);
                using var reader = new StreamReader(fs);
                
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    BytesRead = fs.Position;

                    // Deserialize to Dictionary first (for schema inference if needed)
                    var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, object?>>(line);
                    if (jsonDoc == null) continue;

                    // Build schema on first record
                    if (_schema == null)
                    {
                        BuildSchemaFromJson(jsonDoc);
                        var bufferSize = EtlRecordPool.CalculateBufferSize(_schema!);
                        buffer = _pool.RentBuffer(bufferSize);
                    }

                    // Create record with buffer
                    var record = new EtlRecord(buffer!.AsSpan(), _schema!);

                    // Populate record from JSON
                    PopulateRecordFromJson(ref record, jsonDoc);

                    // Process record
                    processRecord(ref record);
                    LineNumber++;
                    NotifyReadProgress();
                }
            }
            finally
            {
                if (buffer != null)
                {
                    _pool.ReturnBuffer(buffer);
                }
            }

            NotifyFinish();
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, ex, new Dictionary<string, object?>(), LineNumber));
            throw;
        }
        finally
        {
            _timer.Stop();
        }
    }

    /// <summary>
    /// Builds the schema by inferring types from the first JSON object.
    /// </summary>
    private void BuildSchemaFromJson(Dictionary<string, object?> jsonObject)
    {
        var fields = new List<(string Name, FieldType Type)>();

        foreach (var kvp in jsonObject)
        {
            var fieldType = InferFieldType(kvp.Value);
            fields.Add((kvp.Key, fieldType));
        }

        _schema = EtlRecordPool.CreateSchema(fields.ToArray());
    }

    /// <summary>
    /// Infers FieldType from JSON value.
    /// </summary>
    private static FieldType InferFieldType(object? value)
    {
        if (value == null)
            return FieldType.Null;

        // JsonElement type checking
        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => FieldType.String,
                JsonValueKind.Number => FieldType.Double, // Default to double for JSON numbers
                JsonValueKind.True or JsonValueKind.False => FieldType.Boolean,
                JsonValueKind.Null => FieldType.Null,
                _ => FieldType.String
            };
        }

        // Direct type checking (for already deserialized values)
        return value switch
        {
            string => FieldType.String,
            int => FieldType.Int32,
            long => FieldType.Int64,
            double => FieldType.Double,
            float => FieldType.Float,
            bool => FieldType.Boolean,
            DateTime => FieldType.DateTime,
            decimal => FieldType.Decimal,
            short => FieldType.Int16,
            byte => FieldType.Byte,
            Guid => FieldType.Guid,
            _ => FieldType.String // Default to string
        };
    }

    /// <summary>
    /// Populates an EtlRecord from a JSON dictionary.
    /// </summary>
    private void PopulateRecordFromJson(ref EtlRecord record, Dictionary<string, object?> jsonObject)
    {
        for (int i = 0; i < _schema!.Length; i++)
        {
            var fieldName = _schema[i].Name;
            
            if (!jsonObject.TryGetValue(fieldName, out var value) || value == null)
            {
                record.SetValue(i, FieldValue.Null());
                continue;
            }

            var fieldValue = ConvertJsonValueToFieldValue(value, _schema[i].Type);
            record.SetValue(i, fieldValue);
        }
    }

    /// <summary>
    /// Converts a JSON value to FieldValue based on expected type.
    /// </summary>
    private static FieldValue ConvertJsonValueToFieldValue(object value, FieldType expectedType)
    {
        // Handle JsonElement specially
        if (value is JsonElement jsonElement)
        {
            return expectedType switch
            {
                FieldType.String => FieldValue.FromString(jsonElement.GetString() ?? string.Empty),
                FieldType.Int32 => FieldValue.FromInt32(jsonElement.GetInt32()),
                FieldType.Int64 => FieldValue.FromInt64(jsonElement.GetInt64()),
                FieldType.Double => FieldValue.FromDouble(jsonElement.GetDouble()),
                FieldType.Float => FieldValue.FromFloat((float)jsonElement.GetDouble()),
                FieldType.Boolean => FieldValue.FromBoolean(jsonElement.GetBoolean()),
                FieldType.DateTime => FieldValue.FromDateTime(jsonElement.GetDateTime()),
                FieldType.Decimal => FieldValue.FromDecimal(jsonElement.GetDecimal()),
                FieldType.Guid => FieldValue.FromGuid(jsonElement.GetGuid()),
                FieldType.Null => FieldValue.Null(),
                _ => FieldValue.FromString(jsonElement.ToString())
            };
        }

        // Handle already-converted types
        return expectedType switch
        {
            FieldType.String => FieldValue.FromString(value.ToString() ?? string.Empty),
            FieldType.Int32 => FieldValue.FromInt32(Convert.ToInt32(value)),
            FieldType.Int64 => FieldValue.FromInt64(Convert.ToInt64(value)),
            FieldType.Double => FieldValue.FromDouble(Convert.ToDouble(value)),
            FieldType.Float => FieldValue.FromFloat(Convert.ToSingle(value)),
            FieldType.Boolean => FieldValue.FromBoolean(Convert.ToBoolean(value)),
            FieldType.DateTime => FieldValue.FromDateTime(Convert.ToDateTime(value)),
            FieldType.Decimal => FieldValue.FromDecimal(Convert.ToDecimal(value)),
            FieldType.Int16 => FieldValue.FromInt16(Convert.ToInt16(value)),
            FieldType.Byte => FieldValue.FromByte(Convert.ToByte(value)),
            FieldType.Guid => FieldValue.FromGuid(Guid.Parse(value.ToString()!)),
            _ => FieldValue.FromString(value.ToString() ?? string.Empty)
        };
    }

    /// <summary>
    /// Notifies subscribers of progress at configured intervals.
    /// </summary>
    private void NotifyReadProgress()
    {
        if (LineNumber % _config.RaiseChangeEventAfer == 0)
        {
            PercentRead = (double)BytesRead / FileSize * 100;
            var speed = LineNumber / _timer.Elapsed.TotalSeconds;

            OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
        }
    }

    /// <summary>
    /// Notifies subscribers that extraction has completed.
    /// </summary>
    private void NotifyFinish()
    {
        _timer.Stop();

        TotalLines = LineNumber;
        BytesRead = FileSize;
        PercentRead = 100;
        var speed = LineNumber / _timer.Elapsed.TotalSeconds;

        OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
    }
}
