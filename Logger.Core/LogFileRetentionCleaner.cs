using System;
using System.Globalization;
using System.IO;

namespace Logger.Core
{
    internal static class LogFileRetentionCleaner
    {
        public static void CleanupExpiredDailyLogFiles(FileLogPathProvider pathProvider, DateTime now)
        {
            if (pathProvider == null || !pathProvider.ShouldCleanupExpiredDailyLogs)
            {
                return;
            }

            string currentPath = pathProvider.GetPath(now);
            string directoryPath = Path.GetDirectoryName(currentPath);
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return;
            }

            string extension = Path.GetExtension(currentPath);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return;
            }

            DateTime cutoffDate = now.Date.AddDays(-pathProvider.RetentionDays);
            string searchPattern = "*." + extension.TrimStart('.');

            string[] filePaths;
            try
            {
                filePaths = Directory.GetFiles(directoryPath, searchPattern);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            foreach (string filePath in filePaths)
            {
                if (!IsExpiredDailyLogFile(filePath, cutoffDate))
                {
                    continue;
                }

                TryDelete(filePath);
            }
        }

        private static bool IsExpiredDailyLogFile(string filePath, DateTime cutoffDate)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Length != 8)
            {
                return false;
            }

            DateTime fileDate;
            if (!DateTime.TryParseExact(
                fileName,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fileDate))
            {
                return false;
            }

            return fileDate < cutoffDate;
        }

        private static void TryDelete(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
