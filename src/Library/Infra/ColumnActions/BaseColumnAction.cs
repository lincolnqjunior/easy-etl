namespace Library.Infra.ColumnActions
{
    public abstract class BaseColumnAction : IColumnAction
    {
        public string Name { get; set; } = string.Empty;
        public int Position { get; set; } = 0;
        public bool IsHeader { get; set; } = false;
        public string? OutputName { get; set; } = null;
        public Type OutputType { get; set; } = typeof(string);
        public ColumnAction Action { get; set; } = ColumnAction.Default;

        public abstract object? ExecuteAction(object? value);
    }

    public enum ColumnAction
    {
        Default,
        Ignore,
        Parse
    }
}