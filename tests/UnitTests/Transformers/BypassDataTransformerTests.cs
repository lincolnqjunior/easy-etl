using Library.Transformers;

namespace Tests.Transformers
{
    public class BypassDataTransformerTests
    {
        [Fact]
        public async Task Transform_ShouldPassThroughDataUnchanged()
        {
            // Arrange
            int OnTransformCount = 0;
            int OnFinishCount = 0;

            var inputData = new List<Dictionary<string, object?>>
            {
                new() { { "key1", "value1" } },
                new() { { "key2", "value2" } }
            };

            var transformer = new BypassDataTransformer(1);
            transformer.OnTransform += (args) =>
            {
                OnTransformCount++;
                Assert.True(args.IngestedLines == OnTransformCount, "OnTransform: Ingested lines count error");
                Assert.True(args.TransformedLines == OnTransformCount, "OnTranform: Transformed lines count error");
                Assert.True(args.PercentDone > 0, "OnTranform: PercentDone should be greater than zero");
                Assert.True(args.TotalLines > 0, "OnTranform: TotalLines should be greater than zero");
                Assert.True(args.Speed > 0, "OnTranform: Speed should be greater than zero");
            };
            transformer.OnFinish += (args) =>
            {
                OnFinishCount++;
                Assert.True(args.IngestedLines == inputData.Count, "OnFinish: Ingested lines should match input size");
                Assert.True(args.TransformedLines == inputData.Count, "OnFinish: TransformedLines lines should match input size");
                Assert.True(args.PercentDone == 100, "OnFinish: PercentDone should be 100%");
                Assert.True(args.TotalLines == inputData.Count, "OnFinish: TotalLines lines should match input size");
                Assert.True(args.Speed > 0, "OnFinish: Speed should be greater than zero");
            };

            // Act
            var outputData = new List<Dictionary<string, object?>>();

            await foreach (var item in transformer.Transform(inputData.ToAsyncEnumerable(), CancellationToken.None))
            {
                outputData.Add(item);
            }

            // Assert
            Assert.Equal(inputData.Count, outputData.Count);
            for (int i = 0; i < inputData.Count; i++)
            {
                Assert.Equal(inputData[i], outputData[i]);
            }

            Assert.True(OnTransformCount > 0, "No OnTransform event was fired.");
            Assert.True(OnFinishCount > 0, "No OnFinish event was fired.");
        }

        [Fact]
        public void ApplyTransformations_ThrowsNotImplementedException()
        {
            // Arrange
            var transformer = new BypassDataTransformer();
            var dummyData = new Dictionary<string, object?>();

            // Act & Assert
            Assert.Throws<NotImplementedException>(() => transformer.ApplyTransformations(dummyData));
        }
    }
}
