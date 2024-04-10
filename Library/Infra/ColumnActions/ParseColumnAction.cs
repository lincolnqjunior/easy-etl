using Ardalis.GuardClauses;
using System;
using System.ComponentModel;

namespace Library.Infra.ColumnActions
{
    public class ParseColumnAction : BaseColumnAction
    {
        private static readonly Dictionary<Type, TypeConverter> converterCache = [];

        public ParseColumnAction(string Name, int Position, bool IsHeader, string OutputName, Type OutputType)
        {
            this.Name = Guard.Against.NullOrWhiteSpace(Name);
            this.Position = Guard.Against.Negative(Position);
            this.IsHeader = Guard.Against.Null(IsHeader);
            this.OutputName = OutputName ?? Name;
            this.OutputType = Guard.Against.NullOrInvalidInput(OutputType, nameof(OutputType), x => x.IsValueType || x == typeof(string));
            Action = ColumnAction.Parse;
        }

        public override object? ExecuteAction(object? value)
        {
            if (value is null || (value is string strValue && string.IsNullOrWhiteSpace(strValue)))
            {
                return null;
            }

            if (!converterCache.TryGetValue(OutputType, out var converter))
            {
                converter = TypeDescriptor.GetConverter(OutputType);
                if (converter != null)
                {
                    converterCache[OutputType] = converter;
                }
            }

            if (converter != null && converter.CanConvertFrom(value.GetType()))
            {
                try
                {
                    return converter.ConvertFrom(null, System.Globalization.CultureInfo.InvariantCulture, value) ?? string.Empty;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to parse value '{value}' to type '{OutputType.Name}'.", ex);
                }
            }
            else
            {
                try
                {
                    return Convert.ChangeType(value, OutputType, System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to parse value '{value}' to type '{OutputType.Name}'.", ex);
                }
            }
        }
    }
}
