using Library.Infra;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Library.Transformers
{
    public class DataTransformer(TransformationConfig config) : IDataTransformer
    {
        public event TransformNotificationHandler? OnTransform;
        public event TransformNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        private readonly TransformationConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly Stopwatch _timer = new();

        public long IngestedLines { get; set; }
        public long TransformedLines { get; set; }
        public long ExcludedByFilter { get; set; }
        public double PercentDone { get; set; }
        public long TotalLines { get; set; }
        public double Speed { get; set; }

        public async IAsyncEnumerable<Dictionary<string, object?>> Transform(
            IAsyncEnumerable<Dictionary<string, object?>> data,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _timer.Restart();

            await foreach (var item in data)
            {
                List<Dictionary<string, object?>> transformedItems;

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

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

        private List<Dictionary<string, object?>> ApplyTransformations(Dictionary<string, object?> item)
        {
            var results = new List<Dictionary<string, object?>>();
            if (_config.Transformations.Count == 0) return [item];

            foreach (var filter in _config.Transformations)
            {
                var conditionMet = string.IsNullOrWhiteSpace(filter.Condition) || DynamicEvaluator.EvaluateCondition(item, filter.Condition);

                if (!conditionMet)
                {
                    ExcludedByFilter++;
                    continue;
                }

                if (results.Count == 0) results.Add(new(item));
                results = ApplyFilterActions(results, filter);
            }

            return results;
        }

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

        private static Dictionary<string, object?> TransformResult(Dictionary<string, object?> result, TransformationAction action)
        {
            var transformedResult = new Dictionary<string, object?>(result);

            foreach (var mapping in action.FieldMappings)
            {
                var value = mapping.Value.IsDynamic
                    ? DynamicEvaluator.EvaluateDynamicValue(result, mapping.Value.Value.ToString() ?? string.Empty)
                    : mapping.Value.Value;

                transformedResult[mapping.Key] = value ?? string.Empty;
            }

            return transformedResult;
        }

        private void NotifyProgress()
        {
            if (TotalLines < TransformedLines) TotalLines = TransformedLines;

            if (TransformedLines % _config.NotifyAfter == 0)
            {
                PercentDone = TransformedLines / TotalLines * 100;
                Speed = TransformedLines / _timer.Elapsed.TotalSeconds;
                OnTransform?.Invoke(new TransformNotificationEventArgs(TotalLines, IngestedLines, TransformedLines, ExcludedByFilter, PercentDone, Speed));
            }
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
