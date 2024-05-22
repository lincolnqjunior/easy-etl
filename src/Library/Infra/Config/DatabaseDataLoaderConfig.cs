namespace Library.Infra.Config
{
    public record DatabaseDataLoaderConfig : IDataBaseLoaderConfig 
    {
        public string CultureInfo { get; set; } = "en-US";
        public int RaiseChangeEventAfer { get; set; } = 10_000;

        public string ConnectionString { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public long BatchSize { get; set; } = 50_000;
        public int WriteThreads { get; set; } = 2;
    }
}
