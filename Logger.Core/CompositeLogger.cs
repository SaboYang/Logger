using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Logger.Core.Models;

namespace Logger.Core
{
    internal sealed class CompositeLogger : ILoggerOutput, ILogViewSource, ILogSessionSource, ILogFileSource, ILogLevelThreshold
    {
        private readonly ILoggerOutput[] _outputs;
        private readonly ILogViewSource _viewSource;
        private readonly ILogSessionSource _sessionSource;
        private readonly ILogFileSource _fileSource;
        private int _minimumLevel = (int)LogLevel.Info;

        public CompositeLogger(
            ILogViewSource viewSource,
            ILogSessionSource sessionSource,
            ILogFileSource fileSource,
            params ILoggerOutput[] outputs)
        {
            _viewSource = viewSource ?? throw new ArgumentNullException(nameof(viewSource));
            _sessionSource = sessionSource;
            _fileSource = fileSource;
            _outputs = outputs ?? Array.Empty<ILoggerOutput>();
        }

        public object SyncRoot
        {
            get { return _viewSource.SyncRoot; }
        }

        public ObservableCollection<LogEntry> Entries
        {
            get { return _viewSource.Entries; }
        }

        public int MaxEntries
        {
            get { return _viewSource.MaxEntries; }
            set { _viewSource.MaxEntries = value; }
        }

        public Guid SessionId
        {
            get { return _sessionSource != null ? _sessionSource.SessionId : Guid.Empty; }
        }

        public DateTime SessionStartedAt
        {
            get { return _sessionSource != null ? _sessionSource.SessionStartedAt : DateTime.MinValue; }
        }

        public int SessionEntryCount
        {
            get { return _sessionSource != null ? _sessionSource.SessionEntryCount : 0; }
        }

        public bool IsFileOutputEnabled
        {
            get { return _fileSource != null && _fileSource.IsFileOutputEnabled; }
        }

        public string LogFilePath
        {
            get { return _fileSource != null ? _fileSource.LogFilePath : null; }
        }

        public LogLevel MinimumLevel
        {
            get { return (LogLevel)Volatile.Read(ref _minimumLevel); }
            set
            {
                Interlocked.Exchange(ref _minimumLevel, (int)value);
                PropagateMinimumLevel(value);
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
            if (!LogEntryFilter.MeetsMinimumLevel(level, MinimumLevel))
            {
                return;
            }

            foreach (ILoggerOutput output in _outputs)
            {
                output?.AddLog(level, message);
            }
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            List<LogEntry> entryBatch = SnapshotEntries(entries);
            if (entryBatch.Count == 0)
            {
                return;
            }

            List<LogEntry> filteredEntries = LogEntryFilter.FilterEntries(entryBatch, MinimumLevel);
            if (filteredEntries.Count == 0)
            {
                return;
            }

            foreach (ILoggerOutput output in _outputs)
            {
                output?.AddLogs(filteredEntries);
            }
        }

        public IReadOnlyList<LogEntry> GetSessionEntriesSnapshot()
        {
            return _sessionSource != null
                ? _sessionSource.GetSessionEntriesSnapshot()
                : Array.Empty<LogEntry>();
        }

        private static List<LogEntry> SnapshotEntries(IEnumerable<LogEntry> entries)
        {
            List<LogEntry> snapshot = new List<LogEntry>();
            if (entries == null)
            {
                return snapshot;
            }

            foreach (LogEntry entry in entries)
            {
                if (entry != null)
                {
                    snapshot.Add(entry);
                }
            }

            return snapshot;
        }

        private void PropagateMinimumLevel(LogLevel minimumLevel)
        {
            SetMinimumLevel(_viewSource as ILogLevelThreshold, minimumLevel);
            SetMinimumLevel(_sessionSource as ILogLevelThreshold, minimumLevel);

            foreach (ILoggerOutput output in _outputs)
            {
                SetMinimumLevel(output as ILogLevelThreshold, minimumLevel);
            }
        }

        private static void SetMinimumLevel(ILogLevelThreshold threshold, LogLevel minimumLevel)
        {
            if (threshold != null)
            {
                threshold.MinimumLevel = minimumLevel;
            }
        }

    }
}
