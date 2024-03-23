namespace Library.Loaders.Json
{
    public class JsonDataLoaderConfig
    {
        public string CultureInfo { get; set; } = "pt-BR";
        public int NotifyAfter { get; set; } = 1000;
        public string OutputPath { get; set; } = string.Empty;
        public bool IndentJson { get; set; } = false;
        public bool IsJsonl { get; set; } = false;

    }
}
