namespace Library.Infra.ColumnActions
{
    public interface IColumnAction
    {
        public string Name { get; set; }
        public int Position { get; set; }
        public bool IsHeader { get; set; }
        public string? OutputName { get; set; }
        public Type OutputType { get; set; }
        public ColumnAction Action { get; set; }

        public abstract object? ExecuteAction(object? value);        
    }
}