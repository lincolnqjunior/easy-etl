using Ardalis.GuardClauses;

namespace Library.Infra.ColumnActions
{
    public class DefaultColumnAction : BaseColumnAction
    {
        public DefaultColumnAction(string Name, int Position, bool IsHeader, string OutputName, Type OutputType)
        {
            this.Name = Guard.Against.NullOrWhiteSpace(Name);
            this.Position = Guard.Against.Negative(Position);
            this.IsHeader = Guard.Against.Null(IsHeader);
            this.OutputName = OutputName ?? Name;
            this.OutputType = Guard.Against.NullOrInvalidInput(OutputType, nameof(OutputType), x => x.IsValueType || x == typeof(string));
            Action = ColumnAction.Default;
        }

        public override object ExecuteAction(object value) => value;
    }
}