using Library.Infra.ZeroAlloc;
using Library.Transformers;

namespace Tests.Transformers;

/// <summary>
/// Tests for BypassDataTransformerV2.
/// Ensures zero-allocation bypass transformer works correctly.
/// </summary>
public class BypassDataTransformerV2Tests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitialize()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String)
        );

        // Act
        var transformer = new BypassDataTransformerV2(schema);

        // Assert
        Assert.NotNull(transformer);
        Assert.Equal(schema, transformer.InputSchema);
        Assert.Equal(schema, transformer.OutputSchema);
    }

    [Fact]
    public void Constructor_WithNullSchema_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BypassDataTransformerV2(null!));
    }

    [Fact]
    public void InputSchema_ShouldMatchOutputSchema()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String),
            ("Age", FieldType.Int32)
        );
        var transformer = new BypassDataTransformerV2(schema);

        // Act & Assert
        Assert.Same(transformer.InputSchema, transformer.OutputSchema);
    }

    [Fact]
    public void Transform_ShouldPassThroughUnmodified()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String)
        );
        var pool = new EtlRecordPool();
        var transformer = new BypassDataTransformerV2(schema);

        var buffer = pool.RentBuffer(EtlRecordPool.CalculateBufferSize(schema));
        var input = new EtlRecord(buffer, schema);
        input.SetValue("Id", FieldValue.FromInt32(123));
        input.SetValue("Name", FieldValue.FromString("Test"));

        int? outputId = null;
        string? outputName = null;
        int callbackCount = 0;

        // Act
        transformer.Transform(ref input, pool, (ref EtlRecord output) =>
        {
            callbackCount++;
            outputId = output.GetValue("Id").AsInt32();
            outputName = output.GetValue("Name").AsString();
        });

        // Assert
        Assert.Equal(1, callbackCount);
        Assert.Equal(123, outputId);
        Assert.Equal("Test", outputName);
        Assert.Equal(1, transformer.IngestedLines);
        Assert.Equal(1, transformer.TransformedLines);
        Assert.Equal(0, transformer.ExcludedByFilter);

        // Cleanup
        pool.ReturnBuffer(buffer);
    }

    [Fact]
    public void Transform_MultipleRecords_ShouldPassThroughAll()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Value", FieldType.Double)
        );
        var pool = new EtlRecordPool();
        var transformer = new BypassDataTransformerV2(schema, notifyAfter: 2);

        var buffer = pool.RentBuffer(EtlRecordPool.CalculateBufferSize(schema));
        var processedIds = new List<int>();
        var processedValues = new List<double>();

        // Act - Process 5 records
        for (int i = 1; i <= 5; i++)
        {
            var input = new EtlRecord(buffer, schema);
            input.SetValue("Id", FieldValue.FromInt32(i));
            input.SetValue("Value", FieldValue.FromDouble(i * 10.0));

            transformer.Transform(ref input, pool, (ref EtlRecord output) =>
            {
                processedIds.Add(output.GetValue("Id").AsInt32());
                processedValues.Add(output.GetValue("Value").AsDouble());
            });
        }

        // Assert
        Assert.Equal(5, processedIds.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, processedIds);
        Assert.Equal(new[] { 10.0, 20.0, 30.0, 40.0, 50.0 }, processedValues);
        Assert.Equal(5, transformer.IngestedLines);
        Assert.Equal(5, transformer.TransformedLines);

        // Cleanup
        pool.ReturnBuffer(buffer);
    }

    [Fact]
    public void Transform_ShouldTrackMetrics()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(("Id", FieldType.Int32));
        var pool = new EtlRecordPool();
        var transformer = new BypassDataTransformerV2(schema, notifyAfter: 100);

        var buffer = pool.RentBuffer(EtlRecordPool.CalculateBufferSize(schema));

        // Act - Process 10 records
        for (int i = 0; i < 10; i++)
        {
            var input = new EtlRecord(buffer, schema);
            input.SetValue("Id", FieldValue.FromInt32(i));
            transformer.Transform(ref input, pool, (ref EtlRecord _) => { });
        }

        // Assert
        Assert.Equal(10, transformer.IngestedLines);
        Assert.Equal(10, transformer.TransformedLines);
        Assert.Equal(0, transformer.ExcludedByFilter);

        // Cleanup
        pool.ReturnBuffer(buffer);
    }

    [Fact]
    public void OnTransform_ShouldFireAfterNotifyThreshold()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(("Id", FieldType.Int32));
        var pool = new EtlRecordPool();
        var transformer = new BypassDataTransformerV2(schema, notifyAfter: 5);

        int eventCount = 0;
        transformer.OnTransform += (args) => { eventCount++; };

        var buffer = pool.RentBuffer(EtlRecordPool.CalculateBufferSize(schema));

        // Act - Process 12 records (should trigger at 5 and 10)
        for (int i = 0; i < 12; i++)
        {
            var input = new EtlRecord(buffer, schema);
            input.SetValue("Id", FieldValue.FromInt32(i));
            transformer.Transform(ref input, pool, (ref EtlRecord _) => { });
        }

        // Assert
        Assert.Equal(2, eventCount); // At 5 and 10

        // Cleanup
        pool.ReturnBuffer(buffer);
    }

    [Fact]
    public void Complete_ShouldFireOnFinishEvent()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(("Id", FieldType.Int32));
        var transformer = new BypassDataTransformerV2(schema);

        bool onFinishCalled = false;
        long? finalTransformedLines = null;
        
        transformer.OnFinish += (args) =>
        {
            onFinishCalled = true;
            finalTransformedLines = args.TransformedLines;
        };

        // Act
        transformer.Complete();

        // Assert
        Assert.True(onFinishCalled);
        Assert.NotNull(finalTransformedLines);
    }

    [Fact]
    public void PercentDone_ShouldCalculateCorrectly()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(("Id", FieldType.Int32));
        var pool = new EtlRecordPool();
        var transformer = new BypassDataTransformerV2(schema, notifyAfter: 1);

        var buffer = pool.RentBuffer(EtlRecordPool.CalculateBufferSize(schema));

        // Act - Process records
        for (int i = 0; i < 10; i++)
        {
            var input = new EtlRecord(buffer, schema);
            transformer.Transform(ref input, pool, (ref EtlRecord _) => { });
        }

        transformer.Complete();

        // Assert
        Assert.Equal(100, transformer.PercentDone);

        // Cleanup
        pool.ReturnBuffer(buffer);
    }

    [Fact]
    public void Speed_ShouldBeCalculated()
    {
        // Arrange
        var schema = EtlRecordPool.CreateSchema(("Id", FieldType.Int32));
        var pool = new EtlRecordPool();
        var transformer = new BypassDataTransformerV2(schema, notifyAfter: 100);

        var buffer = pool.RentBuffer(EtlRecordPool.CalculateBufferSize(schema));

        // Act - Process records
        for (int i = 0; i < 100; i++)
        {
            var input = new EtlRecord(buffer, schema);
            transformer.Transform(ref input, pool, (ref EtlRecord _) => { });
        }

        transformer.Complete();

        // Assert
        Assert.True(transformer.Speed > 0);

        // Cleanup
        pool.ReturnBuffer(buffer);
    }
}
