using Library.Infra;
using Xunit;

namespace Tests.Infra
{
    public class TransformationTests
    {
        [Fact]
        public void TransformationFilter_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var filter = new TransformationFilter();

            // Assert
            Assert.Equal("true", filter.Condition);
            Assert.NotNull(filter.Actions);
            Assert.Empty(filter.Actions);
        }

        [Fact]
        public void TransformationFilter_CustomCondition_ShouldAccept()
        {
            // Arrange & Act
            var filter = new TransformationFilter
            {
                Condition = "item[\"Status\"] == \"Active\""
            };

            // Assert
            Assert.Equal("item[\"Status\"] == \"Active\"", filter.Condition);
        }

        [Fact]
        public void TransformationFilter_WithActions_ShouldAccept()
        {
            // Arrange & Act
            var filter = new TransformationFilter
            {
                Actions =
                [
                    new TransformationAction
                    {
                        FieldMappings = new Dictionary<string, FieldMapping>
                        {
                            ["Name"] = new FieldMapping { Value = "Test", IsDynamic = false }
                        }
                    }
                ]
            };

            // Assert
            Assert.NotNull(filter.Actions);
            Assert.Single(filter.Actions);
        }

        [Fact]
        public void TransformationAction_WithStaticMapping_ShouldAccept()
        {
            // Arrange & Act
            var action = new TransformationAction
            {
                FieldMappings = new Dictionary<string, FieldMapping>
                {
                    ["Field1"] = new FieldMapping { Value = "StaticValue", IsDynamic = false },
                    ["Field2"] = new FieldMapping { Value = "123", IsDynamic = false }
                }
            };

            // Assert
            Assert.NotNull(action.FieldMappings);
            Assert.Equal(2, action.FieldMappings.Count);
            Assert.Equal("StaticValue", action.FieldMappings["Field1"].Value);
            Assert.False(action.FieldMappings["Field1"].IsDynamic);
        }

        [Fact]
        public void TransformationAction_WithDynamicMapping_ShouldAccept()
        {
            // Arrange & Act
            var action = new TransformationAction
            {
                FieldMappings = new Dictionary<string, FieldMapping>
                {
                    ["DynamicField"] = new FieldMapping { Value = "item[\"SourceField\"]", IsDynamic = true }
                }
            };

            // Assert
            Assert.True(action.FieldMappings["DynamicField"].IsDynamic);
            Assert.Equal("item[\"SourceField\"]", action.FieldMappings["DynamicField"].Value);
        }

        [Fact]
        public void FieldMapping_StringValue_ShouldAccept()
        {
            // Arrange & Act
            var mapping = new FieldMapping { Value = "TestString", IsDynamic = false };

            // Assert
            Assert.Equal("TestString", mapping.Value);
            Assert.False(mapping.IsDynamic);
        }

        [Fact]
        public void FieldMapping_NumericValue_ShouldAccept()
        {
            // Arrange & Act
            var mapping = new FieldMapping { Value = "42", IsDynamic = false };

            // Assert
            Assert.Equal("42", mapping.Value);
        }

        [Fact]
        public void FieldMapping_BooleanExpression_ShouldAccept()
        {
            // Arrange & Act
            var mapping = new FieldMapping { Value = "item[\"IsActive\"] == true", IsDynamic = true };

            // Assert
            Assert.True(mapping.IsDynamic);
            Assert.Contains("IsActive", mapping.Value as string);
        }

        [Fact]
        public void FieldMapping_ComplexExpression_ShouldAccept()
        {
            // Arrange & Act  
            var mapping = new FieldMapping 
            { 
                Value = "item[\"Price\"] * 1.1", 
                IsDynamic = true 
            };

            // Assert
            Assert.True(mapping.IsDynamic);
            Assert.Contains("Price", mapping.Value as string);
            Assert.Contains("*", mapping.Value as string);
        }

        [Fact]
        public void TransformationFilter_MultipleActions_ShouldAccept()
        {
            // Arrange & Act
            var filter = new TransformationFilter
            {
                Condition = "item[\"Type\"] == \"Premium\"",
                Actions =
                [
                    new TransformationAction
                    {
                        FieldMappings = new Dictionary<string, FieldMapping>
                        {
                            ["Name"] = new FieldMapping { Value = "Test", IsDynamic = false }
                        }
                    }
                ]
            };

            // Assert
            Assert.Single(filter.Actions);
            Assert.Single(filter.Actions[0].FieldMappings);
        }

        [Fact]
        public void TransformationAction_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var action = new TransformationAction();

            // Assert
            Assert.NotNull(action.FieldMappings);
            Assert.Empty(action.FieldMappings);
        }

        [Fact]
        public void TransformationAction_WithFieldMappings_ShouldAccept()
        {
            // Arrange & Act
            var action = new TransformationAction
            {
                FieldMappings = new Dictionary<string, FieldMapping>
                {
                    ["Field1"] = new FieldMapping { Value = "Value1", IsDynamic = false },
                    ["Field2"] = new FieldMapping { Value = "item[\"OldField\"]", IsDynamic = true }
                }
            };

            // Assert
            Assert.Equal(2, action.FieldMappings.Count);
            Assert.False(action.FieldMappings["Field1"].IsDynamic);
            Assert.True(action.FieldMappings["Field2"].IsDynamic);
        }

        [Fact]
        public void FieldMapping_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var mapping = new FieldMapping();

            // Assert
            Assert.Equal(string.Empty, mapping.Value);
            Assert.False(mapping.IsDynamic);
        }

        [Fact]
        public void FieldMapping_WithStaticValue_ShouldAccept()
        {
            // Arrange & Act
            var mapping = new FieldMapping
            {
                Value = "Static Value",
                IsDynamic = false
            };

            // Assert
            Assert.Equal("Static Value", mapping.Value);
            Assert.False(mapping.IsDynamic);
        }

        [Fact]
        public void FieldMapping_WithDynamicValue_ShouldAccept()
        {
            // Arrange & Act
            var mapping = new FieldMapping
            {
                Value = "item[\"SourceField\"].ToString().ToUpper()",
                IsDynamic = true
            };

            // Assert
            Assert.Equal("item[\"SourceField\"].ToString().ToUpper()", mapping.Value);
            Assert.True(mapping.IsDynamic);
        }

        [Fact]
        public void FieldMapping_WithNumericValue_ShouldAccept()
        {
            // Arrange & Act
            var mapping = new FieldMapping
            {
                Value = 42,
                IsDynamic = false
            };

            // Assert
            Assert.Equal(42, mapping.Value);
        }

        [Fact]
        public void FieldMapping_WithBoolValue_ShouldAccept()
        {
            // Arrange & Act
            var mapping = new FieldMapping
            {
                Value = true,
                IsDynamic = false
            };

            // Assert
            Assert.Equal(true, mapping.Value);
        }
    }
}
