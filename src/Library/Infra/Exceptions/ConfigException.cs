namespace Library.Infra.Exceptions
{
    using System;

    public class ConfigException : Exception
    {
        public string ConfigName { get; private set; }
        public string? PropertyName { get; private set; }

        public ConfigException(string message, string configName, string? propertyName)
            : base(message)
        {
            ConfigName = configName;
            PropertyName = propertyName;
        }

        public ConfigException(string message, Exception inner, string configName, string? propertyName)
            : base(message, inner)
        {
            ConfigName = configName;
            PropertyName = propertyName;
        }
    }
}
