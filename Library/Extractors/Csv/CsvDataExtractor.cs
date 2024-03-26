using Library.Infra;
using Library.Infra.ColumnActions;
using nietras.SeparatedValues;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]
namespace Library.Extractors.Csv
{
    /// <summary>
    /// Extracts data from a CSV file according to the specified configuration.
    /// </summary>
    public class CsvDataExtractor(DataExtractorConfig config) : IDataExtractor
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long TotalLines { get; set; }
        public int LineNumber { get; set; }
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }

        private readonly DataExtractorConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly Stopwatch _timer = new();
        private bool first = true;
        private Dictionary<string, object?> rowData = [];


        /// <summary>
        /// Performs the data extraction from a CSV file, processing each row with the provided RowAction delegate.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to be processed.</param>
        /// <param name="processRow">The action to perform on each row of data.</param>
        public void Extract(RowAction processRow)
        {
            try
            {
                Init(config.FilePath);

                using var reader = Sep.New(_config.Delimiter).Reader().FromFile(config.FilePath);
                // Configure column actions based on the extractor configuration.
                var actions = _config.ColumnsConfig
                    .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
                    .ToDictionary(x => x.Position, x => x);

                foreach (var line in reader)
                {
                    // Skip the header row if specified in the configuration.
                    if (_config.HasHeader && first)
                    {
                        first = false;
                        continue;
                    }

                    rowData.Clear();
                    foreach (var action in actions.Values)
                    {
                        // Parse each column value based on the column configuration.
                        var columnValue = line[action.Position];
                        BytesRead += System.Text.Encoding.Unicode.GetByteCount(columnValue.Span);
                        rowData[action.OutputName] = ParseValue(columnValue.Span, action.OutputType);
                    }

                    // Process the row with the provided delegate.
                    processRow(ref rowData);
                    LineNumber++;
                    NotifyReadProgress();
                }

                NotifyFinish();
            }
            catch (Exception ex)
            {
                // Handle any exceptions by invoking the OnError event.
                OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, ex, new Dictionary<string, object?>(), LineNumber));
                throw;
            }
        }

        /// <summary>
        /// Validate the file and set initial properties.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to be processed.</param>
        private void Init(string filePath)
        {
            _timer.Restart();

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) throw new FileNotFoundException("File not found", filePath);

            FileSize = fileInfo.Length;
            TotalLines= (FileSize > 1_073_741_824) ? FileStreamHelper.CountLinesParallel(filePath) : FileStreamHelper.CountLines(filePath);            
            LineNumber = 0;
            BytesRead = 0;
        }

        /// <summary>
        /// Notifies subscribers of progress at configured intervals and upon completion.
        /// </summary>        
        private void NotifyReadProgress()
        {
            if (LineNumber % _config.NotifyAfter == 0)
            {
                PercentRead = (double)BytesRead / FileSize * 100;
                var speed = TotalLines / _timer.Elapsed.TotalSeconds;

                OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
            }
        }

        /// <summary>
        /// Notifies subscribers that the extraction process has completed.
        /// </summary>        
        private void NotifyFinish()
        {
            _timer.Stop();

            TotalLines = LineNumber;
            BytesRead = FileSize;
            PercentRead = 100;
            var speed = TotalLines / _timer.Elapsed.TotalSeconds;

            OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
        }

        /// <summary>
        /// Parses a column value from the CSV file into the specified output type.
        /// </summary>
        /// <param name="valueSpan">The value to parse, as a ReadOnlySpan<char>.</param>
        /// <param name="outputType">The Type to parse the value into.</param>
        /// <returns>The parsed value, cast to the specified output type.</returns>        
        internal static object ParseValue(ReadOnlySpan<char> valueSpan, Type outputType)
        {
            return outputType switch
            {
                Type t when t == typeof(string) => valueSpan.ToString(),
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
    }
}
