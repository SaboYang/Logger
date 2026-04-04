using System;

namespace Logger.Core
{
    internal sealed class FileLogPathProvider
    {
        private readonly string _loggerName;
        private readonly string _logRootDirectoryPath;
        private readonly string _extension;
        private readonly LogFileRollingMode _rollingMode;
        private readonly string _fixedPath;

        private FileLogPathProvider(string fixedPath)
        {
            _fixedPath = fixedPath;
        }

        public FileLogPathProvider(
            string loggerName,
            string logRootDirectoryPath,
            string extension,
            LogFileRollingMode rollingMode)
        {
            _loggerName = LoggerPathUtility.NormalizeLoggerName(loggerName);
            _logRootDirectoryPath = LoggerPathUtility.ResolveLogRootDirectory(logRootDirectoryPath);
            _extension = NormalizeExtension(extension);
            _rollingMode = rollingMode;
        }

        public string CurrentPath
        {
            get
            {
                return string.IsNullOrWhiteSpace(_fixedPath)
                    ? GetPath(DateTime.Now)
                    : _fixedPath;
            }
        }

        public static FileLogPathProvider CreateFixed(string fixedPath)
        {
            return new FileLogPathProvider(fixedPath);
        }

        public string GetPath(DateTime timestamp)
        {
            return string.IsNullOrWhiteSpace(_fixedPath)
                ? LoggerPathUtility.BuildLogFilePath(_loggerName, _logRootDirectoryPath, timestamp, _extension, _rollingMode)
                : _fixedPath;
        }

        private static string NormalizeExtension(string extension)
        {
            string normalized = (extension ?? string.Empty).Trim().TrimStart('.');
            return string.IsNullOrWhiteSpace(normalized) ? "log" : normalized;
        }
    }
}
