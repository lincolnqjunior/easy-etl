using Library;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Readers;
using Library.Transformers;
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
            var config = JsonConvert.DeserializeObject<FileReadConfig>(StaticFiles.default_reader_config, settings);
            var transformConfig = JsonConvert.DeserializeObject<TransformationConfig>(StaticFiles.dynamic_transform_over_default_csv_config);

            File.WriteAllText(filePath, StaticFiles.default_csv_file);

            var reader = new CsvFileReader(config);
            var transformer = new DataTransformer(transformConfig);
            var etl = new EasyEtl(reader, transformer);

            long expectedRead = 0;
            long expectedTransform = 0;
            long ignored = 0;

            reader.OnFinish += args =>
            {
                expectedRead = args.LineCount;
            };
            transformer.OnFinish += args =>
            {
                expectedTransform = args.IngestedLines;
                ignored = args.ExcludedByFilter;
            };

            // Act
            etl.Init(filePath);

            // Assert
            Assert.Equal(1000, expectedRead);
            Assert.Equal(1000, expectedTransform);
            Assert.Equal(100, ignored);

            File.Delete(filePath);
        }
    }
}
