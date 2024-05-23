using Library.Infra.ColumnActions;

namespace Library.Infra.Config
{
    public record CsvDataExtractorConfig : ICsvConfig, IFileExtractorConfig
    {
        public string CultureInfo { get; set; } = "en-US";
        public int RaiseChangeEventAfer { get; set; } = 10_000;

        public char Delimiter { get; set; } = ';';        
        public bool HasHeader { get; set; } = true;
        
        public string FilePath { get; set; } = string.Empty;

        public List<IColumnAction> Columns { get; set; } = [];
    }
}