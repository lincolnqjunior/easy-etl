using Library.Infra;
using System.Collections.Generic;
using System.Diagnostics;

namespace Library.Transformers
{
    public delegate void TransformNotification(TransformNotificationEventArgs args);

    public class DataTransformer
    {
        public event TransformNotification OnTransform;
        public event TransformNotification OnFinish;

        private readonly TransformationConfig _config;
        private long _ingestedLines = 0;
        private long _transformedLines = 0;
        private long _excludedByFilter = 0;
        private readonly Stopwatch _timer = new();

        public long IngestedLines => _ingestedLines;
        public long TransformedLines => _transformedLines;
        public long ExcludedByFilter => _excludedByFilter;

        public DataTransformer(TransformationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async IAsyncEnumerable<Dictionary<string, object>> Transform(IAsyncEnumerable<Dictionary<string, object>> data)
        {
            _timer.Restart();
            await foreach (var item in data)
            {
                _ingestedLines++;
                var transformedItems = ApplyTransformations(item);
                foreach (var transformedItem in transformedItems)
                {
                    _transformedLines++;
                    NotifyProgress();
                    yield return transformedItem;
                }
            }
            NotifyFinish();
        }

        private List<Dictionary<string, object>> ApplyTransformations(Dictionary<string, object> item)
        {
            var results = new List<Dictionary<string, object>>();
            if (_config.Transformations.Count == 0) return [item];

            foreach (var filter in _config.Transformations)
            {
                var conditionMet = string.IsNullOrWhiteSpace(filter.Condition) || DynamicEvaluator.EvaluateCondition(item, filter.Condition);

                if (!conditionMet)
                {
                    _excludedByFilter++;
                    continue;
                }

                if (results.Count == 0) results.Add(new(item));
                results = ApplyFilterActions(results, filter);
            }

            return results;
        }

        private static List<Dictionary<string, object>> ApplyFilterActions(List<Dictionary<string, object>> results, TransformationFilter filter)
        {
            var newResults = new List<Dictionary<string, object>>();

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

        private static Dictionary<string, object> TransformResult(Dictionary<string, object> result, TransformationAction action)
        {
            var transformedResult = new Dictionary<string, object>(result);

            foreach (var mapping in action.FieldMappings)
            {
                var value = mapping.Value.IsDynamic
                    ? DynamicEvaluator.EvaluateDynamicValue(result, mapping.Value.Value.ToString())
                    : mapping.Value.Value;

                transformedResult[mapping.Key] = value;
            }

            return transformedResult;
        }

        private void NotifyProgress()
        {
            if (_transformedLines % _config.NotifyAfter == 0)
            {
                var speed = _transformedLines / _timer.Elapsed.TotalSeconds;
                OnTransform?.Invoke(new TransformNotificationEventArgs(_transformedLines, _ingestedLines, _excludedByFilter, speed));
            }
        }

        private void NotifyFinish()
        {
            var speed = _transformedLines / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new TransformNotificationEventArgs(_transformedLines, _ingestedLines, _excludedByFilter, speed));
        }
    }
}
