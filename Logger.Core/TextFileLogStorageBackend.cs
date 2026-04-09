using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Logger.Core.Models;

namespace Logger.Core
{
    public sealed class TextFileLogStorageBackend : ILogStorageBackend, ILogFileSource, IDisposable
    {
        private const int StreamBufferSize = 64 * 1024;
        private static readonly UTF8Encoding FileEncoding = new UTF8Encoding(false);

        private readonly object _syncRoot = new object();
        private readonly FileLogPathProvider _pathProvider;
        private readonly HashSet<string> _headerWrittenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, WriterState> _writerStates =
            new Dictionary<string, WriterState>(StringComparer.OrdinalIgnoreCase);
        private DateTime _lastRetentionCleanupDate = DateTime.MinValue;
        private bool _disposed;

        public TextFileLogStorageBackend(string logFilePath)
            : this(FileLogPathProvider.CreateFixed(logFilePath))
        {
        }

        internal TextFileLogStorageBackend(FileLogPathProvider pathProvider)
        {
            _pathProvider = pathProvider;
        }

        public bool IsFileOutputEnabled
        {
            get { return !string.IsNullOrWhiteSpace(LogFilePath); }
        }

        public string LogFilePath
        {
            get { return _pathProvider != null ? _pathProvider.CurrentPath : null; }
        }

        public void WriteBatch(IReadOnlyList<LogEntry> entries, LogStorageContext context)
        {
            if (!IsFileOutputEnabled || entries == null || entries.Count == 0)
            {
                return;
            }

            Dictionary<string, List<LogEntry>> entryGroups = GroupEntriesByFilePath(entries);
            if (entryGroups.Count == 0)
            {
                return;
            }

            lock (_syncRoot)
            {
                ThrowIfDisposed();
                EnsureRetentionCleanup();

                foreach (KeyValuePair<string, List<LogEntry>> entryGroup in entryGroups)
                {
                    WriterState writerState = GetOrCreateWriterState(entryGroup.Key, context);
                    if (writerState == null)
                    {
                        continue;
                    }

                    WriteEntries(writerState, entryGroup.Value);
                }
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

                foreach (WriterState writerState in _writerStates.Values)
                {
                    writerState.Dispose();
                }

                _writerStates.Clear();
                _headerWrittenPaths.Clear();
            }
        }

        private Dictionary<string, List<LogEntry>> GroupEntriesByFilePath(IReadOnlyList<LogEntry> entries)
        {
            Dictionary<string, List<LogEntry>> groups = new Dictionary<string, List<LogEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (LogEntry entry in entries)
            {
                string logFilePath = _pathProvider != null
                    ? _pathProvider.GetPath(entry != null ? entry.Timestamp : DateTime.Now)
                    : null;

                if (string.IsNullOrWhiteSpace(logFilePath))
                {
                    continue;
                }

                List<LogEntry> group;
                if (!groups.TryGetValue(logFilePath, out group))
                {
                    group = new List<LogEntry>();
                    groups[logFilePath] = group;
                }

                group.Add(entry);
            }

            return groups;
        }

        private WriterState GetOrCreateWriterState(string logFilePath, LogStorageContext context)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                return null;
            }

            WriterState writerState;
            if (_writerStates.TryGetValue(logFilePath, out writerState))
            {
                return writerState;
            }

            string directoryPath = Path.GetDirectoryName(logFilePath);
            if (string.IsNullOrEmpty(directoryPath))
            {
                return null;
            }

            Directory.CreateDirectory(directoryPath);

            bool shouldWriteHeader = !_headerWrittenPaths.Contains(logFilePath)
                && (!File.Exists(logFilePath) || new FileInfo(logFilePath).Length == 0);

            FileStream stream = new FileStream(
                logFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite,
                StreamBufferSize,
                FileOptions.SequentialScan);
            StreamWriter writer = new StreamWriter(stream, FileEncoding, StreamBufferSize)
            {
                NewLine = Environment.NewLine
            };
            writerState = new WriterState(writer, stream);
            _writerStates[logFilePath] = writerState;

            if (shouldWriteHeader)
            {
                WriteHeader(writer, context);
                writer.Flush();
            }

            _headerWrittenPaths.Add(logFilePath);
            return writerState;
        }

        private void EnsureRetentionCleanup()
        {
            if (_pathProvider == null || !_pathProvider.ShouldCleanupExpiredDailyLogs)
            {
                return;
            }

            DateTime today = DateTime.Today;
            if (_lastRetentionCleanupDate == today)
            {
                return;
            }

            LogFileRetentionCleaner.CleanupExpiredDailyLogFiles(_pathProvider, DateTime.Now);
            _lastRetentionCleanupDate = today;
        }

        private static void WriteEntries(WriterState writerState, IReadOnlyList<LogEntry> entries)
        {
            if (writerState == null || entries == null || entries.Count == 0)
            {
                return;
            }

            StringBuilder builder = new StringBuilder(entries.Count * 96);
            for (int index = 0; index < entries.Count; index++)
            {
                AppendEntry(builder, entries[index]);
            }

            if (builder.Length == 0)
            {
                return;
            }

            writerState.Writer.Write(builder.ToString());
            writerState.Writer.Flush();
        }

        private static void WriteHeader(TextWriter writer, LogStorageContext context)
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

        private static void AppendEntry(StringBuilder builder, LogEntry entry)
        {
            string message = entry?.Message ?? string.Empty;
            message = message.Replace("\r\n", "\n").Replace('\r', '\n');
            message = message.Replace("\n", Environment.NewLine + "    ");

            builder.Append((entry != null ? entry.Timestamp : DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss.fff"));
            builder.Append(" [");
            builder.Append(entry != null ? entry.LevelText : "INFO");
            builder.Append("] ");
            builder.Append(message);
            builder.AppendLine();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        private sealed class WriterState : IDisposable
        {
            public WriterState(StreamWriter writer, FileStream stream)
            {
                Writer = writer;
                Stream = stream;
            }

            public StreamWriter Writer { get; }

            public FileStream Stream { get; }

            public void Dispose()
            {
                Writer.Dispose();
                Stream.Dispose();
            }
        }
    }
}
