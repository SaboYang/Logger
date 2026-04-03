using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Logger.Core.Models;

namespace Logger.Core
{
    internal sealed class FileLogWriter : ILoggerOutput, ILogFileSource
    {
        private static readonly UTF8Encoding FileEncoding = new UTF8Encoding(false);

        private readonly ConcurrentQueue<string> _pendingLines = new ConcurrentQueue<string>();
        private readonly object _fileSyncRoot = new object();
        private readonly LoggerSessionInfo _sessionInfo;
        private readonly string _logFilePath;
        private int _flushWorkerRunning;
        private int _outputFaulted;
        private bool _headerWritten;

        public FileLogWriter(LoggerSessionInfo sessionInfo, string logRootDirectoryPath)
        {
            _sessionInfo = sessionInfo ?? throw new ArgumentNullException(nameof(sessionInfo));
            _logFilePath = BuildLogFilePath(_sessionInfo.LoggerName, logRootDirectoryPath, _sessionInfo.StartedAt, _sessionInfo.SessionId);
        }

        public bool IsFileOutputEnabled
        {
            get { return !string.IsNullOrEmpty(_logFilePath) && Volatile.Read(ref _outputFaulted) == 0; }
        }

        public string LogFilePath
        {
            get { return _logFilePath; }
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

            EnqueueEntries(new[] { new LogEntry(DateTime.Now, level, normalizedMessage) });
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            List<LogEntry> normalizedEntries = LogEntrySanitizer.NormalizeEntries(entries);
            if (normalizedEntries.Count == 0)
            {
                return;
            }

            EnqueueEntries(normalizedEntries);
        }

        private void EnqueueEntries(IEnumerable<LogEntry> entries)
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

                _pendingLines.Enqueue(FormatEntry(entry));
                hasPendingLines = true;
            }

            if (hasPendingLines)
            {
                ScheduleFlush();
            }
        }

        private void ScheduleFlush()
        {
            if (!IsFileOutputEnabled)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _flushWorkerRunning, 1, 0) != 0)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ => FlushPendingLines());
        }

        private void FlushPendingLines()
        {
            try
            {
                while (IsFileOutputEnabled)
                {
                    List<string> lines = DequeuePendingLines();
                    if (lines.Count == 0)
                    {
                        return;
                    }

                    try
                    {
                        WriteLines(lines);
                    }
                    catch
                    {
                        Interlocked.Exchange(ref _outputFaulted, 1);
                        return;
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _flushWorkerRunning, 0);

                if (!_pendingLines.IsEmpty && IsFileOutputEnabled)
                {
                    ScheduleFlush();
                }
            }
        }

        private List<string> DequeuePendingLines()
        {
            List<string> lines = new List<string>();
            string line;
            while (_pendingLines.TryDequeue(out line))
            {
                lines.Add(line);
            }

            return lines;
        }

        private void WriteLines(List<string> lines)
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
                    if (!_headerWritten)
                    {
                        WriteHeader(writer);
                        _headerWritten = true;
                    }

                    foreach (string line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }

        private void WriteHeader(StreamWriter writer)
        {
            writer.WriteLine("=== Logger Session Started ===");
            writer.WriteLine("Logger: " + _sessionInfo.LoggerName);
            writer.WriteLine("SessionId: " + _sessionInfo.SessionId.ToString("N"));
            writer.WriteLine("StartedAt: " + _sessionInfo.StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            writer.WriteLine("================================");
        }

        private static string FormatEntry(LogEntry entry)
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
            string normalizedValue = string.IsNullOrWhiteSpace(value) ? "Default" : value.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(normalizedValue.Length);

            foreach (char ch in normalizedValue)
            {
                builder.Append(Array.IndexOf(invalidChars, ch) >= 0 ? '_' : ch);
            }

            string sanitizedValue = builder.ToString().Trim();
            return string.IsNullOrWhiteSpace(sanitizedValue) ? "Default" : sanitizedValue;
        }
    }
}
