# EasyETL - AI Agent Onboarding Guide

## Quick Start for AI Agents

EasyETL is a high-performance .NET 8.0 library for building ETL (Extract, Transform, Load) pipelines. This documentation is specifically designed to help AI agents understand the codebase and contribute effectively.

## Project Overview

**Technology Stack:**
- .NET 8.0
- C# with nullable reference types enabled
- Async/await patterns throughout
- System.Threading.Channels for data flow
- XUnit for testing

**Build Status:** ✅ All 89 tests passing

## Core Architecture

### The ETL Pipeline Pattern

EasyETL implements a **streaming pipeline architecture** using three main interfaces:

```
IDataExtractor → IDataTransformer → IDataLoader
     (Extract)         (Transform)        (Load)
```

**Key Design Principles:**
1. **Streaming**: Data flows through channels, not loaded in memory
2. **Asynchronous**: All I/O operations are async
3. **Event-driven**: Progress and errors reported via events
4. **Type-safe**: Strong typing with nullable reference types
5. **Configurable**: JSON-based configuration for all components

### Main Entry Point: `EasyEtl.cs`

The `EasyEtl` class orchestrates the entire pipeline:

```csharp
// Location: src/Library/EasyEtl.cs
public class EasyEtl
{
    public event EasyEtlProgressEventHandler? OnChange;
    public event EasyEtlProgressEventHandler? OnComplete;
    public event EasyEtlErrorEventHandler? OnError;
    
    public async Task Execute()
}
```

**How it works:**
1. Creates two unbounded/bounded channels for data flow
2. Runs Extract, Transform, and Load stages in parallel using `Task.WhenAll`
3. Manages telemetry and progress tracking via `EasyEtlTelemetry`
4. Handles cancellation and error propagation

## Repository Structure

```
/
├── src/Library/                    # Main library code
│   ├── EasyEtl.cs                 # Pipeline orchestrator
│   ├── Extractors/                # Data sources (CSV, JSON, SQL, SQLite, Parquet)
│   │   └── IDataExtractor.cs      # Extractor interface
│   ├── Transformers/              # Data transformation logic
│   │   ├── IDataTransformer.cs    # Transformer interface
│   │   ├── BypassDataTransformer.cs
│   │   └── DynamicDataTransformer.cs
│   ├── Loaders/                   # Data destinations (CSV, JSON, SQL, SQLite)
│   │   └── IDataLoader.cs         # Loader interface
│   └── Infra/                     # Infrastructure code
│       ├── EtlDataProgress.cs     # Progress tracking
│       ├── EasyEtlTelemetry.cs    # Event management
│       ├── Config/                # Configuration classes
│       ├── EventArgs/             # Event argument classes
│       ├── ColumnActions/         # Column mapping/parsing
│       └── Helpers/               # Utilities
├── examples/                       # Usage examples (6 scenarios)
├── tests/
│   ├── UnitTests/                 # 89 unit tests
│   └── Benchmark/                 # Performance benchmarks
└── ai_docs/                       # This documentation
```

## Core Interfaces

### IDataExtractor

**Purpose:** Read data from a source and emit records

```csharp
// Location: src/Library/Extractors/IDataExtractor.cs
public interface IDataExtractor
{
    // Events
    event ReadNotification? OnRead;
    event ReadNotification? OnFinish;
    event EasyEtlErrorEventHandler OnError;
    
    // Progress properties
    long TotalLines { get; set; }
    int LineNumber { get; set; }
    long BytesRead { get; set; }
    double PercentRead { get; set; }
    long FileSize { get; set; }
    
    // Main method
    void Extract(RowAction processRow);
}
```

**Implementations:**
- `CsvDataExtractor` - CSV files using `Sep` library (high performance)
- `JsonDataExtractor` - JSON/JSONL files using streaming
- `SqlDataExtractor` - SQL Server databases
- `SqliteDataExtractor` - SQLite databases
- `ParquetDataExtractor` - Parquet files

### IDataTransformer

**Purpose:** Transform data records (filter, map, modify)

```csharp
// Location: src/Library/Transformers/IDataTransformer.cs
public interface IDataTransformer
{
    // Events
    event TransformNotificationHandler OnTransform;
    event TransformNotificationHandler OnFinish;
    event EasyEtlErrorEventHandler OnError;
    
    // Progress properties
    long IngestedLines { get; set; }
    long TransformedLines { get; set; }
    long ExcludedByFilter { get; set; }
    double PercentDone { get; set; }
    long TotalLines { get; set; }
    
    // Main methods
    IAsyncEnumerable<Dictionary<string, object?>> Transform(
        IAsyncEnumerable<Dictionary<string, object?>> data, 
        CancellationToken cancellationToken);
    List<Dictionary<string, object?>> ApplyTransformations(Dictionary<string, object?> item);
}
```

**Implementations:**
- `BypassDataTransformer` - Pass-through (no transformation)
- `DynamicDataTransformer` - Rule-based transformations with conditional logic

### IDataLoader

**Purpose:** Write transformed data to a destination

```csharp
// Location: src/Library/Loaders/IDataLoader.cs
public interface IDataLoader
{
    // Events
    event LoadNotificationHandler? OnWrite;
    event LoadNotificationHandler? OnFinish;
    event EasyEtlErrorEventHandler OnError;
    
    // Progress properties
    long CurrentLine { get; set; }
    long TotalLines { get; set; }
    double PercentWritten { get; set; }
    
    // Main method
    Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, 
              CancellationToken cancellationToken);
}
```

**Implementations:**
- `CsvDataLoader` - CSV files
- `JsonDataLoader` - JSON/JSONL files
- `SqlDataLoader` - SQL Server bulk insert
- `SqliteDataLoader` - SQLite bulk insert

## Data Flow Model

All data flows as `Dictionary<string, object?>` representing rows:

```csharp
var row = new Dictionary<string, object?>
{
    ["Id"] = 1,
    ["Name"] = "John",
    ["Age"] = 30,
    ["Salary"] = 50000.0,
    ["HireDate"] = DateTime.Now
};
```

**Why Dictionary?**
- Schema flexibility (different sources/destinations)
- Easy to transform (add/remove/rename columns)
- Supports dynamic evaluation

## Common Development Tasks

### Adding a New Extractor

1. **Create class** implementing `IDataExtractor`
2. **Add configuration class** in `Infra/Config/`
3. **Implement `Extract` method** to read data synchronously
4. **Emit events** (`OnRead`, `OnFinish`, `OnError`)
5. **Update progress properties** (`LineNumber`, `BytesRead`, etc.)
6. **Add unit tests** in `tests/UnitTests/Extractors/`

**Example pattern:**
```csharp
public class MyDataExtractor : IDataExtractor
{
    public void Extract(RowAction processRow)
    {
        // Read data from source
        foreach (var record in ReadFromSource())
        {
            LineNumber++;
            var row = new Dictionary<string, object?> { /* map fields */ };
            processRow(ref row);
            OnRead?.Invoke(/* progress */);
        }
        OnFinish?.Invoke(/* final progress */);
    }
}
```

### Adding a New Loader

1. **Create class** implementing `IDataLoader`
2. **Add configuration class** in `Infra/Config/`
3. **Implement `Load` method** as async method consuming `IAsyncEnumerable`
4. **Emit events** (`OnWrite`, `OnFinish`, `OnError`)
5. **Update progress properties** (`CurrentLine`, `PercentWritten`)
6. **Add unit tests** in `tests/UnitTests/Loaders/`

**Example pattern:**
```csharp
public class MyDataLoader : IDataLoader
{
    public async Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, 
                           CancellationToken cancellationToken)
    {
        await foreach (var row in data.WithCancellation(cancellationToken))
        {
            CurrentLine++;
            // Write to destination
            WriteToDestination(row);
            
            if (CurrentLine % RaiseChangeEventAfter == 0)
                OnWrite?.Invoke(/* progress */);
        }
        OnFinish?.Invoke(/* final progress */);
    }
}
```

### Adding Transformation Logic

The `DynamicDataTransformer` supports dynamic transformations via configuration:

```json
{
  "Transformations": [
    {
      "Condition": "row[\"Age\"] > 18",
      "Actions": [
        {
          "FieldMappings": {
            "Status": { "Value": "Adult", "IsDynamic": false },
            "Category": { "Value": "row[\"Department\"]", "IsDynamic": true }
          }
        }
      ]
    }
  ]
}
```

## Testing Strategy

### Test Organization

```
tests/UnitTests/
├── Extractors/          # Extractor tests
├── Loaders/            # Loader tests
├── Transformers/       # Transformer tests
├── Integration/        # End-to-end pipeline tests
└── Infra/             # Infrastructure tests
```

### Running Tests

```bash
# Build
dotnet build EasyETL.sln

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/UnitTests/UnitTests.csproj

# Run with filter
dotnet test --filter "FullyQualifiedName~CsvDataExtractor"
```

### Test Patterns

**Unit tests use XUnit and follow AAA pattern:**
```csharp
[Fact]
public async Task ComponentName_Scenario_ExpectedBehavior()
{
    // Arrange
    var config = new MyConfig { /* setup */ };
    var component = new MyComponent(config);
    
    // Act
    await component.Execute();
    
    // Assert
    Assert.Equal(expected, actual);
}
```

**Integration tests** verify full pipeline:
```csharp
[Fact]
public async Task EasyEtl_CsvToJson_ShouldProcessAllRecords()
{
    var extractor = new CsvDataExtractor(csvConfig);
    var transformer = new DynamicDataTransformer(transformConfig);
    var loader = new JsonDataLoader(jsonConfig);
    
    var etl = new EasyEtl(extractor, transformer, loader);
    await etl.Execute();
    
    // Verify results
}
```

## Configuration Patterns

All components use JSON configuration for flexibility:

### Extractor Configuration Example

```json
{
  "FilePath": "/path/to/data.csv",
  "HasHeader": true,
  "Delimiter": ",",
  "RaiseChangeEventAfter": 1000,
  "Columns": [
    {
      "Type": "ParseColumnAction",
      "OutputName": "Id",
      "Position": 0,
      "OutputType": "System.Int32"
    },
    {
      "Type": "DefaultColumnAction",
      "OutputName": "Name",
      "Position": 1,
      "OutputType": "System.String"
    }
  ]
}
```

### Loader Configuration Example

```json
{
  "ConnectionString": "Data Source=mydb.db",
  "TableName": "TargetTable",
  "RaiseChangeEventAfter": 1000
}
```

## Performance Considerations

1. **Channel Sizing:** Use bounded channels to control memory
   ```csharp
   var etl = new EasyEtl(extractor, transformer, loader, channelSize: 1000);
   ```

2. **Bulk Operations:** Loaders use bulk insert (SQL) or buffered writes (files)

3. **Streaming:** Large files never loaded entirely in memory

4. **Event Frequency:** Control via `RaiseChangeEventAfter` property

## Error Handling

**Three levels of error handling:**

1. **Component-level:** Each component emits `OnError` event
2. **Pipeline-level:** `EasyEtl` handles errors and cancels pipeline
3. **Application-level:** Consumer handles `EasyEtl.OnError`

**Example:**
```csharp
etl.OnError += (args) => {
    Console.WriteLine($"Error in {args.Type}: {args.Exception.Message}");
    // Log, retry, or gracefully shutdown
};
```

## Dependencies

**Current versions** (see `src/Library/Library.csproj`):

| Package | Version | Purpose |
|---------|---------|---------|
| Ardalis.GuardClauses | 4.5.0 | Input validation |
| JsonStreamer.NewtonsoftJson.Client | 1.0.0 | Streaming JSON parsing |
| Microsoft.Data.Sqlite | 8.0.3 | SQLite data access |
| Newtonsoft.Json | 13.0.3 | JSON serialization |
| Parquet.Net | 4.23.4 | Parquet file support |
| Sep | 0.4.4 | High-performance CSV parsing |
| System.Data.SqlClient | 4.8.6 | SQL Server access |
| Z.Expressions.Eval | 6.1.2 | Dynamic expression evaluation |

## Code Style Guidelines

1. **Use nullable reference types:** All reference types annotated with `?` when nullable
2. **Async all the way:** Use async/await for I/O operations
3. **Event naming:** Prefix with `On` (e.g., `OnChange`, `OnError`)
4. **Interface segregation:** Small, focused interfaces
5. **Guard clauses:** Use `Ardalis.GuardClauses` for validation
6. **Progress tracking:** All long-running operations report progress

## Common Pitfalls

1. **Don't buffer entire datasets** - Use streaming patterns
2. **Don't ignore CancellationToken** - Always pass and check it
3. **Don't forget to complete channels** - Call `Writer.Complete()` or `Writer.Complete(exception)`
4. **Don't skip event invocation** - Events are crucial for monitoring
5. **Test with large datasets** - Performance issues only show at scale

## Example Usage

**Basic pipeline:**
```csharp
// Configure components
var extractorConfig = new CsvDataExtractorConfig { FilePath = "input.csv" };
var loaderConfig = new JsonDataLoaderConfig { FilePath = "output.jsonl" };

// Create components
var extractor = new CsvDataExtractor(extractorConfig);
var loader = new JsonDataLoader(loaderConfig);

// Create and execute pipeline
var etl = new EasyEtl(extractor, loader);
etl.OnComplete += (args) => Console.WriteLine("Done!");
await etl.Execute();
```

**See `/examples` for 6 complete scenarios.**

## Questions to Ask Before Making Changes

1. Will this change affect memory usage with large datasets?
2. Does this require new configuration options?
3. Are there existing patterns I should follow?
4. Do I need to add telemetry/progress tracking?
5. What error scenarios should I handle?
6. How will I test this with large datasets?

## Getting Help

1. Check `examples/` for usage patterns
2. Review existing implementations in same category
3. Read tests for expected behaviors
4. Check `architecture.md` for design decisions

---

*This documentation is optimized for AI agent comprehension and rapid onboarding. Last updated: 2025-11-05*