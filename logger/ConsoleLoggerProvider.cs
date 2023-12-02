using System;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace queue_consumer
{
    [Microsoft.Extensions.Logging.ProviderAlias("CustomConsole")]
    public class ConsoleLoggerProvider : queue_consumer.LoggerProvider
    {
        bool Terminated;
        ConcurrentQueue<LogEntry> InfoQueue = new ConcurrentQueue<LogEntry>();
        void WriteLogLine()
        {
            LogEntry Info = null;
            if (InfoQueue.TryDequeue(out Info))
            {
                StringBuilder SB = new StringBuilder();
                SB.Append(Info.TimeStampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"));
                SB.Append(" ");
                switch (Info.Level)
                {
                    case LogLevel.Critical:
                        SB.Append("FATAL");
                        break;
                    case LogLevel.Debug:
                        SB.Append("DEBUG");
                        break;
                    case LogLevel.Error:
                        SB.Append("ERROR");
                        break;
                    case LogLevel.Warning:
                        SB.Append("WARN");
                        break;
                    case LogLevel.Information:
                        SB.Append("INFO");
                        break;
                    default:
                        SB.Append("INFO");
                        break;
                }
                SB.Append(" [");
                SB.Append(Info.ServiceName);
                SB.Append(",");
                SB.Append(Info.TraceId.ToString());
                SB.Append(",");
                SB.Append(Info.Span);
                SB.Append(",");
                SB.Append(Info.UniqueId);
                SB.Append("] ");
                SB.Append(Info.Pid.ToString());
                SB.Append(" --- [");
                SB.Append(Info.ThreadId.ToString());
                SB.Append("] --- ");
                SB.Append(Info.ClassName);
                SB.Append(" : ");
                SB.Append(Info.Message);
                Console.WriteLine(SB.ToString());
            }
        }

        void WriteLogJSON()
        {
            LogEntry Info = null;
            if (InfoQueue.TryDequeue(out Info))
            {
                Console.WriteLine(JsonConvert.SerializeObject(Info));
            }
        }

        void ThreadProc()
        {
            Task.Run(() =>
            {
                while (!Terminated)
                {
                    try
                    {
                        if (Settings.LogStructure == LogStructureEnum.Stackdriver)
                        {
                            WriteLogJSON();
                        }
                        else
                        {
                            WriteLogLine();
                        }
                        System.Threading.Thread.Sleep(100);
                    }
                    catch // (Exception ex)
                    {
                    }
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            Terminated = true;
            base.Dispose(disposing);
        }

        /*public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> Settings)
            : this(Settings.CurrentValue)
        {
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/change-tokens
            SettingsChangeToken = Settings.OnChange(settings =>
            {
                this.Settings = settings;
            });
        }*/

        public ConsoleLoggerProvider(ConsoleLoggerOptions Settings)
        {
            this.Settings = Settings;
            ThreadProc();
        }

        public override bool IsEnabled(LogLevel logLevel)
        {
            bool Result = logLevel != LogLevel.None
                && this.Settings.LogLevel != LogLevel.None
                && Convert.ToInt32(logLevel) >= Convert.ToInt32(this.Settings.LogLevel);
            return Result;
        }

        public override void WriteLog(LogEntry Info)
        {
            InfoQueue.Enqueue(Info);
        }

        internal ConsoleLoggerOptions Settings { get; private set; }
    }

}
