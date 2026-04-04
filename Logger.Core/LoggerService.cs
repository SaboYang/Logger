using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Logger.Core
{
    public sealed class LoggerService : ILoggerService
    {
        private readonly ConcurrentDictionary<string, Lazy<ILoggerOutput>> _loggers =
            new ConcurrentDictionary<string, Lazy<ILoggerOutput>>(StringComparer.OrdinalIgnoreCase);

        private readonly ILoggerFactory _factory;

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
            string normalizedName = LoggerPathUtility.NormalizeLoggerName(name);
            Lazy<ILoggerOutput> logger = _loggers.GetOrAdd(
                normalizedName,
                CreateLoggerEntry);
            return logger.Value;
        }

        public bool TryGetLogger(string name, out ILoggerOutput logger)
        {
            string normalizedName = LoggerPathUtility.NormalizeLoggerName(name);
            Lazy<ILoggerOutput> loggerEntry;
            if (_loggers.TryGetValue(normalizedName, out loggerEntry))
            {
                logger = loggerEntry.Value;
                return true;
            }

            logger = null;
            return false;
        }

        private Lazy<ILoggerOutput> CreateLoggerEntry(string normalizedName)
        {
            return new Lazy<ILoggerOutput>(
                () => _factory.CreateLogger(normalizedName) ?? new LogStore(),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}
