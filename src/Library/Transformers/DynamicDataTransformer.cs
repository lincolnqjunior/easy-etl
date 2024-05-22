using Library.Infra;
using Library.Infra.EventArgs;
using Library.Infra.Helpers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Library.Transformers
{
    /// <summary>
    /// Transforms data dynamically based on specified transformations.
    /// </summary>
    public class DynamicDataTransformer : IDataTransformer
    {
        public event TransformNotificationHandler? OnTransform;
        public event TransformNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        private readonly TransformationConfig _config;
        private readonly Stopwatch _timer = new();

        public long IngestedLines { get; set; }
        public long TransformedLines { get; set; }
        public long ExcludedByFilter { get; set; }
        public double PercentDone { get; set; }
        public long TotalLines { get; set; }
        public double Speed { get; set; }

        public DynamicDataTransformer(TransformationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Applies configured transformations to each item in the provided data stream.
        /// </summary>
        /// <param name="data">The data to transform.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of transformed data dictionaries.</returns>
        public async IAsyncEnumerable<Dictionary<string, object?>> Transform(
            IAsyncEnumerable<Dictionary<string, object?>> data,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _timer.Restart();

            await foreach (var item in data.WithCancellation(cancellationToken))
            {
                List<Dictionary<string, object?>> transformedItems;

                try
                {
                    IngestedLines++;
                    transformedItems = ApplyTransformations(item);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Transform, ex, item, TransformedLines));
                    yield break;
                }

                foreach (var transformedItem in transformedItems)
                {
                    TransformedLines++;
                    NotifyProgress();
                    yield return transformedItem;
                }
            }

            _timer.Stop();
            NotifyFinish();
        }

        /// <summary>
        /// Applies all configured filters and actions to an item.
        /// </summary>
        /// <param name="item">The data item to transform.</param>
        /// <returns>A list of transformed items.</returns>
        public List<Dictionary<string, object?>> ApplyTransformations(Dictionary<string, object?> item)
        {
            // Return early if no transformations are configured.
            if (_config.Transformations.Count == 0)
            {
                return [item];
            }

            var results = new List<Dictionary<string, object?>>();

            foreach (var filter in _config.Transformations)
            {
                var conditionMet = string.IsNullOrWhiteSpace(filter.Condition) || DynamicEvaluator.EvaluateCondition(item, filter.Condition);

                if (!conditionMet)
                {
                    ExcludedByFilter++;
                    continue;
                }

                // Apply each filter's actions to generate new result sets.
                var filteredResults = ApplyFilterActions(results.Count == 0 ? [item] : results, filter);
                results.AddRange(filteredResults);
            }

            return results;
        }

        /// <summary>
        /// Applies the actions of a single filter to all current results.
        /// </summary>
        /// <param name="results">The current results to apply actions to.</param>
        /// <param name="filter">The filter containing actions to apply.</param>
        /// <returns>A new list of results after applying the filter actions.</returns>
        private static List<Dictionary<string, object?>> ApplyFilterActions(List<Dictionary<string, object?>> results, TransformationFilter filter)
        {
            var newResults = new List<Dictionary<string, object?>>();

            foreach (var action in filter.Actions)
            {
                foreach (var result in results)
                {
                    var transformedResult = TransformResult(result, action);
                    newResults.Add(transformedResult);
                }
            }

            return newResults;
        }

        /// <summary>
        /// Transforms a single result based on the specified action.
        /// </summary>
        /// <param name="result">The result to transform.</param>
        /// <param name="action">The action to apply.</param>
        /// <returns>A new result after applying the action.</returns>
        private static Dictionary<string, object?> TransformResult(Dictionary<string, object?> result, TransformationAction action)
        {
            var transformedResult = new Dictionary<string, object?>(result);

            foreach (var mapping in action.FieldMappings)
            {
                var value = mapping.Value.IsDynamic ? DynamicEvaluator.EvaluateDynamicValue(result, mapping.Value.Value?.ToString() ?? string.Empty) : mapping.Value.Value;
                transformedResult[mapping.Key] = value ?? DBNull.Value;
            }

            return transformedResult;
        }

        /// <summary>
        /// Notifies subscribers of progress after processing a batch of items.
        /// </summary>
        private void NotifyProgress()
        {
            if (TransformedLines % _config.RaiseChangeEventAfer == 0 || TransformedLines == TotalLines)
            {
                if (TotalLines < TransformedLines) TotalLines = TransformedLines;
                PercentDone = (double)TransformedLines / TotalLines * 100;
                Speed = TransformedLines / _timer.Elapsed.TotalSeconds;
                OnTransform?.Invoke(new TransformNotificationEventArgs(TotalLines, IngestedLines, TransformedLines, ExcludedByFilter, PercentDone, Speed));
            }
        }

        /// <summary>
        /// Notifies subscribers that the transformation process has finished.
        /// </summary>
        private void NotifyFinish()
        {
            if (TotalLines < TransformedLines) TotalLines = TransformedLines;
            PercentDone = 100;
            Speed = TotalLines / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new TransformNotificationEventArgs(TotalLines, IngestedLines, TransformedLines, ExcludedByFilter, PercentDone, Speed));
        }
    }
}
