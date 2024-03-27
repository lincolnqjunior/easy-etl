namespace Tests.Infra
{
    public class AsyncEnumerableDataReaderTests
    {
        [Theory]
        [InlineData("Name", "John Doe", 0)]
        [InlineData("Age", 30, 1)]
        public async Task ReadAsync_FetchesCorrectValues(string columnName, object expectedValue, int columnIndex)
        {
            // Arrange
            var inputData = new List<Dictionary<string, object?>> {
                new() {
                    { "Name", "John Doe" },
                    { "Age", 30 }
                }
            };

            var reader = new Library.Infra.AsyncEnumerableDataReader(inputData.ToAsyncEnumerable());

            // Act & Assert
            bool hasData = await reader.ReadAsync();
            Assert.True(hasData);

            // Testing access to the value by column name
            var valueByName = reader[columnName];
            Assert.Equal(expectedValue, valueByName);

            // Testing access to the value by column index
            var valueByIndex = reader[columnIndex];
            Assert.Equal(expectedValue, valueByIndex);

            // Verify that the reader has no more data
            hasData = await reader.ReadAsync();
            Assert.False(hasData);
        }

        [Theory]
        //[InlineData(new object?[] { }, 0, true)] // Column with null value        
        [InlineData(new object?[] { null }, 0, true)] // Column with null value        
        [InlineData(new object?[] { "Not Null" }, 0, false)] // Column with a non-null string
        [InlineData(new object?[] { 0 }, 0, false)] // Column with a non-null integer value
        public async Task IsDBNull_ReturnsExpectedResult(object?[] rowValues, int columnIndex, bool expected)
        {
            // Arrange
            var inputData = new List<Dictionary<string, object?>>
            {
                new(rowValues.Select((value, index) => new KeyValuePair<string, object?>($"Column{index}", value)))
            };

            // Creating an instance of the reader with input data
            var reader = new Library.Infra.AsyncEnumerableDataReader(inputData.ToAsyncEnumerable());

            // Act & Assert
            bool hasData = await reader.ReadAsync();
            Assert.True(hasData); // Ensuring that data is successfully read

            // Testing if IsDBNull correctly identifies null columns
            bool result = reader.IsDBNull(columnIndex);
            Assert.Equal(expected, result); // Asserting that the result matches the expected outcome

            // Verifying that no more data is available for reading
            hasData = await reader.ReadAsync();
            Assert.False(hasData); // Ensuring that no further data is read
        }
    }
}
