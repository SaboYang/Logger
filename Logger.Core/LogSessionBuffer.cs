using System;
using System.Collections.Generic;
using Logger.Core.Models;

namespace Logger.Core
{
    internal sealed class LogSessionBuffer : ILoggerOutput, ILogSessionSource
    {
        private readonly object _syncRoot = new object();
        private readonly List<LogEntry> _entries = new List<LogEntry>();
        private readonly LoggerSessionInfo _sessionInfo;

        public LogSessionBuffer(LoggerSessionInfo sessionInfo)
        {
            _sessionInfo = sessionInfo ?? throw new ArgumentNullException(nameof(sessionInfo));
        }

        public Guid SessionId
        {
            get { return _sessionInfo.SessionId; }
        }

        public DateTime SessionStartedAt
        {
            get { return _sessionInfo.StartedAt; }
        }

        public int SessionEntryCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _entries.Count;
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
            string normalizedMessage = LogEntrySanitizer.NormalizeMessage(message);
            if (normalizedMessage == null)
            {
                return;
            }

            lock (_syncRoot)
            {
                _entries.Add(new LogEntry(DateTime.Now, level, normalizedMessage));
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
                _entries.AddRange(normalizedEntries);
            }
        }

        public IReadOnlyList<LogEntry> GetSessionEntriesSnapshot()
        {
            lock (_syncRoot)
            {
                return _entries.ToArray();
            }
        }
    }
}
