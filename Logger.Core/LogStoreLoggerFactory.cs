using System;
using System.IO;

namespace Logger.Core
{
    public sealed class LogStoreLoggerFactory : ILoggerFactory
    {
        private readonly string _logRootDirectoryPath;

        public LogStoreLoggerFactory(string logRootDirectoryPath = null)
        {
            _logRootDirectoryPath = string.IsNullOrWhiteSpace(logRootDirectoryPath)
                ? Path.Combine(AppContext.BaseDirectory, "Logs")
                : logRootDirectoryPath.Trim();
        }

        public ILoggerOutput CreateLogger(string name)
        {
            string loggerName = string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
            LoggerSessionInfo sessionInfo = new LoggerSessionInfo(loggerName);
            LogStore logStore = new LogStore();
            LogSessionBuffer sessionBuffer = new LogSessionBuffer(sessionInfo);
            FileLogWriter fileLogWriter = new FileLogWriter(sessionInfo, _logRootDirectoryPath);

            return new CompositeLogger(logStore, sessionBuffer, fileLogWriter, logStore, sessionBuffer, fileLogWriter);
        }
    }
}
