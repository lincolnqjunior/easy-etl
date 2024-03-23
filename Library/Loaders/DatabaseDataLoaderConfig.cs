namespace Library.Loaders
{
    public class DatabaseDataLoaderConfig
    {
        public string CultureInfo { get; set; } = "pt-BR";
        public int NotifyAfter { get; set; } = 1000;
        public string ConnectionString { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
    }
}
