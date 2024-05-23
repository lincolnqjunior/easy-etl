using Library.Infra.EventArgs;
using Library.Loaders.Csv;
using Moq;

namespace Tests.Loaders
{
    public class CsvDataLoaderTests
    {
        private static IEnumerable<Dictionary<string, object?>> GetTestData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new Dictionary<string, object?> {
                    { "Index", i },
                    { "StringValue", "text" },
                    { "ShortValue", (short)123 },
                    { "LongValue", 123L },
                    { "DoubleValue", 123.45 },
                    { "DecimalValue", 123.45m },
                    { "FloatValue", 123.45f },
                    { "DateTimeValue", new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { "BoolValue", true },
                    { "GuidValue", Guid.NewGuid() },
                    { "NullValue", null }
                };
            }
        }

        private static async IAsyncEnumerable<Dictionary<string, object?>> GetAsyncEnumerable(IEnumerable<Dictionary<string, object?>> data)
        {
            foreach (var row in data)
            {
                yield return row;
                await Task.Delay(1);
            }
        }

        [Theory]
        [InlineData(1, 10, 10)]
        [InlineData(2, 10, 5)]
        [InlineData(3, 10, 3)]
        [InlineData(4, 10, 2)]
        [InlineData(5, 10, 2)]
        public async Task Load_ShouldRaiseOnWriteEventCorrectly(int notifyAfter, int rowsOfData, int timesCalled)
        {
            // Arrange
            var config = new CsvDataLoaderConfig
            {
                OutputPath = $"output-{notifyAfter}.csv",
                RaiseChangeEventAfer = notifyAfter
            };

            File.Delete(config.OutputPath);

            bool writeEventRaised = false;
            int writeEventCount = 0;
            bool finishEventRaised = false;
            
            
            var loader = new CsvDataLoader(config);
            loader.OnWrite +=args => { writeEventCount++; writeEventRaised = true; };
            loader.OnFinish += args => { finishEventRaised = true; };

            var data = GetAsyncEnumerable(GetTestData(rowsOfData));

            // Act
            await loader.Load(data, CancellationToken.None);

            // Assert
            Assert.True(writeEventRaised, "The OnWrite event was not fired indicating the load may not have completed.");
            Assert.True(finishEventRaised, "The OnFinish event was not fired indicating the load may not have completed.");
            Assert.Equal(timesCalled, writeEventCount);
        }

        // [Fact]
        // public async Task Load_ShouldInvokeOnErrorEventWhenExceptionOccurs()
        // {
        //     // Arrange
        //     var config = new CsvDataLoaderConfig { OutputPath = "T:\\inexistent-path\\output.csv" };

        //     var loader = new CsvDataLoader(config);
        //     var data = GetAsyncEnumerable(GetTestData(1));

        //     var mockOnError = new Mock<EasyEtlErrorEventHandler>();
        //     loader.OnError += mockOnError.Object;

        //     // Act
        //     await loader.Load(data, CancellationToken.None);

        //     // Assert
        //     mockOnError.Verify(handler => handler(It.IsAny<ErrorNotificationEventArgs>()), Times.Once);
        // }

        [Fact]
        public async Task Load_ShouldFailOnUnsupportedTypes()
        {
            // Arrange
            var config = new CsvDataLoaderConfig { OutputPath = "output.csv" };

            var loader = new CsvDataLoader(config);
            var data = GetAsyncEnumerable(
            [
                new Dictionary<string, object?> { { "UnsupportedType", GetType() } }
            ]);

            var mockOnError = new Mock<EasyEtlErrorEventHandler>();
            loader.OnError += mockOnError.Object;

            // Act
            await loader.Load(data, CancellationToken.None);

            // Assert
            mockOnError.Verify(handler => handler(It.IsAny<ErrorNotificationEventArgs>()), Times.Once);
        }
    }
}
