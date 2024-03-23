namespace Library.Loaders
{
    public delegate void LoadNotificationHandler(LoadNotificationEventArgs args);

    public struct LoadNotificationEventArgs(long lineCount, long totalLines, double writePercentage, double speed)
    {        
        public long LineCount { get; set; } = lineCount;
        public long TotalLines { get; set; } = totalLines;
        public double WritePercentage { get; set; } = writePercentage;
        public double Speed { get; set; } = speed;
    }
}