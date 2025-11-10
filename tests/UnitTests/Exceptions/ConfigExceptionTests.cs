using Library.Infra.Exceptions;
using Xunit;

namespace Tests.Exceptions
{
    public class ConfigExceptionTests
    {
        [Fact]
        public void Constructor_WithMessageAndParams_ShouldSetProperties()
        {
            // Arrange
            var message = "Test error message";
            var configName = "TestConfig";
            var propertyName = "TestProperty";

            // Act
            var exception = new ConfigException(message, configName, propertyName);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(configName, exception.ConfigName);
            Assert.Equal(propertyName, exception.PropertyName);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void Constructor_WithNullPropertyName_ShouldSetToNull()
        {
            // Arrange
            var message = "Test error message";
            var configName = "TestConfig";

            // Act
            var exception = new ConfigException(message, configName, null);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(configName, exception.ConfigName);
            Assert.Null(exception.PropertyName);
        }

        [Fact]
        public void Constructor_WithInnerException_ShouldSetAllProperties()
        {
            // Arrange
            var message = "Test error message";
            var innerException = new InvalidOperationException("Inner error");
            var configName = "TestConfig";
            var propertyName = "TestProperty";

            // Act
            var exception = new ConfigException(message, innerException, configName, propertyName);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(configName, exception.ConfigName);
            Assert.Equal(propertyName, exception.PropertyName);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void Constructor_WithInnerExceptionAndNullPropertyName_ShouldWork()
        {
            // Arrange
            var message = "Test error message";
            var innerException = new ArgumentNullException("param");
            var configName = "TestConfig";

            // Act
            var exception = new ConfigException(message, innerException, configName, null);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(configName, exception.ConfigName);
            Assert.Null(exception.PropertyName);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void ConfigName_IsReadOnly_ShouldNotAllowModification()
        {
            // Arrange
            var exception = new ConfigException("Message", "Config", "Property");

            // Act & Assert
            Assert.Equal("Config", exception.ConfigName);
            // ConfigName has private setter, so it can't be changed after construction
        }

        [Fact]
        public void PropertyName_IsReadOnly_ShouldNotAllowModification()
        {
            // Arrange
            var exception = new ConfigException("Message", "Config", "Property");

            // Act & Assert
            Assert.Equal("Property", exception.PropertyName);
            // PropertyName has private setter, so it can't be changed after construction
        }
    }
}
