using Library.Infra.ColumnActions;

namespace Tests.Infra.ColumnActions
{
    public class DefaultColumnActionTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_ThrowsArgumentException_ForWhiteSpaceName(string name)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new DefaultColumnAction(name, 0, true, "OutputName", typeof(string)));

            // Optionally verify the exception message or specific parameter name
            Assert.Contains(nameof(DefaultColumnAction.Name), exception.Message);
        }

        [Theory]
        [InlineData(null)]
        public void Constructor_ThrowsArgumentException_ForNullName(string name)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new DefaultColumnAction(name, 0, true, "OutputName", typeof(string)));

            // Optionally verify the exception message or specific parameter name
            Assert.Contains(nameof(DefaultColumnAction.Name), exception.Message);
        }

        [Fact]
        public void Constructor_ThrowsArgumentException_ForNegativePosition()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new DefaultColumnAction("Name", -1, true, "OutputName", typeof(string)));

            // Optional check of the exception's message or parameter name
            Assert.Contains("Position", exception.Message);
        }

        [Fact]
        public void Constructor_ThrowsArgumentException_ForInvalidOutputType()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new DefaultColumnAction("Name", 0, true, "OutputName", typeof(DefaultColumnAction)));

            // Optional check of the exception's message or parameter name
            Assert.Contains("Input OutputType did not satisfy the options (Parameter 'OutputType')", exception.Message);
        }

        [Theory]
        [InlineData("test", typeof(string))]
        [InlineData(123, typeof(int))]
        [InlineData(null, typeof(int?))]
        [InlineData(1.9, typeof(decimal))]
        [InlineData(true, typeof(bool))]
        public void ExecuteAction_ReturnsSameValue(object data, Type type)
        {
            // Arrange
            var action = new DefaultColumnAction("Name", 0, true, "OutputName", type);
            var testValue = data;

            // Act
            var result = action.ExecuteAction(testValue);

            // Assert
            Assert.Equal(testValue, result);
        }

        [Fact]
        public void ExecuteAction_WithNullValue_ForStringType_ShouldReturnEmptyString()
        {
            // Arrange
            var action = new DefaultColumnAction("Name", 0, true, "OutputName", typeof(string));

            // Act
            var result = action.ExecuteAction(null);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void ExecuteAction_WithNullValue_ForNonNullableInt_ShouldReturnDefaultInt()
        {
            // Arrange
            var action = new DefaultColumnAction("Name", 0, true, "OutputName", typeof(int));

            // Act
            var result = action.ExecuteAction(null);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void ExecuteAction_WithNullValue_ForNonNullableBool_ShouldReturnDefaultBool()
        {
            // Arrange
            var action = new DefaultColumnAction("Name", 0, true, "OutputName", typeof(bool));

            // Act
            var result = action.ExecuteAction(null);

            // Assert
            Assert.Equal(false, result);
        }
    }
}