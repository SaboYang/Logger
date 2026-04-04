using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Logger.Core.Models;

namespace Logger.Core
{
    public class LogStore : ILoggerOutput, ILogViewSource, ILogLevelThreshold, IDisposable
    {
        private readonly BulkObservableCollection<LogEntry> _entries = new BulkObservableCollection<LogEntry>();
        private readonly object _syncRoot = new object();
        private int _maxEntries = 500;
        private LogLevel _minimumLevel = LogLevel.Trace;
        private bool _disposed;

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

        public LogLevel MinimumLevel
        {
            get
            {
                lock (_syncRoot)
                {
                    return _minimumLevel;
                }
            }
            set
            {
                lock (_syncRoot)
                {
                    _minimumLevel = value;
                }
            }
        }

        public void SetMinimumLevel(LogLevel minimumLevel)
        {
            MinimumLevel = minimumLevel;
        }

        public void Trace(string message)
        {
            AddLog(LogLevel.Trace, message);
        }

        public void Debug(string message)
        {
            AddLog(LogLevel.Debug, message);
        }

        public void Info(string message)
        {
            AddLog(LogLevel.Info, message);
        }

        public void Success(string message)
        {
            AddLog(LogLevel.Success, message);
        }

        public void Warning(string message)
        {
            AddLog(LogLevel.Warn, message);
        }

        public void Error(string message)
        {
            AddLog(LogLevel.Error, message);
        }

        public void Fatal(string message)
        {
            AddLog(LogLevel.Fatal, message);
        }

        public void AddLog(LogLevel level, string message)
        {
            string normalizedMessage = LogEntrySanitizer.NormalizeMessage(message);
            if (normalizedMessage == null)
            {
                return;
            }

            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                if (!LogEntryFilter.MeetsMinimumLevel(level, _minimumLevel))
                {
                    return;
                }

                _entries.Add(new LogEntry(DateTime.Now, level, normalizedMessage));
                TrimEntries();
            }
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            List<LogEntry> normalizedEntries = LogEntrySanitizer.NormalizeEntries(entries);
            if (normalizedEntries.Count == 0)
            {
                return;
            }

            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                List<LogEntry> filteredEntries = LogEntryFilter.FilterEntries(normalizedEntries, _minimumLevel);
                if (filteredEntries.Count == 0)
                {
                    return;
                }

                _entries.AddRange(filteredEntries);
                TrimEntries();
            }
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _entries.ReplaceAll(Array.Empty<LogEntry>());
            }
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
