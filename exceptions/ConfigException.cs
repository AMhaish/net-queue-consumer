using System;

namespace queue_consumer
{
    [Serializable]
    class ConfigException : Exception
    {
        public ConfigException(string configName) : base(String.Format("Missing configuration value: {0}", configName)) { }

    }
}