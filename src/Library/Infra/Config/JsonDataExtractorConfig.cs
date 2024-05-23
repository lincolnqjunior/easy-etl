using Library.Infra.ColumnActions;

namespace Library.Infra.Config
{
    public record JsonDataExtractorConfig : IJsonConfig, IFileExtractorConfig
    {
        public string CultureInfo { get; set; } = "pt-BR";
        public int RaiseChangeEventAfer { get; set; } = 10_000;

        public string FilePath { get; set; } = string.Empty;

        public bool IndentJson { get; set; } = false;
        public bool IsJsonl { get; set; } = false;

        public List<IColumnAction> Columns { get; set; } = [];
    }
}
