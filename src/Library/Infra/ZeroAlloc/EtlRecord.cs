using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Library.Infra.ZeroAlloc;

/// <summary>
/// A zero-allocation record structure for ETL operations.
/// Uses ref struct to ensure stack-only allocation and Span-based APIs for zero-copy access.
/// This structure is designed to be rented from a pool, used, and returned without heap allocations.
/// </summary>
public ref struct EtlRecord
{
    private Span<byte> _buffer;
    private ReadOnlySpan<FieldDescriptor> _schema;
    private readonly bool _isValid;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlRecord"/> struct.
    /// </summary>
    /// <param name="buffer">The buffer to store field data.</param>
    /// <param name="schema">The schema describing field layout.</param>
    public EtlRecord(Span<byte> buffer, ReadOnlySpan<FieldDescriptor> schema)
    {
        _buffer = buffer;
        _schema = schema;
        _isValid = true;
    }

    /// <summary>
    /// Gets the number of fields in this record.
    /// </summary>
    public readonly int FieldCount => _schema.Length;

    /// <summary>
    /// Gets a value indicating whether this record is valid and usable.
    /// </summary>
    public readonly bool IsValid => _isValid;

    /// <summary>
    /// Gets the field descriptor at the specified index.
    /// </summary>
    /// <param name="index">The field index.</param>
    /// <returns>The field descriptor.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref readonly FieldDescriptor GetFieldDescriptor(int index)
    {
        if (index < 0 || index >= _schema.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        return ref _schema[index];
    }

    /// <summary>
    /// Gets the field descriptor by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field descriptor, or null if not found.</returns>
    public readonly FieldDescriptor? GetFieldDescriptorByName(string name)
    {
        for (int i = 0; i < _schema.Length; i++)
        {
            if (_schema[i].Name == name)
                return _schema[i];
        }
        return null;
    }

    /// <summary>
    /// Gets the field index by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field index, or -1 if not found.</returns>
    public readonly int GetFieldIndex(string name)
    {
        for (int i = 0; i < _schema.Length; i++)
        {
            if (_schema[i].Name == name)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Gets the field value at the specified index.
    /// </summary>
    /// <param name="index">The field index.</param>
    /// <returns>The field value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly FieldValue GetValue(int index)
    {
        if (index < 0 || index >= _schema.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref readonly var descriptor = ref _schema[index];
        var fieldBuffer = _buffer.Slice(descriptor.Offset, descriptor.Length);

        return descriptor.Type switch
        {
            FieldType.Null => FieldValue.Null(),
            FieldType.Int32 => FieldValue.FromInt32(BitConverter.ToInt32(fieldBuffer)),
            FieldType.Int64 => FieldValue.FromInt64(BitConverter.ToInt64(fieldBuffer)),
            FieldType.Double => FieldValue.FromDouble(BitConverter.ToDouble(fieldBuffer)),
            FieldType.Float => FieldValue.FromFloat(BitConverter.ToSingle(fieldBuffer)),
            FieldType.Boolean => FieldValue.FromBoolean(fieldBuffer[0] != 0),
            FieldType.DateTime => FieldValue.FromDateTime(new DateTime(BitConverter.ToInt64(fieldBuffer))),
            FieldType.Decimal => FieldValue.FromDecimal(ReadDecimal(fieldBuffer)),
            FieldType.Int16 => FieldValue.FromInt16(BitConverter.ToInt16(fieldBuffer)),
            FieldType.Byte => FieldValue.FromByte(fieldBuffer[0]),
            FieldType.Guid => FieldValue.FromGuid(new Guid(fieldBuffer)),
            FieldType.String => FieldValue.FromString(ReadString(fieldBuffer)),
            _ => throw new InvalidOperationException($"Unknown field type: {descriptor.Type}")
        };
    }

    /// <summary>
    /// Gets the field value by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field value.</returns>
    public readonly FieldValue GetValue(string name)
    {
        var index = GetFieldIndex(name);
        if (index < 0)
            throw new ArgumentException($"Field '{name}' not found", nameof(name));
        
        return GetValue(index);
    }

    /// <summary>
    /// Sets the field value at the specified index.
    /// </summary>
    /// <param name="index">The field index.</param>
    /// <param name="value">The field value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(int index, FieldValue value)
    {
        if (index < 0 || index >= _schema.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref readonly var descriptor = ref _schema[index];
        
        if (descriptor.Type != value.Type && value.Type != FieldType.Null)
            throw new InvalidOperationException($"Type mismatch: field is {descriptor.Type}, value is {value.Type}");

        var fieldBuffer = _buffer.Slice(descriptor.Offset, descriptor.Length);

        switch (value.Type)
        {
            case FieldType.Null:
                fieldBuffer.Clear();
                break;
            case FieldType.Int32:
                BitConverter.TryWriteBytes(fieldBuffer, value.AsInt32());
                break;
            case FieldType.Int64:
                BitConverter.TryWriteBytes(fieldBuffer, value.AsInt64());
                break;
            case FieldType.Double:
                BitConverter.TryWriteBytes(fieldBuffer, value.AsDouble());
                break;
            case FieldType.Float:
                BitConverter.TryWriteBytes(fieldBuffer, value.AsFloat());
                break;
            case FieldType.Boolean:
                fieldBuffer[0] = value.AsBoolean() ? (byte)1 : (byte)0;
                break;
            case FieldType.DateTime:
                BitConverter.TryWriteBytes(fieldBuffer, value.AsDateTime().Ticks);
                break;
            case FieldType.Decimal:
                WriteDecimal(fieldBuffer, value.AsDecimal());
                break;
            case FieldType.Int16:
                BitConverter.TryWriteBytes(fieldBuffer, value.AsInt16());
                break;
            case FieldType.Byte:
                fieldBuffer[0] = value.AsByte();
                break;
            case FieldType.Guid:
                value.AsGuid().TryWriteBytes(fieldBuffer);
                break;
            case FieldType.String:
                WriteString(fieldBuffer, value.AsString());
                break;
            default:
                throw new InvalidOperationException($"Unknown field type: {value.Type}");
        }
    }

    /// <summary>
    /// Sets the field value by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="value">The field value to set.</param>
    public void SetValue(string name, FieldValue value)
    {
        var index = GetFieldIndex(name);
        if (index < 0)
            throw new ArgumentException($"Field '{name}' not found", nameof(name));
        
        SetValue(index, value);
    }

    /// <summary>
    /// Clears all field data in the record.
    /// </summary>
    public void Clear()
    {
        _buffer.Clear();
    }

    /// <summary>
    /// Converts the record to a dictionary (legacy compatibility).
    /// Note: This allocates memory and should be avoided in hot paths.
    /// </summary>
    /// <returns>A dictionary containing the field values.</returns>
    public readonly Dictionary<string, object?> ToDictionary()
    {
        var dict = new Dictionary<string, object?>(_schema.Length);
        
        for (int i = 0; i < _schema.Length; i++)
        {
            var descriptor = _schema[i];
            var value = GetValue(i);
            dict[descriptor.Name] = value.ToObject();
        }
        
        return dict;
    }

    /// <summary>
    /// Creates an EtlRecord from a dictionary (legacy compatibility).
    /// Note: This allocates memory and should be avoided in hot paths.
    /// </summary>
    /// <param name="buffer">The buffer to store field data.</param>
    /// <param name="schema">The schema describing field layout.</param>
    /// <param name="dictionary">The dictionary containing field values.</param>
    /// <returns>A new EtlRecord.</returns>
    public static EtlRecord FromDictionary(Span<byte> buffer, ReadOnlySpan<FieldDescriptor> schema, Dictionary<string, object?> dictionary)
    {
        var record = new EtlRecord(buffer, schema);
        
        foreach (var kvp in dictionary)
        {
            var index = record.GetFieldIndex(kvp.Key);
            if (index >= 0)
            {
                var value = ConvertToFieldValue(kvp.Value, schema[index].Type);
                record.SetValue(index, value);
            }
        }
        
        return record;
    }

    /// <summary>
    /// Converts an object to a FieldValue based on the target type.
    /// </summary>
    private static FieldValue ConvertToFieldValue(object? value, FieldType targetType)
    {
        if (value == null)
            return FieldValue.Null();

        return targetType switch
        {
            FieldType.Int32 => FieldValue.FromInt32(Convert.ToInt32(value)),
            FieldType.Int64 => FieldValue.FromInt64(Convert.ToInt64(value)),
            FieldType.Double => FieldValue.FromDouble(Convert.ToDouble(value)),
            FieldType.Float => FieldValue.FromFloat(Convert.ToSingle(value)),
            FieldType.Boolean => FieldValue.FromBoolean(Convert.ToBoolean(value)),
            FieldType.DateTime => FieldValue.FromDateTime(Convert.ToDateTime(value)),
            FieldType.String => FieldValue.FromString(value.ToString()),
            FieldType.Decimal => FieldValue.FromDecimal(Convert.ToDecimal(value)),
            FieldType.Int16 => FieldValue.FromInt16(Convert.ToInt16(value)),
            FieldType.Byte => FieldValue.FromByte(Convert.ToByte(value)),
            FieldType.Guid => FieldValue.FromGuid(value is Guid guid ? guid : Guid.Parse(value.ToString()!)),
            _ => throw new InvalidOperationException($"Cannot convert to {targetType}")
        };
    }

    /// <summary>
    /// Reads a string from a byte buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? ReadString(ReadOnlySpan<byte> buffer)
    {
        // Find the null terminator
        var nullIndex = buffer.IndexOf((byte)0);
        if (nullIndex < 0)
            nullIndex = buffer.Length;

        if (nullIndex == 0)
            return null;

        return Encoding.UTF8.GetString(buffer.Slice(0, nullIndex));
    }

    /// <summary>
    /// Writes a string to a byte buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteString(Span<byte> buffer, string? value)
    {
        buffer.Clear(); // Clear first to ensure null termination
        
        if (string.IsNullOrEmpty(value))
            return;

        var bytesWritten = Encoding.UTF8.GetBytes(value.AsSpan(), buffer);
        
        // Ensure null termination if there's space
        if (bytesWritten < buffer.Length)
            buffer[bytesWritten] = 0;
    }

    /// <summary>
    /// Reads a decimal from a byte buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static decimal ReadDecimal(ReadOnlySpan<byte> buffer)
    {
        var bits = new int[4];
        bits[0] = BitConverter.ToInt32(buffer.Slice(0, 4));
        bits[1] = BitConverter.ToInt32(buffer.Slice(4, 4));
        bits[2] = BitConverter.ToInt32(buffer.Slice(8, 4));
        bits[3] = BitConverter.ToInt32(buffer.Slice(12, 4));
        return new decimal(bits);
    }

    /// <summary>
    /// Writes a decimal to a byte buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteDecimal(Span<byte> buffer, decimal value)
    {
        var bits = decimal.GetBits(value);
        BitConverter.TryWriteBytes(buffer.Slice(0, 4), bits[0]);
        BitConverter.TryWriteBytes(buffer.Slice(4, 4), bits[1]);
        BitConverter.TryWriteBytes(buffer.Slice(8, 4), bits[2]);
        BitConverter.TryWriteBytes(buffer.Slice(12, 4), bits[3]);
    }
}
