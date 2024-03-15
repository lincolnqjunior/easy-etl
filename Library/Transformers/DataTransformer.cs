using Library.Infra;

namespace Library.Transformers
{
    public class DataTransformer(TransformationConfig config)
    {
        readonly TransformationConfig _config = config;        

        public async IAsyncEnumerable<Dictionary<string, object>> Transform(IAsyncEnumerable<Dictionary<string, object>> data)
        {
            await foreach (var item in data)
            {
                var transformedItems = ApplyTransformations(item, _config.Transformations);
                foreach (var transformedItem in transformedItems)
                {
                    yield return transformedItem;
                }
            }
        }


        private static List<Dictionary<string, object>> ApplyTransformations(Dictionary<string, object> item, List<TransformationFilter> filters)
        {
            var results = new List<Dictionary<string, object>> { new(item) };

            foreach (var filter in filters)
            {
                bool conditionMet = string.IsNullOrWhiteSpace(filter.Condition) || DynamicEvaluator.EvaluateCondition(item, filter.Condition);

                if (conditionMet)
                {
                    var newResults = new List<Dictionary<string, object>>();
                    foreach (var action in filter.Actions)
                    {
                        foreach (var result in results)
                        {
                            var transformedResult = new Dictionary<string, object>(result);
                            foreach (var mapping in action.FieldMappings)
                            {
                                var value = mapping.Value.IsDynamic ? DynamicEvaluator.EvaluateDynamicValue(result, mapping.Value.Value.ToString()) : mapping.Value.Value;
                                transformedResult[mapping.Key] = value;
                            }
                            newResults.Add(transformedResult);
                        }
                    }
                    results = newResults;
                }
            }

            return results;
        }
    }
}
