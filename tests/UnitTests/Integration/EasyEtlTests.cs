using Library;
using Library.Extractors.Csv;
using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Loaders.Json;
using Library.Transformers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tests.Configs;

namespace Tests.Integration
{
    [Trait("Category", "IntegrationTest")]
    public class EasyEtlTests
    {
        [Fact]
        public async Task Should_Extract_Transform_Load()
        {
            // // Arrange
            // var settings = new JsonSerializerSettings();
            // settings.Converters.Add(new ColumnActionConverter());

            // var filePath = Path.GetTempFileName() + ".csv";
            // var readConfig = JsonConvert.DeserializeObject<CsvDataExtractorConfig>(StaticFiles.default_reader_config, settings) ?? throw new Exception();
            // readConfig.FilePath = filePath;

            // var transformConfig = JsonConvert.DeserializeObject<TransformationConfig>(StaticFiles.dynamic_transform_over_default_csv_config) ?? throw new Exception();
            // var writeConfig = JsonConvert.DeserializeObject<JsonDataLoaderConfig>(StaticFiles.default_json_writer_config) ?? throw new Exception();

            // File.WriteAllText(filePath, StaticFiles.default_csv_file);

            // var extractor = new CsvDataExtractor(readConfig);
            // var transformer = new DynamicDataTransformer(transformConfig);
            // var loader = new JsonDataLoader(writeConfig);

            // var etl = new EasyEtl(extractor, transformer, loader);

            // // Assert
            // long expectedTransform = 0;
            // bool exceptionThrow = false;
            // bool completed = false;
            // bool extractCompleted = false;
            // bool transformCompleted = false;
            // bool loadCompleted = false;

            // extractor.OnFinish += args =>
            // {
            //     extractCompleted = true;
            //     Assert.Equal(1000, args.LineCount);
            // };
            // transformer.OnFinish += args =>
            // {
            //     transformCompleted = true;
            //     expectedTransform = args.TransformedLines;
            //     Assert.Equal(100, args.ExcludedByFilter);
            // };
            // loader.OnFinish += args =>
            // {
            //     loadCompleted = true;
            //     Assert.Equal(100, args.WritePercentage);
            //     Assert.Equal(expectedTransform, args.LineCount);
            // };

            // etl.OnComplete += args => completed = true;
            // etl.OnError += args => exceptionThrow = true;

            // // Act
            // await etl.Execute();
            // Assert.True(extractCompleted, "The extract process did not complete successfully.");
            // Assert.True(transformCompleted, "The transform process did not complete successfully.");
            // Assert.True(loadCompleted, "The load process did not complete successfully.");
            // Assert.True(completed, "The ETL process did not complete successfully.");
            // Assert.False(exceptionThrow, "An exception was thrown during the ETL process.");

            // File.Delete(filePath);
        }
    }
}
