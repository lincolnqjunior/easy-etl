using Newtonsoft.Json;

namespace Library.Infra
{
    public class TransformationConfig
    {
        [JsonProperty("transformations")]
        public List<TransformationFilter> Transformations { get; set; } = [];

        [JsonProperty("notify-after")]
        public int NotifyAfter { get; set; } = 1000;
    }

    public class TransformationFilter
    {
        [JsonProperty("condition")]
        public string Condition { get; set; } = "true";

        [JsonProperty("filters")]
        public List<TransformationFilter> Filters { get; set; } = [];

        [JsonProperty("actions")]
        public List<TransformationAction> Actions { get; set; } = [];
    }

    public class TransformationAction
    {
        public Dictionary<string, FieldMapping> FieldMappings { get; set; } = [];
    }

    public class FieldMapping
    {
        public object Value { get; set; } = string.Empty;
        public bool IsDynamic { get; set; } = false;
    }
}
