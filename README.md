# EasyETL

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Code Coverage](https://img.shields.io/badge/coverage-62.8%25-yellow)](./coverage/report/index.html)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/lincolnqjunior/easy-etl)
[![Tests](https://img.shields.io/badge/tests-375%2F375-brightgreen)](./tests/UnitTests)

A high-performance .NET 8.0 library for building ETL (Extract, Transform, Load) pipelines with two architecture options:

- **V1**: Traditional Dictionary-based approach for simplicity and flexibility
- **V2**: Zero-allocation optimized architecture for maximum performance

Both architectures coexist, allowing you to choose the right balance between simplicity and performance for your use case.

## 🚀 Features

### Core Capabilities
- **Streaming Architecture**: Process data as it flows, never load entire datasets in memory
- **Event-Driven**: Observable progress and errors at every stage
- **Composable**: Mix and match extractors, transformers, and loaders
- **Type-Safe**: Strong typing with nullable reference types

### V1 Architecture (Dictionary-based)
- **Simple API**: Easy to understand and use
- **Schema Flexibility**: Dynamic column mapping at runtime
- **Full Feature Set**: All extractors, transformers, and loaders available
- **Production Ready**: Stable and well-tested

### V2 Architecture (Zero-Allocation)
- **High Performance**: 2.1x faster than V1 for large datasets
- **Memory Efficient**: 98% reduction in allocations (2.4MB → 48KB per 10K rows)
- **Minimal GC Pressure**: 95% fewer Gen0 collections
- **Span-based APIs**: Modern .NET performance patterns
- **Status**: Extractors and Bypass Transformer complete; Loaders in development

## 📊 Supported Data Sources

### Extractors (V1 & V2)
- ✅ **CSV** - High-performance parsing with Sep library (98.7% coverage)
- ✅ **JSON/JSONL** - Streaming with System.Text.Json
- ✅ **SQL Server** - Modern async with Microsoft.Data.SqlClient
- ✅ **SQLite** - In-memory or file-based
- ✅ **Parquet** - Columnar format support

### Loaders (V1 only)
- ✅ **CSV** - Buffered writes (92.5% coverage)
- ✅ **JSON/JSONL** - Zero-allocation serialization (84.8% coverage)
- ✅ **SQL Server** - Bulk insert with SqlBulkCopy
- ✅ **SQLite** - Transaction-based batch inserts
- ⏳ **V2 Loaders** - In development

### Transformers
- ✅ **Bypass** - Pass-through with no modifications (V1 & V2, 100% coverage)
- ✅ **Dynamic** - Rule-based transformations (V1 only, 100% coverage)
- ⏳ **Dynamic V2** - In development

## 📦 Installation

```bash
dotnet add package EasyETL
```

## 🎯 Quick Start

### V1 API (Dictionary-based - Recommended for most use cases)

```csharp
using Library;
using Library.Extractors.Csv;
using Library.Loaders.Json;

// Configure extractor
var extractorConfig = new CsvDataExtractorConfig 
{ 
    FilePath = "input.csv",
    HasHeader = true
};

// Configure loader
var loaderConfig = new JsonDataLoaderConfig 
{ 
    OutputPath = "output.jsonl",
    IsJsonl = true
};

// Create pipeline
var extractor = new CsvDataExtractor(extractorConfig);
var loader = new JsonDataLoader(loaderConfig);
var etl = new EasyEtl(extractor, loader);

// Execute with progress tracking
etl.OnChange += (args) => 
{
    var progress = args.Progress[EtlType.Global];
    Console.WriteLine($"{progress.PercentComplete:F1}% - {progress.Speed:F0} rows/s");
};

await etl.Execute();
```

### V2 API (Zero-allocation - For high-performance scenarios)

```csharp
using Library;
using Library.Extractors.Csv;
using Library.Transformers;
using Library.Infra.ZeroAlloc;

// Configure V2 extractor
var extractorConfig = new CsvDataExtractorConfig 
{ 
    FilePath = "input.csv",
    HasHeader = true
};

// Create V2 pipeline
var extractor = new CsvDataExtractorV2(extractorConfig);
var transformer = new BypassDataTransformerV2(extractor.Schema);
// Note: V2 loaders not yet available - use V1 loaders with adapter

var etl = new EasyEtlV2(extractor, transformer, /* loader */);

await etl.Execute();
```

## 📈 Performance

### V2 Benchmark Results (CSV Extractor, 10K rows)

| Metric | V1 (Dictionary) | V2 (Zero-Alloc) | Improvement |
|--------|----------------|-----------------|-------------|
| **Mean Time** | 48.23 ms | 22.87 ms | **2.11x faster** |
| **Allocations** | 2,485 KB | 48 KB | **98% reduction** |
| **Gen0 Collections** | 42 | 2 | **95% fewer** |
| **Gen1 Collections** | 14 | 0 | **100% fewer** |
| **Throughput** | 207K rows/s | 437K rows/s | **2.11x faster** |

See [benchmark-results-csv.md](./ai_docs/benchmark-results-csv.md) for detailed analysis.

### Current Test Coverage
- **Total Tests**: 375 (100% passing)
- **V1 Components**: Fully tested
- **V2 ZeroAlloc Structures**: 89 tests
- **V2 Extractors**: 15 tests  
- **V2 Pipeline**: 21 integration tests

### Zero-Allocation Roadmap

See [implementation-status.md](./ai_docs/implementation-status.md) for complete progress:
- ✅ **Phase 1**: Foundation (100%) - ZeroAlloc structures
- ✅ **Phase 2**: Pipeline (100%) - EasyEtlV2, interfaces
- ✅ **Phase 3**: Extractors (100%) - All 5 extractors V2
- 🟡 **Phase 4**: Transformers (50%) - Bypass complete, Dynamic pending
- ⏳ **Phase 5**: Loaders (0%) - In development
- ⏳ **Phases 6-8**: Optimization, documentation, validation

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator -reports:"./coverage/*/coverage.cobertura.xml" \
                -targetdir:"./coverage/report" \
                -reporttypes:"Html;TextSummary"
```

### Current Test Coverage

| Component | Coverage | Status |
|-----------|----------|--------|
| CsvDataExtractor (V1) | 98.7% | ✅ Excellent |
| CsvDataLoader (V1) | 92.5% | ✅ Excellent |
| JsonDataLoader (V1) | 84.8% | ✅ Good |
| DynamicDataTransformer (V1) | 100% | ✅ Perfect |
| BypassDataTransformer (V1) | 100% | ✅ Perfect |
| ZeroAlloc Structures (V2) | 100% | ✅ Perfect (89 tests) |
| V2 Extractors | 100% | ✅ All 5 tested (15 tests) |
| V2 Pipeline | 100% | ✅ Integration tests (21 tests) |
| Database Extractors | Low | ⚠️ Basic tests only |
| Database Loaders | Low | ⚠️ Basic tests only |

## 📚 Documentation

- **[Implementation Status](./ai_docs/implementation-status.md)** - Current progress and metrics
- **[Zero-Allocation Patterns](./ai_docs/zero-allocation-patterns.md)** - V2 usage guide
- **[Benchmark Results](./ai_docs/benchmark-results-csv.md)** - Detailed performance analysis
- **[User Story - Zero Allocation](./ai_docs/user-story-zero-allocation.md)** - Original refactoring plan
- **[Architecture Guide](./ai_docs/architecture.md)** - Deep dive into design
- **[Development Guide](./ai_docs/development-guide.md)** - How to contribute
- **[API Reference](./ai_docs/api-reference.md)** - Complete API documentation

## 🏗️ Architecture

### V1 Architecture (Dictionary-based)
```
┌─────────────────────────────────────────────────────┐
│                    EasyEtl (V1)                     │
│              (Pipeline Orchestrator)                │
│                                                     │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐       │
│  │Extractor │──▶│Transformer│──▶│  Loader  │       │
│  └──────────┘   └──────────┘   └──────────┘       │
│                                                     │
│     Channels (Dictionary<string, object?>)          │
│     ↓           ↓            ↓                      │
│  Extract    Transform      Load                    │
│  (Thread)   (Async)       (Async)                  │
└─────────────────────────────────────────────────────┘
```

### V2 Architecture (Zero-Allocation)
```
┌─────────────────────────────────────────────────────┐
│                   EasyEtlV2 (V2)                    │
│           (Zero-Allocation Pipeline)                │
│                                                     │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐       │
│  │ExtractorV2│─▶│TransformerV2│─▶│ LoaderV2│       │
│  └──────────┘   └──────────┘   └──────────┘       │
│                                                     │
│  Channels (ref EtlRecord) + EtlRecordPool           │
│     ↓           ↓            ↓                      │
│  Extract    Transform      Load                    │
│  (Span)     (ref)         (Span)                   │
└─────────────────────────────────────────────────────┘
```

### Key Design Principles
1. **Streaming**: Data flows through channels, not loaded in memory
2. **Asynchronous**: All I/O operations are async
3. **Event-driven**: Progress and errors reported via events
4. **Type-safe**: Strong typing with nullable reference types
5. **Configurable**: JSON-based configuration for all components
6. **V2**: Zero-copy with Span<T>, ArrayPool<T>, and ref structs

## 🤝 Contributing

We welcome contributions! Please see our [Development Guide](./ai_docs/development-guide.md) for:
- Build and test commands
- Code style guidelines
- Testing patterns
- Performance considerations

### Quick Contribution Guide

1. Fork the repository
2. Create a feature branch
3. Write tests (target: 90% coverage)
4. Ensure all tests pass
5. Submit a pull request

## 📋 Requirements

- .NET 8.0 or higher
- C# 12 with nullable reference types

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

Built with modern .NET libraries:
- **[Sep](https://github.com/nietras/Sep)** v0.11.4 - High-performance CSV parsing
- **System.Text.Json** - Zero-allocation JSON processing
- **[Parquet.Net](https://github.com/aloneguid/parquet-dotnet)** v5.3.0 - Parquet file support
- **Microsoft.Data.SqlClient** v6.0.0 - Modern SQL Server client
- **ArrayPool<T>** - Memory pooling for zero-allocation

---

**Status**: Active Development  
**Version**: 1.0.0-preview  
**Last Updated**: November 10, 2025

For detailed implementation status and roadmap, see [Implementation Status](./ai_docs/implementation-status.md).
