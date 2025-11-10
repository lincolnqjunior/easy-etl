using Library.Extractors.Csv;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.ZeroAlloc;

namespace Tests.Extractors.Csv;

/// <summary>
/// Tests for CsvDataExtractorV2 - zero-allocation CSV extractor.
/// </summary>
public class CsvDataExtractorV2Tests
{
    [Fact]
    public void Constructor_WithValidConfig_ShouldInitialize()
    {
        // Arrange
        var config = CreateBasicConfig("test.csv");

        // Act
        var extractor = new CsvDataExtractorV2(config);

        // Assert
        Assert.NotNull(extractor);
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CsvDataExtractorV2(null!));
    }

    [Fact]
    public void Constructor_WithCustomPool_ShouldUsePool()
    {
        // Arrange
        var config = CreateBasicConfig("test.csv");
        var pool = new EtlRecordPool(2048, 50);

        // Act
        var extractor = new CsvDataExtractorV2(config, pool);

        // Assert
        Assert.NotNull(extractor);
    }

    // Helper method
    private CsvDataExtractorConfig CreateBasicConfig(string filePath)
    {
        return new CsvDataExtractorConfig
        {
            FilePath = filePath,
            HasHeader = false,
            Delimiter = ',',
            RaiseChangeEventAfer = 1000,
            Columns = new List<IColumnAction>
            {
                new DefaultColumnAction("Id", 0, false, "Id", typeof(int)),
                new DefaultColumnAction("Name", 1, false, "Name", typeof(string))
            }
        };
    }
}
