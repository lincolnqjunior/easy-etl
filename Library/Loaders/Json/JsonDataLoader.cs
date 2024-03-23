using Library.Infra;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Library.Loaders.Json
{
    public class JsonDataLoader(JsonDataLoaderConfig config) : IDataLoader
    {
        private readonly JsonDataLoaderConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly Stopwatch _timer = new();

        public event LoadNotificationHandler? OnWrite;
        public event LoadNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long CurrentLine { get; set; }
        public long TotalLines { get; set; }
        public double PercentWritten { get; set; }

        public async Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)
        {
            _timer.Restart();            

            if (_config.IsJsonl) await AsJsonLines(data, cancellationToken);
            else await AsJson(data, cancellationToken);

            _timer.Stop();

            OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, 100, CurrentLine / _timer.Elapsed.TotalSeconds));
        }

        private async Task AsJson(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)
        {
            await using var outputStream = new FileStream(_config.OutputPath, FileMode.Create, FileAccess.Write);
            await using var writer = new Utf8JsonWriter(outputStream, new JsonWriterOptions { Indented = _config.IndentJson });
            var settings = new JsonSerializerOptions { WriteIndented = _config.IndentJson };

            writer.WriteStartArray();

            await foreach (var record in data)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    JsonSerializer.Serialize(writer, record, settings);
                    CurrentLine++;
                    UpdateProgress();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, record, CurrentLine));
                }               
            }

            writer.WriteEndArray();
            await writer.FlushAsync(cancellationToken);
        }

        private async Task AsJsonLines(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)
        {
            await using var outputStream = new FileStream(_config.OutputPath, FileMode.Create, FileAccess.Write);
            var settings = new JsonSerializerOptions { WriteIndented = false };
            await foreach (var record in data)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var jsonLine = JsonSerializer.Serialize(record, settings);
                    await outputStream.WriteAsync(Encoding.UTF8.GetBytes(jsonLine + Environment.NewLine), cancellationToken);
                    CurrentLine++;
                    UpdateProgress();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, record, CurrentLine));
                }                
            }
        }

        private void UpdateProgress()
        {
            PercentWritten = (double)CurrentLine / TotalLines * 100;
            if (CurrentLine % _config.NotifyAfter == 0)
            {
                OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, PercentWritten, CurrentLine / _timer.Elapsed.TotalSeconds));
            }
        }
    }
}
