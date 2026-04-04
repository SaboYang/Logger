using System;
using System.IO;

namespace Logger.Core
{
    public sealed class CsvFileLogStorageBackendFactory : ILogStorageBackendFactory
    {
        private readonly string _logRootDirectoryPath;
        private readonly LogFileRollingMode _rollingMode;

        public CsvFileLogStorageBackendFactory(
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
                "csv",
                _rollingMode);

            return new CsvFileLogStorageBackend(pathProvider);
        }
    }
}
