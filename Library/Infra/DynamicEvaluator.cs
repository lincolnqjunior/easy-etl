using System.Linq.Expressions;
using Z.Expressions;

namespace Library.Infra
{
    public class DynamicEvaluator
    {
        public static bool EvaluateCondition(Dictionary<string, object> item, string condition)
        {
            try
            {
                return Eval.Execute<bool>(condition, new { item });
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Could not evaluate condition {condition}.", ex);
            }
        }

        public static object? EvaluateDynamicValue(Dictionary<string, object> item, string expression)
        {
            try
            {
                return Eval.Execute<object>(expression, new { item });
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Could not evaluate expression {expression}.", ex);
            }
        }
    }
}
