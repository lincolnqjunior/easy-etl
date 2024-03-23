namespace Library.Infra
{    
    public delegate void EasyEtlErrorEventHandler(ErrorNotificationEventArgs args);

    public struct ErrorNotificationEventArgs(EtlType source, Exception exception, IDictionary<string, object?> rowData, long lineNumber)
    {
        public EtlType Source { get; set; } = source;
        public Exception Exception { get; set; } = exception;
        public long ErrorLine { get; set; } = lineNumber;
        public IDictionary<string, object?> RowData { get; set; } = rowData;
    }
}
