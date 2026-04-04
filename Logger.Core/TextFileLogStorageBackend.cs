using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Logger.Core.Models;

namespace Logger.Core
{
    public sealed class TextFileLogStorageBackend : ILogStorageBackend, ILogFileSource
    {
        private static readonly UTF8Encoding FileEncoding = new UTF8Encoding(false);

        private readonly object _syncRoot = new object();
        private readonly FileLogPathProvider _pathProvider;
        private readonly HashSet<string> _headerWrittenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public TextFileLogStorageBackend(string logFilePath)
            : this(FileLogPathProvider.CreateFixed(logFilePath))
        {
        }

        internal TextFileLogStorageBackend(FileLogPathProvider pathProvider)
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
                    bool shouldWriteHeader = !_headerWrittenPaths.Contains(logFilePath)
                        && (!File.Exists(logFilePath) || new FileInfo(logFilePath).Length == 0);

                    using (FileStream stream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (StreamWriter writer = new StreamWriter(stream, FileEncoding))
                    {
                        if (shouldWriteHeader)
                        {
                            WriteHeader(writer, context);
                        }

                        _headerWrittenPaths.Add(logFilePath);

                        foreach (LogEntry entry in entryGroup.Value)
                        {
                            writer.WriteLine(FormatEntry(entry));
                        }
                    }
                }
            }
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

        private static void WriteHeader(StreamWriter writer, LogStorageContext context)
        {
            if (context == null)
            {
                return;
            }

            writer.WriteLine("=== Logger Session Started ===");
            writer.WriteLine("Logger: " + context.LoggerName);
            writer.WriteLine("SessionId: " + context.SessionId.ToString("N"));
            writer.WriteLine("StartedAt: " + context.SessionStartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            writer.WriteLine("================================");
        }

        private static string FormatEntry(LogEntry entry)
        {
            string message = entry?.Message ?? string.Empty;
            message = message.Replace("\r\n", "\n").Replace('\r', '\n');
            message = message.Replace("\n", Environment.NewLine + "    ");

            return string.Format(
                "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}",
                entry != null ? entry.Timestamp : DateTime.Now,
                entry != null ? entry.LevelText : "INFO",
                message);
        }
    }
}
