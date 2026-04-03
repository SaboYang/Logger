using System;
using System.Collections.Generic;

namespace Logger.Core
{
    public sealed class LoggerService : ILoggerService
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, ILoggerOutput> _loggers =
            new Dictionary<string, ILoggerOutput>(StringComparer.OrdinalIgnoreCase);

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
            string normalizedName = NormalizeName(name);

            lock (_syncRoot)
            {
                ILoggerOutput logger;
                if (!_loggers.TryGetValue(normalizedName, out logger))
                {
                    logger = _factory.CreateLogger(normalizedName) ?? new LogStore();
                    _loggers.Add(normalizedName, logger);
                }

                return logger;
            }
        }

        public bool TryGetLogger(string name, out ILoggerOutput logger)
        {
            string normalizedName = NormalizeName(name);

            lock (_syncRoot)
            {
                return _loggers.TryGetValue(normalizedName, out logger);
            }
        }

        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Default";
            }

            return name.Trim();
        }
    }
}
