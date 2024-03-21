
using CSVFile;
using Library.Readers;
using nietras.SeparatedValues;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Library.Writers
{
    public delegate void WriteNotification(WriteNotificationEventArgs args);

    public class CsvFileWriter(CsvFileWriterConfig config) : IDataWriter
    {
        public event WriteNotification? OnWrite;
        public event WriteNotification? OnFinish;

        private readonly CsvFileWriterConfig _config = config;

        public long CurrentLine { get; set; } = 0;
        public long TotalLines { get; set; } = 0;
        public double PercentWriten { get; set; } = 0;

        private readonly Stopwatch _timer = new();

        public async Task Write(IAsyncEnumerable<Dictionary<string, object>> data)
        {
            _timer.Restart();
            var writer = Sep.New(_config.Delimiter).Writer().ToFile(_config.OutputPath);

            await foreach (var row in data)
            {
                await Task.Run(() => ProcessRow(writer, row));
                PercentWriten = (double)CurrentLine / TotalLines * 100;

                if (CurrentLine % _config.NotifyAfter == 0)
                    OnWrite?.Invoke(new WriteNotificationEventArgs(CurrentLine, TotalLines, PercentWriten, CurrentLine / _timer.Elapsed.TotalSeconds));
            }

            _timer.Stop();
            OnFinish?.Invoke(new WriteNotificationEventArgs(CurrentLine, TotalLines, 100, CurrentLine / _timer.Elapsed.TotalSeconds));
        }

        public void ProcessRow(SepWriter writer, Dictionary<string, object> row)
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
                else line[kvp.Key].Set(kvp.Value.ToString());
            }

            CurrentLine++;
        }
    }
}
