using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using Logger.Core.Models;

namespace Logger.Core
{
    public class LogStore : ILoggerOutput, ILogViewSource, ILogSessionSource, ILogFileSource
    {
        private static readonly UTF8Encoding FileEncoding = new UTF8Encoding(false);

        private readonly BulkObservableCollection<LogEntry> _entries = new BulkObservableCollection<LogEntry>();
        private readonly List<LogEntry> _sessionEntries = new List<LogEntry>();
        private readonly ConcurrentQueue<string> _pendingFileLines = new ConcurrentQueue<string>();
        private readonly object _syncRoot = new object();
        private readonly object _fileSyncRoot = new object();
        private readonly Guid _sessionId = Guid.NewGuid();
        private readonly DateTime _sessionStartedAt = DateTime.Now;
        private readonly string _loggerName;
        private readonly string _logFilePath;
        private int _maxEntries = 500;
        private int _fileFlushWorkerRunning;
        private int _fileOutputFaulted;
        private bool _sessionHeaderWritten;

        public LogStore()
            : this(null, null, false)
        {
        }

        public LogStore(string loggerName, string logRootDirectoryPath)
            : this(loggerName, logRootDirectoryPath, true)
        {
        }

        private LogStore(string loggerName, string logRootDirectoryPath, bool enableFileOutput)
        {
            _loggerName = NormalizeLoggerName(loggerName);
            if (enableFileOutput)
            {
                _logFilePath = BuildLogFilePath(_loggerName, logRootDirectoryPath, _sessionStartedAt, _sessionId);
            }
        }

        public object SyncRoot
        {
            get { return _syncRoot; }
        }

        public ObservableCollection<LogEntry> Entries
        {
            get { return _entries; }
        }

        public Guid SessionId
        {
            get { return _sessionId; }
        }

        public DateTime SessionStartedAt
        {
            get { return _sessionStartedAt; }
        }

        public int SessionEntryCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _sessionEntries.Count;
                }
            }
        }

        public bool IsFileOutputEnabled
        {
            get { return !string.IsNullOrEmpty(_logFilePath) && Volatile.Read(ref _fileOutputFaulted) == 0; }
        }

        public string LogFilePath
        {
            get { return _logFilePath; }
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

            LogEntry entry = new LogEntry(DateTime.Now, level, normalizedMessage);

            lock (_syncRoot)
            {
                _entries.Add(entry);
                _sessionEntries.Add(entry);
                TrimEntries();
            }

            EnqueueFileEntries(new[] { entry });
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
                _sessionEntries.AddRange(normalizedEntries);
                TrimEntries();
            }

            EnqueueFileEntries(normalizedEntries);
        }

        public IReadOnlyList<LogEntry> GetSessionEntriesSnapshot()
        {
            lock (_syncRoot)
            {
                return _sessionEntries.ToArray();
            }
        }

        private void EnqueueFileEntries(IEnumerable<LogEntry> entries)
        {
            if (!IsFileOutputEnabled || entries == null)
            {
                return;
            }

            bool hasPendingLines = false;
            foreach (LogEntry entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                _pendingFileLines.Enqueue(FormatEntryForFile(entry));
                hasPendingLines = true;
            }

            if (hasPendingLines)
            {
                ScheduleFileFlush();
            }
        }

        private void ScheduleFileFlush()
        {
            if (!IsFileOutputEnabled)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _fileFlushWorkerRunning, 1, 0) != 0)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ => FlushPendingFileLines());
        }

        private void FlushPendingFileLines()
        {
            try
            {
                while (IsFileOutputEnabled)
                {
                    List<string> lines = DequeuePendingFileLines();
                    if (lines.Count == 0)
                    {
                        return;
                    }

                    try
                    {
                        WriteFileLines(lines);
                    }
                    catch
                    {
                        Interlocked.Exchange(ref _fileOutputFaulted, 1);
                        return;
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _fileFlushWorkerRunning, 0);

                if (!_pendingFileLines.IsEmpty && IsFileOutputEnabled)
                {
                    ScheduleFileFlush();
                }
            }
        }

        private List<string> DequeuePendingFileLines()
        {
            List<string> lines = new List<string>();
            string line;
            while (_pendingFileLines.TryDequeue(out line))
            {
                lines.Add(line);
            }

            return lines;
        }

        private void WriteFileLines(List<string> lines)
        {
            string directoryPath = Path.GetDirectoryName(_logFilePath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                return;
            }

            Directory.CreateDirectory(directoryPath);

            lock (_fileSyncRoot)
            {
                using (FileStream stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (StreamWriter writer = new StreamWriter(stream, FileEncoding))
                {
                    if (!_sessionHeaderWritten)
                    {
                        WriteSessionHeader(writer);
                        _sessionHeaderWritten = true;
                    }

                    foreach (string line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }

        private void WriteSessionHeader(StreamWriter writer)
        {
            writer.WriteLine("=== Logger Session Started ===");
            writer.WriteLine("Logger: " + _loggerName);
            writer.WriteLine("SessionId: " + _sessionId.ToString("N"));
            writer.WriteLine("StartedAt: " + _sessionStartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            writer.WriteLine("================================");
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

        private static string FormatEntryForFile(LogEntry entry)
        {
            string message = entry.Message ?? string.Empty;
            message = message.Replace("\r\n", "\n").Replace('\r', '\n');
            message = message.Replace("\n", Environment.NewLine + "    ");

            return string.Format(
                "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}",
                entry.Timestamp,
                entry.LevelText,
                message);
        }

        private static string NormalizeLoggerName(string loggerName)
        {
            if (string.IsNullOrWhiteSpace(loggerName))
            {
                return "Default";
            }

            return loggerName.Trim();
        }

        private static string BuildLogFilePath(
            string loggerName,
            string logRootDirectoryPath,
            DateTime sessionStartedAt,
            Guid sessionId)
        {
            string rootDirectoryPath = string.IsNullOrWhiteSpace(logRootDirectoryPath)
                ? Path.Combine(AppContext.BaseDirectory, "Logs")
                : logRootDirectoryPath.Trim();

            string loggerDirectoryName = SanitizePathSegment(loggerName);
            string dateDirectoryName = sessionStartedAt.ToString("yyyyMMdd");
            string fileName = string.Format(
                "{0}_{1}.log",
                sessionStartedAt.ToString("HHmmss_fff"),
                sessionId.ToString("N").Substring(0, 8));

            return Path.Combine(rootDirectoryPath, loggerDirectoryName, dateDirectoryName, fileName);
        }

        private static string SanitizePathSegment(string value)
        {
            string normalizedValue = NormalizeLoggerName(value);
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(normalizedValue.Length);

            foreach (char ch in normalizedValue)
            {
                builder.Append(Array.IndexOf(invalidChars, ch) >= 0 ? '_' : ch);
            }

            string sanitizedValue = builder.ToString().Trim();
            return string.IsNullOrWhiteSpace(sanitizedValue) ? "Default" : sanitizedValue;
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
