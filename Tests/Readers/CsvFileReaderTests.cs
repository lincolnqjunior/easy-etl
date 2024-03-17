using Library.Infra.ColumnActions;
using Library.Readers;

namespace Tests.Readers
{
    public class CsvFileReaderTests
    {
        private readonly string[] defaultCsv = ["header1,header2", "value11,value12", "value21,value22", "value31,value32"];

        private readonly FileReadConfig config = new()
        {
            HasHeader = true,
            Delimiter = ',',
            NotifyAfter = 2,
            ColumnsConfig =
            [
                new DefaultColumnAction("Header 1", 0, true, "Cabeçalho", typeof(string)),
                new DefaultColumnAction("Campo 1", 0, false, "Campo", typeof(string)),
            ]
        };

        [Fact]
        public async Task Read_FiresOnReadEventAfterConfiguredLines()
        {
            // Configuração inicial
            var tempFileName = Path.GetTempFileName();
            File.WriteAllLines(tempFileName, defaultCsv);

            var reader = new CsvFileReader(config);
            var eventFired = false;
            reader.OnRead += args => { eventFired = true; };

            // Execução
            await foreach (var _ in reader.Read(tempFileName)) { }

            // Limpeza
            File.Delete(tempFileName);

            // Assertiva
            Assert.True(eventFired, "O evento OnRead deve ser disparado após o número configurado de linhas ter sido lido.");
        }

        [Fact]
        public async Task Read_FiresOnFinishEventAfterCompletion()
        {
            var tempFileName = Path.GetTempFileName();
            File.WriteAllLines(tempFileName, defaultCsv);            

            var reader = new CsvFileReader(config);
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
            var reader = new CsvFileReader(config);

            // Act & Assert
            var exception = await Record.ExceptionAsync(async () =>
            {
                await foreach (var _ in reader.Read("nonexistent.csv")) { }
            });

            Assert.IsType<FileNotFoundException>(exception);
        }

        [Fact]
        public async Task Read_UpdatesLineNumberCorrectly()
        {
            // Configuração inicial
            var tempFileName = Path.GetTempFileName();
            File.WriteAllLines(tempFileName, defaultCsv);

            var reader = new CsvFileReader(config);

            var count = 0;
            await foreach (var _ in reader.Read(tempFileName))
            {
                count++;
                Assert.Equal(count, reader.LineNumber);
            }

            File.Delete(tempFileName);
        }

        [Fact]
        public async Task Read_UpdatesBytesReadAndFileSizeCorrectly()
        {
            var tempFileName = Path.GetTempFileName();
            File.WriteAllLines(tempFileName, defaultCsv);

            var reader = new CsvFileReader(config);

            long fileSize = new FileInfo(tempFileName).Length;
            await foreach (var _ in reader.Read(tempFileName)) { }

            Assert.True(reader.BytesRead > 0);
            Assert.Equal(fileSize, reader.FileSize);

            File.Delete(tempFileName);
        }

        [Fact]
        public async Task Read_UpdatesPercentReadCorrectly()
        {
            var tempFileName = Path.GetTempFileName();
            File.WriteAllLines(tempFileName, defaultCsv);

            var reader = new CsvFileReader(config);

            await foreach (var _ in reader.Read(tempFileName)) { }

            Assert.Equal(100, reader.PercentRead);

            File.Delete(tempFileName);
        }
    }
}