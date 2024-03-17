using Library.Infra.ColumnActions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Library.Readers
{
    public class JsonFileReader : IFileReader
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;
                
        private readonly FileReadConfig _config;
        private readonly Stopwatch _timer = new();

        private int _objectNumber = 0;
        private long _fileSize;
        private double _percentRead;
        private long _bytesRead;        

        public int LineNumber => _objectNumber;
        public long BytesRead => _bytesRead;
        public double PercentRead => _percentRead;
        public long FileSize => _fileSize;


        public JsonFileReader(FileReadConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async IAsyncEnumerable<Dictionary<string, object>> Read(string filePath)
        {
            _fileSize = ValidateFile(filePath).Length;
            _bytesRead = 0L;

            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader) { SupportMultipleContent = true };
            var serializer = new JsonSerializer();

            while (await jsonReader.ReadAsync())
            {
                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    _objectNumber++;
                    var jObject = serializer.Deserialize<JObject>(jsonReader) ?? throw new InvalidOperationException("Failed to deserialize JObject.");
                    _bytesRead += System.Text.Encoding.UTF8.GetByteCount(jObject.ToString());
                    var dic = ProcessJsonObject(jObject);
                    NotifyProgress();
                    yield return dic;
                }
            }

            NotifyFinish();
        }

        private static FileInfo ValidateFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) throw new FileNotFoundException("File not found", filePath);
            return fileInfo;
        }

        private Dictionary<string, object> ProcessJsonObject(JObject jObject)
        {
            var dic = new Dictionary<string, object>();

            foreach (var property in jObject.Properties())
            {
                var action = _config.ColumnsConfig.Find(x => x.Name == property.Name);
                if (action == null) continue;

                var jsonValue = property.Value?.ToObject(action.OutputType);
                if (jsonValue == null) continue;

                var value = ColumnActionFactory.CreateAction(action).ExecuteAction(jsonValue);
                dic.Add(action.OutputName ?? action.Name, value);
            }

            return dic;
        }

        private void NotifyProgress()
        {
            if (_objectNumber % _config.NotifyAfter == 0)
            {
                _percentRead = CalculatePercentage(_bytesRead, _fileSize);
                double speed = _objectNumber / _timer.Elapsed.TotalSeconds;
                OnRead?.Invoke(new ReadNotificationEventArgs(_objectNumber, _fileSize, _bytesRead, _percentRead, speed));
            }
        }

        private void NotifyFinish()
        {
            _percentRead = 100;
            double speed = _objectNumber / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new ReadNotificationEventArgs(_objectNumber, _fileSize, _bytesRead, _percentRead, speed));
        }

        private static double CalculatePercentage(long bytesRead, long fileSize) => (double)bytesRead / fileSize * 100;
    }
}
