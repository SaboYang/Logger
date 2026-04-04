using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Logger.Core.Models;

namespace Logger.Core
{
    internal sealed class StorageLogWriter : ILoggerOutput
    {
        private readonly ConcurrentQueue<LogEntry> _pendingEntries = new ConcurrentQueue<LogEntry>();
        private readonly LogStorageContext _context;
        private readonly ILogStorageBackend _storageBackend;
        private int _flushWorkerRunning;
        private int _outputFaulted;

        public StorageLogWriter(LogStorageContext context, ILogStorageBackend storageBackend)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _storageBackend = storageBackend ?? throw new ArgumentNullException(nameof(storageBackend));
        }

        public void SetMinimumLevel(LogLevel minimumLevel)
        {
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
            if (normalizedMessage == null
                || Volatile.Read(ref _outputFaulted) != 0)
            {
                return;
            }

            EnqueueEntries(new[] { new LogEntry(DateTime.Now, level, normalizedMessage) });
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            if (Volatile.Read(ref _outputFaulted) != 0)
            {
                return;
            }

            List<LogEntry> normalizedEntries = LogEntrySanitizer.NormalizeEntries(entries);
            if (normalizedEntries.Count == 0)
            {
                return;
            }

            EnqueueEntries(normalizedEntries);
        }

        private void EnqueueEntries(IEnumerable<LogEntry> entries)
        {
            bool hasPendingEntries = false;
            foreach (LogEntry entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                _pendingEntries.Enqueue(entry);
                hasPendingEntries = true;
            }

            if (hasPendingEntries)
            {
                ScheduleFlush();
            }
        }

        private void ScheduleFlush()
        {
            if (Volatile.Read(ref _outputFaulted) != 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _flushWorkerRunning, 1, 0) != 0)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ => FlushPendingEntries());
        }

        private void FlushPendingEntries()
        {
            try
            {
                while (Volatile.Read(ref _outputFaulted) == 0)
                {
                    List<LogEntry> entries = DequeuePendingEntries();
                    if (entries.Count == 0)
                    {
                        return;
                    }

                    try
                    {
                        _storageBackend.WriteBatch(entries, _context);
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

                if (!_pendingEntries.IsEmpty && Volatile.Read(ref _outputFaulted) == 0)
                {
                    ScheduleFlush();
                }
            }
        }

        private List<LogEntry> DequeuePendingEntries()
        {
            List<LogEntry> entries = new List<LogEntry>();
            LogEntry entry;
            while (_pendingEntries.TryDequeue(out entry))
            {
                entries.Add(entry);
            }

            return entries;
        }
    }
}
