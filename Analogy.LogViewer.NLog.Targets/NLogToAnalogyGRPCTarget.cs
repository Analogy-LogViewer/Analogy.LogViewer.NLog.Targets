using Analogy.Interfaces;
using Analogy.Interfaces.DataTypes;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Analogy.LogViewer.NLog.Targets
{
    [Target("NLogAnalogyGRPCTarget")]
    public class NLogToAnalogyGRPCTarget : AsyncTaskTarget
    {
        private static readonly int ProcessId = Process.GetCurrentProcess().Id;

        private LogServer.Clients.AnalogyMessageProducer _producer;

        [RequiredParameter]
        public Layout Address { get; set; }

        public NLogToAnalogyGRPCTarget() : this("http://localhost:6000")
        {
        }

        public NLogToAnalogyGRPCTarget(string address)
        {
            Address = address;
            Layout = "${message}";

            // IncludeCallSite = true
        }

        protected override void InitializeTarget()
        {
            if (ContextProperties.Count == 0)
            {
                ContextProperties.Add(new TargetPropertyWithContext(Constants.MachineName, "${MachineName}"));
                ContextProperties.Add(new TargetPropertyWithContext(Constants.ProcessName, "${ProcessName}"));
                ContextProperties.Add(new TargetPropertyWithContext(Constants.UserName, "${Environment-User}"));
                ContextProperties.Add(new TargetPropertyWithContext(Constants.Source, "${logger}"));
                ContextProperties.Add(new TargetPropertyWithContext(Constants.ThreadId, "${ThreadId}"));
            }

            base.InitializeTarget();

            var address = RenderLogEvent(Address, LogEventInfo.CreateNullEvent());
            _producer = new LogServer.Clients.AnalogyMessageProducer(address);
        }

        protected override void CloseTarget()
        {
            base.CloseTarget();
            _producer?.Dispose();
        }

        protected override Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken)
        {
            var logLevel = ConvertLogLevel(logEvent.Level);
            var logMessage = RenderLogEvent(Layout, logEvent);

            int processId = ProcessId;
            int threadId = 0;
            string sourceName = logEvent.LoggerName;
            string machineName = string.Empty;
            string processName = string.Empty;
            string userName = string.Empty;
            string categoryName = string.Empty;
            for (int i = 0; i < ContextProperties.Count; ++i)
            {
                var contextProperty = ContextProperties[i];
                if (Constants.MachineName.Equals(contextProperty.Name))
                {
                    machineName = RenderLogEvent(contextProperty.Layout, logEvent);
                }
                else if (Constants.ProcessName.Equals(contextProperty.Name))
                {
                    processName = RenderLogEvent(contextProperty.Layout, logEvent);
                }
                else if (Constants.UserName.Equals(contextProperty.Name))
                {
                    userName = RenderLogEvent(contextProperty.Layout, logEvent);
                }
                else if (Constants.Category.Equals(contextProperty.Name))
                {
                    categoryName = RenderLogEvent(contextProperty.Layout, logEvent);
                }
                else if (Constants.Source.Equals(contextProperty.Name))
                {
                    sourceName = RenderLogEvent(contextProperty.Layout, logEvent);
                }
                else if (Constants.ThreadId.Equals(contextProperty.Name))
                {
                    var threadIdProperty = RenderLogEvent(contextProperty.Layout, logEvent);
                    if (!string.IsNullOrEmpty(threadIdProperty))
                    {
                        int.TryParse(threadIdProperty, out threadId);
                    }
                }
                else if (Constants.ProcessId.Equals(contextProperty.Name))
                {
                    var processIdProperty = RenderLogEvent(contextProperty.Layout, logEvent);
                    if (!string.IsNullOrEmpty(processIdProperty))
                    {
                        int.TryParse(processIdProperty, out processId);
                    }
                }
            }

            string memberName = logEvent.CallerMemberName ?? "";
            string filePath = logEvent.CallerFilePath ?? "";
            return _producer?.Log(logMessage, sourceName, logLevel, categoryName, machineName, userName, processName,
                processId, threadId, null, memberName, logEvent.CallerLineNumber, filePath);
        }

        private static AnalogyLogLevel ConvertLogLevel(LogLevel logLevel)
        {
            if (logLevel == LogLevel.Error)
            {
                return AnalogyLogLevel.Error;
            }

            if (logLevel == LogLevel.Debug)
            {
                return AnalogyLogLevel.Debug;
            }
            if (logLevel == LogLevel.Fatal)
            {
                return AnalogyLogLevel.Critical;
            }
            if (logLevel == LogLevel.Info)
            {
                return AnalogyLogLevel.Information;
            }
            if (logLevel == LogLevel.Trace)
            {
                return AnalogyLogLevel.Trace;
            }
            if (logLevel == LogLevel.Warn)
            {
                return AnalogyLogLevel.Warning;
            }
            if (logLevel == LogLevel.Off)
            {
                return AnalogyLogLevel.None;
            }
            return AnalogyLogLevel.Unknown;
        }
    }
}