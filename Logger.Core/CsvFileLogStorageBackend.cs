using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Logger.Core.Models;

namespace Logger.Core
{
    public sealed class CsvFileLogStorageBackend : ILogStorageBackend, ILogFileSource
    {
        private static readonly UTF8Encoding FileEncoding = new UTF8Encoding(false);

        private readonly object _syncRoot = new object();
        private readonly FileLogPathProvider _pathProvider;
        private readonly HashSet<string> _headerWrittenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private DateTime _lastRetentionCleanupDate = DateTime.MinValue;

        public CsvFileLogStorageBackend(string logFilePath)
            : this(FileLogPathProvider.CreateFixed(logFilePath))
        {
        }

        internal CsvFileLogStorageBackend(FileLogPathProvider pathProvider)
        {
            _pathProvider = pathProvider;
        }

        public bool IsFileOutputEnabled
        {
            get { return !string.IsNullOrWhiteSpace(LogFilePath); }
        }

        public string LogFilePath
        {
            get { return _pathProvider != null ? _pathProvider.CurrentPath : null; }
        }

        public void WriteBatch(IReadOnlyList<LogEntry> entries, LogStorageContext context)
        {
            if (!IsFileOutputEnabled || entries == null || entries.Count == 0)
            {
                return;
            }

            Dictionary<string, List<LogEntry>> entryGroups = GroupEntriesByFilePath(entries);
            foreach (KeyValuePair<string, List<LogEntry>> entryGroup in entryGroups)
            {
                string logFilePath = entryGroup.Key;
                string directoryPath = Path.GetDirectoryName(logFilePath);
                if (string.IsNullOrEmpty(directoryPath))
                {
                    continue;
                }

                Directory.CreateDirectory(directoryPath);

                lock (_syncRoot)
                {
                    EnsureRetentionCleanup();

                    bool shouldWriteHeader = !_headerWrittenPaths.Contains(logFilePath)
                        && (!File.Exists(logFilePath) || new FileInfo(logFilePath).Length == 0);

                    using (FileStream stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (StreamWriter writer = new StreamWriter(stream, FileEncoding))
                    {
                        if (shouldWriteHeader)
                        {
                            writer.WriteLine("Timestamp,Level,Message");
                        }

                        _headerWrittenPaths.Add(logFilePath);

                        foreach (LogEntry entry in entryGroup.Value)
                        {
                            writer.WriteLine(string.Format(
                                "{0},{1},{2}",
                                Escape(entry != null ? entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff") : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")),
                                Escape(entry != null ? entry.LevelText : "INFO"),
                                Escape(entry != null ? entry.Message : string.Empty)));
                        }
                    }
                }
            }
        }

        private void EnsureRetentionCleanup()
        {
            if (_pathProvider == null || !_pathProvider.ShouldCleanupExpiredDailyLogs)
            {
                return;
            }

            DateTime today = DateTime.Today;
            if (_lastRetentionCleanupDate == today)
            {
                return;
            }

            LogFileRetentionCleaner.CleanupExpiredDailyLogFiles(_pathProvider, DateTime.Now);
            _lastRetentionCleanupDate = today;
        }

        private Dictionary<string, List<LogEntry>> GroupEntriesByFilePath(IReadOnlyList<LogEntry> entries)
        {
            Dictionary<string, List<LogEntry>> groups = new Dictionary<string, List<LogEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (LogEntry entry in entries)
            {
                string logFilePath = _pathProvider != null
                    ? _pathProvider.GetPath(entry != null ? entry.Timestamp : DateTime.Now)
                    : null;

                if (string.IsNullOrWhiteSpace(logFilePath))
                {
                    continue;
                }

                List<LogEntry> group;
                if (!groups.TryGetValue(logFilePath, out group))
                {
                    group = new List<LogEntry>();
                    groups[logFilePath] = group;
                }

                group.Add(entry);
            }

            return groups;
        }

        private static string Escape(string value)
        {
            string text = value ?? string.Empty;
            text = text.Replace("\"", "\"\"");
            return "\"" + text + "\"";
        }
    }
}
