using Library.Infra;

namespace Tests.Infra
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
        public void ExecuteAction_ReturnsStringValue()
        {
            // Arrange
            var action = new DefaultColumnAction("Name", 0, true, "OutputName", typeof(string));
            var testValue = 123; // A test value that is not a string

            // Act
            var result = action.ExecuteAction(testValue);

            // Assert
            Assert.Equal(testValue.ToString(), result);
        }

        [Fact]
        public void ExecuteAction_ReturnsEmptyStringForNullValue()
        {
            // Arrange
            var action = new DefaultColumnAction("Name", 0, true, "OutputName", typeof(string));

            // Act
            var result = action.ExecuteAction(null);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}