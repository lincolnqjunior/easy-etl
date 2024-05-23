using Library.Infra.ColumnActions;

namespace Library.Infra.Config
{
    public record ParquetDataExtractorConfig : IMultiFileExtractorConfig
    {
        public string CultureInfo { get; set; } = "en-US";
        public int RaiseChangeEventAfer { get; set; } = 10_000;
        public string Directory { get; set; } = string.Empty;
        public string Mask { get; set; } = "*.parquet";

        public List<IColumnAction> Columns { get; set; } = [];        
    }
}