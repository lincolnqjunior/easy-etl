using Library.Infra;

namespace Library.Extractors
{
    public delegate void RowAction(ref Dictionary<string, object?> row);

    public interface IDataExtractor
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;
        public event EasyEtlErrorEventHandler OnError;

        public long TotalLines { get; set; }
        public int LineNumber { get; set; }
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }

        void Extract(RowAction processRow);
    }
}