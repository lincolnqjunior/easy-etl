using System.Buffers;

namespace Library.Infra.ZeroAlloc;

/// <summary>
/// Pool for managing EtlRecord buffers with zero allocation.
/// Uses ArrayPool to rent and return buffers, avoiding heap allocations.
/// </summary>
public sealed class EtlRecordPool
{
    private readonly ArrayPool<byte> _bufferPool;
    private readonly ArrayPool<FieldDescriptor> _schemaPool;
    private readonly int _defaultBufferSize;
    private readonly int _maxFieldCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="EtlRecordPool"/> class.
    /// </summary>
    /// <param name="defaultBufferSize">Default buffer size in bytes for each record.</param>
    /// <param name="maxFieldCount">Maximum number of fields per record.</param>
    public EtlRecordPool(int defaultBufferSize = 1024, int maxFieldCount = 100)
    {
        if (defaultBufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(defaultBufferSize), "Buffer size must be positive");
        
        if (maxFieldCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxFieldCount), "Max field count must be positive");

        _defaultBufferSize = defaultBufferSize;
        _maxFieldCount = maxFieldCount;
        _bufferPool = ArrayPool<byte>.Shared;
        _schemaPool = ArrayPool<FieldDescriptor>.Shared;
    }

    /// <summary>
    /// Gets the default buffer size.
    /// </summary>
    public int DefaultBufferSize => _defaultBufferSize;

    /// <summary>
    /// Gets the maximum field count.
    /// </summary>
    public int MaxFieldCount => _maxFieldCount;

    /// <summary>
    /// Rents a buffer from the pool.
    /// </summary>
    /// <param name="minimumSize">Minimum buffer size in bytes.</param>
    /// <returns>A rented byte array.</returns>
    public byte[] RentBuffer(int minimumSize = 0)
    {
        var size = minimumSize > 0 ? minimumSize : _defaultBufferSize;
        var buffer = _bufferPool.Rent(size);
        
        // Clear the buffer to ensure clean state
        Array.Clear(buffer, 0, buffer.Length);
        
        return buffer;
    }

    /// <summary>
    /// Returns a buffer to the pool.
    /// </summary>
    /// <param name="buffer">The buffer to return.</param>
    /// <param name="clearArray">Whether to clear the buffer before returning.</param>
    public void ReturnBuffer(byte[] buffer, bool clearArray = true)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        _bufferPool.Return(buffer, clearArray);
    }

    /// <summary>
    /// Rents a schema array from the pool.
    /// </summary>
    /// <param name="fieldCount">Number of fields in the schema.</param>
    /// <returns>A rented FieldDescriptor array.</returns>
    public FieldDescriptor[] RentSchema(int fieldCount)
    {
        if (fieldCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(fieldCount), "Field count must be positive");
        
        if (fieldCount > _maxFieldCount)
            throw new ArgumentOutOfRangeException(nameof(fieldCount), $"Field count exceeds maximum of {_maxFieldCount}");

        return _schemaPool.Rent(fieldCount);
    }

    /// <summary>
    /// Returns a schema array to the pool.
    /// </summary>
    /// <param name="schema">The schema array to return.</param>
    /// <param name="clearArray">Whether to clear the array before returning.</param>
    public void ReturnSchema(FieldDescriptor[] schema, bool clearArray = false)
    {
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));

        _schemaPool.Return(schema, clearArray);
    }

    /// <summary>
    /// Creates a buffer context that automatically returns the buffer when disposed.
    /// </summary>
    /// <param name="minimumSize">Minimum buffer size in bytes.</param>
    /// <returns>A buffer context.</returns>
    public BufferContext CreateBufferContext(int minimumSize = 0)
    {
        return new BufferContext(this, minimumSize);
    }

    /// <summary>
    /// Creates a schema context that automatically returns the schema when disposed.
    /// </summary>
    /// <param name="schema">The schema array.</param>
    /// <returns>A schema context.</returns>
    public SchemaContext CreateSchemaContext(FieldDescriptor[] schema)
    {
        return new SchemaContext(this, schema);
    }

    /// <summary>
    /// Calculates the required buffer size for a given schema.
    /// </summary>
    /// <param name="schema">The schema to calculate buffer size for.</param>
    /// <returns>The required buffer size in bytes.</returns>
    public static int CalculateBufferSize(ReadOnlySpan<FieldDescriptor> schema)
    {
        if (schema.Length == 0)
            return 0;

        var maxOffset = 0;
        var maxLength = 0;

        for (int i = 0; i < schema.Length; i++)
        {
            var end = schema[i].Offset + schema[i].Length;
            if (end > maxOffset + maxLength)
            {
                maxOffset = schema[i].Offset;
                maxLength = schema[i].Length;
            }
        }

        return maxOffset + maxLength;
    }

    /// <summary>
    /// Creates a schema for common data types with automatic layout.
    /// </summary>
    /// <param name="fields">Field definitions (name, type).</param>
    /// <returns>An array of field descriptors with calculated offsets.</returns>
    public static FieldDescriptor[] CreateSchema(params (string Name, FieldType Type)[] fields)
    {
        var schema = new FieldDescriptor[fields.Length];
        var offset = 0;

        for (int i = 0; i < fields.Length; i++)
        {
            var (name, type) = fields[i];
            var length = GetTypeSize(type);
            
            schema[i] = new FieldDescriptor(name, type, offset, length, i);
            offset += length;
        }

        return schema;
    }

    /// <summary>
    /// Gets the size in bytes for a field type.
    /// </summary>
    private static int GetTypeSize(FieldType type)
    {
        return type switch
        {
            FieldType.Null => 0,
            FieldType.Byte => 1,
            FieldType.Boolean => 1,
            FieldType.Int16 => 2,
            FieldType.Int32 => 4,
            FieldType.Float => 4,
            FieldType.Int64 => 8,
            FieldType.Double => 8,
            FieldType.DateTime => 8,
            FieldType.Decimal => 16,
            FieldType.Guid => 16,
            FieldType.String => 256, // Default string buffer size
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown type: {type}")
        };
    }

    /// <summary>
    /// Context for automatic buffer management.
    /// </summary>
    public readonly struct BufferContext : IDisposable
    {
        private readonly EtlRecordPool _pool;
        private readonly byte[] _buffer;

        internal BufferContext(EtlRecordPool pool, int minimumSize)
        {
            _pool = pool;
            _buffer = pool.RentBuffer(minimumSize);
        }

        /// <summary>
        /// Gets the rented buffer.
        /// </summary>
        public byte[] Buffer => _buffer;

        /// <summary>
        /// Gets a span over the buffer.
        /// </summary>
        public Span<byte> AsSpan() => _buffer.AsSpan();

        /// <summary>
        /// Disposes the context and returns the buffer to the pool.
        /// </summary>
        public void Dispose()
        {
            _pool.ReturnBuffer(_buffer);
        }
    }

    /// <summary>
    /// Context for automatic schema management.
    /// </summary>
    public readonly struct SchemaContext : IDisposable
    {
        private readonly EtlRecordPool _pool;
        private readonly FieldDescriptor[] _schema;

        internal SchemaContext(EtlRecordPool pool, FieldDescriptor[] schema)
        {
            _pool = pool;
            _schema = schema;
        }

        /// <summary>
        /// Gets the schema array.
        /// </summary>
        public FieldDescriptor[] Schema => _schema;

        /// <summary>
        /// Gets a read-only span over the schema.
        /// </summary>
        public ReadOnlySpan<FieldDescriptor> AsSpan() => _schema.AsSpan();

        /// <summary>
        /// Disposes the context and returns the schema to the pool.
        /// </summary>
        public void Dispose()
        {
            _pool.ReturnSchema(_schema);
        }
    }
}
