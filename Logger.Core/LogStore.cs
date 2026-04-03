using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Logger.Core.Models;

namespace Logger.Core
{
    public class LogStore : ILoggerOutput, ILogViewSource
    {
        private readonly BulkObservableCollection<LogEntry> _entries = new BulkObservableCollection<LogEntry>();
        private readonly object _syncRoot = new object();
        private int _maxEntries = 500;

        public object SyncRoot
        {
            get { return _syncRoot; }
        }

        public ObservableCollection<LogEntry> Entries
        {
            get { return _entries; }
        }

        public int MaxEntries
        {
            get { return _maxEntries; }
            set
            {
                lock (_syncRoot)
                {
                    _maxEntries = Math.Max(1, value);
                    TrimEntries();
                }
            }
        }

        public void AddTrace(string message)
        {
            AddLog(LogLevel.Trace, message);
        }

        public void AddDebug(string message)
        {
            AddLog(LogLevel.Debug, message);
        }

        public void AddInfo(string message)
        {
            AddLog(LogLevel.Info, message);
        }

        public void AddSuccess(string message)
        {
            AddLog(LogLevel.Success, message);
        }

        public void AddWarning(string message)
        {
            AddLog(LogLevel.Warn, message);
        }

        public void AddError(string message)
        {
            AddLog(LogLevel.Error, message);
        }

        public void AddFatal(string message)
        {
            AddLog(LogLevel.Fatal, message);
        }

        public void AddLog(LogLevel level, string message)
        {
            string normalizedMessage = NormalizeMessage(message);
            if (normalizedMessage == null)
            {
                return;
            }

            lock (_syncRoot)
            {
                _entries.Add(new LogEntry(DateTime.Now, level, normalizedMessage));
                TrimEntries();
            }
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            List<LogEntry> normalizedEntries = new List<LogEntry>();
            foreach (LogEntry entry in entries)
            {
                LogEntry normalizedEntry = NormalizeEntry(entry);
                if (normalizedEntry != null)
                {
                    normalizedEntries.Add(normalizedEntry);
                }
            }

            if (normalizedEntries.Count == 0)
            {
                return;
            }

            lock (_syncRoot)
            {
                _entries.AddRange(normalizedEntries);
                TrimEntries();
            }
        }

        private static LogEntry NormalizeEntry(LogEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            string normalizedMessage = NormalizeMessage(entry.Message);
            if (normalizedMessage == null)
            {
                return null;
            }

            if (normalizedMessage == entry.Message)
            {
                return entry;
            }

            return new LogEntry(entry.Timestamp, entry.Level, normalizedMessage);
        }

        private static string NormalizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            return message.Trim();
        }

        private void TrimEntries()
        {
            int overflowCount = _entries.Count - _maxEntries;
            if (overflowCount <= 0)
            {
                return;
            }

            _entries.RemoveRange(0, overflowCount);
        }
    }
}
