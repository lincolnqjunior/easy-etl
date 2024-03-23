using Library.Infra;
using Library.Transformers;
using Moq;
using System.Linq;

namespace Tests.Transformers
{
    public class DataTransformerTests
    {
        private readonly TransformationConfig _config = new() { NotifyAfter = 10, Transformations = [] };
        private TransformationConfig DefaultConfig => _config;

        private TransformationConfig ConfigWithFilterOnIndex
        {
            get
            {
                DefaultConfig.Transformations.Clear();
                DefaultConfig.Transformations.Add(
                       new TransformationFilter
                       {
                           Condition = "item[\"Index\"] <= 10",
                           Actions =
                            [
                                new TransformationAction
                            {
                                FieldMappings = new Dictionary<string, FieldMapping>
                                {
                                    { "New Field", new FieldMapping { Value = "New Value", IsDynamic = false } }
                                }
                            }
                            ]
                       });

                return DefaultConfig;
            }
        }

        private TransformationConfig ConfigWithDynamicCopy
        {
            get
            {
                DefaultConfig.Transformations.Clear();
                DefaultConfig.Transformations.Add(
                        new TransformationFilter
                        {
                            Condition = "true",
                            Actions =
                            [
                                new TransformationAction
                            {
                                FieldMappings = new Dictionary<string, FieldMapping>
                                {
                                    { "Target Field", new FieldMapping { Value = "item[\"Source Field\"]", IsDynamic = true } }
                                }
                            }
                            ]
                        });


                return DefaultConfig;
            }
        }

        private TransformationConfig ConfigWithDoubleAction
        {
            get
            {
                DefaultConfig.Transformations.Clear();
                DefaultConfig.Transformations.Add(
                        new TransformationFilter
                        {
                            Condition = "true", // Always true for simplicity
                            Actions =
                            [
                                // First transformation action: modifies the original value
                                new TransformationAction
                                {
                                    FieldMappings = new Dictionary<string, FieldMapping>
                                    {
                                        ["Original Value"] = new FieldMapping { Value = "\"Modified Value 1\"", IsDynamic = true }
                                    }
                                },
                                // Second transformation action: duplicates the row and modifies the value
                                new TransformationAction
                                {
                                    FieldMappings = new Dictionary<string, FieldMapping>
                                    {
                                        ["Original Value"] = new FieldMapping { Value = "\"Modified Value 2\"", IsDynamic = true }
                                    }
                                }
                            ]
                        });

                return DefaultConfig;
            }
        }

        [Fact]
        public async Task Transform_WhenConditionIsMet_ShouldApplyTransformation()
        {
            // Arrange: Initialize the transformer with a predefined configuration.
            var transformer = new DataTransformer(ConfigWithFilterOnIndex);

            // Act: Perform the transformation with an input that meets the defined condition.
            var result = transformer.Transform(GetAsyncEnumerable(new Dictionary<string, object?> { { "Index", 5 } }), new CancellationToken());

            // Assert: Verify that the transformation was applied by checking for the new field and its value.
            Assert.Equal("New Value", (await result.SingleAsync())["New Field"]);
        }

        [Fact]
        public async Task Transform_WhenConditionIsNotMet_ShouldNotApplyTransformation()
        {
            // Arrange
            var transformer = new DataTransformer(ConfigWithFilterOnIndex);

            // Create a dictionary that does not meet the condition of the transformation filter.
            // According to _config, the condition is "item[\"Index\"] <= 10".
            // Therefore, we will use a value for "Index" that does not meet this condition.
            var input = new Dictionary<string, object?> { { "Index", 15 } };

            // Act
            var result = transformer.Transform(GetAsyncEnumerable(input), new CancellationToken());

            // Assert
            // Checks if the key "New Field" was not added since the condition was not met.
            Assert.False(await result.AnyAsync(item => item.ContainsKey("New Field")), "The transformation should not be applied when the condition is not met.");
        }

        [Fact]
        public async Task Transform_WhenConditionIsMet_ShouldDynamicallyCopyFieldValue()
        {
            // Arrange the transformation configuration to dynamically copy values.
            var transformer = new DataTransformer(ConfigWithDynamicCopy);

            // Act by providing an input dictionary that includes the 'sourceField'.
            var input = new Dictionary<string, object?> { { "Source Field", "expectedValue" } };
            var result = transformer.Transform(GetAsyncEnumerable(input), new CancellationToken());

            // Assert that the value was correctly copied to 'targetField'.
            Assert.Equal("expectedValue", (await result.SingleAsync())["Target Field"]);
        }

        [Fact]
        public async Task Transform_WhenConditionIsMet_ShouldApplyDoubleAction()
        {
            // Arrange: Set up the DataTransformer with a configuration that includes two actions.
            // The first action modifies the original value, and the second duplicates the row and modifies the value again.
            var transformer = new DataTransformer(ConfigWithDoubleAction);

            // Act: Provide an input dictionary that simulates a row with an 'Original Value'.
            // This simulates processing a single row of data through the transformation pipeline.
            var input = new Dictionary<string, object?> { { "Original Value", "Original Value" } };
            var result = transformer.Transform(GetAsyncEnumerable(input), new CancellationToken());

            // Assert: Verify that both actions were applied correctly.
            // The first action should modify the original value to 'Modified Value 1',
            // and the second action should create a new row with the value modified to 'Modified Value 2'.
            Assert.Equal("Modified Value 1", (await result.FirstAsync())["Original Value"]);
            Assert.Equal("Modified Value 2", (await result.LastAsync())["Original Value"]);
        }

        public static async IAsyncEnumerable<Dictionary<string, object?>> GetAsyncEnumerable(Dictionary<string, object?> row)
        {
            var task = Task.FromResult(row);
            var result = await task;
            yield return result;
        }

        //[Fact]
        //public async Task Transform_WithTrueConditionAndNoActions_ShouldReturnNoRecords()
        //{
        //    // Arrange
        //    var config = new TransformationConfig
        //    {
        //        Transformations =
        //        [
        //            new TransformationFilter
        //            {
        //                Condition = "true",
        //                Actions = new List<TransformationAction>()
        //            }
        //        ]
        //    };

        //    var transformer = new DataTransformer(config);
        //    var inputData = new[] { new Dictionary<string, object> { ["originalValue"] = "Original Value" } }.ToAsyncEnumerable();

        //    // Act
        //    var result = await transformer.Transform(inputData).ToListAsync();

        //    // Assert
        //    Assert.Empty(result);
        //}

        //[Fact]
        //public async Task Transform_WithoutTransformation_ShouldReturnOriginalRow()
        //{
        //    // Arrange
        //    var config = new TransformationConfig { Transformations = [] };

        //    var originalValue = "Original Value";
        //    var inputData = new[] { new Dictionary<string, object> { ["originalValue"] = originalValue } }.ToAsyncEnumerable();
        //    var transformer = new DataTransformer(config);

        //    // Act
        //    var resultList = await transformer.Transform(inputData).ToListAsync();

        //    // Assert
        //    Assert.Single(resultList);
        //    Assert.True(resultList[0].ContainsKey("originalValue"), "Result should contain the 'originalValue' key.");
        //    Assert.Equal(originalValue, resultList[0]["originalValue"]);
        //}

        //[Theory]
        //[InlineData(100, 10, 90)] // Testing with 10 rows filtered out
        //[InlineData(100, 0, 100)] // Testing with 0 rows filtered out
        //public async Task NotifyProgress_ShouldInvokeOnTransformWithExpectedValues(long totalLines, long excluded, long expectedTransformed)
        //{
        //    // Arrange
        //    int totalTimesFired = 0;
        //    var config = new TransformationConfig
        //    {
        //        NotifyAfter = 1,
        //        Transformations =
        //        [
        //            new TransformationFilter
        //            {
        //                Condition = $"item[\"count\"] >= {excluded}",
        //                Actions =
        //                [
        //                    new TransformationAction
        //                    {
        //                        FieldMappings = new Dictionary<string, FieldMapping>
        //                        {
        //                            { "status", new FieldMapping { Value = "Active", IsDynamic = false } }
        //                        }
        //                    }
        //                ]
        //            }
        //        ]
        //    };

        //    var transformer = new DataTransformer(config);
        //    transformer.OnTransform += args => totalTimesFired++;

        //    var inputData = Enumerable.Range(0, (int)totalLines)
        //        .Select(i => new Dictionary<string, object> { { "count", i }, { "status", "Inactive" } })
        //        .ToAsyncEnumerable();

        //    // Act
        //    await transformer.Transform(inputData).ToListAsync();

        //    // Assert
        //    Assert.Equal(expectedTransformed, totalTimesFired);
        //    Assert.Equal(totalLines, transformer.IngestedLines);
        //    Assert.Equal(expectedTransformed, transformer.TransformedLines);
        //    Assert.Equal(excluded, transformer.ExcludedByFilter);
        //}

        //[Fact]
        //public async Task NotifyFinish_ShouldInvokeOnFinishWithExpectedValues()
        //{
        //    // Arrange
        //    var config = new TransformationConfig { Transformations = [] };
        //    bool eventFired = false;
        //    var transformer = new DataTransformer(config);
        //    transformer.OnFinish += args => eventFired = true;

        //    var inputData = Enumerable.Range(0, 100)
        //        .Select(_ => new Dictionary<string, object> { { "originalValue", "Original Value" } })
        //        .ToAsyncEnumerable();

        //    // Act
        //    await transformer.Transform(inputData).ToListAsync();

        //    // Assert
        //    Assert.True(eventFired, "OnFinish event should have been fired.");
        //}
    }
}


