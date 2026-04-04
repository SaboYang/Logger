using System;
using System.IO;
using System.Text;

namespace Logger.Core
{
    internal static class LoggerPathUtility
    {
        public static string NormalizeLoggerName(string loggerName)
        {
            return string.IsNullOrWhiteSpace(loggerName) ? "Default" : loggerName.Trim();
        }

        public static string ResolveLogRootDirectory(string logRootDirectoryPath)
        {
            return string.IsNullOrWhiteSpace(logRootDirectoryPath)
                ? Path.Combine(AppContext.BaseDirectory, "Logs")
                : logRootDirectoryPath.Trim();
        }

        public static string BuildLogFilePath(
            string loggerName,
            string logRootDirectoryPath,
            DateTime sessionStartedAt,
            Guid sessionId,
            string extension)
        {
            string rootDirectoryPath = ResolveLogRootDirectory(logRootDirectoryPath);
            string loggerDirectoryName = SanitizePathSegment(loggerName);
            string dateDirectoryName = sessionStartedAt.ToString("yyyyMMdd");
            string fileName = string.Format(
                "{0}_{1}.{2}",
                sessionStartedAt.ToString("HHmmss_fff"),
                sessionId.ToString("N").Substring(0, 8),
                extension);

            return Path.Combine(rootDirectoryPath, loggerDirectoryName, dateDirectoryName, fileName);
        }

        public static string SanitizePathSegment(string value)
        {
            string normalizedValue = NormalizeLoggerName(value);
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
