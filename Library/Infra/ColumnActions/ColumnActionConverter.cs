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
            var type = jsonObject["Type"].Value<string>();
            var name = jsonObject["ColumnName"].Value<string>();
            var position = jsonObject["Position"].Value<int>();
            var isHeader = jsonObject["IsHeader"].Value<bool>(); 
            var outputName = jsonObject["OutputName"].Value<string>();
            var outputType = Type.GetType(jsonObject["OutputType"].Value<string>());

            IColumnAction columnAction = type switch
            {
                "ParseColumnAction" => new ParseColumnAction(name, position, isHeader, outputName, outputType),
                "DefaultColumnAction" => new DefaultColumnAction(name, position, isHeader, outputName, outputType),
                _ => throw new NotImplementedException($"Unknown column action type: {type}")
            };

            return columnAction;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override bool CanWrite => false;
    }
}