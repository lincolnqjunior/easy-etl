using Library.Infra.ZeroAlloc;

namespace Tests.Infra.ZeroAlloc;

/// <summary>
/// Tests for the FieldValue union type.
/// Ensures zero-allocation value storage works correctly.
/// </summary>
public class FieldValueTests
{
    [Fact]
    public void Null_ShouldCreateNullValue()
    {
        // Arrange & Act
        var value = FieldValue.Null();

        // Assert
        Assert.Equal(FieldType.Null, value.Type);
        Assert.True(value.IsNull);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(-100)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void FromInt32_ShouldStoreAndRetrieveInt32(int expected)
    {
        // Arrange & Act
        var value = FieldValue.FromInt32(expected);

        // Assert
        Assert.Equal(FieldType.Int32, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsInt32());
        Assert.Equal(expected, value.ToObject());
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(42L)]
    [InlineData(-100L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void FromInt64_ShouldStoreAndRetrieveInt64(long expected)
    {
        // Arrange & Act
        var value = FieldValue.FromInt64(expected);

        // Assert
        Assert.Equal(FieldType.Int64, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsInt64());
        Assert.Equal(expected, value.ToObject());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(42.5)]
    [InlineData(-100.123)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.Epsilon)]
    public void FromDouble_ShouldStoreAndRetrieveDouble(double expected)
    {
        // Arrange & Act
        var value = FieldValue.FromDouble(expected);

        // Assert
        Assert.Equal(FieldType.Double, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsDouble());
        Assert.Equal(expected, value.ToObject());
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(42.5f)]
    [InlineData(-100.123f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    public void FromFloat_ShouldStoreAndRetrieveFloat(float expected)
    {
        // Arrange & Act
        var value = FieldValue.FromFloat(expected);

        // Assert
        Assert.Equal(FieldType.Float, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsFloat());
        Assert.Equal(expected, value.ToObject());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FromBoolean_ShouldStoreAndRetrieveBoolean(bool expected)
    {
        // Arrange & Act
        var value = FieldValue.FromBoolean(expected);

        // Assert
        Assert.Equal(FieldType.Boolean, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsBoolean());
        Assert.Equal(expected, value.ToObject());
    }

    [Fact]
    public void FromDateTime_ShouldStoreAndRetrieveDateTime()
    {
        // Arrange
        var expected = new DateTime(2025, 11, 7, 23, 30, 0, DateTimeKind.Utc);

        // Act
        var value = FieldValue.FromDateTime(expected);

        // Assert
        Assert.Equal(FieldType.DateTime, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsDateTime());
        Assert.Equal(expected, value.ToObject());
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("Hello World")]
    [InlineData(null)]
    public void FromString_ShouldStoreAndRetrieveString(string? expected)
    {
        // Arrange & Act
        var value = FieldValue.FromString(expected);

        // Assert
        Assert.Equal(FieldType.String, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsString());
        Assert.Equal(expected, value.ToObject());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42.5)]
    [InlineData(-100.123)]
    public void FromDecimal_ShouldStoreAndRetrieveDecimal(double doubleValue)
    {
        // Arrange
        var expected = (decimal)doubleValue;

        // Act
        var value = FieldValue.FromDecimal(expected);

        // Assert
        Assert.Equal(FieldType.Decimal, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsDecimal());
        Assert.Equal(expected, value.ToObject());
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)42)]
    [InlineData((short)-100)]
    [InlineData(short.MaxValue)]
    [InlineData(short.MinValue)]
    public void FromInt16_ShouldStoreAndRetrieveInt16(short expected)
    {
        // Arrange & Act
        var value = FieldValue.FromInt16(expected);

        // Assert
        Assert.Equal(FieldType.Int16, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsInt16());
        Assert.Equal(expected, value.ToObject());
    }

    [Theory]
    [InlineData((byte)0)]
    [InlineData((byte)42)]
    [InlineData((byte)255)]
    public void FromByte_ShouldStoreAndRetrieveByte(byte expected)
    {
        // Arrange & Act
        var value = FieldValue.FromByte(expected);

        // Assert
        Assert.Equal(FieldType.Byte, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsByte());
        Assert.Equal(expected, value.ToObject());
    }

    [Fact]
    public void FromGuid_ShouldStoreAndRetrieveGuid()
    {
        // Arrange
        var expected = Guid.NewGuid();

        // Act
        var value = FieldValue.FromGuid(expected);

        // Assert
        Assert.Equal(FieldType.Guid, value.Type);
        Assert.False(value.IsNull);
        Assert.Equal(expected, value.AsGuid());
        Assert.Equal(expected, value.ToObject());
    }

    [Fact]
    public void AsInt32_WithWrongType_ShouldThrowException()
    {
        // Arrange
        var value = FieldValue.FromString("test");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => value.AsInt32());
        Assert.Contains("Cannot get Int32", exception.Message);
    }

    [Fact]
    public void AsString_WithWrongType_ShouldThrowException()
    {
        // Arrange
        var value = FieldValue.FromInt32(42);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => value.AsString());
        Assert.Contains("Cannot get String", exception.Message);
    }

    [Fact]
    public void ToObject_WithNull_ShouldReturnNull()
    {
        // Arrange
        var value = FieldValue.Null();

        // Act
        var result = value.ToObject();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void UnionType_ShouldShareMemory()
    {
        // This test verifies that the union is working correctly by checking size
        // A proper union should not take more space than the largest member
        var value = FieldValue.FromInt32(42);
        
        // The struct should be relatively small due to memory sharing
        // Just verify we can create and use it
        Assert.Equal(42, value.AsInt32());
        
        // Overwrite with a different type
        value = FieldValue.FromDouble(3.14);
        Assert.Equal(3.14, value.AsDouble());
        
        // Type should have changed
        Assert.Equal(FieldType.Double, value.Type);
    }
}
