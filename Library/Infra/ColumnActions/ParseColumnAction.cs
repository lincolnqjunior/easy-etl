using Ardalis.GuardClauses;
using System.ComponentModel;

namespace Library.Infra.ColumnActions
{
    public class ParseColumnAction : BaseColumnAction
    {
        public ParseColumnAction(string Name, int Position, bool IsHeader, string OutputName, Type OutputType)
        {
            this.Name = Guard.Against.NullOrWhiteSpace(Name, nameof(Name));
            this.Position = Guard.Against.Negative(Position, nameof(Position));
            this.IsHeader = Guard.Against.Null(IsHeader, nameof(IsHeader));
            this.OutputName = OutputName ?? Name;
            this.OutputType = Guard.Against.NullOrInvalidInput(OutputType, nameof(OutputType), x => typeof(IConvertible).IsAssignableFrom(x));
            Action = ColumnAction.Parse;
        }

        public override object ExecuteAction(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString())) return null;

            try
            {   
                var converter = TypeDescriptor.GetConverter(OutputType);
                if (converter != null && converter.CanConvertFrom(value.GetType()))
                {
                    return converter.ConvertFrom(null, System.Globalization.CultureInfo.InvariantCulture, value);
                }
                return Convert.ChangeType(value, OutputType);
            }
            catch (Exception ex)
            {                
                throw new InvalidOperationException($"Failed to parse value '{value}' to type '{OutputType.Name}'.", ex);
            }
        }
    }
}
