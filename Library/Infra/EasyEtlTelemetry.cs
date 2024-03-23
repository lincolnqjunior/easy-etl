using Library.Extractors;
using Library.Loaders;
using Library.Transformers;
using System.Diagnostics;

namespace Library.Infra
{
    /// <summary>
    /// Monitors and reports on the progress and status of each stage in the ETL process.
    /// </summary>
    public class EasyEtlTelemetry
    {
        public event EasyEtlProgressEventHandler? OnChange;
        public event EasyEtlErrorEventHandler? OnError;

        private readonly EasyEtl _etlProcess;
        private readonly Stopwatch _timer = new();

        /// <summary>
        /// Stores progress data for each stage in the ETL process.
        /// </summary>
        public Dictionary<EtlType, EtlDataProgress> Progress { get; private set; } = [];

        public EasyEtlTelemetry(EasyEtl etlProcess)
        {
            _etlProcess = etlProcess ?? throw new ArgumentNullException(nameof(etlProcess));
            InitializeProgressTracking();
            SubscribeToEtlEvents();
        }

        private void InitializeProgressTracking()
        {
            foreach (EtlType etlType in Enum.GetValues(typeof(EtlType)))
            {
                Progress[etlType] = new EtlDataProgress();
            }
        }

        private void SubscribeToEtlEvents()
        {
            // Subscriptions to specific ETL component events
            _etlProcess.Extractor.OnRead += args => UpdateExtractProgress(args);
            _etlProcess.Extractor.OnError += args =>
            {
                Progress[EtlType.Extract].Status = EtlStatus.Failed;
                OnError?.Invoke(args);
            };

            _etlProcess.Transformer.OnTransform += args => UpdateTransformProgress(args);
            _etlProcess.Transformer.OnError += args =>
            {
                Progress[EtlType.Transform].Status = EtlStatus.Failed;
                OnError?.Invoke(args);
            };

            _etlProcess.Loader.OnWrite += args => UpdateLoadProgress(args);
            _etlProcess.Loader.OnError += args =>
            {
                Progress[EtlType.Load].Status = EtlStatus.Failed;
                OnError?.Invoke(args);
            };

            _timer.Start();
        }

        private void UpdateExtractProgress(ExtractNotificationEventArgs args)
        {
            UpdateProgress(EtlType.Extract, args.LineCount, args.Total, args.Speed);
        }

        private void UpdateTransformProgress(TransformNotificationEventArgs args)
        {
            UpdateProgress(EtlType.Transform, args.TransformedLines, args.TotalLines, args.Speed);
        }

        private void UpdateLoadProgress(LoadNotificationEventArgs args)
        {
            UpdateProgress(EtlType.Load, args.LineCount, args.TotalLines, args.Speed);
        }

        /// <summary>
        /// Updates progress data for a given ETL stage based on event arguments.
        /// </summary>
        private void UpdateProgress(EtlType etlType, long currentLine, long totalLines, double speed)
        {
            var progress = Progress[etlType];
            progress.CurrentLine = currentLine;
            progress.TotalLines = totalLines;
            progress.PercentComplete = (double)currentLine / totalLines * 100;
            progress.Speed = speed;
            progress.Status = EtlStatus.Running;

            UpdateGlobalProgress();
        }

        private void UpdateGlobalProgress()
        {
            // Calculates global progress based on the progress of all stages.
            var global = Progress[EtlType.Global];
            var load = Progress[EtlType.Load];

            if (load.TotalLines == 0) return;

            global.TotalLines = load.TotalLines;
            global.CurrentLine = load.CurrentLine;
            global.PercentComplete = global.CurrentLine / global.TotalLines * 100;
            global.Speed = global.CurrentLine / _timer.Elapsed.TotalSeconds;
            global.EstimatedTimeToEnd = TimeSpan.FromSeconds((global.TotalLines - global.CurrentLine) / global.Speed);

            var completedStages = Progress.Count(p => p.Key != EtlType.Global && p.Value.Status == EtlStatus.Completed);

            if (completedStages == Progress.Count - 1) // Excludes Global from count            
                global.Status = EtlStatus.Completed;
            else
                global.Status = EtlStatus.Running;

            OnChange?.Invoke(new EasyEtlNotificationEventArgs(Progress));
        }
    }
}
