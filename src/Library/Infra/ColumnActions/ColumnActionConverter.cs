using System.Text.Json;
using System.Text.Json.Serialization;

namespace Library.Infra.ColumnActions
{
    public class ColumnActionConverter : JsonConverter<IColumnAction>
    {
        public override IColumnAction? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            var root = jsonDoc.RootElement;

            var type = root.TryGetProperty("Type", out var typeProp) ? typeProp.GetString() : nameof(DefaultColumnAction);
            var name = root.TryGetProperty("OutputName", out var nameProp) ? nameProp.GetString() : throw new ArgumentException("OutputName can't be null or empty");
            var position = root.TryGetProperty("Position", out var posProp) ? posProp.GetInt32() : 0;
            var isHeader = root.TryGetProperty("IsHeader", out var headerProp) && headerProp.GetBoolean();
            var outputName = root.TryGetProperty("OutputName", out var outNameProp) ? outNameProp.GetString() : name;
            var typeString = root.TryGetProperty("OutputType", out var outTypeProp) ? outTypeProp.GetString() : "System.String";

            Type? outputType;

            // Check if the type is nullable (contains "?") and get the base type.
            bool isNullable = typeString?.EndsWith('?') ?? false;
            if (isNullable && typeString != null)
            {
                // Remove the "?" to get the non-nullable type name.
                string baseTypeName = typeString.TrimEnd('?');

                // Get the base type.
                Type baseType = Type.GetType(baseTypeName) ?? throw new ArgumentException($"OutputType must not be a complex type: {baseTypeName}");

                // Use the base type to create a nullable type.
                outputType = typeof(Nullable<>).MakeGenericType(baseType);
            }
            else
            {
                outputType = Type.GetType(typeString ?? "System.String") ?? throw new ArgumentException($"OutputType must not be a complex type: {typeString}");
            }

            IColumnAction columnAction = type switch
            {
                nameof(ParseColumnAction) => new ParseColumnAction(name!, position, isHeader, outputName!, outputType),
                nameof(DefaultColumnAction) => new DefaultColumnAction(name!, position, isHeader, outputName!, outputType),
                _ => throw new NotImplementedException($"Unknown column action type: {type}")
            };

            return columnAction;
        }

        public override void Write(Utf8JsonWriter writer, IColumnAction value, JsonSerializerOptions options)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }
    }
}