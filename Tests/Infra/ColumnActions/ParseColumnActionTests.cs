using Library.Infra.ColumnActions;

namespace Tests.Infra.ColumnActions
{
    public class ParseColumnActionTests
    {
        public static List<object[]> ShoudConvertValues =>
        [
            ["123", typeof(int), 123],
            ["true", typeof(bool), true],
            ["2021-01-01", typeof(DateTime), new DateTime(2021,01,01)]
        ];

        [Theory]
        [MemberData(nameof(ShoudConvertValues))]
        public void ExecuteAction_WhenConversionIsValid_ShouldConvertValue(string inputValue, Type outputType, object expectedValue)
        {
            // Arrange
            var columnAction = new ParseColumnAction("TestColumn", 0, true, "TestColumn", outputType);

            // Act
            var result = columnAction.ExecuteAction(inputValue);

            // Assert
            Assert.Equal(expectedValue, expectedValue);
        }

        [Fact]
        public void ExecuteAction_WhenValueIsNullOrEmpty_ShouldReturnNull()
        {
            // Arrange
            var columnAction = new ParseColumnAction("TestColumn", 0, true, "TestColumn", typeof(int));

            // Act
            var result = columnAction.ExecuteAction("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ExecuteAction_WhenConversionIsInvalid_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var columnAction = new ParseColumnAction("TestColumn", 0, true, "TestColumn", typeof(int));

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => columnAction.ExecuteAction("invalid"));
            Assert.Contains("Failed to parse value", ex.Message);
        }
    }
}