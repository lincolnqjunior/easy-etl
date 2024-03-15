using Library.Infra;
using Library.Readers;

namespace Tests.Readers
{
    public class CsvFileReaderTests
    {
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
            File.WriteAllLines(tempFileName, ["header1,header2", "value11,value12", "value21,value22", "value31,value32"]);

            var reader = new CsvFileReader(config);
            var eventFired = false;
            reader.OnRead += (linesRead, percentRead, sizeRead, fileSize) => { eventFired = true; };

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
            File.WriteAllLines(tempFileName, new[] { "header1,header2", "value11,value12", "value21,value22" });
            var config = new FileReadConfig
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

            var reader = new CsvFileReader(config);
            var onFinishCalled = false;
            reader.OnFinish += (linesRead, percentRead, sizeRead, fileSize) => { onFinishCalled = true; };

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
    }
}