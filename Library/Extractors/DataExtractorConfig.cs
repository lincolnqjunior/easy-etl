using Library.Infra.ColumnActions;

namespace Library.Extractors
{
    public class DataExtractorConfig
    {
        public char Delimiter { get; set; } = ';';
        public string CultureInfo { get; set; } = "pt-BR";
        public bool HasHeader { get; set; } = true;
        public int NotifyAfter { get; set; } = 1000;
        public string FilePath { get; set; } = string.Empty;

        public List<IColumnAction> ColumnsConfig { get; set; } = [];
    }
}