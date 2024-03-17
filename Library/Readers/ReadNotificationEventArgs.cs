namespace Library.Readers
{
    public class ReadNotificationEventArgs(long lineCount, long fileSize, long ingestedSize, double readPercentage, double speed) : EventArgs
    {
        public long LineCount { get; set; } = lineCount;
        public long FileSize { get; set; } = fileSize;
        public long IngestedSize { get; set; } = ingestedSize;
        public double ReadPercentage { get; set; } = readPercentage;
        public double Speed { get; set; } = speed;
    }
}