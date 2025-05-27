using Library.Infra;

namespace Tests.Infra
{
    public class AsyncEnumerableDataReaderTests
    {
        public static IEnumerable<object[]> GetTestData()
        {
            var rowData = new Dictionary<string, object?>
            {
                { "DateTimeColumn", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                { "DecimalColumn", 123.45m },
                { "DoubleColumn", 123.456 },
                { "FloatColumn", 123.45f },
                { "GuidColumn", Guid.NewGuid() },
                { "Int16Column", (short)123 },
                { "Int32Column", 123 },
                { "Int64Column", 123L },
                { "StringColumn", "Test String" },
                { "BooleanColumn", true },
                { "ByteColumn", (byte)255 },
                { "CharColumn", 'A' }
            };

            var inputData = new List<Dictionary<string, object?>> { rowData };
            var reader = new Library.Infra.AsyncEnumerableDataReader(ConvertToAsyncEnumerable(inputData));
            reader.ReadAsync().Wait();

            return
            [
                [reader, "DateTimeColumn", rowData["DateTimeColumn"]],
                [reader, "DecimalColumn", rowData["DecimalColumn"]],
                [reader, "DoubleColumn", rowData["DoubleColumn"]],
                [reader, "FloatColumn", rowData["FloatColumn"]],
                [reader, "GuidColumn", rowData["GuidColumn"]],
                [reader, "Int16Column", rowData["Int16Column"]],
                [reader, "Int32Column", rowData["Int32Column"]],
                [reader, "Int64Column", rowData["Int64Column"]],
                [reader, "StringColumn", rowData["StringColumn"]],
                [reader, "BooleanColumn", rowData["BooleanColumn"]],
                [reader, "ByteColumn", rowData["ByteColumn"]],
                [reader, "CharColumn", rowData["CharColumn"]]
            ];
        }

        [Theory]
        [MemberData(nameof(GetTestData))]
        public void GetValue_ReturnsExpectedValue(AsyncEnumerableDataReader reader, string columnName, object expectedValue)
        {
            // Arrange
            int columnIndex = reader.GetOrdinal(columnName);

            // Act & Assert
            switch (expectedValue)
            {
                case DateTime expectedDateTime:
                    var actualDateTime = reader.GetDateTime(columnIndex);
                    Assert.Equal(expectedDateTime, actualDateTime);
                    break;
                case decimal expectedDecimal:
                    var actualDecimal = reader.GetDecimal(columnIndex);
                    Assert.Equal(expectedDecimal, actualDecimal);
                    break;
                case double expectedDouble:
                    var actualDouble = reader.GetDouble(columnIndex);
                    Assert.Equal(expectedDouble, actualDouble);
                    break;
                case float expectedFloat:
                    var actualFloat = reader.GetFloat(columnIndex);
                    Assert.Equal(expectedFloat, actualFloat);
                    break;
                case Guid expectedGuid:
                    var actualGuid = reader.GetGuid(columnIndex);
                    Assert.Equal(expectedGuid, actualGuid);
                    break;
                case short expectedInt16:
                    var actualInt16 = reader.GetInt16(columnIndex);
                    Assert.Equal(expectedInt16, actualInt16);
                    break;
                case int expectedInt32:
                    var actualInt32 = reader.GetInt32(columnIndex);
                    Assert.Equal(expectedInt32, actualInt32);
                    break;
                case long expectedInt64:
                    var actualInt64 = reader.GetInt64(columnIndex);
                    Assert.Equal(expectedInt64, actualInt64);
                    break;
                case string expectedString:
                    var actualString = reader.GetString(columnIndex);
                    Assert.Equal(expectedString, actualString);
                    break;
                case bool expectedBool:
                    var actualBool = reader.GetBoolean(columnIndex);
                    Assert.Equal(expectedBool, actualBool);
                    break;
                case byte expectedByte:
                    var actualByte = reader.GetByte(columnIndex);
                    Assert.Equal(expectedByte, actualByte);
                    break;
                case char expectedChar:
                    var actualChar = reader.GetChar(columnIndex);
                    Assert.Equal(expectedChar, actualChar);
                    break;                
            }
        }

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

            var reader = new Library.Infra.AsyncEnumerableDataReader(ConvertToAsyncEnumerable(inputData));

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
            var reader = new Library.Infra.AsyncEnumerableDataReader(ConvertToAsyncEnumerable(inputData));

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

        [Theory]
        [InlineData("FirstName", 0)] // First column name and expected index
        [InlineData("LastName", 1)] // Second column name and expected index
        [InlineData("Age", 2)] // Third column name and expected index
        public async Task GetOrdinal_ReturnsCorrectIndex_ForColumnName(string columnName, int expectedIndex)
        {
            // Setting up input data with three columns
            var inputData = new List<Dictionary<string, object?>>
            {
                new() {
                    { "FirstName", "John" },
                    { "LastName", "Doe" },
                    { "Age", 30 }
                }
            };

            // Creating an instance of the reader
            var reader = new Library.Infra.AsyncEnumerableDataReader(ConvertToAsyncEnumerable(inputData));

            // Moving to the first record
            await reader.ReadAsync();

            // Verifying that GetOrdinal returns the correct index for each column name
            int actualIndex = reader.GetOrdinal(columnName);
            Assert.Equal(expectedIndex, actualIndex);
        }

        [Fact]
        public async Task Reader_IsCorrectlyMarkedAsClosed_AfterDisposal()
        {
            // Setting up input data with any column
            var inputData = new List<Dictionary<string, object?>>
            {
                new() { { "SampleColumn", "SampleValue" } }
            };

            // Creating an instance of the reader
            var reader = new Library.Infra.AsyncEnumerableDataReader(ConvertToAsyncEnumerable(inputData));

            // Moving to the first record (not strictly necessary for this test, but mimics typical usage)
            await reader.ReadAsync();

            // Disposing the reader
            await reader.DisposeAsync();

            // Verifying that the reader is correctly marked as closed after disposal
            Assert.True(reader.IsClosed);
        }

        [Fact]
        public void Read_SynchronouslyReadsData()
        {
            // Arrange
            var inputData = new List<Dictionary<string, object?>>
            {
                new() { { "Column1", "Data1" }, { "Column2", "Data2" } }
            };

            var reader = new Library.Infra.AsyncEnumerableDataReader(ConvertToAsyncEnumerable(inputData));
            bool canRead = reader.Read();

            // Assert
            Assert.True(canRead);

            // Trying to read the next record
            bool canReadAgain = reader.Read();
            Assert.False(canReadAgain); // Should return false as there is no more data to read
        }

        // Helper method to convert IEnumerable<T> to IAsyncEnumerable<T>
        private static async IAsyncEnumerable<T> ConvertToAsyncEnumerable<T>(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                await Task.Yield(); // Simulate asynchrony
                yield return item;
            }
        }
    }
}
