# EasyETL Architecture

## Design Philosophy

EasyETL is built on three core principles:

1. **Streaming First:** Process data as it flows, never load entire datasets in memory
2. **Event-Driven:** Observable progress and errors at every stage
3. **Composability:** Mix and match extractors, transformers, and loaders

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        EasyEtl                              │
│                   (Pipeline Orchestrator)                    │
│                                                             │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────┐  │
│  │              │   │              │   │              │  │
│  │  Extractor   │──▶│ Transformer  │──▶│   Loader     │  │
│  │              │   │              │   │              │  │
│  └──────────────┘   └──────────────┘   └──────────────┘  │
│         │                  │                  │            │
│         ▼                  ▼                  ▼            │
│  ┌──────────────────────────────────────────────────────┐ │
│  │            EasyEtlTelemetry                          │ │
│  │        (Progress & Event Aggregation)                │ │
│  └──────────────────────────────────────────────────────┘ │
│         │                                                  │
└─────────┼──────────────────────────────────────────────────┘
          │
          ▼
    Application
  (Event Handlers)
```

## Data Flow Architecture

### Channel-Based Pipeline

EasyETL uses `System.Threading.Channels` for efficient data flow:

```
Extract Thread          Transform Thread         Load Thread
     │                       │                       │
     ▼                       ▼                       ▼
  ┌─────┐               ┌─────┐                 ┌─────┐
  │Read │               │Read │                 │Read │
  │Data │               │Data │                 │Data │
  └─────┘               └─────┘                 └─────┘
     │                       │                       │
     ▼                       ▼                       ▼
┌──────────┐          ┌──────────┐            ┌──────────┐
│ Process  │          │Transform │            │  Write   │
│   Row    │          │   Row    │            │   Row    │
└──────────┘          └──────────┘            └──────────┘
     │                       │                       │
     ▼                       ▼                       ▼
┌──────────┐          ┌──────────┐            ┌──────────┐
│  Write   │          │  Write   │            │  Emit    │
│ Channel  │          │ Channel  │            │  Event   │
└──────────┘          └──────────┘            └──────────┘
     │                       │                       │
     └───────────────────────┴───────────────────────┘
                             │
                       Runs in Parallel
```

**Key Features:**
- **Unbounded Channels:** Default mode, no backpressure
- **Bounded Channels:** Optional, controls memory via backpressure
- **Parallel Execution:** All three stages run concurrently
- **Async Communication:** Non-blocking write/read operations

### Data Record Format

All data flows as `Dictionary<string, object?>`:

```csharp
public delegate void RowAction(ref Dictionary<string, object?> row);
```

**Why Dictionary?**
- **Schema Flexibility:** Different sources have different schemas
- **Dynamic Transformation:** Easy to add/remove/rename fields
- **Type Safety:** Values are `object?` but strongly typed in implementations
- **Performance:** Adequate for row-by-row processing

## Component Architecture

### 1. Extractor Layer

**Responsibility:** Read data from source, emit row-by-row

```
┌─────────────────────────────────────┐
│       IDataExtractor                │
│  ┌───────────────────────────────┐  │
│  │ Extract(RowAction processRow) │  │
│  └───────────────────────────────┘  │
│                                     │
│  Events:  OnRead, OnFinish, OnError │
│  Progress: LineNumber, BytesRead    │
└─────────────────────────────────────┘
         │         │         │
         ▼         ▼         ▼
    ┌─────┐   ┌─────┐   ┌─────┐
    │ CSV │   │JSON │   │ SQL │
    └─────┘   └─────┘   └─────┘
```

**Implementations:**

| Extractor | Source | Key Features |
|-----------|--------|--------------|
| CsvDataExtractor | CSV files | Uses `Sep` library, high-speed parsing, column type parsing |
| JsonDataExtractor | JSON/JSONL | Streaming parser, handles large files |
| SqlDataExtractor | SQL Server | Batched reads, connection pooling |
| SqliteDataExtractor | SQLite | In-memory or file-based |
| ParquetDataExtractor | Parquet | Columnar format, efficient reads |

**Key Design Decisions:**
1. **Synchronous Extract:** Simplifies implementation, runs on dedicated thread
2. **Callback Pattern:** Uses delegate for row processing (zero allocation)
3. **Progress Tracking:** Built-in via properties and events
4. **Error Handling:** Exceptions caught and emitted via `OnError` event

### 2. Transformer Layer

**Responsibility:** Modify, filter, or enrich data records

```
┌─────────────────────────────────────────────────┐
│           IDataTransformer                      │
│  ┌───────────────────────────────────────────┐  │
│  │ Transform(IAsyncEnumerable<> data)        │  │
│  │ ApplyTransformations(Dictionary<> item)   │  │
│  └───────────────────────────────────────────┘  │
│                                                 │
│  Events:  OnTransform, OnFinish, OnError        │
│  Progress: IngestedLines, TransformedLines      │
└─────────────────────────────────────────────────┘
         │                            │
         ▼                            ▼
    ┌─────────┐              ┌─────────────────┐
    │ Bypass  │              │ Dynamic         │
    │         │              │ (Rule-Based)    │
    └─────────┘              └─────────────────┘
```

**Implementations:**

1. **BypassDataTransformer:**
   - Pass-through, no modifications
   - Used when `transformer` parameter is null in `EasyEtl` constructor
   - Zero overhead

2. **DynamicDataTransformer:**
   - Rule-based transformations
   - Conditional logic: `if (condition) then (actions)`
   - Field mapping: Copy, rename, compute
   - Uses `Z.Expressions.Eval` for dynamic evaluation
   - Can multiply rows (1 → N transformations)

**Transformation Flow:**
```
Input Row
    │
    ▼
Check Condition ─────[false]────▶ Skip
    │
   [true]
    │
    ▼
Apply Actions
    │
    ▼
Multiple Output Rows?
    │         │
   [No]      [Yes]
    │         │
    ▼         ▼
Output Row(s)
```

### 3. Loader Layer

**Responsibility:** Write transformed data to destination

```
┌─────────────────────────────────────────────┐
│            IDataLoader                      │
│  ┌───────────────────────────────────────┐  │
│  │ Load(IAsyncEnumerable<> data)         │  │
│  └───────────────────────────────────────┘  │
│                                             │
│  Events:  OnWrite, OnFinish, OnError        │
│  Progress: CurrentLine, PercentWritten      │
└─────────────────────────────────────────────┘
         │         │         │
         ▼         ▼         ▼
    ┌─────┐   ┌─────┐   ┌─────┐
    │ CSV │   │JSON │   │ SQL │
    └─────┘   └─────┘   └─────┘
```

**Implementations:**

| Loader | Destination | Key Features |
|--------|-------------|--------------|
| CsvDataLoader | CSV files | Buffered writes, configurable delimiter |
| JsonDataLoader | JSON/JSONL | One JSON object per line |
| SqlDataLoader | SQL Server | Bulk insert using `SqlBulkCopy` |
| SqliteDataLoader | SQLite | Transaction-based batch inserts |

**Key Design Decisions:**
1. **Async Load:** Natural fit for I/O operations
2. **Bulk Operations:** SQL loaders use batching for performance
3. **Buffering:** File loaders buffer writes to reduce I/O calls
4. **Transaction Support:** Database loaders wrap in transactions

### 4. Infrastructure Layer

#### EasyEtlTelemetry

**Purpose:** Aggregate events from all components and emit consolidated progress

```
┌────────────────────────────────────────┐
│        EasyEtlTelemetry                │
│                                        │
│  Subscribes to:                        │
│    • Extractor.OnRead                  │
│    • Extractor.OnFinish                │
│    • Transformer.OnTransform           │
│    • Transformer.OnFinish              │
│    • Loader.OnWrite                    │
│    • Loader.OnFinish                   │
│    • *.OnError                         │
│                                        │
│  Maintains:                            │
│    • EtlDataProgress[Extract]          │
│    • EtlDataProgress[Transform]        │
│    • EtlDataProgress[Load]             │
│    • EtlDataProgress[Global]           │
│                                        │
│  Emits:                                │
│    • OnChange (aggregated progress)    │
│    • OnError (any component error)     │
└────────────────────────────────────────┘
```

**Progress Tracking:**
```csharp
public class EtlDataProgress
{
    public long CurrentLine { get; set; }
    public long TotalLines { get; set; }
    public double PercentComplete { get; set; }
    public EtlStatus Status { get; set; }  // Running, Completed, Failed
    public double Speed { get; set; }       // Lines per second
    public TimeSpan EstimatedTimeToEnd { get; set; }
}
```

#### Configuration System

All components configured via JSON:

```
┌─────────────────────────────────────┐
│      Configuration Classes          │
│                                     │
│  • CsvDataExtractorConfig           │
│  • JsonDataExtractorConfig          │
│  • ParquetDataExtractorConfig       │
│  • DatabaseDataLoaderConfig         │
│  • CsvDataLoaderConfig              │
│  • TransformationConfig             │
│                                     │
│  Deserialization:                   │
│    JsonConvert.DeserializeObject<T> │
└─────────────────────────────────────┘
```

**Design Pattern: Configuration as Code**
- Strongly-typed configuration classes
- JSON serialization support
- Validation via `Ardalis.GuardClauses`

#### Column Actions System

Flexible column mapping and type conversion:

```
┌────────────────────────────────────┐
│       IColumnAction                │
│  ┌──────────────────────────────┐  │
│  │ ParseColumn(...)             │  │
│  └──────────────────────────────┘  │
└────────────────────────────────────┘
         │              │
         ▼              ▼
┌───────────────┐  ┌─────────────┐
│ Parse         │  │ Default     │
│ (Type Conv)   │  │ (As-Is)     │
└───────────────┘  └─────────────┘
```

**Supported Types:**
- System.String
- System.Int32, System.Int64
- System.Double, System.Decimal
- System.DateTime
- System.Boolean
- System.Guid

## Threading Model

### Execution Model

```
Main Thread
    │
    ▼
EasyEtl.Execute() ──────▶ Creates CancellationTokenSource
    │
    ├─────▶ Task.Run(() => Extract())    [Thread Pool]
    │           └─ Synchronous extraction
    │
    ├─────▶ Transform()                   [Async]
    │           └─ IAsyncEnumerable iteration
    │
    ├─────▶ Load()                        [Async]
    │           └─ IAsyncEnumerable iteration
    │
    ▼
Task.WhenAll([Extract, Transform, Load])
    │
    ▼
OnComplete event
```

**Key Points:**
1. **Extract:** Runs on thread pool via `Task.Run`
2. **Transform:** Async method, may use thread pool
3. **Load:** Async method, primarily I/O bound
4. **Channels:** Thread-safe, enable lock-free communication
5. **Cancellation:** Single `CancellationTokenSource` for entire pipeline

### Cancellation Strategy

```
Error in any stage
    │
    ▼
OnError event ───▶ _cts.Cancel()
    │
    ▼
All stages receive CancellationToken.IsCancellationRequested
    │
    ▼
Channels complete with exception
    │
    ▼
Pipeline terminates
```

## Error Handling Architecture

### Error Propagation

```
Component throws Exception
    │
    ▼
Component.OnError event
    │
    ▼
EasyEtlTelemetry.OnError
    │
    ▼
EasyEtl.OnError event
    │
    ▼
Pipeline cancellation (_cts.Cancel())
    │
    ▼
Other stages terminate gracefully
```

**Error Information:**
```csharp
public class ErrorNotificationEventArgs
{
    public EtlType Type { get; set; }      // Extract, Transform, Load
    public Exception Exception { get; set; }
    public Dictionary<string, object?> CurrentRow { get; set; }
    public long CurrentLine { get; set; }
}
```

### Exception Handling Strategy

1. **Component Level:** Catch, emit `OnError`, rethrow
2. **Pipeline Level:** Catch in `Execute()`, cancel pipeline
3. **Channel Level:** Complete channels with exception
4. **Application Level:** Handle `OnError` event

## Performance Architecture

### Memory Management

**Streaming Design:**
- Data never fully loaded in memory
- Rows processed one at a time
- Channels provide natural backpressure when bounded

**Bounded Channels Example:**
```csharp
// Limit to 1000 rows in flight
var etl = new EasyEtl(extractor, transformer, loader, channelSize: 1000);
```

### Optimization Techniques

1. **Bulk Operations:**
   - `SqlBulkCopy` for SQL Server (10-100x faster)
   - Batched transactions for SQLite
   - Buffered file writes

2. **Lazy Evaluation:**
   - `IAsyncEnumerable` enables pull-based processing
   - Transformer doesn't process until loader pulls

3. **Zero-Copy Patterns:**
   - Extractor uses `ref Dictionary` in callback
   - Avoids unnecessary dictionary copies

4. **Event Throttling:**
   - `RaiseChangeEventAfter` controls event frequency
   - Reduces overhead for large datasets

### Benchmarks

From `tests/Benchmark/`:
- **CSV to SQL:** 100,000 rows in <2 seconds
- **Large Files:** Handles multi-GB files with constant memory
- **Throughput:** 50,000+ rows/second typical

## Extensibility Points

### Adding New Extractors

1. Implement `IDataExtractor`
2. Create configuration class
3. Use existing patterns from `CsvDataExtractor`
4. Add tests

### Adding New Loaders

1. Implement `IDataLoader`
2. Create configuration class
3. Use `SqlDataLoader` as reference for databases
4. Use `CsvDataLoader` as reference for files
5. Add tests

### Adding New Transformers

**Option 1:** Extend `DynamicDataTransformer`
- Add new action types
- Extend `TransformationConfig`

**Option 2:** Create new transformer
- Implement `IDataTransformer`
- Follow async enumerable pattern

### Custom Progress Tracking

Subscribe to events:
```csharp
etl.OnChange += (args) => {
    var progress = args.Progress[EtlType.Global];
    Console.WriteLine($"{progress.PercentComplete:F1}% - {progress.Speed:F0} rows/s");
};
```

## Design Decisions

### Why Channels?

**Alternatives considered:**
- **BlockingCollection:** Not async-friendly
- **TPL Dataflow:** More complex, overkill for linear pipeline
- **Direct method calls:** Couples components tightly

**Channels chosen because:**
- Built for async scenarios
- Simple API
- High performance
- Natural backpressure support

### Why Dictionary<string, object?>?

**Alternatives considered:**
- **Strongly-typed classes:** Requires code generation or runtime compilation
- **DataTable:** Legacy, not async-friendly
- **Arrow/RecordBatch:** Too complex for general use

**Dictionary chosen because:**
- Flexible schema
- Easy to work with
- Good performance for row-by-row
- Supports dynamic transformations

### Why Synchronous Extract?

**Extractor is synchronous but runs on Task.Run:**
- Most data sources (files) are synchronous by nature
- Simpler implementation for extractor authors
- Dedicated thread ensures streaming doesn't block
- Channel writes are async, so no blocking

### Why Three Separate Interfaces?

**Could have used single `IEtlComponent`:**
- Different semantics (sync vs async)
- Different lifecycle (pull vs push)
- Clear separation of concerns
- Easier to test individually

## Future Architecture Considerations

### Potential Enhancements

1. **Parallel Processing:**
   - Multiple transformer tasks
   - Partitioned channels
   - Order preservation challenges

2. **Checkpointing:**
   - Restart from failure point
   - State persistence
   - Idempotency requirements

3. **Schema Evolution:**
   - Track schema changes
   - Automatic migration
   - Backward compatibility

4. **Distributed Processing:**
   - Multiple nodes
   - Work distribution
   - Result aggregation

### Stability Considerations

**Current design is stable for:**
- Single-node processing
- Linear pipelines
- Millions of rows
- Files up to TB scale (streaming)

**Known limitations:**
- No built-in retry logic
- No checkpoint/resume
- Single pipeline (no branching)
- In-process only

---

*This architecture document provides a deep understanding of EasyETL's design for AI agents to effectively contribute to the codebase.*
