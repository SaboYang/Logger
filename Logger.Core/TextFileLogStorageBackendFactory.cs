using System;
using System.IO;
using System.Text;

namespace Logger.Core
{
    public sealed class TextFileLogStorageBackendFactory : ILogStorageBackendFactory
    {
        private readonly string _logRootDirectoryPath;

        public TextFileLogStorageBackendFactory(string logRootDirectoryPath = null)
        {
            _logRootDirectoryPath = string.IsNullOrWhiteSpace(logRootDirectoryPath)
                ? Path.Combine(AppContext.BaseDirectory, "Logs")
                : logRootDirectoryPath.Trim();
        }

        public ILogStorageBackend CreateBackend(LogStorageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new TextFileLogStorageBackend(BuildLogFilePath(context, "log"));
        }

        private string BuildLogFilePath(LogStorageContext context, string extension)
        {
            string loggerDirectoryName = SanitizePathSegment(context.LoggerName);
            string dateDirectoryName = context.SessionStartedAt.ToString("yyyyMMdd");
            string fileName = string.Format(
                "{0}_{1}.{2}",
                context.SessionStartedAt.ToString("HHmmss_fff"),
                context.SessionId.ToString("N").Substring(0, 8),
                extension);

            return Path.Combine(_logRootDirectoryPath, loggerDirectoryName, dateDirectoryName, fileName);
        }

        private static string SanitizePathSegment(string value)
        {
            string normalizedValue = string.IsNullOrWhiteSpace(value) ? "Default" : value.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(normalizedValue.Length);

            foreach (char ch in normalizedValue)
            {
                builder.Append(Array.IndexOf(invalidChars, ch) >= 0 ? '_' : ch);
            }

            string sanitizedValue = builder.ToString().Trim();
            return string.IsNullOrWhiteSpace(sanitizedValue) ? "Default" : sanitizedValue;
        }
    }
}
