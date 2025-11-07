using Library.Infra.ZeroAlloc;

namespace Tests.Infra.ZeroAlloc;

/// <summary>
/// Tests for the EtlRecord ref struct.
/// Ensures zero-allocation record operations work correctly.
/// </summary>
public class EtlRecordTests
{
    private static FieldDescriptor[] CreateSimpleSchema()
    {
        return new[]
        {
            new FieldDescriptor("Id", FieldType.Int32, 0, 4, 0),
            new FieldDescriptor("Name", FieldType.String, 4, 100, 1),
            new FieldDescriptor("Age", FieldType.Int32, 104, 4, 2),
            new FieldDescriptor("Salary", FieldType.Double, 108, 8, 3),
            new FieldDescriptor("IsActive", FieldType.Boolean, 116, 1, 4)
        };
    }

    [Fact]
    public void Constructor_ShouldInitializeRecord()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();

        // Act
        var record = new EtlRecord(buffer, schema);

        // Assert
        Assert.True(record.IsValid);
        Assert.Equal(5, record.FieldCount);
    }

    [Fact]
    public void GetFieldDescriptor_ShouldReturnCorrectDescriptor()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);

        // Act
        ref readonly var descriptor = ref record.GetFieldDescriptor(1);

        // Assert
        Assert.Equal("Name", descriptor.Name);
        Assert.Equal(FieldType.String, descriptor.Type);
        Assert.Equal(4, descriptor.Offset);
        Assert.Equal(100, descriptor.Length);
    }

    [Fact]
    public void GetFieldDescriptor_WithInvalidIndex_ShouldThrow()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);

        // Act & Assert - Test negative index
        ArgumentOutOfRangeException? exception1 = null;
        try
        {
            record.GetFieldDescriptor(-1);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            exception1 = ex;
        }
        Assert.NotNull(exception1);

        // Act & Assert - Test too large index
        ArgumentOutOfRangeException? exception2 = null;
        try
        {
            record.GetFieldDescriptor(10);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            exception2 = ex;
        }
        Assert.NotNull(exception2);
    }

    [Fact]
    public void GetFieldDescriptorByName_ShouldReturnCorrectDescriptor()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);

        // Act
        var descriptor = record.GetFieldDescriptorByName("Age");

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal("Age", descriptor.Value.Name);
        Assert.Equal(FieldType.Int32, descriptor.Value.Type);
    }

    [Fact]
    public void GetFieldDescriptorByName_WithInvalidName_ShouldReturnNull()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);

        // Act
        var descriptor = record.GetFieldDescriptorByName("NonExistent");

        // Assert
        Assert.Null(descriptor);
    }

    [Fact]
    public void GetFieldIndex_ShouldReturnCorrectIndex()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);

        // Act
        var index = record.GetFieldIndex("Salary");

        // Assert
        Assert.Equal(3, index);
    }

    [Fact]
    public void GetFieldIndex_WithInvalidName_ShouldReturnMinusOne()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);

        // Act
        var index = record.GetFieldIndex("NonExistent");

        // Assert
        Assert.Equal(-1, index);
    }

    [Fact]
    public void SetValue_AndGetValue_Int32_ShouldRoundTrip()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);
        var expectedValue = FieldValue.FromInt32(42);

        // Act
        record.SetValue(0, expectedValue);
        var actualValue = record.GetValue(0);

        // Assert
        Assert.Equal(FieldType.Int32, actualValue.Type);
        Assert.Equal(42, actualValue.AsInt32());
    }

    [Fact]
    public void SetValue_AndGetValue_String_ShouldRoundTrip()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);
        var expectedValue = FieldValue.FromString("John Doe");

        // Act
        record.SetValue(1, expectedValue);
        var actualValue = record.GetValue(1);

        // Assert
        Assert.Equal(FieldType.String, actualValue.Type);
        Assert.Equal("John Doe", actualValue.AsString());
    }

    [Fact]
    public void SetValue_AndGetValue_Double_ShouldRoundTrip()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);
        var expectedValue = FieldValue.FromDouble(75000.50);

        // Act
        record.SetValue(3, expectedValue);
        var actualValue = record.GetValue(3);

        // Assert
        Assert.Equal(FieldType.Double, actualValue.Type);
        Assert.Equal(75000.50, actualValue.AsDouble());
    }

    [Fact]
    public void SetValue_AndGetValue_Boolean_ShouldRoundTrip()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);
        var expectedValue = FieldValue.FromBoolean(true);

        // Act
        record.SetValue(4, expectedValue);
        var actualValue = record.GetValue(4);

        // Assert
        Assert.Equal(FieldType.Boolean, actualValue.Type);
        Assert.True(actualValue.AsBoolean());
    }

    [Fact]
    public void SetValue_ByName_AndGetValue_ByName_ShouldRoundTrip()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);
        var expectedValue = FieldValue.FromInt32(30);

        // Act
        record.SetValue("Age", expectedValue);
        var actualValue = record.GetValue("Age");

        // Assert
        Assert.Equal(FieldType.Int32, actualValue.Type);
        Assert.Equal(30, actualValue.AsInt32());
    }

    [Fact]
    public void SetValue_WithTypeMismatch_ShouldThrow()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);
        var wrongTypeValue = FieldValue.FromString("wrong");

        // Act
        InvalidOperationException? exception = null;
        try
        {
            record.SetValue(0, wrongTypeValue);
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        // Assert
        Assert.NotNull(exception);
        Assert.Contains("Type mismatch", exception.Message);
    }

    [Fact]
    public void Clear_ShouldClearAllFields()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);
        
        record.SetValue(0, FieldValue.FromInt32(42));
        record.SetValue(2, FieldValue.FromInt32(30));

        // Act
        record.Clear();

        // Assert
        var value0 = record.GetValue(0);
        var value2 = record.GetValue(2);
        Assert.Equal(0, value0.AsInt32());
        Assert.Equal(0, value2.AsInt32());
    }

    [Fact]
    public void ToDictionary_ShouldConvertToLegacyFormat()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);
        
        record.SetValue("Id", FieldValue.FromInt32(1));
        record.SetValue("Name", FieldValue.FromString("Alice"));
        record.SetValue("Age", FieldValue.FromInt32(25));
        record.SetValue("Salary", FieldValue.FromDouble(60000.0));
        record.SetValue("IsActive", FieldValue.FromBoolean(true));

        // Act
        var dict = record.ToDictionary();

        // Assert
        Assert.Equal(5, dict.Count);
        Assert.Equal(1, dict["Id"]);
        Assert.Equal("Alice", dict["Name"]);
        Assert.Equal(25, dict["Age"]);
        Assert.Equal(60000.0, dict["Salary"]);
        Assert.Equal(true, dict["IsActive"]);
    }

    [Fact]
    public void FromDictionary_ShouldCreateRecordFromLegacyFormat()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var dict = new Dictionary<string, object?>
        {
            ["Id"] = 2,
            ["Name"] = "Bob",
            ["Age"] = 35,
            ["Salary"] = 80000.0,
            ["IsActive"] = false
        };

        // Act
        var record = EtlRecord.FromDictionary(buffer, schema, dict);

        // Assert
        Assert.Equal(2, record.GetValue("Id").AsInt32());
        Assert.Equal("Bob", record.GetValue("Name").AsString());
        Assert.Equal(35, record.GetValue("Age").AsInt32());
        Assert.Equal(80000.0, record.GetValue("Salary").AsDouble());
        Assert.False(record.GetValue("IsActive").AsBoolean());
    }

    [Fact]
    public void SetValue_WithNull_ShouldClearField()
    {
        // Arrange
        var buffer = new byte[200];
        var schema = CreateSimpleSchema();
        var record = new EtlRecord(buffer, schema);
        
        record.SetValue(0, FieldValue.FromInt32(42));

        // Act
        record.SetValue(0, FieldValue.Null());
        var value = record.GetValue(0);

        // Assert
        Assert.Equal(0, value.AsInt32()); // Cleared to zero
    }

    [Fact]
    public void AllTypes_ShouldRoundTripCorrectly()
    {
        // Arrange
        var schema = new[]
        {
            new FieldDescriptor("Int32", FieldType.Int32, 0, 4, 0),
            new FieldDescriptor("Int64", FieldType.Int64, 4, 8, 1),
            new FieldDescriptor("Double", FieldType.Double, 12, 8, 2),
            new FieldDescriptor("Float", FieldType.Float, 20, 4, 3),
            new FieldDescriptor("Boolean", FieldType.Boolean, 24, 1, 4),
            new FieldDescriptor("DateTime", FieldType.DateTime, 25, 8, 5),
            new FieldDescriptor("String", FieldType.String, 33, 50, 6),
            new FieldDescriptor("Decimal", FieldType.Decimal, 83, 16, 7),
            new FieldDescriptor("Int16", FieldType.Int16, 99, 2, 8),
            new FieldDescriptor("Byte", FieldType.Byte, 101, 1, 9),
            new FieldDescriptor("Guid", FieldType.Guid, 102, 16, 10)
        };
        var buffer = new byte[200];
        var record = new EtlRecord(buffer, schema);

        var testDateTime = new DateTime(2025, 11, 7, 12, 30, 0);
        var testGuid = Guid.NewGuid();

        // Act & Assert
        record.SetValue(0, FieldValue.FromInt32(123));
        Assert.Equal(123, record.GetValue(0).AsInt32());

        record.SetValue(1, FieldValue.FromInt64(123456789L));
        Assert.Equal(123456789L, record.GetValue(1).AsInt64());

        record.SetValue(2, FieldValue.FromDouble(123.456));
        Assert.Equal(123.456, record.GetValue(2).AsDouble());

        record.SetValue(3, FieldValue.FromFloat(12.34f));
        Assert.Equal(12.34f, record.GetValue(3).AsFloat());

        record.SetValue(4, FieldValue.FromBoolean(true));
        Assert.True(record.GetValue(4).AsBoolean());

        record.SetValue(5, FieldValue.FromDateTime(testDateTime));
        Assert.Equal(testDateTime, record.GetValue(5).AsDateTime());

        record.SetValue(6, FieldValue.FromString("test"));
        Assert.Equal("test", record.GetValue(6).AsString());

        record.SetValue(7, FieldValue.FromDecimal(123.45m));
        Assert.Equal(123.45m, record.GetValue(7).AsDecimal());

        record.SetValue(8, FieldValue.FromInt16((short)123));
        Assert.Equal((short)123, record.GetValue(8).AsInt16());

        record.SetValue(9, FieldValue.FromByte(255));
        Assert.Equal(255, record.GetValue(9).AsByte());

        record.SetValue(10, FieldValue.FromGuid(testGuid));
        Assert.Equal(testGuid, record.GetValue(10).AsGuid());
    }
}
