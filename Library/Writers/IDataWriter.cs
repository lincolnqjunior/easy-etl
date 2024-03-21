using Library.Readers;

namespace Library.Writers
{
    public interface IDataWriter
    {
        public event WriteNotification? OnWrite;
        public event WriteNotification? OnFinish;

        public long CurrentLine { get; set; }
        public long TotalLines { get; set; }
        public double PercentWriten { get; set; }

        Task Write(IAsyncEnumerable<Dictionary<string, object>> data);
    }
}
