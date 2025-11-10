# Zero-Allocation Patterns in EasyETL

**Last Updated:** 2025-11-07  
**Status:** Phase 1 Complete  
**Related:** [user-story-zero-allocation.md](user-story-zero-allocation.md)

---

## Overview

This document describes the zero-allocation structures and patterns implemented in EasyETL to achieve high-performance, CPU-bounded ETL operations with minimal garbage collection pressure.

## Core Components

### 1. FieldType Enum

A byte-sized enumeration representing all supported data types:

```csharp
public enum FieldType : byte
{
    Null, Int32, Int64, Double, Float, Boolean, 
    DateTime, String, Decimal, Int16, Byte, Guid
}
```

**Purpose:** Type identification without boxing  
**Size:** 1 byte  
**Location:** `Library.Infra.ZeroAlloc.FieldType`

### 2. FieldDescriptor Struct

Describes field layout within an EtlRecord buffer:

```csharp
public struct FieldDescriptor
{
    public string Name { get; set; }
    public int Offset { get; set; }
    public int Length { get; set; }
    public FieldType Type { get; set; }
    public int Index { get; set; }
}
```

**Purpose:** Field metadata for zero-copy access  
**Location:** `Library.Infra.ZeroAlloc.FieldDescriptor`

### 3. FieldValue Union Type

A discriminated union for type-safe value storage without boxing:

```csharp
[StructLayout(LayoutKind.Explicit)]
public struct FieldValue
{
    [FieldOffset(0)] private FieldType _type;
    [FieldOffset(8)] private int _int32Value;
    [FieldOffset(8)] private long _int64Value;
    [FieldOffset(8)] private double _doubleValue;
    // ... other fields share the same memory via FieldOffset(8)
}
```

**Key Features:**
- **Zero Boxing:** Value types stored without heap allocation
- **Type Safety:** Runtime type checking via `Type` property
- **Memory Efficient:** All numeric types share 8-16 bytes via explicit layout
- **Factory Methods:** `FromInt32()`, `FromDouble()`, etc.
- **Accessors:** `AsInt32()`, `AsDouble()`, etc. with type validation

**Example:**
```csharp
// No boxing occurs
var value = FieldValue.FromInt32(42);
int result = value.AsInt32(); // No unboxing

// Traditional approach (causes boxing)
object boxed = 42; // Boxing allocation
int unboxed = (int)boxed; // Unboxing
```

**Location:** `Library.Infra.ZeroAlloc.FieldValue`

### 4. EtlRecord Ref Struct

A stack-only record structure using Span-based APIs:

```csharp
public ref struct EtlRecord
{
    private Span<byte> _buffer;
    private ReadOnlySpan<FieldDescriptor> _schema;
    
    public FieldValue GetValue(int index);
    public void SetValue(int index, FieldValue value);
}
```

**Key Features:**
- **Stack-Only:** `ref struct` ensures no heap allocation
- **Span-Based:** Zero-copy field access via `Span<byte>`
- **Type-Safe:** Validates types on get/set operations
- **Efficient:** `AggressiveInlining` on hot path methods
- **Legacy Compatible:** `ToDictionary()` / `FromDictionary()` adapters

**Example:**
```csharp
var schema = EtlRecordPool.CreateSchema(
    ("Id", FieldType.Int32),
    ("Name", FieldType.String),
    ("Age", FieldType.Int32)
);

var buffer = new byte[512];
var record = new EtlRecord(buffer, schema);

// Zero-copy field access
record.SetValue("Id", FieldValue.FromInt32(123));
record.SetValue("Name", FieldValue.FromString("Alice"));

var id = record.GetValue("Id").AsInt32();
var name = record.GetValue("Name").AsString();
```

**Location:** `Library.Infra.ZeroAlloc.EtlRecord`

### 5. EtlRecordPool

Pool-based resource management using ArrayPool:

```csharp
public sealed class EtlRecordPool
{
    public byte[] RentBuffer(int minimumSize = 0);
    public void ReturnBuffer(byte[] buffer, bool clearArray = true);
    
    public FieldDescriptor[] RentSchema(int fieldCount);
    public void ReturnSchema(FieldDescriptor[] schema, bool clearArray = false);
    
    public static FieldDescriptor[] CreateSchema(params (string Name, FieldType Type)[] fields);
}
```

**Key Features:**
- **ArrayPool Integration:** Reuses buffers across operations
- **Automatic Sizing:** Calculates buffer requirements from schema
- **Context Management:** `using` pattern for automatic cleanup
- **Schema Builder:** Auto-layout field descriptors with offset calculation

**Example:**
```csharp
var pool = new EtlRecordPool();

// Manual management
var buffer = pool.RentBuffer(512);
try
{
    var record = new EtlRecord(buffer, schema);
    // Use record...
}
finally
{
    pool.ReturnBuffer(buffer);
}

// Automatic management (recommended)
using var context = pool.CreateBufferContext(512);
var record = new EtlRecord(context.AsSpan(), schema);
// Buffer automatically returned when context disposes
```

**Location:** `Library.Infra.ZeroAlloc.EtlRecordPool`

## Performance Characteristics

### Baseline (Traditional Dictionary Approach)

```csharp
// Per 10,000 rows with 7 fields each
var row = new Dictionary<string, object?>(); // Allocation: ~144 bytes
row["Id"] = 42;        // Boxing: 16 bytes
row["Age"] = 30;       // Boxing: 16 bytes
row["Salary"] = 75000.0; // Boxing: 24 bytes
// Total per row: ~200+ bytes
// Total for 10K rows: ~2+ MB allocated
```

**Issues:**
- Dictionary allocation per row
- Boxing of all value types
- Gen0 collections every few thousand rows
- CPU time spent in GC

### Zero-Allocation Approach

```csharp
// Pool creates reusable buffer once
var buffer = pool.RentBuffer(512); // ~512 bytes rented from pool

// Process 10,000 rows
for (int i = 0; i < 10000; i++)
{
    var record = new EtlRecord(buffer, schema); // Stack-only, no allocation
    record.SetValue(0, FieldValue.FromInt32(i)); // No boxing
    record.SetValue(1, FieldValue.FromInt32(30)); // No boxing
    record.SetValue(2, FieldValue.FromDouble(75000.0)); // No boxing
    // Total allocation: 0 bytes (buffer reused)
}

pool.ReturnBuffer(buffer); // Buffer returned to pool
// Total for 10K rows: ~0 bytes allocated (pool overhead only)
```

**Benefits:**
- **Zero allocations** in hot path
- **No boxing/unboxing** overhead
- **Minimal GC pressure** (Gen0 collections rare)
- **CPU-bound** processing

### Benchmark Results

Run `dotnet run --project tests/Benchmark/Benchmark.csproj -c Release` to see actual measurements.

Expected improvements:
- **Allocations:** 99%+ reduction (MB → KB)
- **Throughput:** 2-3x faster for large datasets
- **GC Time:** 90%+ reduction
- **Latency:** More consistent (no GC pauses)

## Usage Patterns

### Pattern 1: Simple Field Access

```csharp
var pool = new EtlRecordPool();
var schema = EtlRecordPool.CreateSchema(
    ("Id", FieldType.Int32),
    ("Name", FieldType.String)
);

using var context = pool.CreateBufferContext();
var record = new EtlRecord(context.AsSpan(), schema);

record.SetValue("Id", FieldValue.FromInt32(123));
record.SetValue("Name", FieldValue.FromString("Alice"));

var id = record.GetValue("Id").AsInt32();
var name = record.GetValue("Name").AsString();
```

### Pattern 2: Legacy Compatibility

```csharp
// Convert from Dictionary to EtlRecord
var dict = new Dictionary<string, object?> { ["Id"] = 123 };
var record = EtlRecord.FromDictionary(buffer, schema, dict);

// Convert back to Dictionary
var newDict = record.ToDictionary();
```

### Pattern 3: Batch Processing

```csharp
var pool = new EtlRecordPool();
var schema = EtlRecordPool.CreateSchema(/* ... */);
var buffer = pool.RentBuffer();

try
{
    foreach (var row in dataSource)
    {
        var record = new EtlRecord(buffer, schema);
        
        // Populate from source
        record.SetValue(0, FieldValue.FromInt32(row.Id));
        record.SetValue(1, FieldValue.FromString(row.Name));
        
        // Process
        ProcessRecord(ref record);
        
        // Buffer is reused for next iteration
    }
}
finally
{
    pool.ReturnBuffer(buffer);
}
```

### Pattern 4: Schema Building

```csharp
// Automatic layout with calculated offsets
var schema = EtlRecordPool.CreateSchema(
    ("Id", FieldType.Int32),        // Offset: 0, Length: 4
    ("Name", FieldType.String),     // Offset: 4, Length: 256
    ("Age", FieldType.Int32),       // Offset: 260, Length: 4
    ("Salary", FieldType.Double)    // Offset: 264, Length: 8
);

var bufferSize = EtlRecordPool.CalculateBufferSize(schema); // 272 bytes
```

## Best Practices

### ✅ DO

1. **Use `ref struct` for transient data**
   - EtlRecord is designed for pipeline processing
   - Don't store in fields or return from methods

2. **Reuse buffers**
   - Rent once, use for multiple records
   - Return to pool when done

3. **Use `using` for automatic cleanup**
   ```csharp
   using var context = pool.CreateBufferContext();
   ```

4. **Profile allocation**
   - Use BenchmarkDotNet's `[MemoryDiagnoser]`
   - Verify zero allocations in hot paths

5. **Pre-calculate buffer sizes**
   ```csharp
   var size = EtlRecordPool.CalculateBufferSize(schema);
   var buffer = pool.RentBuffer(size);
   ```

### ❌ DON'T

1. **Don't capture `ref struct` in lambdas/closures**
   ```csharp
   // ERROR: Cannot use ref local inside lambda
   var record = new EtlRecord(buffer, schema);
   someList.ForEach(x => record.SetValue(/* ... */)); // Won't compile
   ```

2. **Don't store `ref struct` in fields**
   ```csharp
   // ERROR: Cannot use ref struct as field type
   class MyClass
   {
       private EtlRecord _record; // Won't compile
   }
   ```

3. **Don't forget to return buffers**
   ```csharp
   // BAD: Buffer never returned
   var buffer = pool.RentBuffer();
   // ... use buffer ...
   // Missing: pool.ReturnBuffer(buffer);
   ```

4. **Don't mix pool instances**
   ```csharp
   // BAD: Rented from pool1, returned to pool2
   var buffer = pool1.RentBuffer();
   pool2.ReturnBuffer(buffer); // Wrong pool!
   ```

## Type Size Reference

| Type      | Size (bytes) | Notes                    |
|-----------|--------------|--------------------------|
| Null      | 0            | No storage needed        |
| Byte      | 1            |                          |
| Boolean   | 1            | 0 = false, 1 = true      |
| Int16     | 2            |                          |
| Int32     | 4            |                          |
| Float     | 4            |                          |
| Int64     | 8            |                          |
| Double    | 8            |                          |
| DateTime  | 8            | Stored as ticks          |
| Decimal   | 16           | 4 int32 components       |
| Guid      | 16           |                          |
| String    | 256          | Default UTF-8 buffer     |

## Testing

All zero-alloc structures have comprehensive unit tests:

```bash
# Run all ZeroAlloc tests
dotnet test --filter "FullyQualifiedName~ZeroAlloc"

# Run benchmarks
dotnet run --project tests/Benchmark/Benchmark.csproj -c Release
```

**Test Coverage:**
- FieldValue: 45 tests
- EtlRecord: 18 tests
- EtlRecordPool: 26 tests
- **Total:** 89 tests

## Future Work

- [ ] **Phase 2:** Integrate with EasyEtl pipeline
- [ ] **Phase 3:** Update extractors to use EtlRecord
- [ ] **Phase 4:** Update transformers to use EtlRecord
- [ ] **Phase 5:** Update loaders to use EtlRecord
- [ ] **Optimization:** SIMD operations for field copying
- [ ] **Optimization:** String interning for common values
- [ ] **Optimization:** Custom allocator for very large datasets

## References

- [Memory\<T\> and Span\<T\> usage guidelines](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/)
- [High-performance C# collections](https://learn.microsoft.com/en-us/dotnet/standard/collections/high-performance)
- [ArrayPool\<T\> class](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1)
- [ref struct (C# Reference)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct)

---

**Author:** GitHub Copilot AI Agent  
**Reviewers:** Pending  
**Version:** 1.0
