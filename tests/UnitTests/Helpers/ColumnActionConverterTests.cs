using Library.Infra.ColumnActions;
using System.Text.Json;
using Xunit;

namespace UnitTests.Helpers
{
    public class ColumnActionConverterTests
    {
        private readonly ColumnActionConverter _converter;
        private readonly JsonSerializerOptions _options;

        public ColumnActionConverterTests()
        {
            _converter = new ColumnActionConverter();
            _options = new JsonSerializerOptions
            {
                Converters = { _converter }
            };
        }

        [Fact]
        public void Read_DefaultColumnAction_ShouldDeserializeCorrectly()
        {
            // Arrange
            var json = @"{
                ""Type"": ""DefaultColumnAction"",
                ""OutputName"": ""TestColumn"",
                ""Position"": 1,
                ""IsHeader"": false,
                ""OutputType"": ""System.Int32""
            }";

            // Act
            var result = JsonSerializer.Deserialize<IColumnAction>(json, _options);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DefaultColumnAction>(result);
            Assert.Equal("TestColumn", result.OutputName);
            Assert.Equal(1, result.Position);
            Assert.False(result.IsHeader);
            Assert.Equal(typeof(int), result.OutputType);
        }

        [Fact]
        public void Read_ParseColumnAction_ShouldDeserializeCorrectly()
        {
            // Arrange
            var json = @"{
                ""Type"": ""ParseColumnAction"",
                ""OutputName"": ""ParsedColumn"",
                ""Position"": 2,
                ""IsHeader"": true,
                ""OutputType"": ""System.Double""
            }";

            // Act
            var result = JsonSerializer.Deserialize<IColumnAction>(json, _options);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ParseColumnAction>(result);
            Assert.Equal("ParsedColumn", result.OutputName);
            Assert.Equal(2, result.Position);
            Assert.True(result.IsHeader);
            Assert.Equal(typeof(double), result.OutputType);
        }

        [Fact]
        public void Read_WithoutType_ShouldDefaultToDefaultColumnAction()
        {
            // Arrange
            var json = @"{
                ""OutputName"": ""DefaultTypeColumn"",
                ""Position"": 0
            }";

            // Act
            var result = JsonSerializer.Deserialize<IColumnAction>(json, _options);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DefaultColumnAction>(result);
        }

        [Fact]
        public void Read_WithNullableType_ShouldHandleCorrectly()
        {
            // Arrange
            var json = @"{
                ""Type"": ""DefaultColumnAction"",
                ""OutputName"": ""NullableColumn"",
                ""Position"": 3,
                ""IsHeader"": false,
                ""OutputType"": ""System.Int32?""
            }";

            // Act
            var result = JsonSerializer.Deserialize<IColumnAction>(json, _options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(typeof(int?), result.OutputType);
        }

        [Fact]
        public void Read_WithoutOutputType_ShouldDefaultToString()
        {
            // Arrange
            var json = @"{
                ""OutputName"": ""StringColumn"",
                ""Position"": 4
            }";

            // Act
            var result = JsonSerializer.Deserialize<IColumnAction>(json, _options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(typeof(string), result.OutputType);
        }

        [Fact]
        public void Read_WithoutOutputName_ShouldThrowArgumentException()
        {
            // Arrange
            var json = @"{
                ""Type"": ""DefaultColumnAction"",
                ""Position"": 5
            }";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                JsonSerializer.Deserialize<IColumnAction>(json, _options));
        }

        [Fact]
        public void Read_WithInvalidType_ShouldThrowNotImplementedException()
        {
            // Arrange
            var json = @"{
                ""Type"": ""UnknownColumnAction"",
                ""OutputName"": ""TestColumn"",
                ""Position"": 6
            }";

            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                JsonSerializer.Deserialize<IColumnAction>(json, _options));
        }

        [Fact]
        public void Read_WithInvalidOutputType_ShouldThrowArgumentException()
        {
            // Arrange
            var json = @"{
                ""Type"": ""DefaultColumnAction"",
                ""OutputName"": ""TestColumn"",
                ""Position"": 7,
                ""OutputType"": ""Some.Invalid.Type""
            }";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                JsonSerializer.Deserialize<IColumnAction>(json, _options));
        }

        [Fact]
        public void Read_WithInvalidNullableType_ShouldThrowArgumentException()
        {
            // Arrange
            var json = @"{
                ""Type"": ""DefaultColumnAction"",
                ""OutputName"": ""TestColumn"",
                ""Position"": 8,
                ""OutputType"": ""Some.Invalid.Type?""
            }";

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                JsonSerializer.Deserialize<IColumnAction>(json, _options));
        }

        [Fact]
        public void Write_ShouldThrowNotImplementedException()
        {
            // Arrange
            var columnAction = new DefaultColumnAction("Test", 0, false, "Output", typeof(string));

            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
                JsonSerializer.Serialize<IColumnAction>(columnAction, _options));
        }

        [Fact]
        public void Read_WithDifferentOutputName_ShouldUseIt()
        {
            // Arrange
            var json = @"{
                ""Type"": ""ParseColumnAction"",
                ""OutputName"": ""SourceName"",
                ""Position"": 9,
                ""IsHeader"": false,
                ""OutputType"": ""System.String""
            }";

            // Act
            var result = JsonSerializer.Deserialize<IColumnAction>(json, _options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SourceName", result.OutputName);
        }

        [Fact]
        public void Read_WithoutPosition_ShouldDefaultToZero()
        {
            // Arrange
            var json = @"{
                ""Type"": ""DefaultColumnAction"",
                ""OutputName"": ""NoPositionColumn""
            }";

            // Act
            var result = JsonSerializer.Deserialize<IColumnAction>(json, _options);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Position);
        }

        [Fact]
        public void Read_WithoutIsHeader_ShouldDefaultToFalse()
        {
            // Arrange
            var json = @"{
                ""Type"": ""DefaultColumnAction"",
                ""OutputName"": ""NoHeaderColumn"",
                ""Position"": 10
            }";

            // Act
            var result = JsonSerializer.Deserialize<IColumnAction>(json, _options);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsHeader);
        }
    }
}
