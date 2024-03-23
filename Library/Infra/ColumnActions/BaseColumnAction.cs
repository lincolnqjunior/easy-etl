namespace Library.Infra.ColumnActions
{
    public abstract class BaseColumnAction : IColumnAction
    {
        public string Name { get; set; } = string.Empty;
        public int Position { get; set; }
        public bool IsHeader { get; set; }
        public string OutputName { get; set; } = string.Empty;
        public Type OutputType { get; set; } = typeof(string);
        public ColumnAction Action { get; set; }

        public abstract object? ExecuteAction(object value);
    }

    public enum ColumnAction
    {
        Default,
        Ignore,
        Parse
    }
}