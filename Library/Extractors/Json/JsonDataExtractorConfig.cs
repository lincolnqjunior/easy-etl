namespace Library.Extractors.Json
{
    public class JsonDataExtractorConfig
    {
        public string CultureInfo { get; set; } = "pt-BR";
        public int NotifyAfter { get; set; } = 1000;
        public string FilePath { get; set; } = string.Empty;
        public bool IndentJson { get; set; } = false;
        public bool IsJsonl { get; set; } = false;

    }
}
