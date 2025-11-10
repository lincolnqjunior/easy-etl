namespace Library.Infra.ZeroAlloc;

/// <summary>
/// Represents the data type of a field in an EtlRecord.
/// Used to avoid boxing/unboxing of value types.
/// </summary>
public enum FieldType : byte
{
    /// <summary>Null value</summary>
    Null = 0,
    
    /// <summary>32-bit integer</summary>
    Int32 = 1,
    
    /// <summary>64-bit integer</summary>
    Int64 = 2,
    
    /// <summary>Double-precision floating point</summary>
    Double = 3,
    
    /// <summary>Single-precision floating point</summary>
    Float = 4,
    
    /// <summary>Boolean value</summary>
    Boolean = 5,
    
    /// <summary>DateTime value</summary>
    DateTime = 6,
    
    /// <summary>String value (stored separately in string pool)</summary>
    String = 7,
    
    /// <summary>Decimal value</summary>
    Decimal = 8,
    
    /// <summary>16-bit integer</summary>
    Int16 = 9,
    
    /// <summary>8-bit integer</summary>
    Byte = 10,
    
    /// <summary>GUID value</summary>
    Guid = 11
}
