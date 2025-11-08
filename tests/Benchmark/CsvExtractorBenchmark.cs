using BenchmarkDotNet.Attributes;
using Library.Extractors.Csv;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.ZeroAlloc;

namespace Benchmark;

/// <summary>
/// Benchmarks comparing CSV extraction V1 (Dictionary) vs V2 (EtlRecord).
/// Measures actual performance improvements with real CSV data.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class CsvExtractorBenchmark
{
    private string? _testCsvPath;
    private CsvDataExtractorConfig? _configV1;
    private CsvDataExtractorConfig? _configV2;

    [Params(100, 1000, 10000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Create test CSV file
        _testCsvPath = Path.GetTempFileName();
        CreateTestCsvFile(_testCsvPath, RowCount);

        // Configure V1 extractor
        _configV1 = new CsvDataExtractorConfig
        {
            FilePath = _testCsvPath,
            HasHeader = true,
            Delimiter = ',',
            RaiseChangeEventAfer = 10000, // Don't raise events during benchmark
            Columns = new List<IColumnAction>
            {
                new DefaultColumnAction("Id", 0, false, "Id", typeof(int)),
                new DefaultColumnAction("Name", 1, false, "Name", typeof(string)),
                new DefaultColumnAction("Age", 2, false, "Age", typeof(int)),
                new DefaultColumnAction("Salary", 3, false, "Salary", typeof(double)),
                new DefaultColumnAction("Active", 4, false, "Active", typeof(bool)),
                new DefaultColumnAction("Department", 5, false, "Department", typeof(string))
            }
        };

        // Configure V2 extractor (same config)
        _configV2 = new CsvDataExtractorConfig
        {
            FilePath = _testCsvPath,
            HasHeader = true,
            Delimiter = ',',
            RaiseChangeEventAfer = 10000,
            Columns = new List<IColumnAction>
            {
                new DefaultColumnAction("Id", 0, false, "Id", typeof(int)),
                new DefaultColumnAction("Name", 1, false, "Name", typeof(string)),
                new DefaultColumnAction("Age", 2, false, "Age", typeof(int)),
                new DefaultColumnAction("Salary", 3, false, "Salary", typeof(double)),
                new DefaultColumnAction("Active", 4, false, "Active", typeof(bool)),
                new DefaultColumnAction("Department", 5, false, "Department", typeof(string))
            }
        };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_testCsvPath != null && File.Exists(_testCsvPath))
        {
            File.Delete(_testCsvPath);
        }
    }

    /// <summary>
    /// Baseline: V1 CSV extractor using Dictionary<string, object?> (causes allocations).
    /// </summary>
    [Benchmark(Baseline = true)]
    public void V1_CsvExtractor_Dictionary()
    {
        var extractor = new CsvDataExtractor(_configV1!);
        int rowCount = 0;

        extractor.Extract((ref Dictionary<string, object?> row) =>
        {
            // Process row - access fields to prevent optimization
            var id = (int)row["Id"]!;
            var name = (string)row["Name"]!;
            var active = (bool)row["Active"]!;
            
            rowCount++;
            
            // Prevent dead code elimination
            if (id < 0 || name.Length == 0 || !active)
            {
                throw new InvalidOperationException();
            }
        });

        if (rowCount != RowCount)
        {
            throw new InvalidOperationException($"Expected {RowCount} rows, got {rowCount}");
        }
    }

    /// <summary>
    /// Optimized: V2 CSV extractor using EtlRecord (zero allocations in hot path).
    /// </summary>
    [Benchmark]
    public void V2_CsvExtractor_EtlRecord()
    {
        var extractor = new CsvDataExtractorV2(_configV2!);
        int rowCount = 0;

        extractor.Extract((ref EtlRecord record) =>
        {
            // Process record - access fields to prevent optimization
            var id = record.GetValue("Id").AsInt32();
            var name = record.GetValue("Name").AsString();
            var active = record.GetValue("Active").AsBoolean();
            
            rowCount++;
            
            // Prevent dead code elimination
            if (id < 0 || name?.Length == 0 || !active)
            {
                throw new InvalidOperationException();
            }
        });

        if (rowCount != RowCount)
        {
            throw new InvalidOperationException($"Expected {RowCount} rows, got {rowCount}");
        }
    }

    /// <summary>
    /// Measures overhead of Dictionary operations alone.
    /// </summary>
    [Benchmark]
    public void Overhead_Dictionary_Creation()
    {
        for (int i = 0; i < RowCount; i++)
        {
            var dict = new Dictionary<string, object?>
            {
                ["Id"] = i,
                ["Name"] = $"User{i}",
                ["Age"] = 30 + (i % 40),
                ["Salary"] = 50000.0 + (i * 100.0),
                ["Active"] = i % 2 == 0,
                ["Department"] = (i % 5) switch
                {
                    0 => "Engineering",
                    1 => "Sales",
                    2 => "Marketing",
                    3 => "HR",
                    _ => "Operations"
                }
            };

            // Access to prevent optimization
            var id = (int)dict["Id"];
            if (id < 0) throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// Measures overhead of EtlRecord operations alone.
    /// </summary>
    [Benchmark]
    public void Overhead_EtlRecord_Creation()
    {
        var pool = new EtlRecordPool();
        var schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String),
            ("Age", FieldType.Int32),
            ("Salary", FieldType.Double),
            ("Active", FieldType.Boolean),
            ("Department", FieldType.String)
        );

        var buffer = pool.RentBuffer(EtlRecordPool.CalculateBufferSize(schema));
        try
        {
            for (int i = 0; i < RowCount; i++)
            {
                var record = new EtlRecord(buffer.AsSpan(), schema);
                
                record.SetValue(0, FieldValue.FromInt32(i));
                record.SetValue(1, FieldValue.FromString($"User{i}"));
                record.SetValue(2, FieldValue.FromInt32(30 + (i % 40)));
                record.SetValue(3, FieldValue.FromDouble(50000.0 + (i * 100.0)));
                record.SetValue(4, FieldValue.FromBoolean(i % 2 == 0));
                record.SetValue(5, FieldValue.FromString((i % 5) switch
                {
                    0 => "Engineering",
                    1 => "Sales",
                    2 => "Marketing",
                    3 => "HR",
                    _ => "Operations"
                }));

                // Access to prevent optimization
                var id = record.GetValue(0).AsInt32();
                if (id < 0) throw new InvalidOperationException();
            }
        }
        finally
        {
            pool.ReturnBuffer(buffer);
        }
    }

    private void CreateTestCsvFile(string filePath, int rows)
    {
        using var writer = new StreamWriter(filePath);
        
        // Write header
        writer.WriteLine("Id,Name,Age,Salary,Active,Department");

        // Write data rows
        for (int i = 1; i <= rows; i++)
        {
            var name = $"User{i}";
            var age = 25 + (i % 40);
            var salary = 45000.0 + (i * 50.0);
            var active = i % 2 == 0 ? "true" : "false";
            var department = (i % 5) switch
            {
                0 => "Engineering",
                1 => "Sales",
                2 => "Marketing",
                3 => "HR",
                _ => "Operations"
            };

            writer.WriteLine($"{i},{name},{age},{salary},{active},{department}");
        }
    }
}
