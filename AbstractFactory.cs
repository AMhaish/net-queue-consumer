using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace queue_consumer
{
    public abstract class AbstractFactory
    {
        protected readonly ILogger _logger;
        protected readonly IConfiguration _configuration;

        public AbstractFactory(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        protected string ReadConfigurationValue(string key)
        {
            string value = "";
            if (!string.IsNullOrEmpty(_configuration.GetValue<string>(key)))
            {
                value = _configuration.GetValue<string>(key);
            }
            else
            {
                throw new ConfigException(key);
            }
            return value;
        }
    }
}