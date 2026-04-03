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
            return new LogStore(name, _logRootDirectoryPath);
        }
    }
}
