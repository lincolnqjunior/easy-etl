using Library.Infra;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Library.Readers
{
    public class JsonFileReader(FileReadConfig config) : IFileReader
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;

        private int _objectNumber = 0;
        private readonly FileReadConfig _config = config;

        public async IAsyncEnumerable<Dictionary<string, object>> Read(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) throw new FileNotFoundException("File not found", filePath);
            var fileSize = fileInfo.Length;
            var bytesRead = 0;

            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            var serializer = new JsonSerializer();

            while (await jsonReader.ReadAsync())
            {
                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    _objectNumber++;
                    var jObject = serializer.Deserialize<JObject>(jsonReader) ??
                        throw new FieldAccessException($"Unable to deserialize json object. Path: {jsonReader.Path}");

                    bytesRead += jObject.ToString().Length;
                    var dic = new Dictionary<string, object>();

                    foreach (var property in jObject.Properties())
                    {
                        var action = _config.ColumnsConfig.Find(x => x.Name == property.Name);
                        if (action == null) continue;

                        if (property.Value != null)
                        {
                            var jsonValue = property.Value.ToObject(action.OutputType) ?? 
                                throw new InvalidOperationException($"Invalid ColumnConfig OutputType for column {action.Name}");
                            
                            var value = ColumnActionFactory.CreateAction(action).ExecuteAction(jsonValue);
                            dic.Add(action.OutputName ?? action.Name, value);
                        }
                    }

                    if (_objectNumber % _config.NotifyAfter == 0)
                    {
                        double percentRead = (double)bytesRead / fileSize * 100;
                        OnRead?.Invoke(_objectNumber, percentRead, bytesRead, fileSize);
                    }

                    yield return dic;
                }
            }

            OnFinish?.Invoke(_objectNumber, 100, fileSize, fileSize);
        }
    }
}
