namespace Library.Loaders
{
    public class DatabaseDataLoaderConfig
    {
        public string CultureInfo { get; set; } = "pt-BR";
        public int NotifyAfter { get; set; } = 10_000;
        public string ConnectionString { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public long BatchSize { get; set; } = 10_000;
    }
}
