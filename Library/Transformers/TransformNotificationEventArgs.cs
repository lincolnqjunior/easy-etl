namespace Library.Transformers
{
    public class TransformNotificationEventArgs(long ingestedLines, long transformedLines, long excludedByFilter, double speed) : EventArgs
    {
        public long IngestedLines { get; set; } = ingestedLines;
        public long TransformedLines { get; set; } = transformedLines;
        public long ExcludedByFilter { get; set; } = excludedByFilter;
        public double Speed { get; set; } = speed;
    }
}
