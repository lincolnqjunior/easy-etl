namespace Library.Writers
{
    public struct WriteNotificationEventArgs(long lineCount, long totalLines, double writePercentage, double speed)
    {
        public long LineCount { get; set; } = lineCount;
        public long TotalLines { get; set; } = totalLines;
        public double WritePercentage { get; set; } = writePercentage;
        public double Speed { get; set; } = speed;
    }
}