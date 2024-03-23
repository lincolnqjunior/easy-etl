using Library.Infra;
using nietras.SeparatedValues;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Library.Loaders.Csv
{
    public class CsvDataLoader(CsvDataLoaderConfig config) : IDataLoader
    {
        public event LoadNotificationHandler? OnWrite;
        public event LoadNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        private readonly CsvDataLoaderConfig _config = config;

        public long CurrentLine { get; set; } = 0;
        public long TotalLines { get; set; } = 0;
        public double PercentWritten { get; set; } = 0;

        private readonly Stopwatch _timer = new();

        public async Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken)
        {
            _timer.Restart();
            var writer = Sep.New(_config.Delimiter).Writer().ToFile(_config.OutputPath);

            await foreach (var row in data)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Run(() => HandleRow(writer, row), cancellationToken);
                    PercentWritten = (double)CurrentLine / TotalLines * 100;

                    if (CurrentLine % _config.NotifyAfter == 0)
                        OnWrite?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, PercentWritten, CurrentLine / _timer.Elapsed.TotalSeconds));
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Load, ex, row, CurrentLine));
                }
            }

            _timer.Stop();
            OnFinish?.Invoke(new LoadNotificationEventArgs(CurrentLine, TotalLines, 100, CurrentLine / _timer.Elapsed.TotalSeconds));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void HandleRow(SepWriter writer, Dictionary<string, object?> row)
        {
            using var line = writer.NewRow();

            foreach (var kvp in row)
            {
                if (kvp.Value is string strValue) line[kvp.Key].Set(strValue);
                else if (kvp.Value is int intValue) line[kvp.Key].Format(intValue);
                else if (kvp.Value is short shortValue) line[kvp.Key].Format(shortValue);
                else if (kvp.Value is long longValue) line[kvp.Key].Format(longValue);
                else if (kvp.Value is double doubleValue) line[kvp.Key].Format(doubleValue);
                else if (kvp.Value is decimal decimalValue) line[kvp.Key].Format(decimalValue);
                else if (kvp.Value is float floatValue) line[kvp.Key].Format(floatValue);
                else if (kvp.Value is DateTime dateValue) line[kvp.Key].Format(dateValue);
                else if (kvp.Value is bool boolValue) line[kvp.Key].Set(boolValue.ToString());
                else if (kvp.Value is Guid guidValue) line[kvp.Key].Format(guidValue);
                else line[kvp.Key].Set(kvp.Value?.ToString());
            }

            CurrentLine++;
        }
    }
}
