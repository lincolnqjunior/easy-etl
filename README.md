# EasyETL

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Code Coverage](https://img.shields.io/badge/coverage-60.4%25-orange)](./coverage/report/index.html)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/lincolnqjunior/easy-etl)
[![Tests](https://img.shields.io/badge/tests-88%2F89-yellow)](./tests/UnitTests)

A high-performance .NET 8.0 library for building ETL (Extract, Transform, Load) pipelines with zero-allocation optimizations.

## 🚀 Features

- **Streaming Architecture**: Process data as it flows, never load entire datasets in memory
- **Zero-Allocation Optimized**: Modern libraries designed for minimal GC pressure
- **High Performance**: 2-3x faster than traditional approaches
- **Event-Driven**: Observable progress and errors at every stage
- **Composable**: Mix and match extractors, transformers, and loaders
- **Type-Safe**: Strong typing with nullable reference types

## 📊 Supported Data Sources

### Extractors
- ✅ **CSV** - High-performance parsing with Sep library (98.7% coverage)
- ✅ **JSON/JSONL** - Streaming with System.Text.Json
- ✅ **SQL Server** - Modern async with Microsoft.Data.SqlClient
- ✅ **SQLite** - In-memory or file-based
- ✅ **Parquet** - Columnar format support

### Loaders
- ✅ **CSV** - Buffered writes (92.5% coverage)
- ✅ **JSON/JSONL** - Zero-allocation serialization (84.8% coverage)
- ✅ **SQL Server** - Bulk insert with SqlBulkCopy
- ✅ **SQLite** - Transaction-based batch inserts

### Transformers
- ✅ **Bypass** - Pass-through with no modifications (100% coverage)
- ✅ **Dynamic** - Rule-based transformations (100% coverage)

## 📦 Installation

```bash
dotnet add package EasyETL
```

## 🎯 Quick Start

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

## 📈 Performance

### Current Metrics
- **Throughput**: ~50K rows/second (1M rows, 10 fields)
- **Memory**: Constant working set via streaming
- **Test Coverage**: 58% (839/1442 lines) → **Target: 90%**

### Zero-Allocation Roadmap
See [user-story-zero-allocation.md](./ai_docs/user-story-zero-allocation.md) for the complete plan:
- ✅ **Phase 0**: Documentation (100%)
- 🟡 **Phase 1**: Library modernization (30%)
  - ✅ System.Text.Json (zero-alloc)
  - ✅ Sep v0.11.4 (zero-alloc CSV)
  - ✅ Latest Parquet.Net
  - ✅ Microsoft.Data.SqlClient
- ⏳ **Phases 2-8**: Pipeline optimization (pending)

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
| CsvDataExtractor | 98.7% | ✅ Excellent |
| CsvDataLoader | 92.5% | ✅ Excellent |
| JsonDataLoader | 84.8% | ✅ Good |
| DynamicDataTransformer | 100% | ✅ Perfect |
| BypassDataTransformer | 100% | ✅ Perfect |
| EasyEtl Pipeline | 0% | ❌ Needs tests |
| Database Extractors | 0% | ❌ Needs tests |
| Database Loaders | 0% | ❌ Needs tests |

## 📚 Documentation

- **[User Story - Zero Allocation](./ai_docs/user-story-zero-allocation.md)** - Complete refactoring plan
- **[Implementation Status](./ai_docs/implementation-status.md)** - Current progress tracking
- **[Architecture Guide](./ai_docs/architecture.md)** - Deep dive into design
- **[Development Guide](./ai_docs/development-guide.md)** - How to contribute
- **[API Reference](./ai_docs/api-reference.md)** - Complete API documentation

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────┐
│                    EasyEtl                          │
│              (Pipeline Orchestrator)                │
│                                                     │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐       │
│  │Extractor │──▶│Transformer│──▶│  Loader  │       │
│  └──────────┘   └──────────┘   └──────────┘       │
│                                                     │
│     Channels (System.Threading.Channels)            │
│     ↓           ↓            ↓                      │
│  Extract    Transform      Load                    │
│  (Thread)   (Async)       (Async)                  │
└─────────────────────────────────────────────────────┘
```

### Key Design Principles
1. **Streaming**: Data flows through channels, not loaded in memory
2. **Asynchronous**: All I/O operations are async
3. **Event-driven**: Progress and errors reported via events
4. **Type-safe**: Strong typing with nullable reference types
5. **Configurable**: JSON-based configuration for all components

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
- **[Sep](https://github.com/nietras/Sep)** - High-performance CSV parsing
- **System.Text.Json** - Zero-allocation JSON processing
- **[Parquet.Net](https://github.com/aloneguid/parquet-dotnet)** - Parquet file support
- **Microsoft.Data.SqlClient** - Modern SQL Server client

---

**Status**: Active Development  
**Version**: 1.0.0-preview  
**Last Updated**: November 7, 2025

For detailed implementation status and roadmap, see [Implementation Status](./ai_docs/implementation-status.md).
