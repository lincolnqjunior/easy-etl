using Library;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Readers;
using Library.Transformers;
using Library.Writers;
using Newtonsoft.Json;
using Tests.Configs;

namespace Tests.Integration
{
    public class EasyEtlTests
    {
        [Fact]
        public void Should_Extract_Transform_Load()
        {
            // Arrange
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ColumnActionConverter());

            var filePath = Path.GetTempFileName() + ".csv";
            var readConfig = JsonConvert.DeserializeObject<FileReadConfig>(StaticFiles.default_reader_config, settings);
            var transformConfig = JsonConvert.DeserializeObject<TransformationConfig>(StaticFiles.dynamic_transform_over_default_csv_config);
            var writeConfig = JsonConvert.DeserializeObject<CsvFileWriterConfig>(StaticFiles.default_csv_writer_config);

            File.WriteAllText(filePath, StaticFiles.default_csv_file);

            var reader = new CsvFileReader(readConfig);
            var transformer = new DataTransformer(transformConfig);
            var writer = new CsvFileWriter(writeConfig);

            var etl = new EasyEtl(reader, transformer, writer);

            // Assert
            long expectedTransform = 0;
            reader.OnFinish += args =>
            {
                Assert.Equal(1000, args.LineCount);
            };
            transformer.OnFinish += args =>
            {
                expectedTransform = args.TransformedLines;
                Assert.Equal(100, args.ExcludedByFilter);
            };
            writer.OnFinish += args =>
            {
                Assert.Equal(100, args.WritePercentage);
                Assert.Equal(expectedTransform, args.LineCount);
            };

            // Act
            etl.Init(filePath);
            File.Delete(filePath);            
        }
    }
}
