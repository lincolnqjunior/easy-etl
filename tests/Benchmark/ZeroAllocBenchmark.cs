using BenchmarkDotNet.Attributes;
using Library.Infra.ZeroAlloc;

namespace Benchmark;

/// <summary>
/// Benchmarks for zero-allocation structures.
/// Measures allocation differences between traditional Dictionary approach and new EtlRecord.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ZeroAllocBenchmark
{
    private const int RowCount = 10_000;
    private FieldDescriptor[]? _schema;
    private EtlRecordPool? _pool;
    private byte[]? _buffer;

    [GlobalSetup]
    public void Setup()
    {
        // Create schema for typical ETL record
        _schema = EtlRecordPool.CreateSchema(
            ("Id", FieldType.Int32),
            ("Name", FieldType.String),
            ("Age", FieldType.Int32),
            ("Salary", FieldType.Double),
            ("IsActive", FieldType.Boolean),
            ("CreatedDate", FieldType.DateTime),
            ("Score", FieldType.Decimal)
        );

        _pool = new EtlRecordPool();
        var bufferSize = EtlRecordPool.CalculateBufferSize(_schema);
        _buffer = _pool.RentBuffer(bufferSize);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_pool != null && _buffer != null)
        {
            _pool.ReturnBuffer(_buffer);
        }
    }

    /// <summary>
    /// Baseline: Traditional approach using Dictionary (causes allocations).
    /// This is what the current codebase does.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void TraditionalDictionary()
    {
        for (int i = 0; i < RowCount; i++)
        {
            // This allocates a new Dictionary for each row
            var row = new Dictionary<string, object?>
            {
                ["Id"] = i,
                ["Name"] = $"Person {i}",
                ["Age"] = 20 + (i % 50),
                ["Salary"] = 50000.0 + (i * 100.0),
                ["IsActive"] = i % 2 == 0,
                ["CreatedDate"] = DateTime.Now.AddDays(-i),
                ["Score"] = (decimal)(i * 1.5)
            };

            // Simulate processing
            ProcessDictionary(row);
        }
    }

    /// <summary>
    /// Zero-allocation approach using EtlRecord with pooled buffers.
    /// This is the new approach we're implementing.
    /// </summary>
    [Benchmark]
    public void ZeroAllocEtlRecord()
    {
        for (int i = 0; i < RowCount; i++)
        {
            // Reuse the same buffer - no new allocations
            var record = new EtlRecord(_buffer.AsSpan(), _schema!);
            
            record.SetValue(0, FieldValue.FromInt32(i));
            record.SetValue(1, FieldValue.FromString($"Person {i}"));
            record.SetValue(2, FieldValue.FromInt32(20 + (i % 50)));
            record.SetValue(3, FieldValue.FromDouble(50000.0 + (i * 100.0)));
            record.SetValue(4, FieldValue.FromBoolean(i % 2 == 0));
            record.SetValue(5, FieldValue.FromDateTime(DateTime.Now.AddDays(-i)));
            record.SetValue(6, FieldValue.FromDecimal((decimal)(i * 1.5)));

            // Simulate processing
            ProcessEtlRecord(ref record);
        }
    }

    /// <summary>
    /// Measures the overhead of boxing/unboxing in traditional approach.
    /// </summary>
    [Benchmark]
    public void TraditionalWithBoxing()
    {
        for (int i = 0; i < RowCount; i++)
        {
            // Each value type is boxed when added to Dictionary
            object id = i;
            object age = 20 + (i % 50);
            object salary = 50000.0 + (i * 100.0);
            object isActive = i % 2 == 0;
            object createdDate = DateTime.Now.AddDays(-i);
            object score = (decimal)(i * 1.5);

            // Unboxing when reading
            var idValue = (int)id;
            var ageValue = (int)age;
            var salaryValue = (double)salary;
            var isActiveValue = (bool)isActive;
            var createdDateValue = (DateTime)createdDate;
            var scoreValue = (decimal)score;

            // Use values to prevent optimization
            ConsumeValues(idValue, ageValue, salaryValue, isActiveValue, createdDateValue, scoreValue);
        }
    }

    /// <summary>
    /// Zero-allocation with FieldValue union (no boxing).
    /// </summary>
    [Benchmark]
    public void ZeroAllocFieldValue()
    {
        for (int i = 0; i < RowCount; i++)
        {
            // No boxing - values stored in union
            var id = FieldValue.FromInt32(i);
            var age = FieldValue.FromInt32(20 + (i % 50));
            var salary = FieldValue.FromDouble(50000.0 + (i * 100.0));
            var isActive = FieldValue.FromBoolean(i % 2 == 0);
            var createdDate = FieldValue.FromDateTime(DateTime.Now.AddDays(-i));
            var score = FieldValue.FromDecimal((decimal)(i * 1.5));

            // No unboxing needed
            var idValue = id.AsInt32();
            var ageValue = age.AsInt32();
            var salaryValue = salary.AsDouble();
            var isActiveValue = isActive.AsBoolean();
            var createdDateValue = createdDate.AsDateTime();
            var scoreValue = score.AsDecimal();

            // Use values to prevent optimization
            ConsumeValues(idValue, ageValue, salaryValue, isActiveValue, createdDateValue, scoreValue);
        }
    }

    private void ProcessDictionary(Dictionary<string, object?> row)
    {
        // Simulate reading some values
        var id = (int)row["Id"]!;
        var name = (string)row["Name"]!;
        var active = (bool)row["IsActive"]!;
        
        // Prevent optimization
        if (id < 0 || name.Length == 0 || !active)
        {
            throw new InvalidOperationException();
        }
    }

    private void ProcessEtlRecord(ref EtlRecord record)
    {
        // Simulate reading some values
        var id = record.GetValue(0).AsInt32();
        var name = record.GetValue(1).AsString();
        var active = record.GetValue(4).AsBoolean();
        
        // Prevent optimization
        if (id < 0 || name?.Length == 0 || !active)
        {
            throw new InvalidOperationException();
        }
    }

    private void ConsumeValues(int id, int age, double salary, bool isActive, DateTime date, decimal score)
    {
        // Prevent compiler from optimizing away the values
        if (id < -1 && age < 0 && salary < 0 && !isActive && date == DateTime.MinValue && score < 0)
        {
            throw new InvalidOperationException();
        }
    }
}
