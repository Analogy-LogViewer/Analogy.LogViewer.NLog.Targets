using Analogy.Interfaces;
using NLog;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Analogy.LogViewer.NLog.Targets
{
    [Target("NLogAnalogyGRPCTarget")]
    public class NLogToAnalogyGRPCTarget : AsyncTaskTarget
    {
        private static readonly int ProcessId = Process.GetCurrentProcess().Id;
        private static readonly string ProcessName = Process.GetCurrentProcess().ProcessName;
#if NETCOREAPP3_1
        public static LogServer.Clients.AnalogyMessageProducer producer;

#endif
        public NLogToAnalogyGRPCTarget() : this("http://localhost:6000")
        {
        }
        public NLogToAnalogyGRPCTarget(string address)
        {
            producer = new LogServer.Clients.AnalogyMessageProducer(address, null);
        }
        protected override Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken)
        {
            AnalogyLogLevel level;
            if (logEvent.Level == LogLevel.Error)
            {
                level = AnalogyLogLevel.Error;
            }
            else if (logEvent.Level == LogLevel.Debug)
            {
                level = AnalogyLogLevel.Debug;
            }
            else if (logEvent.Level == LogLevel.Fatal)
            {
                level = AnalogyLogLevel.Critical;
            }
            else if (logEvent.Level == LogLevel.Info)
            {
                level = AnalogyLogLevel.Information;
            }
            else if (logEvent.Level == LogLevel.Off)
            {
                level = AnalogyLogLevel.None;
            }
            else if (logEvent.Level == LogLevel.Trace)
            {
                level = AnalogyLogLevel.Trace;
            }
            else if (logEvent.Level == LogLevel.Warn)
            {
                level = AnalogyLogLevel.Warning;
            }
            else
            {
                level = AnalogyLogLevel.Unknown;
            }
            return producer.Log(logEvent.FormattedMessage, logEvent.CallerClassName, level, string.Empty, Environment.MachineName,
                Environment.UserName, ProcessName, ProcessId, -1, null, logEvent.CallerMemberName, logEvent.CallerLineNumber, logEvent.CallerFilePath);
        }
    }

}
