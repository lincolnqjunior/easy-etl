using Library.Extractors;
using Library.Infra;
using Library.Infra.EventArgs;
using Library.Infra.ZeroAlloc;
using Library.Loaders;
using Library.Transformers;

namespace Library;

/// <summary>
/// Zero-allocation ETL pipeline using V2 interfaces with EtlRecord.
/// Provides the same functionality as EasyEtl but with minimal memory allocations.
/// </summary>
public class EasyEtlV2
{
    public event EasyEtlProgressEventHandler? OnChange;
    public event EasyEtlProgressEventHandler? OnComplete;
    public event EasyEtlErrorEventHandler? OnError;

    public readonly IDataExtractorV2 Extractor;
    public readonly IDataTransformerV2 Transformer;
    public readonly IDataLoaderV2 Loader;
    public readonly EtlRecordPool Pool;

    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EasyEtlV2"/> class.
    /// </summary>
    /// <param name="extractor">Zero-allocation extractor.</param>
    /// <param name="transformer">Zero-allocation transformer.</param>
    /// <param name="loader">Zero-allocation loader.</param>
    /// <param name="pool">Optional record pool. If null, creates a new pool.</param>
    public EasyEtlV2(
        IDataExtractorV2 extractor, 
        IDataTransformerV2 transformer, 
        IDataLoaderV2 loader,
        EtlRecordPool? pool = null)
    {
        Extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        Transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
        Loader = loader ?? throw new ArgumentNullException(nameof(loader));
        Pool = pool ?? new EtlRecordPool();

        // Validate schema compatibility
        ValidateSchemas();
    }

    /// <summary>
    /// Initializes a new instance with bypass transformer.
    /// </summary>
    public EasyEtlV2(
        IDataExtractorV2 extractor, 
        IDataLoaderV2 loader,
        EtlRecordPool? pool = null)
        : this(extractor, new BypassDataTransformerV2(extractor.Schema), loader, pool)
    {
    }

    /// <summary>
    /// Executes the ETL pipeline.
    /// </summary>
    public async Task Execute()
    {
        var telemetry = new EasyEtlTelemetry(this);
        
        telemetry.OnChange += args =>
        {
            Loader.TotalLines = Transformer.TotalLines = Extractor.TotalLines;
            OnChange?.Invoke(args);
        };
        
        telemetry.OnError += args =>
        {
            OnError?.Invoke(args);
            _cts.Cancel();
        };

        try
        {
            await Task.Run(() => ExtractTransformLoad(), _cts.Token);
            OnComplete?.Invoke(new EasyEtlNotificationEventArgs(telemetry.Progress));
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Global, ex, new Dictionary<string, object?>(), 0));
            _cts.Cancel();
        }
        finally
        {
            _cts.Dispose();
        }
    }

    /// <summary>
    /// Extract, transform, and load pipeline - all in zero-allocation fashion.
    /// </summary>
    private void ExtractTransformLoad()
    {
        try
        {
            // Rent buffer once for the entire pipeline
            var extractBufferSize = EtlRecordPool.CalculateBufferSize(Extractor.Schema);
            var extractBuffer = Pool.RentBuffer(extractBufferSize);

            var transformBufferSize = EtlRecordPool.CalculateBufferSize(Transformer.OutputSchema);
            var transformBuffer = Pool.RentBuffer(transformBufferSize);

            try
            {
                Extractor.Extract((ref EtlRecord extractedRecord) =>
                {
                    // Transform record
                    Transformer.Transform(ref extractedRecord, Pool, (ref EtlRecord transformedRecord) =>
                    {
                        // Load record (synchronous due to ref struct limitation)
                        Loader.Load(ref transformedRecord, _cts.Token);
                    });
                });

                // Complete transformer and loader
                if (Transformer is BypassDataTransformerV2 bypass)
                {
                    bypass.Complete();
                }
                
                Loader.Complete(_cts.Token).Wait();
            }
            finally
            {
                Pool.ReturnBuffer(extractBuffer);
                Pool.ReturnBuffer(transformBuffer);
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(new ErrorNotificationEventArgs(
                EtlType.Extract, 
                ex, 
                new Dictionary<string, object?>(), 
                Extractor.LineNumber));
            throw;
        }
    }

    /// <summary>
    /// Validates that schemas are compatible across pipeline stages.
    /// </summary>
    private void ValidateSchemas()
    {
        // Extractor output must match transformer input
        if (Extractor.Schema.Length != Transformer.InputSchema.Length)
        {
            throw new InvalidOperationException(
                $"Schema mismatch: Extractor produces {Extractor.Schema.Length} fields, " +
                $"but Transformer expects {Transformer.InputSchema.Length} fields.");
        }

        // Transformer output must match loader input
        if (Transformer.OutputSchema.Length != Loader.Schema.Length)
        {
            throw new InvalidOperationException(
                $"Schema mismatch: Transformer produces {Transformer.OutputSchema.Length} fields, " +
                $"but Loader expects {Loader.Schema.Length} fields.");
        }

        // Check field name and type compatibility (simplified check)
        for (int i = 0; i < Extractor.Schema.Length && i < Transformer.InputSchema.Length; i++)
        {
            if (Extractor.Schema[i].Name != Transformer.InputSchema[i].Name)
            {
                throw new InvalidOperationException(
                    $"Schema mismatch at field {i}: Extractor has '{Extractor.Schema[i].Name}', " +
                    $"but Transformer expects '{Transformer.InputSchema[i].Name}'.");
            }
        }
    }

    /// <summary>
    /// Telemetry adapter for V2 pipeline.
    /// </summary>
    private class EasyEtlTelemetry
    {
        private readonly EasyEtlV2 _etl;
        public event EasyEtlProgressEventHandler? OnChange;
        public event EasyEtlErrorEventHandler? OnError;

        public Dictionary<EtlType, EtlDataProgress> Progress { get; } = new()
        {
            { EtlType.Extract, new EtlDataProgress() },
            { EtlType.Transform, new EtlDataProgress() },
            { EtlType.Load, new EtlDataProgress() },
            { EtlType.Global, new EtlDataProgress() }
        };

        public EasyEtlTelemetry(EasyEtlV2 etl)
        {
            _etl = etl;
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _etl.Extractor.OnRead += (args) => UpdateProgress(EtlType.Extract, args);
            _etl.Transformer.OnTransform += (args) => UpdateProgress(EtlType.Transform, args);
            _etl.Loader.OnWrite += (args) => UpdateProgress(EtlType.Load, args);

            _etl.Extractor.OnError += (args) => OnError?.Invoke(args);
            _etl.Transformer.OnError += (args) => OnError?.Invoke(args);
            _etl.Loader.OnError += (args) => OnError?.Invoke(args);
        }

        private void UpdateProgress(EtlType type, dynamic args)
        {
            var progress = Progress[type];
            
            if (args is ExtractNotificationEventArgs extractArgs)
            {
                progress.TotalLines = extractArgs.Total;
                progress.PercentComplete = extractArgs.ReadPercentage;
                progress.Speed = extractArgs.Speed;
            }
            else if (args is TransformNotificationEventArgs transformArgs)
            {
                progress.TotalLines = transformArgs.TotalLines;
                progress.PercentComplete = transformArgs.PercentDone;
                progress.Speed = transformArgs.Speed;
            }
            else if (args is LoadNotificationEventArgs loadArgs)
            {
                progress.TotalLines = loadArgs.TotalLines;
                progress.PercentComplete = loadArgs.WritePercentage;
                progress.Speed = loadArgs.Speed;
            }

            UpdateGlobalProgress();
            OnChange?.Invoke(new EasyEtlNotificationEventArgs(Progress));
        }

        private void UpdateGlobalProgress()
        {
            var global = Progress[EtlType.Global];
            var extract = Progress[EtlType.Extract];
            var transform = Progress[EtlType.Transform];
            var load = Progress[EtlType.Load];

            global.TotalLines = Math.Max(extract.TotalLines, Math.Max(transform.TotalLines, load.TotalLines));
            global.PercentComplete = (extract.PercentComplete + transform.PercentComplete + load.PercentComplete) / 3;
            global.Speed = (extract.Speed + transform.Speed + load.Speed) / 3;
        }
    }
}
