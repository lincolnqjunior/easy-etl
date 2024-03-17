using CSVFile;
using Library.Infra.ColumnActions;
using System.Diagnostics;

namespace Library.Readers
{
    public delegate void ReadNotification(ReadNotificationEventArgs args);

    public class CsvFileReader : IFileReader
    {
        public event ReadNotification OnRead;
        public event ReadNotification OnFinish;

        private int _lineNumber;
        private long _fileSize;
        private double _percentRead;
        private long _bytesRead;
        private readonly FileReadConfig _config;
        private readonly Stopwatch _timer = new();

        public int LineNumber => _lineNumber;
        public long BytesRead => _bytesRead;
        public double PercentRead => _percentRead;
        public long FileSize => _fileSize;

        public CsvFileReader(FileReadConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async IAsyncEnumerable<Dictionary<string, object>> Read(string filePath)
        {
            _timer.Restart();
            _lineNumber = 0;

            _fileSize = ValidateFile(filePath).Length;

            _bytesRead = 0;
            var settings = CreateCsvSettings();

            using var cr = CSVReader.FromFile(filePath, settings);
            await foreach (var line in ReadLines(cr))
            {
                yield return line;
            }

            NotifyFinish();
        }

        private static FileInfo ValidateFile(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) throw new FileNotFoundException("File not found", filePath);
            return fileInfo;
        }

        private CSVSettings CreateCsvSettings() => new()
        {
            HeaderRowIncluded = _config.HasHeader,
            FieldDelimiter = _config.Delimiter
        };

        private async IAsyncEnumerable<Dictionary<string, object>> ReadLines(CSVReader cr)
        {
            Dictionary<string, object> headerDictionary = null;

            await foreach (string[] line in cr)
            {
                _lineNumber++;
                _bytesRead += line.Sum(x => x.Length);

                if (_lineNumber == 1 && _config.HasHeader)
                {
                    headerDictionary = ProcessHeader(cr);
                    continue;
                }

                var dic = ProcessLine(line, headerDictionary);
                NotifyReadProgress();
                yield return dic;
            }
        }

        private void NotifyReadProgress()
        {
            if (_lineNumber % _config.NotifyAfter != 0) return;

            _percentRead = (double)_bytesRead / _fileSize * 100;
            double speed = _lineNumber / _timer.Elapsed.TotalSeconds;

            OnRead?.Invoke(new ReadNotificationEventArgs(_lineNumber, _fileSize, _bytesRead, _percentRead, speed));
        }

        private void NotifyFinish()
        {
            _percentRead = 100;
            double speed = _lineNumber / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new ReadNotificationEventArgs(_lineNumber, _fileSize, _bytesRead, _percentRead, speed));
        }

        private Dictionary<string, object> ProcessHeader(CSVReader cr)
        {
            var headerDictionary = new Dictionary<string, object>();
            for (int i = 0; i < cr.Headers.Length; i++)
            {
                var action = _config.ColumnsConfig.Find(x => x.IsHeader && x.Position == i);
                if (action == null) continue;

                var value = ColumnActionFactory.CreateAction(action).ExecuteAction(cr.Headers[i]);
                headerDictionary.Add(action.OutputName ?? action.Name, value);
            }

            return headerDictionary;
        }

        private Dictionary<string, object> ProcessLine(string[] line, Dictionary<string, object> headerDictionary)
        {
            var dic = new Dictionary<string, object>(headerDictionary);

            for (int i = 0; i < line.Length; i++)
            {
                var action = _config.ColumnsConfig.Find(x => !x.IsHeader && x.Position == i);
                if (action == null) continue;

                var value = ColumnActionFactory.CreateAction(action).ExecuteAction(line[i]);
                dic.Add(action.OutputName ?? action.Name, value);
            }

            return dic;
        }
    }
}
