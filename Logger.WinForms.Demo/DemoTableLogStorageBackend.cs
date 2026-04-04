using System;
using System.Collections.Generic;
using Logger.Core;
using Logger.Core.Models;

namespace Logger.WinForms.Demo
{
    internal sealed class DemoTableLogStorageBackend : ILogStorageBackend
    {
        private readonly object _syncRoot = new object();
        private readonly List<DemoTableLogRecord> _records = new List<DemoTableLogRecord>();

        public void WriteBatch(IReadOnlyList<LogEntry> entries, LogStorageContext context)
        {
            if (entries == null || entries.Count == 0 || context == null)
            {
                return;
            }

            lock (_syncRoot)
            {
                foreach (LogEntry entry in entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    _records.Add(
                        new DemoTableLogRecord(
                            context.LoggerName,
                            context.SessionId,
                            entry.Timestamp,
                            entry.LevelText,
                            entry.Message));
                }
            }
        }

        public IReadOnlyList<DemoTableLogRecord> GetRecordsSnapshot()
        {
            lock (_syncRoot)
            {
                return _records.ToArray();
            }
        }
    }

    internal sealed class DemoTableLogRecord
    {
        public DemoTableLogRecord(string loggerName, Guid sessionId, DateTime timestamp, string level, string message)
        {
            LoggerName = loggerName;
            SessionId = sessionId;
            Timestamp = timestamp;
            Level = level;
            Message = message ?? string.Empty;
        }

        public string LoggerName { get; }

        public Guid SessionId { get; }

        public DateTime Timestamp { get; }

        public string Level { get; }

        public string Message { get; }
    }
}
