using Library.Extractors.Parquet;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.ZeroAlloc;
using Xunit;

namespace Tests.Extractors.Parquet;

public class ParquetDataExtractorV2Tests : IDisposable
{
    private readonly string _testDirectory;

    public ParquetDataExtractorV2Tests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"parquet_test_v2_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ParquetDataExtractorV2(null!));
    }

    [Fact]
    public void Constructor_WithValidConfig_ShouldInitialize()
    {
        // Arrange
        var config = new ParquetDataExtractorConfig
        {
            Directory = _testDirectory,
            Mask = "*.parquet"
        };

        // Act
        var extractor = new ParquetDataExtractorV2(config);

        // Assert
        Assert.NotNull(extractor);
        Assert.Equal(0, extractor.LineNumber);
        Assert.Equal(0, extractor.BytesRead);
        Assert.Equal(0, extractor.PercentRead);
    }

    [Fact]
    public void Properties_ShouldBeInitializedToZero()
    {
        // Arrange
        var config = new ParquetDataExtractorConfig
        {
            Directory = _testDirectory
        };
        var extractor = new ParquetDataExtractorV2(config);

        // Assert
        Assert.Equal(0, extractor.TotalLines);
        Assert.Equal(0, extractor.LineNumber);
        Assert.Equal(0, extractor.BytesRead);
        Assert.Equal(0.0, extractor.PercentRead);
        Assert.Equal(0, extractor.FileSize);
    }

    [Fact]
    public void Schema_BeforeExtract_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = new ParquetDataExtractorConfig
        {
            Directory = _testDirectory
        };
        var extractor = new ParquetDataExtractorV2(config);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => extractor.Schema);
    }

    [Fact]
    public void Constructor_WithCustomPool_ShouldUseProvidedPool()
    {
        // Arrange
        var config = new ParquetDataExtractorConfig
        {
            Directory = _testDirectory
        };
        var pool = new EtlRecordPool();

        // Act
        var extractor = new ParquetDataExtractorV2(config, pool);

        // Assert
        Assert.NotNull(extractor);
    }

    [Fact]
    public void Config_DefaultMask_ShouldBeParquet()
    {
        // Arrange & Act
        var config = new ParquetDataExtractorConfig
        {
            Directory = _testDirectory
        };

        // Assert
        Assert.Equal("*.parquet", config.Mask);
    }

    [Fact]
    public void Config_CanSetCustomMask()
    {
        // Arrange & Act
        var config = new ParquetDataExtractorConfig
        {
            Directory = _testDirectory,
            Mask = "*.parq"
        };

        // Assert
        Assert.Equal("*.parq", config.Mask);
    }

    [Fact]
    public void Config_CanSetDirectory()
    {
        // Arrange & Act
        var config = new ParquetDataExtractorConfig
        {
            Directory = "/test/path"
        };

        // Assert
        Assert.Equal("/test/path", config.Directory);
    }

    [Fact]
    public void Config_ColumnsListShouldNotBeNull()
    {
        // Arrange & Act
        var config = new ParquetDataExtractorConfig();

        // Assert
        Assert.NotNull(config.Columns);
        Assert.Empty(config.Columns);
    }

    [Fact]
    public void OnRead_Event_CanBeSubscribed()
    {
        // Arrange
        var config = new ParquetDataExtractorConfig { Directory = _testDirectory };
        var extractor = new ParquetDataExtractorV2(config);
        bool eventCalled = false;

        // Act
        extractor.OnRead += (args) => { eventCalled = true; };

        // Assert - event subscription doesn't throw
        Assert.False(eventCalled); // Event hasn't been triggered yet
    }

    [Fact]
    public void OnFinish_Event_CanBeSubscribed()
    {
        // Arrange
        var config = new ParquetDataExtractorConfig { Directory = _testDirectory };
        var extractor = new ParquetDataExtractorV2(config);
        bool eventCalled = false;

        // Act
        extractor.OnFinish += (args) => { eventCalled = true; };

        // Assert - event subscription doesn't throw
        Assert.False(eventCalled); // Event hasn't been triggered yet
    }

    [Fact]
    public void OnError_Event_CanBeSubscribed()
    {
        // Arrange
        var config = new ParquetDataExtractorConfig { Directory = _testDirectory };
        var extractor = new ParquetDataExtractorV2(config);
        bool eventCalled = false;

        // Act
        extractor.OnError += (args) => { eventCalled = true; };

        // Assert - event subscription doesn't throw
        Assert.False(eventCalled); // Event hasn't been triggered yet
    }

    [Fact]
    public void Config_WithMultipleColumns_ShouldAddCorrectly()
    {
        // Arrange
        var config = new ParquetDataExtractorConfig
        {
            Directory = _testDirectory
        };

        // Act
        config.Columns.Add(new DefaultColumnAction("Id", 0, false, "ID", typeof(int)));
        config.Columns.Add(new DefaultColumnAction("Name", 1, false, "Name", typeof(string)));
        config.Columns.Add(new DefaultColumnAction("Value", 2, false, "Value", typeof(double)));

        // Assert
        Assert.Equal(3, config.Columns.Count);
        Assert.Equal("Id", config.Columns[0].Name);
        Assert.Equal("Name", config.Columns[1].Name);
        Assert.Equal("Value", config.Columns[2].Name);
    }

    [Fact]
    public void Config_RaiseChangeEventAfter_DefaultValue_ShouldBe10000()
    {
        // Arrange & Act
        var config = new ParquetDataExtractorConfig();

        // Assert
        Assert.Equal(10_000, config.RaiseChangeEventAfer);
    }

    [Fact]
    public void Config_CanSetCustomRaiseChangeEventAfter()
    {
        // Arrange & Act
        var config = new ParquetDataExtractorConfig
        {
            RaiseChangeEventAfer = 1000
        };

        // Assert
        Assert.Equal(1000, config.RaiseChangeEventAfer);
    }

    [Fact]
    public void Config_CultureInfo_DefaultValue_ShouldBeEnUS()
    {
        // Arrange & Act
        var config = new ParquetDataExtractorConfig();

        // Assert
        Assert.Equal("en-US", config.CultureInfo);
    }

    [Fact]
    public void Config_CanSetCustomCultureInfo()
    {
        // Arrange & Act
        var config = new ParquetDataExtractorConfig
        {
            CultureInfo = "pt-BR"
        };

        // Assert
        Assert.Equal("pt-BR", config.CultureInfo);
    }
}
