using Library.Infra;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Library.Transformers
{
    /// <summary>
    /// A data transformer that simply passes through the data without transforming it.
    /// </summary>
    public class BypassDataTransformer(TransformationConfig config) : IDataTransformer
    {
        public event TransformNotificationHandler? OnTransform;
        public event TransformNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        private readonly TransformationConfig _config = config;

        public long IngestedLines { get; set; }
        public long TransformedLines { get; set; }
        public long ExcludedByFilter { get; set; }
        public double PercentDone { get; set; }
        public long TotalLines { get; set; }
        public double Speed { get; set; }

        private readonly Stopwatch _timer = new();

        public async IAsyncEnumerable<Dictionary<string, object?>> Transform(
            IAsyncEnumerable<Dictionary<string, object?>> data,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _timer.Restart();

            await foreach (var item in data.WithCancellation(cancellationToken))
            {                
                IngestedLines++;
                TransformedLines++;

                // Simply pass through the item.
                yield return item;

                NotifyProgress();
            }

            _timer.Stop();
            NotifyFinish();
        }

        private void NotifyProgress()
        {
            if (_config.NotifyAfter % TransformedLines != 0) return;

            if (TotalLines < TransformedLines) TotalLines = TransformedLines;
            PercentDone = TransformedLines / (double)TotalLines * 100;
            Speed = TransformedLines / _timer.Elapsed.TotalSeconds;
            OnTransform?.Invoke(new TransformNotificationEventArgs(TotalLines, IngestedLines, TransformedLines, ExcludedByFilter, PercentDone, Speed));
        }

        private void NotifyFinish()
        {
            TotalLines = TransformedLines;
            PercentDone = 100;
            Speed = TransformedLines / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new TransformNotificationEventArgs(TotalLines, IngestedLines, TransformedLines, ExcludedByFilter, PercentDone, Speed));
        }
    }
}
