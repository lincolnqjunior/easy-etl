# EasyETL Development Guide for AI Agents

## Quick Reference

### Build & Test Commands

```bash
# Build entire solution
dotnet build EasyETL.sln

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/UnitTests/UnitTests.csproj

# Run benchmarks
dotnet run --project tests/Benchmark/Benchmark.csproj -c Release

# Run examples
dotnet run --project examples/Playground.csproj
```

### Project Structure at a Glance

```
src/Library/
├── EasyEtl.cs                 # Main orchestrator
├── Extractors/
│   ├── IDataExtractor.cs      # Interface
│   ├── Csv/CsvDataExtractor.cs
│   ├── Json/JsonDataExtractor.cs
│   ├── SQL/SqlDataExtractor.cs
│   ├── SQLite/SqliteDataExtractor.cs
│   └── Parquet/ParquetDataExtractor.cs
├── Transformers/
│   ├── IDataTransformer.cs
│   ├── BypassDataTransformer.cs
│   └── DynamicDataTransformer.cs
├── Loaders/
│   ├── IDataLoader.cs
│   ├── Csv/CsvDataLoader.cs
│   ├── Json/JsonDataLoader.cs
│   ├── SQL/SqlDataLoader.cs
│   └── SQLite/SqliteDataLoader.cs
└── Infra/
    ├── EtlDataProgress.cs
    ├── EasyEtlTelemetry.cs
    ├── Config/                # Configuration classes
    ├── EventArgs/             # Event argument classes
    ├── ColumnActions/         # Column parsing/mapping
    └── Helpers/               # Utility classes
```

## Development Workflows

### Adding a New Data Extractor

**Step-by-step process:**

1. **Create the extractor class:**
   ```bash
   # Create directory if needed
   mkdir -p src/Library/Extractors/MyFormat
   
   # Create the extractor file
   touch src/Library/Extractors/MyFormat/MyFormatDataExtractor.cs
   ```

2. **Implement the interface:**
   ```csharp
   using Library.Extractors;
   using Library.Infra.EventArgs;
   
   namespace Library.Extractors.MyFormat
   {
       public class MyFormatDataExtractor : IDataExtractor
       {
           public event ReadNotification? OnRead;
           public event ReadNotification? OnFinish;
           public event EasyEtlErrorEventHandler OnError;
           
           public long TotalLines { get; set; }
           public int LineNumber { get; set; }
           public long BytesRead { get; set; }
           public double PercentRead { get; set; }
           public long FileSize { get; set; }
           
           private readonly MyFormatDataExtractorConfig _config;
           
           public MyFormatDataExtractor(MyFormatDataExtractorConfig config)
           {
               _config = Guard.Against.Null(config, nameof(config));
               // Initialize resources
           }
           
           public void Extract(RowAction processRow)
           {
               try
               {
                   // Open data source
                   // Set TotalLines if known
                   
                   foreach (var record in ReadRecords())
                   {
                       LineNumber++;
                       var row = ConvertToRow(record);
                       processRow(ref row);
                       
                       // Emit progress event periodically
                       if (LineNumber % _config.RaiseChangeEventAfter == 0)
                       {
                           OnRead?.Invoke(new ExtractNotificationEventArgs(this));
                       }
                   }
                   
                   OnFinish?.Invoke(new ExtractNotificationEventArgs(this));
               }
               catch (Exception ex)
               {
                   OnError?.Invoke(new ErrorNotificationEventArgs(
                       EtlType.Extract, ex, new Dictionary<string, object?>(), LineNumber));
                   throw;
               }
           }
       }
   }
   ```

3. **Create configuration class:**
   ```csharp
   // src/Library/Infra/Config/MyFormatDataExtractorConfig.cs
   namespace Library.Infra.Config
   {
       public class MyFormatDataExtractorConfig
       {
           public string FilePath { get; set; } = string.Empty;
           public int RaiseChangeEventAfter { get; set; } = 1000;
           // Add format-specific properties
       }
   }
   ```

4. **Write unit tests:**
   ```csharp
   // tests/UnitTests/Extractors/MyFormatDataExtractorTests.cs
   using Library.Extractors.MyFormat;
   using Xunit;
   
   namespace UnitTests.Extractors
   {
       public class MyFormatDataExtractorTests
       {
           [Fact]
           public void Extract_WithValidFile_ShouldReadAllRecords()
           {
               // Arrange
               var config = new MyFormatDataExtractorConfig
               {
                   FilePath = "test-data.myformat"
               };
               var extractor = new MyFormatDataExtractor(config);
               int recordCount = 0;
               
               // Act
               extractor.Extract((ref Dictionary<string, object?> row) => 
               {
                   recordCount++;
               });
               
               // Assert
               Assert.Equal(expectedCount, recordCount);
           }
       }
   }
   ```

5. **Test with integration test:**
   ```csharp
   [Fact]
   public async Task EasyEtl_MyFormatToJson_ShouldComplete()
   {
       var extractor = new MyFormatDataExtractor(extractorConfig);
       var loader = new JsonDataLoader(loaderConfig);
       var etl = new EasyEtl(extractor, loader);
       
       await etl.Execute();
       
       // Verify output
   }
   ```

### Adding a New Data Loader

**Step-by-step process:**

1. **Create the loader class:**
   ```csharp
   using Library.Loaders;
   using Library.Infra.EventArgs;
   
   namespace Library.Loaders.MyFormat
   {
       public class MyFormatDataLoader : IDataLoader
       {
           public event LoadNotificationHandler? OnWrite;
           public event LoadNotificationHandler? OnFinish;
           public event EasyEtlErrorEventHandler OnError;
           
           public long CurrentLine { get; set; }
           public long TotalLines { get; set; }
           public double PercentWritten { get; set; }
           
           private readonly MyFormatDataLoaderConfig _config;
           
           public MyFormatDataLoader(MyFormatDataLoaderConfig config)
           {
               _config = Guard.Against.Null(config, nameof(config));
           }
           
           public async Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, 
                                  CancellationToken cancellationToken)
           {
               try
               {
                   // Open destination
                   // Write header if needed
                   
                   await foreach (var row in data.WithCancellation(cancellationToken))
                   {
                       CurrentLine++;
                       WriteRow(row);
                       
                       if (CurrentLine % _config.RaiseChangeEventAfter == 0)
                       {
                           PercentWritten = TotalLines > 0 
                               ? (CurrentLine * 100.0 / TotalLines) 
                               : 0;
                           OnWrite?.Invoke(new LoadNotificationEventArgs(this));
                       }
                   }
                   
                   // Flush/close
                   PercentWritten = 100;
                   OnFinish?.Invoke(new LoadNotificationEventArgs(this));
               }
               catch (Exception ex)
               {
                   OnError?.Invoke(new ErrorNotificationEventArgs(
                       EtlType.Load, ex, new Dictionary<string, object?>(), CurrentLine));
                   throw;
               }
           }
       }
   }
   ```

2. **Create configuration class**
3. **Write unit tests**
4. **Test with integration test**

### Extending Dynamic Transformer

To add new transformation actions to `DynamicDataTransformer`:

1. **Define new action in configuration:**
   ```json
   {
     "Transformations": [
       {
         "Condition": "row[\"Status\"] == \"Active\"",
         "Actions": [
           {
             "Type": "MyNewAction",
             "Parameters": {
               "Field": "ProcessedDate",
               "Value": "DateTime.Now"
             }
           }
         ]
       }
     ]
   }
   ```

2. **Extend TransformationConfig** (if needed)
3. **Modify ApplyTransformations logic**
4. **Add tests for new action**

### Testing Patterns

#### Unit Test Pattern

```csharp
[Fact]
public void Component_Scenario_ExpectedBehavior()
{
    // Arrange
    var config = new ComponentConfig { /* setup */ };
    var component = new Component(config);
    var expectedValue = 42;
    
    // Act
    var result = component.DoSomething();
    
    // Assert
    Assert.Equal(expectedValue, result);
}
```

#### Async Test Pattern

```csharp
[Fact]
public async Task Component_AsyncScenario_ExpectedBehavior()
{
    // Arrange
    var component = new Component(config);
    
    // Act
    await component.ExecuteAsync();
    
    // Assert
    Assert.True(component.IsCompleted);
}
```

#### Event Test Pattern

```csharp
[Fact]
public void Component_EmitsEvents_WhenConditionMet()
{
    // Arrange
    var component = new Component(config);
    var eventRaised = false;
    component.OnEvent += (args) => eventRaised = true;
    
    // Act
    component.TriggerEvent();
    
    // Assert
    Assert.True(eventRaised);
}
```

#### Integration Test Pattern

```csharp
[Fact]
public async Task EasyEtl_EndToEnd_ProcessesAllData()
{
    // Arrange
    var extractor = new CsvDataExtractor(extractorConfig);
    var transformer = new DynamicDataTransformer(transformerConfig);
    var loader = new JsonDataLoader(loaderConfig);
    var etl = new EasyEtl(extractor, transformer, loader);
    
    var completed = false;
    etl.OnComplete += (args) => completed = true;
    
    // Act
    await etl.Execute();
    
    // Assert
    Assert.True(completed);
    Assert.Equal(expectedLines, loader.CurrentLine);
}
```

## Common Implementation Patterns

### Progress Event Emission

**Pattern:**
```csharp
private int _eventCounter = 0;
private readonly int _eventFrequency = 1000;

// In processing loop
_eventCounter++;
if (_eventCounter >= _eventFrequency)
{
    _eventCounter = 0;
    OnProgress?.Invoke(new ProgressEventArgs(this));
}
```

### Error Handling

**Pattern:**
```csharp
try
{
    // Processing logic
}
catch (Exception ex)
{
    OnError?.Invoke(new ErrorNotificationEventArgs(
        EtlType.Extract, // or Transform/Load
        ex,
        currentRow,
        currentLineNumber
    ));
    throw; // Re-throw to propagate to pipeline
}
```

### Configuration Validation

**Pattern:**
```csharp
public MyComponent(MyConfig config)
{
    _config = Guard.Against.Null(config, nameof(config));
    Guard.Against.NullOrWhiteSpace(config.FilePath, nameof(config.FilePath));
    Guard.Against.NegativeOrZero(config.BufferSize, nameof(config.BufferSize));
}
```

### Resource Cleanup

**Pattern:**
```csharp
public class MyExtractor : IDataExtractor, IDisposable
{
    private Stream? _stream;
    
    public void Extract(RowAction processRow)
    {
        _stream = File.OpenRead(_config.FilePath);
        try
        {
            // Process
        }
        finally
        {
            _stream?.Dispose();
        }
    }
    
    public void Dispose()
    {
        _stream?.Dispose();
    }
}
```

### Async Enumerable Pattern

**Pattern:**
```csharp
public async IAsyncEnumerable<Dictionary<string, object?>> Transform(
    IAsyncEnumerable<Dictionary<string, object?>> data,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var row in data.WithCancellation(cancellationToken))
    {
        // Transform logic
        var transformedRows = ApplyTransformations(row);
        
        foreach (var transformedRow in transformedRows)
        {
            yield return transformedRow;
        }
    }
}
```

## Debugging Tips

### Enable Detailed Logging

```csharp
etl.OnChange += (args) => {
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Progress:");
    foreach (var progress in args.Progress)
    {
        Console.WriteLine($"  {progress.Key}: {progress.Value.CurrentLine}/{progress.Value.TotalLines}");
    }
};

etl.OnError += (args) => {
    Console.WriteLine($"Error in {args.Type}: {args.Exception}");
    Console.WriteLine($"Row: {args.CurrentLine}");
    Console.WriteLine($"Data: {string.Join(", ", args.CurrentRow)}");
};
```

### Test with Small Datasets

```csharp
// Create small test file
var testData = Enumerable.Range(1, 100).Select(i => new { Id = i, Name = $"Test{i}" });
File.WriteAllLines("small-test.csv", testData.Select(d => $"{d.Id},{d.Name}"));
```

### Use Bounded Channels to Catch Memory Issues

```csharp
// Force backpressure to expose issues
var etl = new EasyEtl(extractor, transformer, loader, channelSize: 10);
```

### Verify Event Sequence

```csharp
var events = new List<string>();
extractor.OnRead += _ => events.Add("ExtractRead");
extractor.OnFinish += _ => events.Add("ExtractFinish");
transformer.OnTransform += _ => events.Add("Transform");
transformer.OnFinish += _ => events.Add("TransformFinish");
loader.OnWrite += _ => events.Add("LoadWrite");
loader.OnFinish += _ => events.Add("LoadFinish");

// After execution, verify sequence
Assert.Equal(expectedSequence, events);
```

## Performance Optimization

### For Extractors

1. **Batch reads** where possible
2. **Use buffered streams** for file I/O
3. **Minimize allocations** in hot path
4. **Cache parsed values** if same data parsed multiple times
5. **Use `ref` parameters** to avoid dictionary copies

### For Transformers

1. **Avoid string concatenation** in loops
2. **Cache compiled expressions** (DynamicDataTransformer already does this)
3. **Minimize conditional branches** in tight loops
4. **Pre-allocate collections** when size known

### For Loaders

1. **Use bulk operations** for databases
2. **Batch writes** for files
3. **Use transactions** for database inserts
4. **Buffer output** before writing

### General

1. **Profile before optimizing**
2. **Test with realistic data volumes**
3. **Monitor memory usage** with bounded channels
4. **Use BenchmarkDotNet** for micro-optimizations

## Code Review Checklist

Before submitting changes, verify:

- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] Build succeeds with no warnings (or only expected warnings)
- [ ] Code follows existing patterns
- [ ] Events are emitted at appropriate times
- [ ] Progress properties are updated correctly
- [ ] Error handling is comprehensive
- [ ] Configuration is validated
- [ ] Resources are properly disposed
- [ ] Cancellation token is respected
- [ ] Documentation comments added for public APIs
- [ ] Examples updated if needed

## Troubleshooting Common Issues

### Issue: Pipeline hangs

**Likely causes:**
- Channel not completed after extraction
- Forgetting to await async operations
- Deadlock in synchronous-over-async code

**Solution:**
```csharp
// Always complete channels
_extractChannel.Writer.Complete();

// Use ConfigureAwait(false) for library code
await SomeOperationAsync().ConfigureAwait(false);
```

### Issue: Memory grows unbounded

**Likely causes:**
- Buffering entire dataset
- Not using streaming patterns
- Channel unbounded with fast producer

**Solution:**
```csharp
// Use bounded channels
var etl = new EasyEtl(extractor, transformer, loader, channelSize: 1000);

// Ensure streaming patterns
await foreach (var row in data) // Good
// vs
var allData = await data.ToListAsync(); // Bad
```

### Issue: Events not firing

**Likely causes:**
- Forgot to emit event
- Event frequency too high (RaiseChangeEventAfter)
- No event handler subscribed

**Solution:**
```csharp
// Lower event frequency for testing
config.RaiseChangeEventAfter = 1; // Fire every row

// Check event is emitted
OnRead?.Invoke(new ExtractNotificationEventArgs(this));
```

### Issue: Type conversion errors

**Likely causes:**
- Source data type mismatch
- Null handling issues
- Culture-specific parsing

**Solution:**
```csharp
// Use nullable types
object? value = row["Age"];
int? age = value as int?;

// Handle nulls explicitly
if (value != null && int.TryParse(value.ToString(), out int parsed))
{
    age = parsed;
}
```

## Useful Code Snippets

### Create Test Data

```csharp
public static class TestDataGenerator
{
    public static void GenerateCsvFile(string path, int rows)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("Id,Name,Age,Salary,Date");
        
        for (int i = 1; i <= rows; i++)
        {
            writer.WriteLine($"{i},Name{i},{20 + i % 50},{30000 + i * 100},{DateTime.Now:yyyy-MM-dd}");
        }
    }
}
```

### Measure Throughput

```csharp
var stopwatch = Stopwatch.StartNew();
long totalRows = 0;

etl.OnChange += (args) => {
    var progress = args.Progress[EtlType.Global];
    totalRows = progress.CurrentLine;
};

await etl.Execute();
stopwatch.Stop();

Console.WriteLine($"Processed {totalRows} rows in {stopwatch.Elapsed}");
Console.WriteLine($"Throughput: {totalRows / stopwatch.Elapsed.TotalSeconds:F0} rows/sec");
```

### Compare Configurations

```csharp
// Test different channel sizes
foreach (var channelSize in new[] { 0, 100, 1000, 10000 })
{
    var etl = new EasyEtl(extractor, transformer, loader, channelSize);
    var sw = Stopwatch.StartNew();
    await etl.Execute();
    Console.WriteLine($"ChannelSize={channelSize}: {sw.Elapsed}");
}
```

---

*This development guide provides practical patterns and workflows for AI agents working on EasyETL.*
