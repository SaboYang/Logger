using System;
using System.IO;

namespace Logger.Core
{
    public sealed class TextFileLogStorageBackendFactory : ILogStorageBackendFactory
    {
        private readonly string _logRootDirectoryPath;
        private readonly LogFileRollingMode _rollingMode;
        private readonly int _rollingRetentionDays;

        public TextFileLogStorageBackendFactory(
            string logRootDirectoryPath = null,
            LogFileRollingMode rollingMode = LogFileRollingMode.Day,
            int rollingRetentionDays = 30)
        {
            _logRootDirectoryPath = LoggerPathUtility.ResolveLogRootDirectory(logRootDirectoryPath);
            _rollingMode = rollingMode;
            _rollingRetentionDays = Math.Max(1, rollingRetentionDays);
        }

        public ILogStorageBackend CreateBackend(LogStorageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            FileLogPathProvider pathProvider = new FileLogPathProvider(
                context.LoggerName,
                _logRootDirectoryPath,
                "log",
                _rollingMode,
                _rollingRetentionDays);

            return new TextFileLogStorageBackend(pathProvider);
        }
    }
}
