using System;
using Logger.Core.Models;

namespace Logger.Core
{
    public sealed class LogStoreLoggerFactory : ILoggerFactory
    {
        private readonly ILogStorageBackendFactory _storageBackendFactory;
        private readonly LogLevel _minimumLevel;

        public LogStoreLoggerFactory(
            string logRootDirectoryPath = null,
            LogLevel minimumLevel = LogLevel.Trace,
            LogFileRollingMode rollingMode = LogFileRollingMode.Day)
            : this(new TextFileLogStorageBackendFactory(logRootDirectoryPath, rollingMode), minimumLevel)
        {
        }

        public LogStoreLoggerFactory(ILogStorageBackendFactory storageBackendFactory, LogLevel minimumLevel = LogLevel.Trace)
        {
            _storageBackendFactory = storageBackendFactory;
            _minimumLevel = minimumLevel;
        }

        public ILoggerOutput CreateLogger(string name)
        {
            string loggerName = string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
            LogStorageContext storageContext = new LogStorageContext(loggerName, Guid.NewGuid(), DateTime.Now, _minimumLevel);
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
