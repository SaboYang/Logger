using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Logger.Core
{
    public sealed class LoggerService : ILoggerService, IDisposable
    {
        private const int CleanupInterval = 64;

        private readonly ConcurrentDictionary<string, LoggerCacheEntry> _loggers =
            new ConcurrentDictionary<string, LoggerCacheEntry>(StringComparer.OrdinalIgnoreCase);

        private readonly ILoggerFactory _factory;
        private int _accessCount;
        private int _disposed;

        public static LoggerService Shared { get; } = new LoggerService();

        public LoggerService()
            : this(new LogStoreLoggerFactory())
        {
        }

        public LoggerService(ILoggerFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public ILoggerFactory Factory
        {
            get { return _factory; }
        }

        public ILoggerOutput Default
        {
            get { return GetLogger("Default"); }
        }

        public ILoggerOutput GetLogger(string name)
        {
            ThrowIfDisposed();

            string normalizedName = LoggerPathUtility.NormalizeLoggerName(name);
            LoggerCacheEntry loggerEntry = _loggers.GetOrAdd(
                normalizedName,
                CreateLoggerEntry);
            ILoggerOutput logger = loggerEntry.GetOrCreate(
                () => _factory.CreateLogger(normalizedName) ?? new LogStore());

            ScheduleCleanup();
            return logger;
        }

        public bool TryGetLogger(string name, out ILoggerOutput logger)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                logger = null;
                return false;
            }

            string normalizedName = LoggerPathUtility.NormalizeLoggerName(name);
            LoggerCacheEntry loggerEntry;
            if (_loggers.TryGetValue(normalizedName, out loggerEntry))
            {
                if (loggerEntry.TryGetTarget(out logger))
                {
                    return true;
                }

                LoggerCacheEntry removedEntry;
                _loggers.TryRemove(normalizedName, out removedEntry);
            }

            logger = null;
            return false;
        }

        public bool ReleaseLogger(string name)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return false;
            }

            string normalizedName = LoggerPathUtility.NormalizeLoggerName(name);
            LoggerCacheEntry removedEntry;
            if (!_loggers.TryRemove(normalizedName, out removedEntry))
            {
                return false;
            }

            ILoggerOutput logger;
            if (removedEntry.TryGetTarget(out logger))
            {
                DisposeLogger(logger);
            }

            return true;
        }

        public int TrimReleasedLoggers()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                return 0;
            }

            int removedCount = 0;
            foreach (KeyValuePair<string, LoggerCacheEntry> loggerEntry in _loggers)
            {
                if (loggerEntry.Value.IsAlive())
                {
                    continue;
                }

                LoggerCacheEntry removedEntry;
                if (_loggers.TryRemove(loggerEntry.Key, out removedEntry))
                {
                    removedCount++;
                }
            }

            return removedCount;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            foreach (KeyValuePair<string, LoggerCacheEntry> loggerEntry in _loggers)
            {
                LoggerCacheEntry removedEntry;
                if (!_loggers.TryRemove(loggerEntry.Key, out removedEntry))
                {
                    continue;
                }

                ILoggerOutput logger;
                if (removedEntry.TryGetTarget(out logger))
                {
                    DisposeLogger(logger);
                }
            }
        }

        private LoggerCacheEntry CreateLoggerEntry(string normalizedName)
        {
            return new LoggerCacheEntry();
        }

        private void ScheduleCleanup()
        {
            if ((Interlocked.Increment(ref _accessCount) % CleanupInterval) != 0)
            {
                return;
            }

            TrimReleasedLoggers();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(LoggerService));
            }
        }

        private static void DisposeLogger(ILoggerOutput logger)
        {
            IDisposable disposable = logger as IDisposable;
            disposable?.Dispose();
        }

        private sealed class LoggerCacheEntry
        {
            private readonly object _syncRoot = new object();
            private WeakReference<ILoggerOutput> _target = new WeakReference<ILoggerOutput>(null);

            public ILoggerOutput GetOrCreate(Func<ILoggerOutput> factory)
            {
                ILoggerOutput logger;
                if (TryGetTarget(out logger))
                {
                    return logger;
                }

                lock (_syncRoot)
                {
                    if (TryGetTarget(out logger))
                    {
                        return logger;
                    }

                    logger = factory();
                    _target = new WeakReference<ILoggerOutput>(logger);
                    return logger;
                }
            }

            public bool TryGetTarget(out ILoggerOutput logger)
            {
                return _target.TryGetTarget(out logger) && logger != null;
            }

            public bool IsAlive()
            {
                ILoggerOutput logger;
                return TryGetTarget(out logger);
            }
        }
    }
}
