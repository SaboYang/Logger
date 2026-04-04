using System;
using Logger.Core.Models;

namespace Logger.Core
{
    public sealed class LogStoreLoggerFactory : ILoggerFactory
    {
        private readonly ILogStorageBackendFactory _storageBackendFactory;
        private readonly LogLevel _minimumLevel;
        private readonly int _maxBufferedSessionEntries;
        private readonly int _maxPendingStorageEntries;

        public LogStoreLoggerFactory(
            string logRootDirectoryPath = null,
            LogLevel minimumLevel = LogLevel.Trace,
            LogFileRollingMode rollingMode = LogFileRollingMode.Day,
            int maxBufferedSessionEntries = 5000,
            int maxPendingStorageEntries = 5000)
            : this(
                  new TextFileLogStorageBackendFactory(logRootDirectoryPath, rollingMode),
                  minimumLevel,
                  maxBufferedSessionEntries,
                  maxPendingStorageEntries)
        {
        }

        public LogStoreLoggerFactory(
            ILogStorageBackendFactory storageBackendFactory,
            LogLevel minimumLevel = LogLevel.Trace,
            int maxBufferedSessionEntries = 5000,
            int maxPendingStorageEntries = 5000)
        {
            _storageBackendFactory = storageBackendFactory;
            _minimumLevel = minimumLevel;
            _maxBufferedSessionEntries = Math.Max(1, maxBufferedSessionEntries);
            _maxPendingStorageEntries = Math.Max(1, maxPendingStorageEntries);
        }

        public ILoggerOutput CreateLogger(string name)
        {
            string loggerName = string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
            LogStorageContext storageContext = new LogStorageContext(
                loggerName,
                Guid.NewGuid(),
                DateTime.Now,
                _minimumLevel,
                _maxBufferedSessionEntries,
                _maxPendingStorageEntries);
            LogStore logStore = new LogStore();
            LogSessionBuffer sessionBuffer = new LogSessionBuffer(storageContext);
            ILogStorageBackend storageBackend = _storageBackendFactory != null
                ? _storageBackendFactory.CreateBackend(storageContext)
                : null;
            StorageLogWriter storageWriter = storageBackend != null
                ? new StorageLogWriter(storageContext, storageBackend)
                : null;

            if (storageWriter == null)
            {
                CompositeLogger loggerWithoutStorage = new CompositeLogger(logStore, sessionBuffer, null, logStore, sessionBuffer);
                loggerWithoutStorage.MinimumLevel = _minimumLevel;
                return loggerWithoutStorage;
            }

            CompositeLogger logger = new CompositeLogger(
                logStore,
                sessionBuffer,
                storageBackend as ILogFileSource,
                logStore,
                sessionBuffer,
                storageWriter);
            logger.MinimumLevel = _minimumLevel;
            return logger;
        }
    }
}
