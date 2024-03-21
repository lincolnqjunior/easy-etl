using Library.Infra.ColumnActions;
using nietras.SeparatedValues;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]
namespace Library.Readers
{
    public delegate void ReadNotification(ReadNotificationEventArgs args);
    public delegate void RowAction(ref Dictionary<string, object> row);

    public class CsvFileReader(FileReadConfig config) : IFileReader
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;

        private readonly FileReadConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly Stopwatch _timer = new();

        public int LineNumber { get; set; }
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }

        private static FileInfo ValidateFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) throw new FileNotFoundException("File not found", filePath);
            return fileInfo;
        }

        public void Read(string filePath, RowAction processRow)
        {
            _timer.Restart();
            LineNumber = 0;
            BytesRead = 0;
            FileSize = ValidateFile(filePath).Length;

            using var reader = Sep.New(_config.Delimiter).Reader().FromFile(filePath);

            var refRow = new Dictionary<string, object>();

            var actions = _config.ColumnsConfig
                .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
                .ToDictionary(x => x.Position, x => x);

            foreach (var line in reader)
            {
                if (_config.HasHeader && LineNumber == 0) { reader.MoveNext(); }

                LineNumber++;
                refRow.Clear();

                for (int i = 0; i < line.ColCount; i++)
                {
                    BytesRead += System.Text.Encoding.Unicode.GetByteCount(line[i].Span);
                    refRow.Add(actions[i].OutputName ?? actions[i].Name, ParseValue(line[i].Span, actions[i].OutputType));
                }

                NotifyReadProgress();
                processRow(ref refRow);
            }

            NotifyFinish();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static object ParseValue(ReadOnlySpan<char> valueSpan, Type outputType)
        {
            return outputType switch
            {
                Type t when t == typeof(string) => new string(valueSpan),
                Type t when t == typeof(int) => int.Parse(valueSpan),
                Type t when t == typeof(double) => double.Parse(valueSpan),
                Type t when t == typeof(float) => float.Parse(valueSpan),
                Type t when t == typeof(decimal) => decimal.Parse(valueSpan),
                Type t when t == typeof(DateTime) => DateTime.Parse(valueSpan, Thread.CurrentThread.CurrentCulture),
                Type t when t == typeof(bool) => bool.Parse(valueSpan),
                Type t when t == typeof(long) => long.Parse(valueSpan),
                Type t when t == typeof(short) => short.Parse(valueSpan),
                Type t when t == typeof(Guid) => Guid.Parse(valueSpan),
                _ => throw new NotSupportedException($"The type {nameof(outputType)} is not supported."),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifyReadProgress()
        {
            if (LineNumber % _config.NotifyAfter != 0) return;

            PercentRead = (double)BytesRead / FileSize * 100;
            double speed = LineNumber / _timer.Elapsed.TotalSeconds;

            OnRead?.Invoke(new ReadNotificationEventArgs(LineNumber, FileSize, BytesRead, PercentRead, speed));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifyFinish()
        {
            _timer.Stop();
            PercentRead = 100;
            BytesRead = FileSize;
            double speed = LineNumber / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new ReadNotificationEventArgs(LineNumber, FileSize, BytesRead, PercentRead, speed));
        }
    }
}
