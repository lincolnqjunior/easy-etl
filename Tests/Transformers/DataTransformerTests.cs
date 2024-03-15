using Library.Infra;
using Library.Transformers;

namespace Tests.Transformers
{
    public class DataTransformerTests
    {
        [Fact]
        public async Task Transform_WithConditionMet_AppliesTransformation()
        {
            // Arrange
            var config = new TransformationConfig
            {
                Transformations =
                [
                    new TransformationFilter
                {
                    Condition = "item[\"isActive\"].ToString() == \"True\"",
                    Actions =
                    [
                        new TransformationActions
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

            var inputData = new Dictionary<string, object>[] { new() { { "isActive", true }, { "status", "Inactive" } } }.ToAsyncEnumerable();

            // Act
            var result = transformer.Transform(inputData);

            // Assert
            await foreach (var item in result)
            {
                Assert.Equal("Active", item["status"]);
            }
        }

        [Fact]
        public async Task Transform_WithConditionNotMet_DoesNotApplyTransformation()
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
                            new TransformationActions
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

            var inputData = new Dictionary<string, object>[] { new() { { "isActive", true }, { "status", "Pending" } } }.ToAsyncEnumerable();

            // Act
            var result = transformer.Transform(inputData);

            // Assert
            await foreach (var item in result)
            {
                Assert.NotEqual("Inactive", item["status"]);
            }
        }

        [Fact]
        public async Task Transform_ShouldCopyValueFromOneFieldToAnotherUsingDynamicExpression()
        {
            // Arrange
            var config = new TransformationConfig
            {
                Transformations =
                [
                    new TransformationFilter
                    {
                        Condition = "true", // Sempre verdadeiro para simplificar o exemplo
                        Actions =
                        [
                            new TransformationActions
                            {
                                FieldMappings = new Dictionary<string, FieldMapping>
                                {
                                    // Copia o valor de 'sourceField' para 'targetField' dinamicamente
                                    { "targetField", new FieldMapping { Value = "item[\"sourceField\"]", IsDynamic = true } }
                                }
                            }
                        ]
                    }
                ]
            };

            var transformer = new DataTransformer(config);

            var inputData = new Dictionary<string, object>[] { new() { { "sourceField", "Value to Copy" } } }.ToAsyncEnumerable();

            // Act
            var result = transformer.Transform(inputData);

            // Assert
            await foreach (var item in result)
            {
                Assert.True(item.ContainsKey("targetField"), "The targetField should exist in the transformed item.");
                Assert.Equal("Value to Copy", item["targetField"]);
            }
        }

        [Fact]
        public async Task Transform_ShouldDuplicateRowWithDifferentValues()
        {
            // Arrange
            var config = new TransformationConfig
            {
                Transformations =
            [
                new TransformationFilter
                {
                    Condition = "true", // Always true for simplicity
                    Actions =
                    [
                        // Primeira ação de transformação: modifica o valor original
                        new TransformationActions
                        {
                            FieldMappings = new Dictionary<string, FieldMapping>
                            {
                                ["newValue"] = new FieldMapping { Value = "\"Modified Value 1\"", IsDynamic = true }
                            }
                        },
                        // Segunda ação de transformação: duplica a linha e modifica o valor
                        new TransformationActions
                        {
                            FieldMappings = new Dictionary<string, FieldMapping>
                            {
                                ["newValue"] = new FieldMapping { Value = "\"Modified Value 2\"", IsDynamic = true }
                            }
                        }
                    ]
                }
            ]
            };

            var transformer = new DataTransformer(config);
            var inputData = new[] { new Dictionary<string, object> { ["originalValue"] = "Original Value" } }.ToAsyncEnumerable();

            // Act
            var result = transformer.Transform(inputData).ToListAsync();

            // Assert
            var resultList = await result;
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
                        Actions = []
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
        public async Task Transform_WithEmptyFilter_ShouldReturnOriginalRow()
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
            Assert.True(resultList[0].ContainsKey("originalValue"), "O resultado deve conter a chave 'originalValue'.");
            Assert.Equal(originalValue, resultList[0]["originalValue"]);
        }
    }
}
