using CSVFile;
using Library.Infra;
using Library.Infra.ColumnActions;

namespace Library.Readers
{
    public delegate void ReadNotification(int linesRead, double percentRead, long sizeRead, long fileSize);

    public class CsvFileReader(FileReadConfig config) : IFileReader
    {
        public event ReadNotification? OnRead;

        public event ReadNotification? OnFinish;

        private int _lineNumber = 0;
        private readonly FileReadConfig _config = config;        

        public async IAsyncEnumerable<Dictionary<string, object>> Read(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) throw new FileNotFoundException("File not found", filePath);
            var fileSize = fileInfo.Length;
            var bytesRead = 0;

            var headerDictionary = new Dictionary<string, object>();

            var settings = new CSVSettings()
            {
                HeaderRowIncluded = _config.HasHeader,
                FieldDelimiter = _config.Delimiter
            };

            using var cr = CSVReader.FromFile(filePath, settings);
            await foreach (string[] line in cr)
            {
                _lineNumber++;
                bytesRead += line.Sum(x => x.Length);

                var dic = new Dictionary<string, object>();

                if (_lineNumber == 1 && _config.HasHeader)
                {
                    for (int i = 0; i < cr.Headers.Length; i++)
                    {
                        var action = _config.ColumnsConfig.Find(x => x.IsHeader && x.Position == i);
                        if (action == null) continue;

                        var value = ColumnActionFactory.CreateAction(action).ExecuteAction(cr.Headers[i]);
                        headerDictionary.Add(action.OutputName ?? action.Name, value);
                    }

                    continue;
                }

                foreach (var headerColumn in headerDictionary)
                {
                    dic.Add(headerColumn.Key, headerColumn.Value);
                }

                for (int i = 0; i < line.Length; i++)
                {
                    var action = _config.ColumnsConfig.Find(x => !x.IsHeader && x.Position == i);
                    if (action == null) continue;

                    var value = ColumnActionFactory.CreateAction(action).ExecuteAction(cr.Headers[i]);
                    dic.Add(action.OutputName ?? action.Name, value);
                }

                if (_lineNumber % _config.NotifyAfter == 0)
                {
                    double percentRead = (double)bytesRead / fileSize * 100;
                    OnRead?.Invoke(_lineNumber, percentRead, bytesRead, fileSize);
                }

                yield return dic;
            }

            OnFinish?.Invoke(_lineNumber, 100, fileSize, fileSize);
        }
    }
}