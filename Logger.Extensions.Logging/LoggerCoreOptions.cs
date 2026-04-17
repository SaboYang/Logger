using Logger.Core;
using Logger.Core.Models;

namespace Logger.Extensions.Logging
{
    public sealed class LoggerCoreOptions
    {
        public string LogRootDirectoryPath { get; set; }

        public LogFileRollingMode RollingMode { get; set; } = LogFileRollingMode.Day;

        public int RollingRetentionDays { get; set; } = 30;

        public int MaxBufferedSessionEntries { get; set; } = 5000;

        public int MaxPendingStorageEntries { get; set; } = 5000;

        public string SpoolRootDirectoryPath { get; set; }

        public LogSpoolFlushMode SpoolFlushMode { get; set; } = LogSpoolFlushMode.Buffered;

        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;
    }
}
