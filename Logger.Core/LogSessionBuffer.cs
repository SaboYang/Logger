using System;
using System.Collections.Generic;
using Logger.Core.Models;

namespace Logger.Core
{
    internal sealed class LogSessionBuffer : ILoggerOutput, ILogSessionSource, ILogRuntimeMetricsSource, IDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly List<LogEntry> _entries = new List<LogEntry>();
        private readonly LogStorageContext _sessionInfo;
        private readonly int _maxBufferedEntries;
        private int _totalEntryCount;
        private bool _disposed;

        public LogSessionBuffer(LogStorageContext sessionInfo)
        {
            _sessionInfo = sessionInfo ?? throw new ArgumentNullException(nameof(sessionInfo));
            _maxBufferedEntries = _sessionInfo.MaxBufferedSessionEntries;
        }

        public Guid SessionId
        {
            get { return _sessionInfo.SessionId; }
        }

        public DateTime SessionStartedAt
        {
            get { return _sessionInfo.SessionStartedAt; }
        }

        public int SessionEntryCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _totalEntryCount;
                }
            }
        }

        public int BufferedSessionEntryCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _entries.Count;
                }
            }
        }

        public int DroppedPendingEntryCount
        {
            get { return 0; }
        }

        public void SetMinimumLevel(LogLevel minimumLevel)
        {
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

                _entries.Add(new LogEntry(DateTime.Now, level, normalizedMessage));
                _totalEntryCount++;
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

                _entries.AddRange(normalizedEntries);
                _totalEntryCount += normalizedEntries.Count;
                TrimEntries();
            }
        }

        public IReadOnlyList<LogEntry> GetSessionEntriesSnapshot()
        {
            lock (_syncRoot)
            {
                return _entries.ToArray();
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
                _entries.Clear();
                _totalEntryCount = 0;
            }
        }

        private void TrimEntries()
        {
            int overflowCount = _entries.Count - _maxBufferedEntries;
            if (overflowCount <= 0)
            {
                return;
            }

            _entries.RemoveRange(0, overflowCount);
        }
    }
}
