using Library.Infra.Config;
using Newtonsoft.Json;

namespace Library.Infra
{
    public record TransformationConfig : ITransformerConfig
    {
        public int RaiseChangeEventAfer { get; set; } = 1000;

        public string CultureInfo { get; set; } = "en-US";

        public List<TransformationFilter> Transformations { get; set; } = [];
    }

    public record TransformationFilter
    {        
        public string Condition { get; set; } = "true";     
                
        public List<TransformationAction> Actions { get; set; } = [];
    }

    public record TransformationAction
    {
        public Dictionary<string, FieldMapping> FieldMappings { get; set; } = [];
    }

    public record FieldMapping
    {
        public object Value { get; set; } = string.Empty;
        public bool IsDynamic { get; set; } = false;
    }
}
