using Library.Infra;
using Library.Transformers;
using Moq;

namespace Tests.Transformers
{
    public class DataTransformerTests
    {
        [Fact]
        public async Task Transform_WhenConditionIsMet_ShouldApplyTransformation()
        {
            // Arrange
            var config = new TransformationConfig
            {
                Transformations = new List<TransformationFilter>
                {
                    new TransformationFilter
                    {
                        Condition = "item[\"isActive\"].ToString() == \"True\"",
                        Actions = new List<TransformationAction>
                        {
                            new TransformationAction
                            {
                                FieldMappings = new Dictionary<string, FieldMapping>
                                {
                                    { "status", new FieldMapping { Value = "Active", IsDynamic = false } }
                                }
                            }
                        }
                    }
                }
            };

            var transformer = new DataTransformer(config);
            var inputData = new[] { new Dictionary<string, object> { { "isActive", true }, { "status", "Inactive" } } }.ToAsyncEnumerable();

            // Act
            var result = transformer.Transform(inputData);

            // Assert
            await foreach (var item in result)
            {
                Assert.Equal("Active", item["status"]);
            }
        }

        [Fact]
        public async Task Transform_WhenConditionIsNotMet_ShouldNotApplyTransformation()
        {
            // Arrange
            var config = new TransformationConfig
            {
                Transformations =
                [
                    new TransformationFilter
                    {
                        Condition = "item[\"isActive\"].ToString() == \"False\"",
                        Actions =
                        [
                            new TransformationAction
                            {
                                FieldMappings = new Dictionary<string, FieldMapping>
                                {
                                    { "status", new FieldMapping { Value = "Inactive", IsDynamic = false } }
                                }
                            }
                        ]
                    }
                ]
            };

            var transformer = new DataTransformer(config);
            var inputData = new[] { new Dictionary<string, object> { { "isActive", true }, { "status", "Pending" } } }.ToAsyncEnumerable();

            // Act
            var result = await transformer.Transform(inputData).ToListAsync();

            // Assert
            Assert.False(result.Exists(x => x["status"] == "Inactive"), "");
        }

        [Fact]
        public async Task Transform_ShouldCopyValueFromOneFieldToAnotherUsingDynamicExpression()
        {
            // Arrange
            var config = new TransformationConfig
            {
                Transformations = new List<TransformationFilter>
                {
                    new TransformationFilter
                    {
                        Condition = "true", // Always true for simplicity
                        Actions = new List<TransformationAction>
                        {
                            new TransformationAction
                            {
                                FieldMappings = new Dictionary<string, FieldMapping>
                                {
                                    // Dynamically copies value from 'sourceField' to 'targetField'
                                    { "targetField", new FieldMapping { Value = "item[\"sourceField\"]", IsDynamic = true } }
                                }
                            }
                        }
                    }
                }
            };

            var transformer = new DataTransformer(config);
            var inputData = new[] { new Dictionary<string, object> { { "sourceField", "Value to Copy" } } }.ToAsyncEnumerable();

            // Act
            var result = transformer.Transform(inputData);

            // Assert
            await foreach (var item in result)
            {
                Assert.True(item.ContainsKey("targetField"), "The 'targetField' should exist in the transformed item.");
                Assert.Equal("Value to Copy", item["targetField"]);
            }
        }

        [Fact]
        public async Task Transform_ShouldDuplicateRowWithDifferentValues()
        {
            // Arrange
            var config = new TransformationConfig
            {
                Transformations = new List<TransformationFilter>
                {
                    new TransformationFilter
                    {
                        Condition = "true", // Always true for simplicity
                        Actions = new List<TransformationAction>
                        {
                            // First transformation action: modifies the original value
                            new TransformationAction
                            {
                                FieldMappings = new Dictionary<string, FieldMapping>
                                {
                                    ["newValue"] = new FieldMapping { Value = "\"Modified Value 1\"", IsDynamic = true }
                                }
                            },
                            // Second transformation action: duplicates the row and modifies the value
                            new TransformationAction
                            {
                                FieldMappings = new Dictionary<string, FieldMapping>
                                {
                                    ["newValue"] = new FieldMapping { Value = "\"Modified Value 2\"", IsDynamic = true }
                                }
                            }
                        }
                    }
                }
            };

            var transformer = new DataTransformer(config);
            var inputData = new[] { new Dictionary<string, object> { ["originalValue"] = "Original Value" } }.ToAsyncEnumerable();

            // Act
            var resultList = await transformer.Transform(inputData).ToListAsync();

            // Assert
            Assert.Equal(2, resultList.Count);
            Assert.Contains(resultList, item => item.ContainsKey("newValue") && item["newValue"].ToString() == "Modified Value 1");
            Assert.Contains(resultList, item => item.ContainsKey("newValue") && item["newValue"].ToString() == "Modified Value 2");
        }

        [Fact]
        public async Task Transform_WithTrueConditionAndNoActions_ShouldReturnNoRecords()
        {
            // Arrange
            var config = new TransformationConfig
            {
                Transformations =
                [
                    new TransformationFilter
                    {
                        Condition = "true",
                        Actions = new List<TransformationAction>()
                    }
                ]
            };

            var transformer = new DataTransformer(config);
            var inputData = new[] { new Dictionary<string, object> { ["originalValue"] = "Original Value" } }.ToAsyncEnumerable();

            // Act
            var result = await transformer.Transform(inputData).ToListAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task Transform_WithoutTransformation_ShouldReturnOriginalRow()
        {
            // Arrange
            var config = new TransformationConfig { Transformations = [] };

            var originalValue = "Original Value";
            var inputData = new[] { new Dictionary<string, object> { ["originalValue"] = originalValue } }.ToAsyncEnumerable();
            var transformer = new DataTransformer(config);

            // Act
            var resultList = await transformer.Transform(inputData).ToListAsync();

            // Assert
            Assert.Single(resultList);
            Assert.True(resultList[0].ContainsKey("originalValue"), "Result should contain the 'originalValue' key.");
            Assert.Equal(originalValue, resultList[0]["originalValue"]);
        }

        [Theory]
        [InlineData(100, 10, 90)] // Testing with 10 rows filtered out
        [InlineData(100, 0, 100)] // Testing with 0 rows filtered out
        public async Task NotifyProgress_ShouldInvokeOnTransformWithExpectedValues(long totalLines, long excluded, long expectedTransformed)
        {
            // Arrange
            int totalTimesFired = 0;
            var config = new TransformationConfig
            {
                NotifyAfter = 1,
                Transformations =
                [
                    new TransformationFilter
                    {
                        Condition = $"item[\"count\"] >= {excluded}",
                        Actions =
                        [
                            new TransformationAction
                            {
                                FieldMappings = new Dictionary<string, FieldMapping>
                                {
                                    { "status", new FieldMapping { Value = "Active", IsDynamic = false } }
                                }
                            }
                        ]
                    }
                ]
            };

            var transformer = new DataTransformer(config);
            transformer.OnTransform += args => totalTimesFired++;

            var inputData = Enumerable.Range(0, (int)totalLines)
                .Select(i => new Dictionary<string, object> { { "count", i }, { "status", "Inactive" } })
                .ToAsyncEnumerable();

            // Act
            await transformer.Transform(inputData).ToListAsync();

            // Assert
            Assert.Equal(expectedTransformed, totalTimesFired);
            Assert.Equal(totalLines, transformer.IngestedLines);
            Assert.Equal(expectedTransformed, transformer.TransformedLines);
            Assert.Equal(excluded, transformer.ExcludedByFilter);
        }

        [Fact]
        public async Task NotifyFinish_ShouldInvokeOnFinishWithExpectedValues()
        {
            // Arrange
            var config = new TransformationConfig { Transformations = [] };
            bool eventFired = false;
            var transformer = new DataTransformer(config);
            transformer.OnFinish += args => eventFired = true;

            var inputData = Enumerable.Range(0, 100)
                .Select(_ => new Dictionary<string, object> { { "originalValue", "Original Value" } })
                .ToAsyncEnumerable();

            // Act
            await transformer.Transform(inputData).ToListAsync();

            // Assert
            Assert.True(eventFired, "OnFinish event should have been fired.");
        }
    }
}


