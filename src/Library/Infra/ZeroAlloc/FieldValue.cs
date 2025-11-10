using System.Runtime.InteropServices;

namespace Library.Infra.ZeroAlloc;

/// <summary>
/// A union type that can hold any common data type without boxing.
/// Uses explicit layout to share memory between different type representations.
/// This avoids heap allocations when working with value types.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct FieldValue
{
    /// <summary>
    /// The type of data currently stored in this union.
    /// </summary>
    [FieldOffset(0)]
    private FieldType _type;
    
    /// <summary>
    /// Int32 value storage
    /// </summary>
    [FieldOffset(8)]
    private int _int32Value;
    
    /// <summary>
    /// Int64 value storage
    /// </summary>
    [FieldOffset(8)]
    private long _int64Value;
    
    /// <summary>
    /// Double value storage
    /// </summary>
    [FieldOffset(8)]
    private double _doubleValue;
    
    /// <summary>
    /// Float value storage
    /// </summary>
    [FieldOffset(8)]
    private float _floatValue;
    
    /// <summary>
    /// Boolean value storage
    /// </summary>
    [FieldOffset(8)]
    private bool _boolValue;
    
    /// <summary>
    /// DateTime value storage (stored as ticks)
    /// </summary>
    [FieldOffset(8)]
    private long _dateTimeTicks;
    
    /// <summary>
    /// Decimal value storage (16 bytes)
    /// </summary>
    [FieldOffset(8)]
    private decimal _decimalValue;
    
    /// <summary>
    /// Int16 value storage
    /// </summary>
    [FieldOffset(8)]
    private short _int16Value;
    
    /// <summary>
    /// Byte value storage
    /// </summary>
    [FieldOffset(8)]
    private byte _byteValue;
    
    /// <summary>
    /// Guid value storage (16 bytes)
    /// </summary>
    [FieldOffset(8)]
    private Guid _guidValue;
    
    /// <summary>
    /// String reference (stored separately, not zero-alloc)
    /// </summary>
    [FieldOffset(24)]
    private string? _stringValue;
    
    /// <summary>
    /// Gets the type of the current value.
    /// </summary>
    public readonly FieldType Type => _type;
    
    /// <summary>
    /// Gets a value indicating whether this field is null.
    /// </summary>
    public readonly bool IsNull => _type == FieldType.Null;
    
    // Constructors for each type
    
    /// <summary>
    /// Creates a null FieldValue.
    /// </summary>
    public static FieldValue Null() => new() { _type = FieldType.Null };
    
    /// <summary>
    /// Creates a FieldValue containing an Int32.
    /// </summary>
    public static FieldValue FromInt32(int value) => 
        new() { _type = FieldType.Int32, _int32Value = value };
    
    /// <summary>
    /// Creates a FieldValue containing an Int64.
    /// </summary>
    public static FieldValue FromInt64(long value) => 
        new() { _type = FieldType.Int64, _int64Value = value };
    
    /// <summary>
    /// Creates a FieldValue containing a Double.
    /// </summary>
    public static FieldValue FromDouble(double value) => 
        new() { _type = FieldType.Double, _doubleValue = value };
    
    /// <summary>
    /// Creates a FieldValue containing a Float.
    /// </summary>
    public static FieldValue FromFloat(float value) => 
        new() { _type = FieldType.Float, _floatValue = value };
    
    /// <summary>
    /// Creates a FieldValue containing a Boolean.
    /// </summary>
    public static FieldValue FromBoolean(bool value) => 
        new() { _type = FieldType.Boolean, _boolValue = value };
    
    /// <summary>
    /// Creates a FieldValue containing a DateTime.
    /// </summary>
    public static FieldValue FromDateTime(DateTime value) => 
        new() { _type = FieldType.DateTime, _dateTimeTicks = value.Ticks };
    
    /// <summary>
    /// Creates a FieldValue containing a String.
    /// Note: This is not zero-alloc as strings are reference types.
    /// </summary>
    public static FieldValue FromString(string? value) => 
        new() { _type = FieldType.String, _stringValue = value };
    
    /// <summary>
    /// Creates a FieldValue containing a Decimal.
    /// </summary>
    public static FieldValue FromDecimal(decimal value) => 
        new() { _type = FieldType.Decimal, _decimalValue = value };
    
    /// <summary>
    /// Creates a FieldValue containing an Int16.
    /// </summary>
    public static FieldValue FromInt16(short value) => 
        new() { _type = FieldType.Int16, _int16Value = value };
    
    /// <summary>
    /// Creates a FieldValue containing a Byte.
    /// </summary>
    public static FieldValue FromByte(byte value) => 
        new() { _type = FieldType.Byte, _byteValue = value };
    
    /// <summary>
    /// Creates a FieldValue containing a Guid.
    /// </summary>
    public static FieldValue FromGuid(Guid value) => 
        new() { _type = FieldType.Guid, _guidValue = value };
    
    // Getters for each type
    
    /// <summary>
    /// Gets the Int32 value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not Int32.</exception>
    public readonly int AsInt32()
    {
        if (_type != FieldType.Int32)
            throw new InvalidOperationException($"Cannot get Int32 from FieldType.{_type}");
        return _int32Value;
    }
    
    /// <summary>
    /// Gets the Int64 value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not Int64.</exception>
    public readonly long AsInt64()
    {
        if (_type != FieldType.Int64)
            throw new InvalidOperationException($"Cannot get Int64 from FieldType.{_type}");
        return _int64Value;
    }
    
    /// <summary>
    /// Gets the Double value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not Double.</exception>
    public readonly double AsDouble()
    {
        if (_type != FieldType.Double)
            throw new InvalidOperationException($"Cannot get Double from FieldType.{_type}");
        return _doubleValue;
    }
    
    /// <summary>
    /// Gets the Float value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not Float.</exception>
    public readonly float AsFloat()
    {
        if (_type != FieldType.Float)
            throw new InvalidOperationException($"Cannot get Float from FieldType.{_type}");
        return _floatValue;
    }
    
    /// <summary>
    /// Gets the Boolean value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not Boolean.</exception>
    public readonly bool AsBoolean()
    {
        if (_type != FieldType.Boolean)
            throw new InvalidOperationException($"Cannot get Boolean from FieldType.{_type}");
        return _boolValue;
    }
    
    /// <summary>
    /// Gets the DateTime value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not DateTime.</exception>
    public readonly DateTime AsDateTime()
    {
        if (_type != FieldType.DateTime)
            throw new InvalidOperationException($"Cannot get DateTime from FieldType.{_type}");
        return new DateTime(_dateTimeTicks);
    }
    
    /// <summary>
    /// Gets the String value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not String.</exception>
    public readonly string? AsString()
    {
        if (_type != FieldType.String)
            throw new InvalidOperationException($"Cannot get String from FieldType.{_type}");
        return _stringValue;
    }
    
    /// <summary>
    /// Gets the Decimal value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not Decimal.</exception>
    public readonly decimal AsDecimal()
    {
        if (_type != FieldType.Decimal)
            throw new InvalidOperationException($"Cannot get Decimal from FieldType.{_type}");
        return _decimalValue;
    }
    
    /// <summary>
    /// Gets the Int16 value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not Int16.</exception>
    public readonly short AsInt16()
    {
        if (_type != FieldType.Int16)
            throw new InvalidOperationException($"Cannot get Int16 from FieldType.{_type}");
        return _int16Value;
    }
    
    /// <summary>
    /// Gets the Byte value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not Byte.</exception>
    public readonly byte AsByte()
    {
        if (_type != FieldType.Byte)
            throw new InvalidOperationException($"Cannot get Byte from FieldType.{_type}");
        return _byteValue;
    }
    
    /// <summary>
    /// Gets the Guid value.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the type is not Guid.</exception>
    public readonly Guid AsGuid()
    {
        if (_type != FieldType.Guid)
            throw new InvalidOperationException($"Cannot get Guid from FieldType.{_type}");
        return _guidValue;
    }
    
    /// <summary>
    /// Converts the value to an object (may cause boxing for value types).
    /// Use this sparingly, prefer typed accessors.
    /// </summary>
    public readonly object? ToObject()
    {
        return _type switch
        {
            FieldType.Null => null,
            FieldType.Int32 => _int32Value,
            FieldType.Int64 => _int64Value,
            FieldType.Double => _doubleValue,
            FieldType.Float => _floatValue,
            FieldType.Boolean => _boolValue,
            FieldType.DateTime => new DateTime(_dateTimeTicks),
            FieldType.String => _stringValue,
            FieldType.Decimal => _decimalValue,
            FieldType.Int16 => _int16Value,
            FieldType.Byte => _byteValue,
            FieldType.Guid => _guidValue,
            _ => throw new InvalidOperationException($"Unknown FieldType: {_type}")
        };
    }
}
