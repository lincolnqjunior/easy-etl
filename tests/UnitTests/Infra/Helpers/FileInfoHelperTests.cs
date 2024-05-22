using Library.Infra.Helpers;
using System.Text;

namespace Tests.Infra.Helpers
{
    public class FileInfoHelperTests
    {
        private const string TestDirectory = nameof(FileInfoHelperTests);

        public FileInfoHelperTests()
        {
            Directory.CreateDirectory(TestDirectory);
        }

        internal void Dispose()
        {
            Directory.Delete(TestDirectory, recursive: true);
        }

        [Theory]
        [InlineData(0, "utf-8")]
        [InlineData(1, "utf-8")]
        [InlineData(10, "utf-8")]
        [InlineData(0, "utf-16")]
        [InlineData(1, "utf-16")]
        [InlineData(10, "utf-16")]
        [InlineData(0, "utf-32")]
        [InlineData(1, "utf-32")]
        [InlineData(10, "utf-32")]
        public async Task CountLines_ShouldReturnCorrectLineCount(int expectedLineCount, string encodingName)
        {
            // Arrange
            Encoding encoding = Encoding.GetEncoding(encodingName);
            string filePath = Path.Combine(TestDirectory, $"test_{expectedLineCount}_{encodingName}.txt");
            CreateTestFile(filePath, expectedLineCount, encoding);

            // Act
            long lineCount = await new FileInfo(filePath).CountLines(encoding);

            // Assert
            Assert.Equal(expectedLineCount, lineCount);
            Dispose();
        }

        private static void CreateTestFile(string filePath, int lineCount, Encoding encoding)
        {
            using StreamWriter sw = new(filePath, false, encoding);
            for (int i = 0; i < lineCount; i++)
            {
                sw.WriteLine($"Line {i}");
            }
        }
    }

}
