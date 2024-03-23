using Library.Infra.ColumnActions;

namespace Library.Extractors
{
    public class DatabaseDataExtractorConfig
    {
        public string CultureInfo { get; set; } = "pt-BR";
        public int NotifyAfter { get; set; } = 1000;
        public string ConnectionString { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public int PageSize { get; set; } = 1000;
        public string QuerySelect { get; set; } = "SELECT * FROM {0} LIMIT {1} OFFSET {2};";
        public string QueryCount { get; set; } = "SELECT COUNT(*) FROM {0};";

        public List<IColumnAction> ColumnsConfig { get; set; } = [];
    }
}
