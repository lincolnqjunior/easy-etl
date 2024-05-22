using Library.Infra.Config;

namespace Library.Loaders.Csv
{
    public record CsvDataLoaderConfig : ICsvConfig, IFileLoaderConfig
    {
        public string CultureInfo { get; set; } = "pt-BR";
        public int RaiseChangeEventAfer { get; set; } = 10_000;
        
        public char Delimiter { get; set; } = ';';        
        public bool HasHeader { get; set; } = true;
        
        public string OutputPath { get; set; } = string.Empty;        
    }
}
