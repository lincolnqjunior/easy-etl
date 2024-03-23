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
            var type = jsonObject["Type"]?.Value<string>() ?? throw new ArgumentException("Type can't be null or empty");
            var name = jsonObject["ColumnName"]?.Value<string>() ?? throw new ArgumentException("ColumnName can't be null or empty");
            var position = jsonObject["Position"]?.Value<int>() ?? throw new ArgumentException("Position can't be null or empty");
            var isHeader = jsonObject["IsHeader"]?.Value<bool>() ?? throw new ArgumentException("IsHeader can't be null or empty");
            var outputName = jsonObject["OutputName"]?.Value<string>() ?? throw new ArgumentException("OutputName can't be null or empty");
            var typeString = jsonObject["OutputType"]?.Value<string>() ?? throw new ArgumentException("OutputType can't be null or empty");
            var outputType = Type.GetType(typeString) ?? throw new ArgumentException("OutputType can't be null or empty");

            IColumnAction columnAction = type switch
            {
                "ParseColumnAction" => new ParseColumnAction(name, position, isHeader, outputName, outputType),
                "DefaultColumnAction" => new DefaultColumnAction(name, position, isHeader, outputName, outputType),
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