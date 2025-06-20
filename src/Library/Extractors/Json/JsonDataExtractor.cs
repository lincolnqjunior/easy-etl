using Library.Infra;
using System.Diagnostics;
using System.Text.Json;
using Library.Infra.EventArgs;
using Library.Infra.Config;
using Library.Infra.Helpers;

namespace Library.Extractors.Json
{
    public class JsonDataExtractor : IDataExtractor
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        private readonly JsonDataExtractorConfig _config;
        private readonly Stopwatch _timer = new();

        public long TotalLines { get; set; }
        public int LineNumber { get; set; }
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }
        public double Speed { get; set; }

        /// <summary>
        /// Initializes a new instance of the JsonDataExtractor with the specified configuration.
        /// </summary>
        /// <param name="config">The configuration for the data extraction.</param>
        public JsonDataExtractor(JsonDataExtractorConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            var fileInfo = new FileInfo(_config.FilePath);
            if (!fileInfo.Exists) throw new FileNotFoundException("The input file was not found.", _config.FilePath);

            FileSize = fileInfo.Length;
            TotalLines = fileInfo.CountLines().Result;
        }

        /// <summary>
        /// Extracts data from the specified JSON or JSONL file.
        /// </summary>
        /// <param name="processRow">The action to process each row of extracted data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        public async Task Extract(RowAction processRow, CancellationToken cancellationToken)
        {
            try
            {
                _timer.Start();

                using var fs = new FileStream(_config.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
                var jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                await foreach (var rowData in JsonSerializer.DeserializeAsyncEnumerable<Dictionary<string, object?>>(fs, jsonSerializerOptions, cancellationToken))
                {
                    if (rowData == null) continue;
                    BytesRead = fs.Position;
                    var buffer = rowData;
                    ProcessLine(ref buffer, processRow);
                }

                NotifyFinish();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, ex, [], LineNumber));
                throw;
            }
            finally
            {
                _timer.Stop();
            }
        }



        /// <summary>
        /// Processes a single line of JSONL data and notifies about the read progress.
        /// </summary>
        /// <param name="line">The line of JSONL to process.</param>
        /// <param name="processRow">The action to process the row.</param>
        private void ProcessLine(ref Dictionary<string, object?> rowData, RowAction processRow)
        {
            processRow(ref rowData);
            LineNumber++;
            NotifyReadProgress();
        }

        /// <summary>
        /// Notifies subscribers of the progress of data reading.
        /// </summary>
        private void NotifyReadProgress()
        {
            if (LineNumber % _config.RaiseChangeEventAfer == 0)
            {
                PercentRead = (double)BytesRead / FileSize * 100;
                Speed = LineNumber / _timer.Elapsed.TotalSeconds;
                OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, Speed));
            }
        }

        /// <summary>
        /// Notifies subscribers that the data extraction process has completed.
        /// </summary>
        private void NotifyFinish()
        {
            TotalLines = LineNumber;
            PercentRead = 100;
            Speed = LineNumber / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, Speed));
        }
    }
}
