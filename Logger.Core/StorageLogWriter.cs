using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Logger.Core.Models;

namespace Logger.Core
{
    internal sealed class StorageLogWriter : ILoggerOutput, ILogRuntimeMetricsSource, IDisposable
    {
        private const int CapacityWaitMilliseconds = 5;

        private readonly LogStorageContext _context;
        private readonly ILogStorageBackend _storageBackend;
        private readonly FileLogWalSpool _spool;
        private readonly int _maxReplayBatchSize;
        private readonly int _maxSpoolWriteBatchEntries;
        private readonly int _maxPendingSpoolEntries;
        private readonly ConcurrentQueue<PendingSpoolBatch> _pendingSpoolBatches = new ConcurrentQueue<PendingSpoolBatch>();
        private readonly AutoResetEvent _spoolSignal = new AutoResetEvent(false);
        private readonly AutoResetEvent _spoolCapacitySignal = new AutoResetEvent(false);
        private int _spoolWorkerRunning;
        private int _pendingSpoolEntryCount;
        private int _flushWorkerRunning;
        private int _disposed;

        public StorageLogWriter(LogStorageContext context, ILogStorageBackend storageBackend)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _storageBackend = storageBackend ?? throw new ArgumentNullException(nameof(storageBackend));
            _spool = new FileLogWalSpool(_context);
            _maxReplayBatchSize = _context.MaxPendingStorageEntries;
            _maxSpoolWriteBatchEntries = Math.Max(_context.MaxPendingStorageEntries * 4, 4000);
            _maxPendingSpoolEntries = Math.Max(_maxSpoolWriteBatchEntries * 8, 32000);
        }

        public void SetMinimumLevel(LogLevel minimumLevel)
        {
        }

        public int BufferedSessionEntryCount
        {
            get { return 0; }
        }

        public int DroppedPendingEntryCount
        {
            get { return 0; }
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
            if (normalizedMessage == null
                || Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            AppendEntries(
                new[] { new LogEntry(DateTime.Now, level, normalizedMessage) },
                RequiresDurableSpool(level));
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            List<LogEntry> normalizedEntries = LogEntrySanitizer.NormalizeEntries(entries);
            if (normalizedEntries.Count == 0)
            {
                return;
            }

            AppendEntries(normalizedEntries, RequiresDurableSpool(normalizedEntries));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            DrainPendingSpoolEntriesToWal();
            _spoolSignal.Set();
            _spoolCapacitySignal.Set();
            _spoolSignal.Dispose();
            _spoolCapacitySignal.Dispose();
            _spool.Dispose();
        }

        private void AppendEntries(IReadOnlyList<LogEntry> entries, bool requireDurableFlush)
        {
            if (entries == null || entries.Count == 0 || Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            WaitForSpoolCapacity(entries.Count);
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            _pendingSpoolBatches.Enqueue(new PendingSpoolBatch(entries, requireDurableFlush));
            Interlocked.Add(ref _pendingSpoolEntryCount, entries.Count);
            ScheduleSpoolWrite();
        }

        private void ScheduleSpoolWrite()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            _spoolSignal.Set();

            if (Interlocked.CompareExchange(ref _spoolWorkerRunning, 1, 0) != 0)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ => FlushPendingSpoolEntries());
        }

        private void ScheduleFlush()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _flushWorkerRunning, 1, 0) != 0)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ => FlushPendingEntries());
        }

        private void FlushPendingSpoolEntries()
        {
            try
            {
                while (Volatile.Read(ref _disposed) == 0)
                {
                    PendingSpoolWrite write = DequeuePendingSpoolEntries();
                    if (write.Entries.Count == 0)
                    {
                        _spoolSignal.WaitOne(50);

                        if (Volatile.Read(ref _pendingSpoolEntryCount) == 0)
                        {
                            return;
                        }

                        continue;
                    }

                    _spool.AppendEntries(write.Entries, write.RequireDurableFlush);
                    ScheduleFlush();
                }
            }
            finally
            {
                Interlocked.Exchange(ref _spoolWorkerRunning, 0);

                if (Volatile.Read(ref _disposed) == 0
                    && Volatile.Read(ref _pendingSpoolEntryCount) > 0)
                {
                    ScheduleSpoolWrite();
                }
            }
        }

        private void FlushPendingEntries()
        {
            try
            {
                while (Volatile.Read(ref _disposed) == 0)
                {
                    List<LogEntry> entries;
                    long commitOffset;
                    if (!_spool.TryReadNextBatch(_maxReplayBatchSize, out entries, out commitOffset))
                    {
                        return;
                    }

                    if (entries.Count == 0)
                    {
                        _spool.MarkCommitted(commitOffset);
                        continue;
                    }

                    try
                    {
                        _storageBackend.WriteBatch(entries, _context);
                        _spool.MarkCommitted(commitOffset);
                    }
                    catch
                    {
                        if (Volatile.Read(ref _disposed) != 0)
                        {
                            return;
                        }

                        Thread.Sleep(500);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _flushWorkerRunning, 0);

                if (Volatile.Read(ref _disposed) == 0 && _spool.HasPendingEntries)
                {
                    ScheduleFlush();
                }
            }
        }

        private void WaitForSpoolCapacity(int incomingCount)
        {
            while (Volatile.Read(ref _disposed) == 0)
            {
                int pendingCount = Volatile.Read(ref _pendingSpoolEntryCount);
                if (pendingCount + incomingCount <= _maxPendingSpoolEntries)
                {
                    return;
                }

                ScheduleSpoolWrite();
                _spoolCapacitySignal.WaitOne(CapacityWaitMilliseconds);
            }
        }

        private PendingSpoolWrite DequeuePendingSpoolEntries()
        {
            List<LogEntry> entries = new List<LogEntry>();
            bool requireDurableFlush = false;
            PendingSpoolBatch batch;

            while (_pendingSpoolBatches.TryDequeue(out batch))
            {
                int batchCount = batch != null && batch.Entries != null ? batch.Entries.Count : 0;
                if (batchCount > 0)
                {
                    if (batch.RequireDurableFlush)
                    {
                        requireDurableFlush = true;
                    }

                    for (int index = 0; index < batchCount; index++)
                    {
                        LogEntry entry = batch.Entries[index];
                        if (entry != null)
                        {
                            entries.Add(entry);
                        }
                    }
                }

                Interlocked.Add(ref _pendingSpoolEntryCount, -batchCount);
                _spoolCapacitySignal.Set();

                if (entries.Count >= _maxSpoolWriteBatchEntries || requireDurableFlush)
                {
                    break;
                }
            }

            return new PendingSpoolWrite(entries, requireDurableFlush);
        }

        private void DrainPendingSpoolEntriesToWal()
        {
            while (Volatile.Read(ref _pendingSpoolEntryCount) > 0)
            {
                PendingSpoolWrite write = DequeuePendingSpoolEntries();
                if (write.Entries.Count == 0)
                {
                    break;
                }

                _spool.AppendEntries(write.Entries, true);
            }
        }

        private static bool RequiresDurableSpool(LogLevel level)
        {
            return level == LogLevel.Error || level == LogLevel.Fatal;
        }

        private static bool RequiresDurableSpool(IReadOnlyList<LogEntry> entries)
        {
            if (entries == null)
            {
                return false;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                LogEntry entry = entries[index];
                if (entry != null && RequiresDurableSpool(entry.Level))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class PendingSpoolBatch
        {
            public PendingSpoolBatch(IReadOnlyList<LogEntry> entries, bool requireDurableFlush)
            {
                Entries = entries;
                RequireDurableFlush = requireDurableFlush;
            }

            public IReadOnlyList<LogEntry> Entries { get; }

            public bool RequireDurableFlush { get; }
        }

        private sealed class PendingSpoolWrite
        {
            public PendingSpoolWrite(List<LogEntry> entries, bool requireDurableFlush)
            {
                Entries = entries ?? new List<LogEntry>();
                RequireDurableFlush = requireDurableFlush;
            }

            public List<LogEntry> Entries { get; }

            public bool RequireDurableFlush { get; }
        }
    }
}
