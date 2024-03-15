using Newtonsoft.Json;

namespace Library.Infra
{
    public class TransformationConfig
    {
        [JsonProperty("transformations")]
        public List<TransformationFilter> Transformations { get; set; } = [];
    }

    public class TransformationFilter
    {
        [JsonProperty("condition")]
        public string Condition { get; set; } = true.ToString();

        [JsonProperty("filters")]
        public List<TransformationFilter> Filters { get; set; } = [];

        [JsonProperty("actions")]
        public List<TransformationActions> Actions { get; set; } = [];
    }

    public class TransformationActions
    {
        public Dictionary<string, FieldMapping> FieldMappings { get; set; } = [];
    }

    public class FieldMapping
    {
        public object Value { get; set; } = string.Empty;
        public bool IsDynamic { get; set; } = false;
    }
}
