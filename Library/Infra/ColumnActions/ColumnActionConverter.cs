using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Library.Infra.ColumnActions
{
    public class ColumnActionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(IColumnAction);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);
            var type = jsonObject["Type"]?.Value<string>() ?? nameof(DefaultColumnAction);
            var name = jsonObject["OutputName"]?.Value<string>() ?? throw new ArgumentException("OutputName can't be null or empty");
            var position = jsonObject["Position"]?.Value<int>() ?? 0;
            var isHeader = jsonObject["IsHeader"]?.Value<bool>() ?? false;
            var outputName = jsonObject["OutputName"]?.Value<string>() ?? name;
            var typeString = jsonObject["OutputType"]?.Value<string>() ?? "System.String";

            Type? outputType;
                        
            // Check if the type is nullable (contains "?") and get the base type.
            bool isNullable = typeString.EndsWith('?');
            if (isNullable)
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
                outputType = Type.GetType(typeString) ?? throw new ArgumentException($"OutputType must not be a complex type: {typeString}");
            }

            IColumnAction columnAction = type switch
            {
                nameof(ParseColumnAction) => new ParseColumnAction(name, position, isHeader, outputName, outputType),
                nameof(DefaultColumnAction) => new DefaultColumnAction(name, position, isHeader, outputName, outputType),
                _ => throw new NotImplementedException($"Unknown column action type: {type}")
            };

            return columnAction;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override bool CanWrite => false;
    }
}