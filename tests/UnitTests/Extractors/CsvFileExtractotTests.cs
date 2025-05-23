using Library.Extractors.Csv;
using System.Diagnostics;

namespace Tests.Readers
{
    public class CsvFileExtractotTests
    {
        [Fact]
        public void Should_Read_CsvFile()
        {
            // Arrange
            using var fixture = new DefaultCsvFixture(10, 100);
            var reader = new CsvDataExtractor(fixture.Config);

            var linecount = 0;

            // Act
            reader.Extract((ref Dictionary<string, object?> row) => { linecount++; });

            // Assert
            Assert.Equal(100, linecount);
        }

        [Fact]
        public void Should_Perform_Well_With_LargeData()
        {
            var timer = new Stopwatch();
            using var fixture = new DefaultCsvFixture(1_000, 100_000);
            var reader = new CsvDataExtractor(fixture.Config);

            var linecount = 0;

            // Act
            timer.Restart();
            reader.Extract((ref Dictionary<string, object?> row) => { linecount++; });
            timer.Stop();

            // Assert
            Assert.Equal(100_000, linecount);
            Assert.True(timer.ElapsedMilliseconds < 1000, "Should read 1 million rows in less than 1 second");
        }

        [Fact]
        public void Should_Throw_FormatException_When_WrongData()
        {
            // Arrange
            using var fixture = new WrongDataCsvFixture(10, 100);
            var reader = new CsvDataExtractor(fixture.Config);

            // Act
            void act() => reader.Extract((ref Dictionary<string, object?> row) => { });

            // Assert
            Assert.Throws<FormatException>(act);
        }

        [Fact]
        public void Should_Throw_FileNotFoundException_When_File_Not_Found()
        {
            // Arrange
            using var fixture = new DefaultCsvFixture(10, 100);
            fixture.Config.FilePath = "wrong_file_path";
            var reader = new CsvDataExtractor(fixture.Config);

            // Act
            void act() => reader.Extract((ref Dictionary<string, object?> row) => { });

            // Assert
            Assert.Throws<FileNotFoundException>(act);
        }

        [Fact]
        public void Should_Throw_ArgumentNullException_When_Config_is_Null()
        {
            // Arrange
            using var fixture = new WrongDataCsvFixture(10, 100);

            // Act
            void act() => new CsvDataExtractor(null);

            // Assert
            Assert.Throws<ArgumentNullException>(act);
        }

        [Fact]
        public void Reader_Public_Properties_Must_Reflect_Atual_State()
        {
            using var fixture = new DefaultCsvFixture(10, 100);
            var reader = new CsvDataExtractor(fixture.Config);

            var fileInfo = new FileInfo(fixture.FilePath);

            reader.Extract((ref Dictionary<string, object?> row) => { });

            Assert.Equal(100, reader.LineNumber);
            Assert.Equal(fileInfo.Length, reader.FileSize);
            Assert.Equal(fileInfo.Length, reader.BytesRead);
            Assert.Equal(100, reader.PercentRead);
        }

        public static IEnumerable<object[]> GetData()
        {
            var guid = Guid.NewGuid();

            yield return new object[] { "text string", typeof(string), "text string" };
            yield return new object[] { "123", typeof(int), 123 };
            yield return new object[] { "123.45", typeof(double), 123.45 };
            yield return new object[] { "123.45", typeof(float), 123.45f };
            yield return new object[] { "123.45", typeof(decimal), 123.45m };
            yield return new object[] { "2021-01-01", typeof(DateTime), new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            yield return new object[] { "true", typeof(bool), true };
            yield return new object[] { "false", typeof(bool), false };
            yield return new object[] { "123", typeof(long), 123L };
            yield return new object[] { "123", typeof(short), (short)123 };
            yield return new object[] { guid.ToString(), typeof(Guid), guid };
        }

        // [Theory]
        // [MemberData(nameof(GetData))]
        // public void ParseValue_ShouldCorrectlyParseDifferentTypes(string input, Type type, object expected)
        // {
        //     ReadOnlySpan<char> span = input.AsSpan();
        //     object result = CsvDataExtractor.ParseValue(span, type);

        //     Assert.Equal(expected, result);
        // }
    }
}
