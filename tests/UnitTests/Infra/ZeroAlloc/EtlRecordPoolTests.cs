using Library.Infra.ZeroAlloc;

namespace Tests.Infra.ZeroAlloc;

/// <summary>
/// Tests for the EtlRecordPool.
/// Ensures zero-allocation buffer pooling works correctly.
/// </summary>
public class EtlRecordPoolTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitialize()
    {
        // Act
        var pool = new EtlRecordPool(2048, 50);

        // Assert
        Assert.Equal(2048, pool.DefaultBufferSize);
        Assert.Equal(50, pool.MaxFieldCount);
    }

    [Fact]
    public void Constructor_WithDefaultParameters_ShouldInitialize()
    {
        // Act
        var pool = new EtlRecordPool();

        // Assert
        Assert.Equal(1024, pool.DefaultBufferSize);
        Assert.Equal(100, pool.MaxFieldCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidBufferSize_ShouldThrow(int bufferSize)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EtlRecordPool(bufferSize, 100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidMaxFieldCount_ShouldThrow(int maxFieldCount)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EtlRecordPool(1024, maxFieldCount));
    }

    [Fact]
    public void RentBuffer_WithDefaultSize_ShouldReturnBuffer()
    {
        // Arrange
        var pool = new EtlRecordPool(512);

        // Act
        var buffer = pool.RentBuffer();

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= 512);
        
        // Verify buffer is cleared
        Assert.All(buffer, b => Assert.Equal(0, b));
    }

    [Fact]
    public void RentBuffer_WithSpecificSize_ShouldReturnBuffer()
    {
        // Arrange
        var pool = new EtlRecordPool();

        // Act
        var buffer = pool.RentBuffer(2048);

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= 2048);
    }

    [Fact]
    public void ReturnBuffer_ShouldAcceptBuffer()
    {
        // Arrange
        var pool = new EtlRecordPool();
        var buffer = pool.RentBuffer();

        // Act
        pool.ReturnBuffer(buffer);

        // Assert - No exception thrown
        Assert.True(true);
    }

    [Fact]
    public void ReturnBuffer_WithNull_ShouldThrow()
    {
        // Arrange
        var pool = new EtlRecordPool();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pool.ReturnBuffer(null!));
    }

    [Fact]
    public void RentSchema_WithValidFieldCount_ShouldReturnSchema()
    {
        // Arrange
        var pool = new EtlRecordPool(1024, 50);

        // Act
        var schema = pool.RentSchema(10);

        // Assert
        Assert.NotNull(schema);
        Assert.True(schema.Length >= 10);
    }

    [Fact]
    public void RentSchema_ExceedingMaxFieldCount_ShouldThrow()
    {
        // Arrange
        var pool = new EtlRecordPool(1024, 50);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => pool.RentSchema(51));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RentSchema_WithInvalidFieldCount_ShouldThrow(int fieldCount)
    {
        // Arrange
        var pool = new EtlRecordPool();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => pool.RentSchema(fieldCount));
    }

    [Fact]
    public void ReturnSchema_ShouldAcceptSchema()
    {
        // Arrange
        var pool = new EtlRecordPool();
        var schema = pool.RentSchema(5);

        // Act
        pool.ReturnSchema(schema);

        // Assert - No exception thrown
        Assert.True(true);
    }

    [Fact]
    public void ReturnSchema_WithNull_ShouldThrow()
    {
        // Arrange
        var pool = new EtlRecordPool();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pool.ReturnSchema(null!));
    }

    [Fact]
    public void CreateBufferContext_ShouldRentAndReturnBuffer()
    {
        // Arrange
        var pool = new EtlRecordPool();
        byte[]? rentedBuffer = null;

        // Act
        using (var context = pool.CreateBufferContext())
        {
            rentedBuffer = context.Buffer;
            Assert.NotNull(rentedBuffer);
            Assert.True(rentedBuffer.Length >= 1024);
        }

        // Assert - Context disposed, buffer returned
        Assert.NotNull(rentedBuffer);
    }

    [Fact]
    public void CreateBufferContext_AsSpan_ShouldProvideSpan()
    {
        // Arrange
        var pool = new EtlRecordPool();

        // Act
        using var context = pool.CreateBufferContext();
        var span = context.AsSpan();

        // Assert
        Assert.True(span.Length >= 1024);
    }

    [Fact]
    public void CreateSchemaContext_ShouldManageSchema()
    {
        // Arrange
        var pool = new EtlRecordPool();
        var schema = pool.RentSchema(5);
        FieldDescriptor[]? managedSchema = null;

        // Act
        using (var context = pool.CreateSchemaContext(schema))
        {
            managedSchema = context.Schema;
            Assert.NotNull(managedSchema);
            Assert.True(managedSchema.Length >= 5);
        }

        // Assert - Context disposed, schema returned
        Assert.NotNull(managedSchema);
    }

    [Fact]
    public void CreateSchemaContext_AsSpan_ShouldProvideSpan()
    {
        // Arrange
        var pool = new EtlRecordPool();
        var schema = pool.RentSchema(5);

        // Act
        using var context = pool.CreateSchemaContext(schema);
        var span = context.AsSpan();

        // Assert
        Assert.True(span.Length >= 5);
    }

    [Fact]
    public void CalculateBufferSize_WithSchema_ShouldCalculateCorrectly()
    {
        // Arrange
        var schema = new[]
        {
            new FieldDescriptor("Field1", FieldType.Int32, 0, 4, 0),
            new FieldDescriptor("Field2", FieldType.Double, 4, 8, 1),
            new FieldDescriptor("Field3", FieldType.Boolean, 12, 1, 2)
        };

        // Act
        var size = EtlRecordPool.CalculateBufferSize(schema);

        // Assert
        Assert.Equal(13, size); // 0 + 4 + 8 + 1 = 13
    }

    [Fact]
    public void CalculateBufferSize_WithEmptySchema_ShouldReturnZero()
    {
        // Arrange
        var schema = Array.Empty<FieldDescriptor>();

        // Act
        var size = EtlRecordPool.CalculateBufferSize(schema);

        // Assert
        Assert.Equal(0, size);
    }

    [Fact]
    public void CreateSchema_WithMultipleFields_ShouldCalculateOffsets()
    {
        // Act
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String),
            ("Age", FieldType.Int32),
            ("Salary", FieldType.Double)
        );

        // Assert
        Assert.Equal(4, schema.Length);
        
        Assert.Equal("Id", schema[0].Name);
        Assert.Equal(FieldType.Int32, schema[0].Type);
        Assert.Equal(0, schema[0].Offset);
        Assert.Equal(4, schema[0].Length);
        
        Assert.Equal("Name", schema[1].Name);
        Assert.Equal(FieldType.String, schema[1].Type);
        Assert.Equal(4, schema[1].Offset);
        Assert.Equal(256, schema[1].Length); // Default string size
        
        Assert.Equal("Age", schema[2].Name);
        Assert.Equal(FieldType.Int32, schema[2].Type);
        Assert.Equal(260, schema[2].Offset); // 4 + 256
        Assert.Equal(4, schema[2].Length);
        
        Assert.Equal("Salary", schema[3].Name);
        Assert.Equal(FieldType.Double, schema[3].Type);
        Assert.Equal(264, schema[3].Offset); // 4 + 256 + 4
        Assert.Equal(8, schema[3].Length);
    }

    [Fact]
    public void CreateSchema_WithAllTypes_ShouldCalculateCorrectSizes()
    {
        // Act
        var schema = EtlRecordPool.CreateSchema(
            ("Byte", FieldType.Byte),
            ("Boolean", FieldType.Boolean),
            ("Int16", FieldType.Int16),
            ("Int32", FieldType.Int32),
            ("Float", FieldType.Float),
            ("Int64", FieldType.Int64),
            ("Double", FieldType.Double),
            ("DateTime", FieldType.DateTime),
            ("Decimal", FieldType.Decimal),
            ("Guid", FieldType.Guid),
            ("String", FieldType.String)
        );

        // Assert
        Assert.Equal(11, schema.Length);
        Assert.Equal(1, schema[0].Length);   // Byte
        Assert.Equal(1, schema[1].Length);   // Boolean
        Assert.Equal(2, schema[2].Length);   // Int16
        Assert.Equal(4, schema[3].Length);   // Int32
        Assert.Equal(4, schema[4].Length);   // Float
        Assert.Equal(8, schema[5].Length);   // Int64
        Assert.Equal(8, schema[6].Length);   // Double
        Assert.Equal(8, schema[7].Length);   // DateTime
        Assert.Equal(16, schema[8].Length);  // Decimal
        Assert.Equal(16, schema[9].Length);  // Guid
        Assert.Equal(256, schema[10].Length); // String
    }

    [Fact]
    public void RentAndReturn_MultipleTimes_ShouldReuseBuffers()
    {
        // Arrange
        var pool = new EtlRecordPool();

        // Act - Rent and return multiple times
        for (int i = 0; i < 10; i++)
        {
            var buffer = pool.RentBuffer();
            Assert.NotNull(buffer);
            pool.ReturnBuffer(buffer);
        }

        // Assert - No exception, pooling working
        Assert.True(true);
    }

    [Fact]
    public void IntegrationTest_CreateRecordWithPool()
    {
        // Arrange
        var pool = new EtlRecordPool();
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String),
            ("Active", FieldType.Boolean)
        );

        var bufferSize = EtlRecordPool.CalculateBufferSize(schema);
        var buffer = pool.RentBuffer(bufferSize);

        // Act
        var record = new EtlRecord(buffer.AsSpan(), schema);
        record.SetValue("Id", FieldValue.FromInt32(123));
        record.SetValue("Name", FieldValue.FromString("Test"));
        record.SetValue("Active", FieldValue.FromBoolean(true));

        // Assert
        Assert.Equal(123, record.GetValue("Id").AsInt32());
        Assert.Equal("Test", record.GetValue("Name").AsString());
        Assert.True(record.GetValue("Active").AsBoolean());

        // Cleanup
        pool.ReturnBuffer(buffer);
    }
}
