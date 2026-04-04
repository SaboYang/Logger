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
        private readonly string _logFilePath;
        private bool _headerWritten;

        public CsvFileLogStorageBackend(string logFilePath)
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
                        writer.WriteLine("Timestamp,Level,Message");
                        _headerWritten = true;
                    }

                    foreach (LogEntry entry in entries)
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

        private static string Escape(string value)
        {
            string text = value ?? string.Empty;
            text = text.Replace("\"", "\"\"");
            return "\"" + text + "\"";
        }
    }
}
