using Library.Infra;
using Library.Transformers;
using Moq;

namespace Tests.Transformers
{
    public class DynamicDataTransformerTests
    {
        private readonly TransformationConfig _config = new() { RaiseChangeEventAfer = 10, Transformations = [] };

        public static async IAsyncEnumerable<Dictionary<string, object?>> GetAsyncEnumerable(Dictionary<string, object?> row)
        {
            var task = Task.FromResult(row);
            var result = await task;
            yield return result;
        }

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
            var transformer = new DynamicDataTransformer(ConfigWithFilterOnIndex);

            // Act: Perform the transformation with an input that meets the defined condition.
            var result = transformer.Transform(GetAsyncEnumerable(new Dictionary<string, object?> { { "Index", 5 } }), new CancellationToken());

            // Assert: Verify that the transformation was applied by checking for the new field and its value.
            Assert.Equal("New Value", (await result.SingleAsync())["New Field"]);
        }

        [Fact]
        public async Task Transform_WhenConditionIsNotMet_ShouldNotApplyTransformation()
        {
            // Arrange
            var transformer = new DynamicDataTransformer(ConfigWithFilterOnIndex);

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
            var transformer = new DynamicDataTransformer(ConfigWithDynamicCopy);

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
            var transformer = new DynamicDataTransformer(ConfigWithDoubleAction);

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

        [Fact]
        public async Task Transform_WithNoConfiguredTransformations_ShouldReturnItemUnchanged()
        {
            // Arrange
            var transformer = new DynamicDataTransformer(new TransformationConfig { RaiseChangeEventAfer = 10, Transformations = new List<TransformationFilter>() });
            var input = new Dictionary<string, object?> { { "Key", "Value" } };

            // Act
            var result = transformer.Transform(GetAsyncEnumerable(input), new CancellationToken());

            // Assert
            Assert.Equal("Value", (await result.SingleAsync())["Key"]);
        }

        [Fact]
        public async Task Transform_ShouldNotifyProgressCorrectly()
        {
            // Arrange            
            var mockHandler = new Mock<TransformNotificationHandler>();
            var transformer = new DynamicDataTransformer(DefaultConfig with { RaiseChangeEventAfer = 1 });
            transformer.OnTransform += mockHandler.Object;
            transformer.TotalLines = 5;

            // Act: 
            var input = new Dictionary<string, object?> { { "Index", 1 } };
            await foreach (var _ in transformer.Transform(GetAsyncEnumerable(input), new CancellationToken())) { }

            // Assert:
            mockHandler.Verify(h => h(It.IsAny<TransformNotificationEventArgs>()), Times.AtLeastOnce());
        }

        [Fact]
        public async Task Transform_ShouldNotifyFinishCorrectly()
        {
            // Arrange
            var mockHandler = new Mock<TransformNotificationHandler>();
            var transformer = new DynamicDataTransformer(DefaultConfig);
            transformer.OnFinish += mockHandler.Object;
            transformer.TotalLines = 1; // Simular um total de 1 linha para processamento para garantir a chamada de NotifyFinish

            // Act: Simular o processamento
            var input = new Dictionary<string, object?> { { "Key", "Value" } };
            await foreach (var _ in transformer.Transform(GetAsyncEnumerable(input), new CancellationToken())) { }

            // Assert: Verificar se o evento OnFinish foi acionado com os valores esperados
            mockHandler.Verify(h => h(It.IsAny<TransformNotificationEventArgs>()), Times.Once());
        }

        [Fact]
        public async Task Transform_WhenExceptionOccurs_ShouldInvokeOnErrorEvent()
        {
            // Arrange
            var faultyConfig = DefaultConfig with
            {
                Transformations = [
                new TransformationFilter { Condition = "true",
                    Actions = [
                        new TransformationAction {
                            FieldMappings = new Dictionary<string, FieldMapping> {
                                { "FaultyField", new FieldMapping { Value = "will throw", IsDynamic = true }
                            }
                        }
                    }]
                }]
            };

            bool exceptionThrown = false;
            var transformer = new DynamicDataTransformer(faultyConfig);
            transformer.OnError += args => { exceptionThrown = true; };

            // Act
            var input = new Dictionary<string, object?> { { "Key", "Value" } };
            var result = transformer.Transform(GetAsyncEnumerable(input), CancellationToken.None);
            await foreach (var _ in result) { }

            // Assert
            Assert.True(exceptionThrown, "A exception should have been thrown.");
        }
    }
}