using Library.Infra.ColumnActions;

namespace Library.Infra
{
    public class FileReadConfig
    {
        public FileTypes FileType { get; set; } = FileTypes.Csv;
        public char Delimiter { get; set; } = ';';
        public string CultureInfo { get; set; } = "pt-BR";
        public bool HasHeader { get; set; } = true;
        public int NotifyAfter { get; set; } = 1000;

        public List<IColumnAction> ColumnsConfig { get; set; } = [];
    }

    public enum FileTypes
    {
        Csv,
        Xlsx,
        Xls,
        Json,
        Parquet
    }
}