using Library;
using Library.Extractors;
using Library.Extractors.Csv;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Loaders.Json;
using Library.Transformers;
using Newtonsoft.Json;
using Tests.Configs;

namespace Tests.Integration
{
    public class EasyEtlTests
    {
        [Fact]
        public async Task Should_Extract_Transform_Load()
        {
            // Arrange
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ColumnActionConverter());

            var filePath = Path.GetTempFileName() + ".csv";
            var readConfig = JsonConvert.DeserializeObject<DataExtractorConfig>(StaticFiles.default_reader_config, settings) ?? throw new Exception();
            readConfig.FilePath=filePath;

            var transformConfig = JsonConvert.DeserializeObject<TransformationConfig>(StaticFiles.dynamic_transform_over_default_csv_config) ?? throw new Exception(); 
            var writeConfig = JsonConvert.DeserializeObject<JsonDataLoaderConfig>(StaticFiles.default_json_writer_config) ?? throw new Exception();

            File.WriteAllText(filePath, StaticFiles.default_csv_file);

            var reader = new CsvDataExtractor(readConfig);
            var transformer = new DataTransformer(transformConfig);
            var writer = new JsonDataLoader(writeConfig);

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

            etl.OnComplete += args =>
            {
                
            };

            // Act
            await etl.Execute();
            File.Delete(filePath);
        }
    }
}
