using System;
using System.IO;

namespace Logger.Core
{
    public sealed class CsvFileLogStorageBackendFactory : ILogStorageBackendFactory
    {
        private readonly string _logRootDirectoryPath;

        public CsvFileLogStorageBackendFactory(string logRootDirectoryPath = null)
        {
            _logRootDirectoryPath = LoggerPathUtility.ResolveLogRootDirectory(logRootDirectoryPath);
        }

        public ILogStorageBackend CreateBackend(LogStorageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new CsvFileLogStorageBackend(BuildLogFilePath(context, "csv"));
        }

        private string BuildLogFilePath(LogStorageContext context, string extension)
        {
            return LoggerPathUtility.BuildLogFilePath(
                context.LoggerName,
                _logRootDirectoryPath,
                context.SessionStartedAt,
                context.SessionId,
                extension);
        }
    }
}
