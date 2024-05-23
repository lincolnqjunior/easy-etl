namespace Library.Infra
{
    public class EtlDataProgress
    {
        public long CurrentLine { get; set; }
        public long TotalLines { get; set; }        
        public double PercentComplete { get; set; }
        public EtlStatus Status { get; set; }
        public double Speed { get; set; }
        public TimeSpan EstimatedTimeToEnd { get; set; }
    }

    public enum EtlStatus
    {
        Running,
        Completed,
        Failed
    }

    public enum EtlType
    {
        Extract,
        Transform,
        Load,
        Global
    }
}
