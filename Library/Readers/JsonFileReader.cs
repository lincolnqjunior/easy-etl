using Library.Infra;
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

        public int LineNumber { get; set; } = 0;
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }


        public JsonFileReader(FileReadConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async IAsyncEnumerable<Dictionary<string, object>> ReadAsync(string filePath)
        {
            FileSize = ValidateFile(filePath).Length;
            BytesRead = 0L;

            using var stream = File.OpenRead(filePath);
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader) { SupportMultipleContent = true };
            var serializer = new JsonSerializer();

            while (await jsonReader.ReadAsync())
            {
                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    LineNumber++;
                    var jObject = serializer.Deserialize<JObject>(jsonReader) ?? throw new InvalidOperationException("Failed to deserialize JObject.");
                    BytesRead += System.Text.Encoding.UTF8.GetByteCount(jObject.ToString());
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

                var value = action.ExecuteAction(jsonValue);
                dic.Add(action.OutputName ?? action.Name, value);
            }

            return dic;
        }

        private void NotifyProgress()
        {
            if (LineNumber % _config.NotifyAfter == 0)
            {
                PercentRead = CalculatePercentage(BytesRead, FileSize);
                double speed = LineNumber / _timer.Elapsed.TotalSeconds;
                OnRead?.Invoke(new ReadNotificationEventArgs(LineNumber, FileSize, BytesRead, PercentRead, speed));
            }
        }

        private void NotifyFinish()
        {
            PercentRead = 100;
            double speed = LineNumber / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new ReadNotificationEventArgs(LineNumber, FileSize, BytesRead, PercentRead, speed));
        }

        private static double CalculatePercentage(long bytesRead, long fileSize) => (double)bytesRead / fileSize * 100;        

        public void Read(string filePath, RowAction processRow)
        {
            throw new NotImplementedException();
        }
    }
}
