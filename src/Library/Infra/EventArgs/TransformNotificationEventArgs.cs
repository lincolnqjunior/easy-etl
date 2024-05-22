namespace Library.Transformers
{
    public delegate void TransformNotificationHandler(TransformNotificationEventArgs args);

    public struct TransformNotificationEventArgs(long totalLines, long ingestedLines, long transformedLines, long excludedByFilter, double percentDone, double speed)
    {
        public long TotalLines { get; set; } = totalLines;
        public long IngestedLines { get; set; } = ingestedLines;
        public long TransformedLines { get; set; } = transformedLines;
        public long ExcludedByFilter { get; set; } = excludedByFilter;
        public double PercentDone { get; set; } = percentDone;
        public double Speed { get; set; } = speed;
    }
}
