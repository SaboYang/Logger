using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Logger.Core.Models;

namespace Logger.Core
{
    internal sealed class StorageLogWriter : ILoggerOutput, ILogRuntimeMetricsSource, IDisposable
    {
        private readonly ConcurrentQueue<LogEntry> _pendingEntries = new ConcurrentQueue<LogEntry>();
        private readonly LogStorageContext _context;
        private readonly ILogStorageBackend _storageBackend;
        private readonly int _maxPendingEntries;
        private readonly SemaphoreSlim _pendingCapacitySignal;
        private int _flushWorkerRunning;
        private int _outputFaulted;
        private int _pendingEntryCount;
        private int _disposed;

        public StorageLogWriter(LogStorageContext context, ILogStorageBackend storageBackend)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _storageBackend = storageBackend ?? throw new ArgumentNullException(nameof(storageBackend));
            _maxPendingEntries = _context.MaxPendingStorageEntries;
            _pendingCapacitySignal = new SemaphoreSlim(_maxPendingEntries, _maxPendingEntries);
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
                || Volatile.Read(ref _disposed) != 0
                || Volatile.Read(ref _outputFaulted) != 0)
            {
                return;
            }

            EnqueueEntries(new[] { new LogEntry(DateTime.Now, level, normalizedMessage) });
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            if (Volatile.Read(ref _disposed) != 0
                || Volatile.Read(ref _outputFaulted) != 0)
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

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            Interlocked.Exchange(ref _outputFaulted, 1);
            ClearPendingEntries();
            _pendingCapacitySignal.Dispose();
        }

        private void EnqueueEntries(IEnumerable<LogEntry> entries)
        {
            bool hasPendingEntries = false;
            foreach (LogEntry entry in entries)
            {
                if (entry == null
                    || Volatile.Read(ref _disposed) != 0)
                {
                    continue;
                }

                if (!WaitForPendingCapacity())
                {
                    return;
                }

                _pendingEntries.Enqueue(entry);
                Interlocked.Increment(ref _pendingEntryCount);
                hasPendingEntries = true;
            }

            if (hasPendingEntries)
            {
                ScheduleFlush();
            }
        }

        private void ScheduleFlush()
        {
            if (Volatile.Read(ref _disposed) != 0
                || Volatile.Read(ref _outputFaulted) != 0)
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

        private bool WaitForPendingCapacity()
        {
            while (Volatile.Read(ref _disposed) == 0
                && Volatile.Read(ref _outputFaulted) == 0)
            {
                ScheduleFlush();

                try
                {
                    if (_pendingCapacitySignal.Wait(50))
                    {
                        return true;
                    }
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }

            return false;
        }

        private List<LogEntry> DequeuePendingEntries()
        {
            List<LogEntry> entries = new List<LogEntry>();
            LogEntry entry;
            while (_pendingEntries.TryDequeue(out entry))
            {
                entries.Add(entry);
                Interlocked.Decrement(ref _pendingEntryCount);
                ReleasePendingCapacity();
            }

            return entries;
        }

        private void ClearPendingEntries()
        {
            LogEntry entry;
            while (_pendingEntries.TryDequeue(out entry))
            {
                Interlocked.Decrement(ref _pendingEntryCount);
                ReleasePendingCapacity();
            }
        }

        private void ReleasePendingCapacity()
        {
            try
            {
                _pendingCapacitySignal.Release();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SemaphoreFullException)
            {
            }
        }
    }
}
