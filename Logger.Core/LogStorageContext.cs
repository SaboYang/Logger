using System;
using Logger.Core.Models;

namespace Logger.Core
{
    public sealed class LogStorageContext
    {
        public LogStorageContext(
            string loggerName,
            Guid sessionId,
            DateTime sessionStartedAt,
            LogLevel minimumLevel = LogLevel.Trace,
            int maxBufferedSessionEntries = 5000,
            int maxPendingStorageEntries = 5000,
            string spoolRootDirectoryPath = null,
            LogSpoolFlushMode spoolFlushMode = LogSpoolFlushMode.Buffered)
        {
            LoggerName = LoggerPathUtility.NormalizeLoggerName(loggerName);
            SessionId = sessionId == Guid.Empty ? Guid.NewGuid() : sessionId;
            SessionStartedAt = sessionStartedAt == DateTime.MinValue ? DateTime.Now : sessionStartedAt;
            MinimumLevel = minimumLevel;
            MaxBufferedSessionEntries = Math.Max(1, maxBufferedSessionEntries);
            MaxPendingStorageEntries = Math.Max(1, maxPendingStorageEntries);
            SpoolRootDirectoryPath = LoggerPathUtility.ResolveSpoolRootDirectory(spoolRootDirectoryPath);
            SpoolFlushMode = spoolFlushMode;
        }

        public string LoggerName { get; }

        public Guid SessionId { get; }

        public DateTime SessionStartedAt { get; }

        public LogLevel MinimumLevel { get; }

        public int MaxBufferedSessionEntries { get; }

        public int MaxPendingStorageEntries { get; }

        public string SpoolRootDirectoryPath { get; }

        public LogSpoolFlushMode SpoolFlushMode { get; }
    }
}
