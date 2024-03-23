using Library.Infra;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Library.Extractors.Json
{
    public class JsonDataExtractor(DataExtractorConfig config) : IDataExtractor
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        private readonly DataExtractorConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly Stopwatch _timer = new();

        public long TotalLines { get; set; } = 0;
        public int LineNumber { get; set; } = 0;
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }

        public async IAsyncEnumerable<Dictionary<string, object?>> ReadAsync()
        {
            FileSize = ValidateFile(config.FilePath).Length;
            BytesRead = 0L;

            using var stream = File.OpenRead(config.FilePath);
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader) { SupportMultipleContent = true };
            var serializer = new JsonSerializer();

            while (await jsonReader.ReadAsync())
            {
                Dictionary<string, object?> dic = new();
                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    try
                    {
                        LineNumber++;
                        var jObject = serializer.Deserialize<JObject>(jsonReader) ?? throw new InvalidOperationException("Failed to deserialize JObject.");
                        BytesRead += System.Text.Encoding.UTF8.GetByteCount(jObject.ToString());
                        dic = ProcessJsonObject(jObject);
                        NotifyProgress();
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, ex, dic, LineNumber));
                        break;
                        throw;
                    }

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

        private Dictionary<string, object?> ProcessJsonObject(JObject jObject)
        {
            var dic = new Dictionary<string, object?>();

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
                TotalLines = LineNumber;
                PercentRead = CalculatePercentage(BytesRead, FileSize);
                double speed = LineNumber / _timer.Elapsed.TotalSeconds;
                OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
            }
        }

        private void NotifyFinish()
        {
            TotalLines = LineNumber;
            PercentRead = 100;
            double speed = LineNumber / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
        }

        private static double CalculatePercentage(long bytesRead, long fileSize) => (double)bytesRead / fileSize * 100;

        public void Extract(RowAction processRow)
        {
            throw new NotImplementedException();
        }
    }
}
