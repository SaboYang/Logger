using System;
using System.IO;

namespace Logger.Core
{
    public sealed class TextFileLogStorageBackendFactory : ILogStorageBackendFactory
    {
        private readonly string _logRootDirectoryPath;
        private readonly LogFileRollingMode _rollingMode;

        public TextFileLogStorageBackendFactory(
            string logRootDirectoryPath = null,
            LogFileRollingMode rollingMode = LogFileRollingMode.Day)
        {
            _logRootDirectoryPath = LoggerPathUtility.ResolveLogRootDirectory(logRootDirectoryPath);
            _rollingMode = rollingMode;
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
                _rollingMode);

            return new TextFileLogStorageBackend(pathProvider);
        }
    }
}
