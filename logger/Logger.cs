
using Microsoft.Extensions.Logging;
using System;
using System.Collections;

namespace queue_consumer
{
    internal class Logger : ILogger
    {
        public LoggerProvider Provider { get; private set; }

        public Logger(LoggerProvider Provider)
        {
            this.Provider = Provider;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return Provider.ScopeProvider.Push(state);
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return Provider.IsEnabled(logLevel);
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if ((this as ILogger).IsEnabled(logLevel))
            {
                LogEntry Info = new LogEntry();
                Info.Level = logLevel;
                if(state.ToString().Contains("%%")){
                    var messageComponents = state.ToString().Split("%%");
                    Info.Message = messageComponents[0];
                    if(messageComponents.Length > 3){
                        Info.TraceId = messageComponents[1];
                        Info.Span = messageComponents[2];
                        Info.UniqueId = messageComponents[3];
                    }
                } else {
                    Info.Message = exception?.Message ?? state.ToString();
                }
                Info.ClassName = exception?.Source ?? "HostedService";
                if (state is string)
                {
                    Info.Message = state.ToString();
                }
                Provider.WriteLog(Info);
            }
        }

    }
}

