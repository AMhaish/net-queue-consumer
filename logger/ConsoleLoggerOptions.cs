using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;

namespace queue_consumer
{
    public enum LogStructureEnum { Stackdriver, ELK };
    public class ConsoleLoggerOptions
    {
        private readonly IConfiguration _configuration;
        public ConsoleLoggerOptions(IConfiguration configuration)
        {
            _configuration = configuration;
            var data = _configuration.GetValue<string>("LOGS");
            if (data == "Stackdriver")
            {
                LogStructure = LogStructureEnum.Stackdriver;
            }
        }

        public LogLevel LogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Information;
        public LogStructureEnum LogStructure { get; set; } = LogStructureEnum.ELK;
    }
}