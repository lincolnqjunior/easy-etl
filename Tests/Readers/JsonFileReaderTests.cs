using Library.Infra.ColumnActions;
using Library.Readers;
using Newtonsoft.Json;

namespace Tests.Readers
{
    public class JsonFileReaderTests
    {
        private readonly FileReadConfig config = new()
        {
            HasHeader = true,
            Delimiter = ',',
            NotifyAfter = 2,
            ColumnsConfig =
            [
                new DefaultColumnAction("String", 0, false, "String", typeof(string)),
                new DefaultColumnAction("Int32", 0, false, "Int32", typeof(int)),
                new DefaultColumnAction("DateTime", 0, false, "DateTime", typeof(DateTime)),
                new DefaultColumnAction("Double", 0, false, "Double", typeof(double)),
            ]
        };

        private static string WellFormedJson => "[{\"String\":\"value1\",\"Int32\":1,\"DateTime\":\"2021-01-01T00:00:00\",\"Double\":1.1},{\"String\":\"value2\",\"Int32\":2,\"DateTime\":\"2021-01-02T00:00:00\",\"Double\":2.2}]";
        private static string InvalidDateJson => "[{\"String\":\"value1\",\"Int32\":1,\"DateTime\":\"XXXX-XX-XXTXX:XX:XX\",\"Double\":1.1},{\"String\":\"value2\",\"Int32\":2,\"DateTime\":\"2021-01-02T00:00:00\",\"Double\":2.2}]";
        private static string MalformedJson => "[{\"String:\"value1\",\"Int32\":1,\"DateTime\":\"XXXX-XX-XXTXX:XX:XX\",\"Double\":1.1},{\"String\":\"value2\",\"Int32\":2,\"DateTime\":\"2021-01-02T00:00:00\",\"Double\":2.2}]";

        [Fact]
        public async Task Read_FiresOnReadEventAfterConfiguredObjects()
        {
            // Configuração inicial
            var tempFileName = Path.GetTempFileName();
            File.WriteAllText(tempFileName, WellFormedJson);

            var reader = new JsonFileReader(config);
            var eventFired = false;
            reader.OnRead += args => { eventFired = true; };

            // Execução
            await foreach (var _ in reader.Read(tempFileName)) { }

            // Limpeza
            File.Delete(tempFileName);

            // Assertiva
            Assert.True(eventFired, "O evento OnRead deve ser disparado após o número configurado de objetos ter sido lido.");
        }

        [Fact]
        public async Task Read_FiresOnFinishEventAfterCompletion()
        {
            var tempFileName = Path.GetTempFileName();
            File.WriteAllText(tempFileName, WellFormedJson);

            var reader = new JsonFileReader(config);
            var onFinishCalled = false;
            reader.OnFinish += args => { onFinishCalled = true; };

            // Execução
            await foreach (var _ in reader.Read(tempFileName)) { }

            // Limpeza
            File.Delete(tempFileName);

            // Assertiva
            Assert.True(onFinishCalled, "O evento OnFinish deve ser disparado após a conclusão da leitura do arquivo.");
        }

        [Fact]
        public async Task Read_ThrowsFileNotFoundException_ForNonexistentFile()
        {
            // Arrange
            var reader = new JsonFileReader(config);

            // Act & Assert
            var exception = await Record.ExceptionAsync(async () =>
            {
                await foreach (var _ in reader.Read("nonexistent.json")) { }
            });

            Assert.IsType<FileNotFoundException>(exception);
        }

        [Fact]
        public async Task Read_ThrowsFormatExceptionOnInvalidDate()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, InvalidDateJson);
            var reader = new JsonFileReader(config);

            // Act & Assert
            var exception = await Record.ExceptionAsync(async () =>
            {
                await foreach (var _ in reader.Read(tempFile)) { }
            });

            Assert.IsType<FormatException>(exception);

            // Cleanup
            File.Delete(tempFile);
        }

        [Fact]
        public async Task Read_ThrowsJsonReaderExceptionOnMalformed()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, MalformedJson);
            var reader = new JsonFileReader(config);

            // Act & Assert
            var exception = await Record.ExceptionAsync(async () =>
            {
                await foreach (var _ in reader.Read(tempFile)) { }
            });

            Assert.IsType<JsonReaderException>(exception);

            // Cleanup
            File.Delete(tempFile);
        }
    }
}
