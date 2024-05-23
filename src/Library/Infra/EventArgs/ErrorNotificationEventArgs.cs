namespace Library.Infra.EventArgs
{
    public delegate void EasyEtlErrorEventHandler(ErrorNotificationEventArgs args);

    public struct ErrorNotificationEventArgs(EtlType source, Exception exception, Dictionary<string, object?> rowData, long lineNumber)
    {
        public EtlType Source { get; set; } = source;
        public Exception Exception { get; set; } = exception;
        public long ErrorLine { get; set; } = lineNumber;
        public Dictionary<string, object?> RowData { get; set; } = rowData;
    }
}
