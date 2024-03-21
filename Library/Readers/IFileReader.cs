using Library.Infra;

namespace Library.Readers
{
    public interface IFileReader
    {
        public event ReadNotification? OnRead;
        public event ReadNotification? OnFinish;

        public int LineNumber { get; set; }
        public long BytesRead { get; set; }
        public double PercentRead { get; set; }
        public long FileSize { get; set; }

        void Read(string filePath, RowAction processRow);
    }
}