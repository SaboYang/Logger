using System;
using Logger.Core.Models;

namespace Logger.Core
{
    public sealed class LogStorageContext
    {
        public LogStorageContext(string loggerName, Guid sessionId, DateTime sessionStartedAt, LogLevel minimumLevel = LogLevel.Trace)
        {
            LoggerName = LoggerPathUtility.NormalizeLoggerName(loggerName);
            SessionId = sessionId == Guid.Empty ? Guid.NewGuid() : sessionId;
            SessionStartedAt = sessionStartedAt == DateTime.MinValue ? DateTime.Now : sessionStartedAt;
            MinimumLevel = minimumLevel;
        }

        public string LoggerName { get; }

        public Guid SessionId { get; }

        public DateTime SessionStartedAt { get; }

        public LogLevel MinimumLevel { get; }
    }
}
