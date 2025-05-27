using Library.Extractors;
using Library.Infra;
using Library.Infra.EventArgs;
using Library.Loaders;
using Library.Transformers;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace Library
{
    /// <summary>
    /// Manages an ETL (Extract, Transform, Load) process, facilitating data flow and error handling.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the EasyEtl class with specified components and channel size.
    /// </remarks>
    /// <param name="extractor">Component responsible for data extraction.</param>
    /// <param name="transformer">Component responsible for data transformation.</param>
    /// <param name="loader">Component responsible for data loading.</param>
    /// <param name="channelSize">Maximum size of the channels used for passing data between stages. Zero for unbounded</param>
    public class EasyEtl
    {
        public event EasyEtlProgressEventHandler? OnChange;
        public event EasyEtlProgressEventHandler? OnComplete;
        public event EasyEtlErrorEventHandler? OnError;

        public readonly IDataExtractor Extractor;
        public readonly IDataTransformer Transformer;
        public readonly IDataLoader Loader;

        private readonly CancellationTokenSource _cts = new();
        private readonly Channel<Dictionary<string, object?>> _extractChannel;
        private readonly Channel<Dictionary<string, object?>> _transformChannel;

        //private long _totalLinesExtracted = 0;

        public EasyEtl(IDataExtractor extractor, IDataTransformer transformer, IDataLoader loader, int channelSize = 0)
        {
            Extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
            Transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
            Loader = loader ?? throw new ArgumentNullException(nameof(loader));

            _extractChannel = channelSize == 0 ?
                Channel.CreateUnbounded<Dictionary<string, object?>>() :
                Channel.CreateBounded<Dictionary<string, object?>>(new BoundedChannelOptions(channelSize));

            _transformChannel = channelSize == 0 ?
                Channel.CreateUnbounded<Dictionary<string, object?>>() :
                Channel.CreateBounded<Dictionary<string, object?>>(new BoundedChannelOptions(channelSize));
        }

        public EasyEtl(IDataExtractor extractor, IDataLoader loader, int channelSize = 0)
        {
            Extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
            Transformer = new BypassDataTransformer();
            Loader = loader ?? throw new ArgumentNullException(nameof(loader));

            _extractChannel = channelSize == 0 ?
                Channel.CreateUnbounded<Dictionary<string, object?>>() :
                Channel.CreateBounded<Dictionary<string, object?>>(new BoundedChannelOptions(channelSize));

            _transformChannel = channelSize == 0 ?
                Channel.CreateUnbounded<Dictionary<string, object?>>() :
                Channel.CreateBounded<Dictionary<string, object?>>(new BoundedChannelOptions(channelSize));
        }

        /// <summary>
        /// Starts the ETL process pipeline asynchronously
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
                await Task.WhenAll(Task.Run(() => Extract()), Transform(), Load());
                OnComplete?.Invoke(new EasyEtlNotificationEventArgs(telemetry.Progress));
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Global, ex, [], 0));
                _cts.Cancel();
            }
            finally
            {
                _cts.Dispose();
            }
        }

        private async Task Extract()
        {
            try
            {
                // Pass the CancellationToken from _cts to the extractor's Extract method.
                await Extractor.Extract((ref Dictionary<string, object?> row) =>
                {
                    // WriteAsync already accepts a CancellationToken, but _extractChannel.Writer is not directly taking it here.
                    // The channel itself will be completed if _cts.Token is cancelled, which is handled by ReadAllAsync.
                    _extractChannel.Writer.WriteAsync(new Dictionary<string, object?>(row), _cts.Token).AsTask().Wait(_cts.Token);
                    Transformer.TotalLines = Extractor.TotalLines;
                }, _cts.Token);

                _extractChannel.Writer.Complete();
            }
            catch (OperationCanceledException)
            {
                _extractChannel.Writer.Complete(); // Ensure channel is completed on cancellation
            }
            catch (Exception ex)
            {
                _extractChannel.Writer.Complete(ex);
                OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, ex, [], Extractor.LineNumber));
                _cts.Cancel(); // Signal cancellation to other parts of the ETL process
                // No need to re-throw if OnError is expected to handle it and signal cancellation.
            }
        }

        private async Task Transform()
        {
            try
            {
                IAsyncEnumerable<Dictionary<string, object?>> transformedRows = Transformer.Transform(_extractChannel.Reader.ReadAllAsync(_cts.Token), _cts.Token);
                await foreach (var row in transformedRows)
                {
                    await _transformChannel.Writer.WriteAsync(row, _cts.Token);
                    //Loader.TotalLines = _totalLinesExtracted;
                }

                _transformChannel.Writer.Complete();
            }
            catch (Exception ex)
            {
                _transformChannel.Writer.Complete(ex);
                throw; // Re-throw to be caught by the global exception handler in Init().
            }
        }

        private async Task Load()
        {
            try
            {
                await Loader.Load(_transformChannel.Reader.ReadAllAsync(_cts.Token), _cts.Token);
            }
            catch (Exception ex)
            {
                ex.Data.Add("CurrentLine", Loader.CurrentLine);
                throw; // Re-throw to be caught by the global exception handler in Init().
            }
        }
    }
}
