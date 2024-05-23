using Library.Infra.Helpers;

namespace Tests.Infra
{
    public class DynamicEvaluatorTests
    {
        [Theory]
        [InlineData("item[\"key\"] == true", true)] // Boolean true
        [InlineData("item[\"number\"] > 5.5", true)] // Comparing double
        [InlineData("item[\"date\"].Year == 2020", true)] // Year of a date
        [InlineData("item[\"text\"].ToString().StartsWith(\"Test\")", true)] // string starts with
        [InlineData("item[\"number\"] == 6 ? true : false", true)] // ternary operator
        public void EvaluateCondition_WithVariousExpressions_ShouldEvaluateCorrectly(string condition, bool expected)
        {
            // Arrange
            var item = new Dictionary<string, object?>
            {
                ["key"] = true,
                ["number"] = 6.0,
                ["date"] = new DateTime(year: 2020, month: 1, day: 1, hour: 12, minute: 00, second: 00, kind: DateTimeKind.Utc),
                ["text"] = "Test String"
            };

            // Act
            var result = DynamicEvaluator.EvaluateCondition(item, condition);

            // Assert
            Assert.Equal(expected, result);
        }       

        [Fact]
        public void EvaluateCondition_WithInvalidCondition_ShouldThrowInvalidCastException()
        {
            // Arrange
            var item = new Dictionary<string, object?> { ["number"] = 5 };
            string condition = "invalid condition";

            // Act & Assert
            var exception = Assert.Throws<InvalidCastException>(() => DynamicEvaluator.EvaluateCondition(item, condition));
            Assert.Contains("Could not evaluate condition", exception.Message);
        }

        [Theory]
        [InlineData("item[\"number\"] + 100", 106.0)] // double sum
        [InlineData("item[\"text\"].ToString().ToUpper()", "TEST STRING")] // string operations
        [InlineData("item[\"date\"].AddYears(1).Year", 2021)] // DateTime manipulation        
        [InlineData("item[\"nullableNumber\"] ?? 10", 10)] // Null coalescing when value is null
        [InlineData("item.ContainsKey(\"missing\") ? \"found\" : \"not found\"", "not found")] // Ternary operator
        public void EvaluateDynamicValue_WithVariousExpressions_ShouldEvaluateCorrectly(string expression, object expected)
        {
            // Arrange
            var item = new Dictionary<string, object?>
            {
                ["key"] = true,
                ["number"] = 6.0,
                ["date"] = new DateTime(year: 2020, month: 1, day: 1, hour: 12, minute: 00, second: 00, kind: DateTimeKind.Utc),
                ["text"] = "Test String",
                ["nullableNumber"] = default(int?)
            };

            // Act & Assert
            var result = DynamicEvaluator.EvaluateDynamicValue(item, expression);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void EvaluateDynamicValue_WithInvalidExpression_ShouldThrowInvalidCastException()
        {
            // Arrange
            var item = new Dictionary<string, object?> { ["number"] = 5 };
            string expression = "invalid expression";

            // Act & Assert
            var exception = Assert.Throws<InvalidCastException>(() => DynamicEvaluator.EvaluateDynamicValue(item, expression));
            Assert.Contains("Could not evaluate expression", exception.Message);
        }
    }
}
