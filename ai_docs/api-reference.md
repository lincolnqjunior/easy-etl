# EasyETL API Reference

## Core Classes

### EasyEtl

**Namespace:** `Library`  
**Location:** `src/Library/EasyEtl.cs`

Main orchestrator class that manages the ETL pipeline.

#### Constructors

```csharp
public EasyEtl(IDataExtractor extractor, IDataTransformer transformer, IDataLoader loader, int channelSize = 0)
```
Creates an ETL pipeline with all three components.

**Parameters:**
- `extractor` - Component for data extraction
- `transformer` - Component for data transformation
- `loader` - Component for data loading
- `channelSize` - Channel buffer size (0 = unbounded, >0 = bounded)

```csharp
public EasyEtl(IDataExtractor extractor, IDataLoader loader, int channelSize = 0)
```
Creates an ETL pipeline without transformation (uses BypassDataTransformer).

#### Properties

```csharp
public readonly IDataExtractor Extractor
```
The extractor component.

```csharp
public readonly IDataTransformer Transformer
```
The transformer component.

```csharp
public readonly IDataLoader Loader
```
The loader component.

#### Events

```csharp
public event EasyEtlProgressEventHandler? OnChange
```
Raised when progress updates occur in any stage.

```csharp
public event EasyEtlProgressEventHandler? OnComplete
```
Raised when the entire pipeline completes successfully.

```csharp
public event EasyEtlErrorEventHandler? OnError
```
Raised when an error occurs in any stage.

#### Methods

```csharp
public async Task Execute()
```
Starts the ETL pipeline asynchronously. Runs extract, transform, and load stages in parallel.

**Throws:**
- Various exceptions depending on component failures

## Interfaces

### IDataExtractor

**Namespace:** `Library.Extractors`  
**Location:** `src/Library/Extractors/IDataExtractor.cs`

Interface for data extraction components.

#### Events

```csharp
event ReadNotification? OnRead
```
Raised periodically during extraction.

```csharp
event ReadNotification? OnFinish
```
Raised when extraction completes.

```csharp
event EasyEtlErrorEventHandler OnError
```
Raised when an error occurs during extraction.

#### Properties

```csharp
long TotalLines { get; set; }
```
Total number of lines/records to extract (if known).

```csharp
int LineNumber { get; set; }
```
Current line number being processed.

```csharp
long BytesRead { get; set; }
```
Number of bytes read from source.

```csharp
double PercentRead { get; set; }
```
Percentage of data read (0-100).

```csharp
long FileSize { get; set; }
```
Total size of source in bytes (if applicable).

#### Methods

```csharp
void Extract(RowAction processRow)
```
Extracts data from source and processes each row via callback.

**Parameters:**
- `processRow` - Callback delegate to process each extracted row

### IDataTransformer

**Namespace:** `Library.Transformers`  
**Location:** `src/Library/Transformers/IDataTransformer.cs`

Interface for data transformation components.

#### Events

```csharp
event TransformNotificationHandler OnTransform
```
Raised periodically during transformation.

```csharp
event TransformNotificationHandler OnFinish
```
Raised when transformation completes.

```csharp
event EasyEtlErrorEventHandler OnError
```
Raised when an error occurs during transformation.

#### Properties

```csharp
long IngestedLines { get; set; }
```
Number of input lines received.

```csharp
long TransformedLines { get; set; }
```
Number of output lines produced.

```csharp
long ExcludedByFilter { get; set; }
```
Number of lines filtered out.

```csharp
double PercentDone { get; set; }
```
Percentage of transformation complete (0-100).

```csharp
long TotalLines { get; set; }
```
Total lines expected.

#### Methods

```csharp
IAsyncEnumerable<Dictionary<string, object?>> Transform(
    IAsyncEnumerable<Dictionary<string, object?>> data, 
    CancellationToken cancellationToken)
```
Transforms input data stream asynchronously.

**Parameters:**
- `data` - Input data stream
- `cancellationToken` - Cancellation token

**Returns:** Transformed data stream

```csharp
List<Dictionary<string, object?>> ApplyTransformations(Dictionary<string, object?> item)
```
Applies transformations to a single item.

**Parameters:**
- `item` - Input row

**Returns:** List of transformed rows (can be 0, 1, or N rows)

### IDataLoader

**Namespace:** `Library.Loaders`  
**Location:** `src/Library/Loaders/IDataLoader.cs`

Interface for data loading components.

#### Events

```csharp
event LoadNotificationHandler? OnWrite
```
Raised periodically during loading.

```csharp
event LoadNotificationHandler? OnFinish
```
Raised when loading completes.

```csharp
event EasyEtlErrorEventHandler OnError
```
Raised when an error occurs during loading.

#### Properties

```csharp
long CurrentLine { get; set; }
```
Current line being written.

```csharp
long TotalLines { get; set; }
```
Total lines expected.

```csharp
double PercentWritten { get; set; }
```
Percentage of data written (0-100).

#### Methods

```csharp
Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)
```
Loads transformed data to destination asynchronously.

**Parameters:**
- `data` - Transformed data stream
- `cancellationToken` - Cancellation token

## Extractor Implementations

### CsvDataExtractor

**Namespace:** `Library.Extractors.Csv`  
**Location:** `src/Library/Extractors/Csv/CsvDataExtractor.cs`

Extracts data from CSV files using the high-performance `Sep` library.

#### Constructor

```csharp
public CsvDataExtractor(CsvDataExtractorConfig config)
```

#### Configuration

**CsvDataExtractorConfig Properties:**
- `string FilePath` - Path to CSV file
- `bool HasHeader` - Whether file has header row
- `char Delimiter` - Field delimiter (default: ',')
- `int RaiseChangeEventAfter` - Event frequency
- `List<IColumnAction> Columns` - Column definitions

### JsonDataExtractor

**Namespace:** `Library.Extractors.Json`  
**Location:** `src/Library/Extractors/Json/JsonDataExtractor.cs`

Extracts data from JSON/JSONL files using streaming parser.

#### Constructor

```csharp
public JsonDataExtractor(JsonDataExtractorConfig config)
```

#### Configuration

**JsonDataExtractorConfig Properties:**
- `string FilePath` - Path to JSON/JSONL file
- `int RaiseChangeEventAfter` - Event frequency

### SqlDataExtractor

**Namespace:** `Library.Extractors.SQL`  
**Location:** `src/Library/Extractors/SQL/SqlDataExtractor.cs`

Extracts data from SQL Server databases.

#### Constructor

```csharp
public SqlDataExtractor(DatabaseDataExtractorConfig config)
```

#### Configuration

**DatabaseDataExtractorConfig Properties:**
- `string ConnectionString` - SQL Server connection string
- `string Query` - SQL query to execute
- `int RaiseChangeEventAfter` - Event frequency

### SqliteDataExtractor

**Namespace:** `Library.Extractors.SQLite`  
**Location:** `src/Library/Extractors/SQLite/SqliteDataExtractor.cs`

Extracts data from SQLite databases.

#### Constructor

```csharp
public SqliteDataExtractor(DatabaseDataExtractorConfig config)
```

### ParquetDataExtractor

**Namespace:** `Library.Extractors.Parquet`  
**Location:** `src/Library/Extractors/Parquet/ParquetDataExtractor.cs`

Extracts data from Parquet files.

#### Constructor

```csharp
public ParquetDataExtractor(ParquetDataExtractorConfig config)
```

#### Configuration

**ParquetDataExtractorConfig Properties:**
- `string FilePath` - Path to Parquet file
- `int RaiseChangeEventAfter` - Event frequency

## Transformer Implementations

### BypassDataTransformer

**Namespace:** `Library.Transformers`  
**Location:** `src/Library/Transformers/BypassDataTransformer.cs`

Pass-through transformer that performs no modifications.

#### Constructor

```csharp
public BypassDataTransformer()
```

No configuration needed.

### DynamicDataTransformer

**Namespace:** `Library.Transformers`  
**Location:** `src/Library/Transformers/DynamicDataTransformer.cs`

Rule-based transformer with conditional logic and field mapping.

#### Constructor

```csharp
public DynamicDataTransformer(TransformationConfig config)
```

#### Configuration

**TransformationConfig Properties:**
- `int RaiseChangeEventAfter` - Event frequency
- `List<Transformation> Transformations` - List of transformation rules

**Transformation Properties:**
- `string Condition` - C# expression (e.g., "row[\"Age\"] > 18")
- `List<TransformationAction> Actions` - Actions to apply

**TransformationAction Properties:**
- `Dictionary<string, FieldMapping> FieldMappings` - Field mappings

**FieldMapping Properties:**
- `string Value` - Value or expression
- `bool IsDynamic` - Whether Value is an expression

## Loader Implementations

### CsvDataLoader

**Namespace:** `Library.Loaders.Csv`  
**Location:** `src/Library/Loaders/Csv/CsvDataLoader.cs`

Loads data to CSV files.

#### Constructor

```csharp
public CsvDataLoader(CsvDataLoaderConfig config)
```

#### Configuration

**CsvDataLoaderConfig Properties:**
- `string FilePath` - Output file path
- `char Delimiter` - Field delimiter (default: ',')
- `int RaiseChangeEventAfter` - Event frequency
- `bool WriteHeader` - Whether to write header row

### JsonDataLoader

**Namespace:** `Library.Loaders.Json`  
**Location:** `src/Library/Loaders/Json/JsonDataLoader.cs`

Loads data to JSONL files (one JSON object per line).

#### Constructor

```csharp
public JsonDataLoader(JsonDataLoaderConfig config)
```

#### Configuration

**JsonDataLoaderConfig Properties:**
- `string FilePath` - Output file path
- `int RaiseChangeEventAfter` - Event frequency

### SqlDataLoader

**Namespace:** `Library.Loaders.SQL`  
**Location:** `src/Library/Loaders/SQL/SqlDataLoader.cs`

Loads data to SQL Server using bulk insert.

#### Constructor

```csharp
public SqlDataLoader(DatabaseDataLoaderConfig config)
```

#### Configuration

**DatabaseDataLoaderConfig Properties:**
- `string ConnectionString` - SQL Server connection string
- `string TableName` - Target table name
- `int RaiseChangeEventAfter` - Event frequency
- `int BatchSize` - Bulk insert batch size (default: 1000)

### SqliteDataLoader

**Namespace:** `Library.Loaders.SQLite`  
**Location:** `src/Library/Loaders/SQLite/SqliteDataLoader.cs`

Loads data to SQLite using transaction-based batch inserts.

#### Constructor

```csharp
public SqliteDataLoader(DatabaseDataLoaderConfig config)
```

## Infrastructure Classes

### EtlDataProgress

**Namespace:** `Library.Infra`  
**Location:** `src/Library/Infra/EtlDataProgress.cs`

Represents progress information for an ETL stage.

#### Properties

```csharp
public long CurrentLine { get; set; }
```
Current line/row number.

```csharp
public long TotalLines { get; set; }
```
Total expected lines.

```csharp
public double PercentComplete { get; set; }
```
Completion percentage (0-100).

```csharp
public EtlStatus Status { get; set; }
```
Status: Running, Completed, or Failed.

```csharp
public double Speed { get; set; }
```
Processing speed in rows per second.

```csharp
public TimeSpan EstimatedTimeToEnd { get; set; }
```
Estimated time remaining.

### EasyEtlTelemetry

**Namespace:** `Library.Infra`  
**Location:** `src/Library/Infra/EasyEtlTelemetry.cs`

Aggregates progress from all pipeline stages.

#### Constructor

```csharp
public EasyEtlTelemetry(EasyEtl etl)
```

#### Events

```csharp
public event EasyEtlProgressEventHandler? OnChange
```
Raised when any stage reports progress.

```csharp
public event EasyEtlErrorEventHandler? OnError
```
Raised when any stage reports an error.

#### Properties

```csharp
public Dictionary<EtlType, EtlDataProgress> Progress { get; }
```
Progress for each stage (Extract, Transform, Load, Global).

## Event Arguments

### EasyEtlNotificationEventArgs

**Namespace:** `Library.Infra.EventArgs`  
**Location:** `src/Library/Infra/EventArgs/EasyEtlNotificationEventArgs.cs`

Event arguments for progress notifications.

#### Properties

```csharp
public Dictionary<EtlType, EtlDataProgress> Progress { get; }
```
Progress information for all stages.

### ErrorNotificationEventArgs

**Namespace:** `Library.Infra.EventArgs`  
**Location:** `src/Library/Infra/EventArgs/ErrorNotificationEventArgs.cs`

Event arguments for error notifications.

#### Properties

```csharp
public EtlType Type { get; }
```
Stage where error occurred.

```csharp
public Exception Exception { get; }
```
The exception that occurred.

```csharp
public Dictionary<string, object?> CurrentRow { get; }
```
Row being processed when error occurred.

```csharp
public long CurrentLine { get; }
```
Line number where error occurred.

### ExtractNotificationEventArgs

**Namespace:** `Library.Infra.EventArgs`  
**Location:** `src/Library/Infra/EventArgs/ExtractNotificationEventArgs.cs`

Event arguments for extraction progress.

#### Constructor

```csharp
public ExtractNotificationEventArgs(IDataExtractor extractor)
```

### TransformNotificationEventArgs

**Namespace:** `Library.Infra.EventArgs`  
**Location:** `src/Library/Infra/EventArgs/TransformNotificationEventArgs.cs`

Event arguments for transformation progress.

#### Constructor

```csharp
public TransformNotificationEventArgs(IDataTransformer transformer)
```

### LoadNotificationEventArgs

**Namespace:** `Library.Infra.EventArgs`  
**Location:** `src/Library/Infra/EventArgs/LoadNotificationEventArgs.cs`

Event arguments for loading progress.

#### Constructor

```csharp
public LoadNotificationEventArgs(IDataLoader loader)
```

## Enumerations

### EtlType

**Namespace:** `Library.Infra`

Pipeline stage identifier.

#### Values

- `Extract` - Extraction stage
- `Transform` - Transformation stage
- `Load` - Loading stage
- `Global` - Overall pipeline

### EtlStatus

**Namespace:** `Library.Infra`

Processing status.

#### Values

- `Running` - Currently processing
- `Completed` - Completed successfully
- `Failed` - Failed with error

## Delegates

### RowAction

**Namespace:** `Library.Extractors`

```csharp
public delegate void RowAction(ref Dictionary<string, object?> row)
```

Callback for processing extracted rows.

**Parameters:**
- `row` - The row data (passed by reference)

### ReadNotification

**Namespace:** `Library.Infra.EventArgs`

```csharp
public delegate void ReadNotification(ExtractNotificationEventArgs args)
```

Event handler for extraction notifications.

### TransformNotificationHandler

**Namespace:** `Library.Infra.EventArgs`

```csharp
public delegate void TransformNotificationHandler(TransformNotificationEventArgs args)
```

Event handler for transformation notifications.

### LoadNotificationHandler

**Namespace:** `Library.Infra.EventArgs`

```csharp
public delegate void LoadNotificationHandler(LoadNotificationEventArgs args)
```

Event handler for loading notifications.

### EasyEtlProgressEventHandler

**Namespace:** `Library.Infra.EventArgs`

```csharp
public delegate void EasyEtlProgressEventHandler(EasyEtlNotificationEventArgs args)
```

Event handler for pipeline progress notifications.

### EasyEtlErrorEventHandler

**Namespace:** `Library.Infra.EventArgs`

```csharp
public delegate void EasyEtlErrorEventHandler(ErrorNotificationEventArgs args)
```

Event handler for error notifications.

## Column Actions

### IColumnAction

**Namespace:** `Library.Infra.ColumnActions`  
**Location:** `src/Library/Infra/ColumnActions/IColumnAction.cs`

Interface for column mapping/parsing actions.

#### Methods

```csharp
void ParseColumn(ReadOnlySpan<char> colValue, ref Dictionary<string, object?> row)
```

### DefaultColumnAction

**Namespace:** `Library.Infra.ColumnActions`

Reads column value as-is without type conversion.

#### Properties

- `int Position` - Column position
- `string OutputName` - Output field name
- `Type OutputType` - Output field type

### ParseColumnAction

**Namespace:** `Library.Infra.ColumnActions`

Reads and parses column value to specified type.

#### Supported Types

- System.Int32
- System.Int64
- System.Double
- System.Decimal
- System.DateTime
- System.Boolean
- System.Guid
- System.String

## Utility Classes

### DynamicEvaluator

**Namespace:** `Library.Infra.Helpers`  
**Location:** `src/Library/Infra/Helpers/DynamicEvaluator.cs`

Evaluates dynamic expressions using Z.Expressions.Eval.

#### Methods

```csharp
public static object? EvaluateDynamicValue(string expression, Dictionary<string, object?> row)
```

Evaluates expression in context of row data.

**Parameters:**
- `expression` - C# expression string
- `row` - Row data context

**Returns:** Evaluated result

### AsyncEnumerableDataReader

**Namespace:** `Library.Infra.Helpers`  
**Location:** `src/Library/Infra/Helpers/AsyncEnumerableDataReader.cs`

Adapts `IAsyncEnumerable` to `IDataReader` for bulk operations.

---

*Complete API reference for AI agents working with EasyETL.*
