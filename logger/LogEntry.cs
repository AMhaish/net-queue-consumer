using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace queue_consumer
{
    public class LogEntry
    {
        public LogEntry()
        {
            TimeStampUtc = DateTime.UtcNow;
            ServiceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "Net_Queue";
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Pid = Process.GetCurrentProcess().Id;
        }
        [JsonIgnore]
        public int Pid { get; set; }
        [JsonIgnore]
        public int ThreadId { get; set; }
        public string Span { get; set; }
        [JsonIgnore]
        public string Exportable { get; set; }
        public string UniqueId { get; set; }
        public string TraceId { get; set; }
        [JsonIgnore]
        public DateTime TimeStampUtc { get; set; }
        [JsonProperty(PropertyName = "service")]
        public string ServiceName { get; set; }
        [JsonIgnore]
        public LogLevel Level { get; set; }
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
        public string ClassName { get; set; }
        [JsonProperty(PropertyName = "level")]
        public string LevelToString
        {
            get
            {
                switch (Level)
                {
                    case LogLevel.Critical:
                        return "FATAL";
                    case LogLevel.Debug:
                        return "DEBUG";
                    case LogLevel.Error:
                        return "ERROR";
                    case LogLevel.Warning:
                        return "WARN";
                    case LogLevel.Information:
                        return "INFO";
                    default:
                        return "INFO";
                }
            }
        }
    }
}