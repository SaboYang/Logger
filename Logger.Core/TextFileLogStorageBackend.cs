using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Logger.Core.Models;

namespace Logger.Core
{
    public sealed class TextFileLogStorageBackend : ILogStorageBackend, ILogFileSource
    {
        private static readonly UTF8Encoding FileEncoding = new UTF8Encoding(false);

        private readonly object _syncRoot = new object();
        private readonly string _logFilePath;
        private bool _headerWritten;

        public TextFileLogStorageBackend(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public bool IsFileOutputEnabled
        {
            get { return !string.IsNullOrWhiteSpace(_logFilePath); }
        }

        public string LogFilePath
        {
            get { return _logFilePath; }
        }

        public void WriteBatch(IReadOnlyList<LogEntry> entries, LogStorageContext context)
        {
            if (!IsFileOutputEnabled || entries == null || entries.Count == 0)
            {
                return;
            }

            string directoryPath = Path.GetDirectoryName(_logFilePath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                return;
            }

            Directory.CreateDirectory(directoryPath);

            lock (_syncRoot)
            {
                using (FileStream stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter writer = new StreamWriter(stream, FileEncoding))
                {
                    if (!_headerWritten)
                    {
                        WriteHeader(writer, context);
                        _headerWritten = true;
                    }

                    foreach (LogEntry entry in entries)
                    {
                        writer.WriteLine(FormatEntry(entry));
                    }
                }
            }
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
