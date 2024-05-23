namespace Library.Infra.Config
{
    public record JsonDataLoaderConfig : IJsonConfig, IFileLoaderConfig
    {
        public string CultureInfo { get; set; } = "pt-BR";
        public int RaiseChangeEventAfer { get; set; } = 10_000;

        public string OutputPath { get; set; } = string.Empty;

        public bool IndentJson { get; set; } = false;
        public bool IsJsonl { get; set; } = true;
    }
}
