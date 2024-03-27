using Library.Infra.ColumnActions;

namespace Library.Extractors.Parquet
{
    public class ParquetDataExtractorConfig
    {
        public string CultureInfo { get; set; } = "en-US";        
        public int NotifyAfter { get; set; } = 10_000;
        public string Directory { get; set; } = string.Empty;
        public string Mask { get; set; } = "*.parquet";

        public List<IColumnAction> Columns { get; set; } = [];
    }
}