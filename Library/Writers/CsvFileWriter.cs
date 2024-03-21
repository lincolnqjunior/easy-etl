
using CSVFile;

namespace Library.Writers
{
    //public delegate void WriteNotification(int linesWritten, double percentWritten, long sizeWritten, long fileSize);

    public class CsvFileWriter(FileWriterConfig config) : IFileWriter
    {
        //public event ReadNotification? OnRead;

        //public event ReadNotification? OnFinish;

        private int _lineNumber = 0;
        private readonly FileWriterConfig _config = config;

        public async Task WriteAsync(string filePath, IAsyncEnumerable<Dictionary<string, object>> data)
        {
            var bytesRead = 0;

            var settings = new CSVSettings()
            {
                HeaderRowIncluded = _config.HasHeader,
                FieldDelimiter = _config.Delimiter
            };

            using var streamWriter = new StreamWriter(filePath);
            using var writer = new CSVWriter(streamWriter, settings);

            await foreach (var line in data)
            {                
                _lineNumber++;
                bytesRead += line.Sum(x => x.Value.ToString()?.Length ?? 0);

                if (_lineNumber == 1 && _config.HasHeader)
                {
                    var headers = line.Select(x => x.Key).ToArray();
                    await writer.WriteLineAsync(headers);
                    continue;
                }

                await writer.WriteLineAsync(line.Values);

                //if (_lineNumber % _config.NotifyAfter == 0)
                //{
                //    //double percentRead = (double)bytesRead / fileSize * 100;
                //    //OnRead?.Invoke(_lineNumber, percentRead, bytesRead, fileSize);
                //}
            }
        }
    }
}
