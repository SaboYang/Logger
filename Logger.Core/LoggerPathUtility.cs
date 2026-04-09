using System;
using System.Globalization;
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

        public static string ResolveSpoolRootDirectory(string spoolRootDirectoryPath)
        {
            return string.IsNullOrWhiteSpace(spoolRootDirectoryPath)
                ? Path.Combine(AppContext.BaseDirectory, "LogSpool")
                : spoolRootDirectoryPath.Trim();
        }

        public static string BuildSpoolDirectoryPath(string loggerName, string spoolRootDirectoryPath)
        {
            return Path.Combine(
                ResolveSpoolRootDirectory(spoolRootDirectoryPath),
                SanitizePathSegment(loggerName));
        }

        public static string BuildSpoolFilePath(string loggerName, string spoolRootDirectoryPath)
        {
            return Path.Combine(
                BuildSpoolDirectoryPath(loggerName, spoolRootDirectoryPath),
                "current.wal");
        }

        public static string BuildSpoolCheckpointPath(string loggerName, string spoolRootDirectoryPath)
        {
            return Path.Combine(
                BuildSpoolDirectoryPath(loggerName, spoolRootDirectoryPath),
                "current.chk");
        }

        public static string BuildLogFilePath(
            string loggerName,
            string logRootDirectoryPath,
            DateTime timestamp,
            string extension,
            LogFileRollingMode rollingMode)
        {
            string rootDirectoryPath = ResolveLogRootDirectory(logRootDirectoryPath);
            string loggerDirectoryName = SanitizePathSegment(loggerName);
            string normalizedExtension = string.IsNullOrWhiteSpace(extension)
                ? "log"
                : extension.Trim().TrimStart('.');
            DateTime normalizedTimestamp = timestamp == DateTime.MinValue ? DateTime.Now : timestamp;
            string fileName = BuildRollingFileName(normalizedTimestamp, normalizedExtension, rollingMode);

            return Path.Combine(rootDirectoryPath, loggerDirectoryName, fileName);
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

        private static string BuildRollingFileName(DateTime timestamp, string extension, LogFileRollingMode rollingMode)
        {
            switch (rollingMode)
            {
                case LogFileRollingMode.SingleFile:
                    return "current." + extension;
                case LogFileRollingMode.Year:
                    return timestamp.ToString("yyyy") + "." + extension;
                case LogFileRollingMode.Month:
                    return timestamp.ToString("yyyyMM") + "." + extension;
                case LogFileRollingMode.Week:
                    int weekYear;
                    int weekNumber;
                    GetIsoWeek(timestamp, out weekYear, out weekNumber);
                    return string.Format("{0}-W{1:00}.{2}", weekYear, weekNumber, extension);
                case LogFileRollingMode.DayWithRetention:
                case LogFileRollingMode.Day:
                default:
                    return timestamp.ToString("yyyyMMdd") + "." + extension;
            }
        }

        private static void GetIsoWeek(DateTime timestamp, out int weekYear, out int weekNumber)
        {
            DateTime date = timestamp.Date;
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);

            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }

            weekYear = date.Year;
            weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                date,
                CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
        }
    }
}
