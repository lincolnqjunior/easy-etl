using Library.Infra;

namespace Library.Transformers
{
    public interface IDataTransformer
    {
        public event TransformNotificationHandler OnTransform;
        public event TransformNotificationHandler OnFinish;
        public event EasyEtlErrorEventHandler OnError;

        public long IngestedLines { get; set; }
        public long TransformedLines { get; set; }
        public long ExcludedByFilter { get; set; }
        public double PercentDone { get; set; }
        public long TotalLines { get; set; }

        IAsyncEnumerable<Dictionary<string, object?>> Transform(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken);
    }
}
