namespace Library.Infra.EventArgs
{
    public delegate void EasyEtlProgressEventHandler(EasyEtlNotificationEventArgs args);

    public struct EasyEtlNotificationEventArgs(IDictionary<EtlType, EtlDataProgress> progress)
    {
        public IDictionary<EtlType, EtlDataProgress> Progress { get; set; } = progress;
    }
}