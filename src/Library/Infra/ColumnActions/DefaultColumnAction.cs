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

        public override object? ExecuteAction(object? value)
        {
            // If the value is null and the type is string, return String.Empty
            if (value == null && OutputType == typeof(string))
            {
                return string.Empty;
            }

            // Check if outputType is a nullable type
            bool isNullable = Nullable.GetUnderlyingType(OutputType) != null || !OutputType.IsValueType;

            // If the output type is not nullable and the value is null, return the default value for the type
            if (!isNullable && value == null)
            {
                return Activator.CreateInstance(OutputType);
            }

            // Otherwise, return the value as is
            return value;
        }
    }
}