namespace Library.Infra.ZeroAlloc;

/// <summary>
/// Describes the location and type of a field within an EtlRecord's buffer.
/// This struct enables zero-allocation field access by storing metadata about field layout.
/// </summary>
public struct FieldDescriptor
{
    /// <summary>
    /// Gets or sets the name of the field.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets or sets the offset in bytes from the start of the buffer where this field's data begins.
    /// </summary>
    public int Offset { get; set; }
    
    /// <summary>
    /// Gets or sets the length in bytes of this field's data in the buffer.
    /// For variable-length types like strings, this represents the maximum length.
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    /// Gets or sets the data type of this field.
    /// </summary>
    public FieldType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the index of this field in the schema.
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldDescriptor"/> struct.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="type">The field type.</param>
    /// <param name="offset">The offset in the buffer.</param>
    /// <param name="length">The length in the buffer.</param>
    /// <param name="index">The field index.</param>
    public FieldDescriptor(string name, FieldType type, int offset, int length, int index)
    {
        Name = name;
        Type = type;
        Offset = offset;
        Length = length;
        Index = index;
    }
}
