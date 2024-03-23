using Library.Infra;

namespace Library.Loaders
{
    public interface IDataLoader
    {
        public event LoadNotificationHandler? OnWrite;
        public event LoadNotificationHandler? OnFinish;
        public event EasyEtlErrorEventHandler OnError;

        public long CurrentLine { get; set; }
        public long TotalLines { get; set; }
        public double PercentWritten { get; set; }

        Task Load(IAsyncEnumerable<Dictionary<string, object?>> data, CancellationToken cancellationToken);
    }
}
