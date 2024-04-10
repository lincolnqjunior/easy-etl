using Library.Infra;
using Library.Infra.ColumnActions;
using Library.Infra.Config;
using Library.Infra.EventArgs;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using ParquetNet = Parquet;

namespace Library.Extractors.Parquet
{
    struct ParquetColumnData
    {
        public ParquetNet.Data.DataColumn ColumnData { get; set; }
        public string OutputName { get; set; }
    }

    public class ParquetDataExtractor(ParquetDataExtractorConfig config) : IDataExtractor
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;
        public event EasyEtlErrorEventHandler? OnError;

        public long TotalLines { get; set; }
        public int LineNumber { get; set; }
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }

        private readonly ParquetDataExtractorConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly Stopwatch _timer = new();
        private readonly object _lock = new();
        
        private readonly ConcurrentBag<long> bytesRead = new();
        private Dictionary<string, IColumnAction> actions = [];
        private readonly CancellationTokenSource _cts = new();
        private string[] files;
        private void Init()
        {
            _timer.Restart();

            actions = _config.Columns
                        .Where(x => !x.IsHeader && x.Action != ColumnAction.Ignore)
                        .ToDictionary(x => x.Name, x => x);

            files = Directory.GetFiles(_config.Directory, _config.Mask);

            foreach (var file in files)
            {
                using Stream fileStream = File.OpenRead(file);
                using var reader = ParquetNet.ParquetReader.CreateAsync(fileStream).Result;

                TotalLines += reader.Metadata?.RowGroups.Sum(x => x.NumRows) ?? 0;
                FileSize += fileStream.Length;
            }
        }

        public void Extract(RowAction processRow)
        {
            Init();

            var cancelToken = _cts.Token;

            // Obter a lista de arquivos Parquet do diretório.

            var semaphore = new SemaphoreSlim(1);
            var tasks = new List<Task>();

            foreach (var file in files)
            {
                tasks.Add(Task.Run((Func<Task?>)(async () =>
                {
                    try
                    {
                        await semaphore.WaitAsync(cancelToken);
                        _cts.Token.ThrowIfCancellationRequested();

                        using Stream fileStream = File.OpenRead(file);
                        using var reader = await ParquetNet.ParquetReader.CreateAsync(fileStream);

                        List<ParquetColumnData> columnsList = [];
                        for (int rowGroupIndex = 0; rowGroupIndex < reader.RowGroupCount; rowGroupIndex++)
                        {
                            var rowGroupReader = reader.OpenRowGroupReader(rowGroupIndex);
                            long rowCount = rowGroupReader.RowCount;

                            columnsList.Clear();
                            foreach (var dataField in reader.Schema.DataFields)
                            {
                                if (actions.TryGetValue(dataField.Name, out var columnAction))
                                {
                                    columnsList.Add(new ParquetColumnData()
                                    {
                                        OutputName = columnAction.OutputName ?? columnAction.Name,
                                        ColumnData = await rowGroupReader.ReadColumnAsync(dataField)
                                    });
                                }
                            }

                            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
                            {
                                Dictionary<string, object?> rowData = [];
                                lock (_lock) { LineNumber++; }

                                foreach (var column in columnsList)
                                {
                                    rowData[column.OutputName] = column.ColumnData.Data.GetValue(rowIndex);
                                }

                                processRow(ref rowData);

                                lock (_lock)
                                    if (LineNumber % config.RaiseChangeEventAfer == 0)
                                    {
                                        PercentRead = (double)LineNumber / TotalLines * 100;
                                        var speed = LineNumber / _timer.Elapsed.TotalSeconds;
                                        OnRead?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
                                    }

                            }
                        }

                        bytesRead.Add(fileStream.Length);
                        lock (_lock) { BytesRead += bytesRead.Sum(); }
                    }
                    catch (Exception ex)
                    {
                        await _cts.CancelAsync();
                        OnError?.Invoke(new ErrorNotificationEventArgs(EtlType.Extract, ex, [], LineNumber));
                        throw;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                })));
            }

            Task.WaitAll(tasks.ToArray());

            _timer.Stop();
            PercentRead = 100;
            var speed = LineNumber / _timer.Elapsed.TotalSeconds;
            OnFinish?.Invoke(new ExtractNotificationEventArgs(TotalLines, LineNumber, FileSize, BytesRead, PercentRead, speed));
            _cts.Dispose();
        }
    }
}
