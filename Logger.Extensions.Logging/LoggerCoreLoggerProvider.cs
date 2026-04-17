using System;
using System.Collections.Concurrent;
using Logger.Core;
using Microsoft.Extensions.Logging;

namespace Logger.Extensions.Logging
{
    internal sealed class LoggerCoreLoggerProvider : ILoggerProvider
    {
        private readonly ILoggerService _loggerService;
        private readonly ConcurrentDictionary<string, ILogger> _loggers =
            new ConcurrentDictionary<string, ILogger>(StringComparer.Ordinal);

        public LoggerCoreLoggerProvider(ILoggerService loggerService)
        {
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        public ILogger CreateLogger(string categoryName)
        {
            string normalizedCategory = string.IsNullOrWhiteSpace(categoryName) ? "Default" : categoryName;
            return _loggers.GetOrAdd(
                normalizedCategory,
                key => new LoggerCoreLogger(_loggerService.GetLogger(key)));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
