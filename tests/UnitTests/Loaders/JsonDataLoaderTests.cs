using Library.Infra.Config;
using Library.Infra.EventArgs;
using Library.Loaders.Json;
using Moq;

namespace Tests.Loaders
{
    public class JsonDataLoaderTests
    {
        private static IEnumerable<Dictionary<string, object?>> GetTestData(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new Dictionary<string, object?> { { "Index", i }, { "Value", $"Value{i}" } };
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
            var config = new JsonDataLoaderConfig
            {
                OutputPath = "output.json",
                RaiseChangeEventAfer = notifyAfter,
                IsJsonl = true
            };

            var mockOnWriteHandler = new Mock<LoadNotificationHandler>();
            var loader = new JsonDataLoader(config);
            loader.OnWrite += mockOnWriteHandler.Object;

            var data = GetAsyncEnumerable(GetTestData(rowsOfData));

            // Act
            await loader.Load(data, CancellationToken.None);

            // Assert
            mockOnWriteHandler.Verify(handler => handler(It.IsAny<LoadNotificationEventArgs>()), Times.Exactly(timesCalled));
            File.Delete(config.OutputPath);
        }

        // [Theory]
        // [InlineData(true)]
        // [InlineData(false)]
        // public async Task Load_ShouldInvokeOnErrorEventWhenExceptionOccurs(bool isJsonLines)
        // {
        //     // Arrange
        //     var config = new JsonDataLoaderConfig { OutputPath = "T:\\inexistent-path\\output.json", IsJsonl = isJsonLines };

        //     var loader = new JsonDataLoader(config);
        //     var data = GetAsyncEnumerable(GetTestData(1));

        //     bool exceptionThrown = false;
        //     Exception? exception = null;

        //     loader.OnError += args =>
        //     {
        //         exceptionThrown = true;
        //         exception = args.Exception;
        //     };

        //     // Act
        //     await loader.Load(data, CancellationToken.None);

        //     // Assert
        //     Assert.True(exceptionThrown, "A exception should be thrown.");
        //     Assert.IsType<DirectoryNotFoundException>(exception);
        // }

        // [Theory]
        // [InlineData(false, "[{\"Index\":0,\"Value\":\"Value0\"}]")]
        // [InlineData(true, "{\"Index\":0,\"Value\":\"Value0\"}\r\n")]
        // public async Task Load_ShouldWriteCorrectJson(bool isJsonLines, string expectedJson)
        // {
        //     // Arrange
        //     var config = new JsonDataLoaderConfig { OutputPath = "output.json", IsJsonl = isJsonLines };
        //     var loader = new JsonDataLoader(config);
        //     var data = GetAsyncEnumerable(GetTestData(1));

        //     // Act
        //     await loader.Load(data, CancellationToken.None);
        //     var json = await File.ReadAllTextAsync(config.OutputPath);

        //     // Assert
        //     Assert.Equal(expectedJson, json);
        //     File.Delete(config.OutputPath);
        // }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Load_ShouldInvokeOnErrorEventWhenPropertyAccessFails(bool isJsonLines)
        {
            // Arrange
            var config = new JsonDataLoaderConfig { OutputPath = "output.json", IsJsonl = isJsonLines };
            var loader = new JsonDataLoader(config);

            var data = GetAsyncEnumerable([new() { { "Faulty", new FaultyProperty() } }]);

            var mockOnError = new Mock<EasyEtlErrorEventHandler>();
            loader.OnError += mockOnError.Object;

            // Act & Assert
            await loader.Load(data, CancellationToken.None);
            mockOnError.Verify(handler => handler(It.IsAny<ErrorNotificationEventArgs>()), Times.Once());
            File.Delete(config.OutputPath);
        }

        private class FaultyProperty
        {
            public string Value => throw new InvalidOperationException("Simulated property access exception.");
        }
    }
}
