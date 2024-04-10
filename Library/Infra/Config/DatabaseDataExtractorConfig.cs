using Library.Infra.ColumnActions;

namespace Library.Infra.Config
{
    public record DatabaseDataExtractorConfig : IDataBaseExtractorConfig
    {
        public string CultureInfo { get; set; } = "pt-BR";
        public int RaiseChangeEventAfer { get; set; } = 10_000;
        
        public string ConnectionString { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public int PageSize { get; set; } = 1000;
        public string QuerySelect { get; set; } = "SELECT * FROM {0} LIMIT {1} OFFSET {2};";
        public string QueryCount { get; set; } = "SELECT COUNT(*) FROM {0};";
        
        public List<IColumnAction> Columns { get; set; } = [];
    }
}
