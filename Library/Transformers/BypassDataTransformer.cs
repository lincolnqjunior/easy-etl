using Library.Infra;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Library.Transformers
{
    /// <summary>
    /// A data transformer that bypasses the transformation process, directly passing
    /// through the input data to the output without any modifications.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="BypassDataTransformer"/> class.
    /// </remarks>
    public class BypassDataTransformer(int notifyAfter = 1000) : IDataTransformer
    {
        public event TransformNotificationHandler? OnTransform;
        public event TransformNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long IngestedLines { get; set; }
        public long TransformedLines { get; set; }
        public long ExcludedByFilter { get; set; }
        public double PercentDone { get; set; }
        public long TotalLines { get; set; }
        public double Speed { get; private set; }

        private readonly Stopwatch _timer = new();

        /// <summary>
        /// Passes through each item in the provided data stream without modification.
        /// </summary>
        /// <param name="data">The data to bypass transformation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of the original data items.</returns>
        public async IAsyncEnumerable<Dictionary<string, object?>> Transform(
            IAsyncEnumerable<Dictionary<string, object?>> data,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _timer.Restart();

            await foreach (var item in data.WithCancellation(cancellationToken))
            {
                IngestedLines++;
                TransformedLines++;

                NotifyProgress();

                yield return item;
            }

            _timer.Stop();
            NotifyFinish();
        }

        /// <summary>
        /// Notifies subscribers about the progress after processing each batch of items.
        /// </summary>
        private void NotifyProgress()
        {
            // Notify subscribers periodically based on the number of lines ingested.
            if (notifyAfter % 1000 == 0) // Assuming a default notify period of 1000 lines.
            {
                PercentDone = (double)TransformedLines / IngestedLines * 100;
                Speed = IngestedLines / _timer.Elapsed.TotalSeconds;
                OnTransform?.Invoke(new TransformNotificationEventArgs(IngestedLines, IngestedLines, TransformedLines, ExcludedByFilter, PercentDone, Speed));
            }
        }

        /// <summary>
        /// Notifies subscribers that the transformation process has finished.
        /// </summary>
        private void NotifyFinish()
        {
            PercentDone = 100;
            Speed = TransformedLines / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new TransformNotificationEventArgs(IngestedLines, IngestedLines, TransformedLines, ExcludedByFilter, PercentDone, Speed));
        }

        public List<Dictionary<string, object?>> ApplyTransformations(Dictionary<string, object?> item)
        {
            throw new NotImplementedException();
        }
    }
}
