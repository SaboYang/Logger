using System;
using System.Text;
using Logger.Core;
using Microsoft.Extensions.Logging;
using CoreLogLevel = Logger.Core.Models.LogLevel;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Logger.Extensions.Logging
{
    internal sealed class LoggerCoreLogger : ILogger
    {
        private readonly ILoggerOutput _logger;

        public LoggerCoreLogger(ILoggerOutput logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(MsLogLevel logLevel)
        {
            if (logLevel == MsLogLevel.None)
            {
                return false;
            }

            ILogLevelThreshold threshold = _logger as ILogLevelThreshold;
            if (threshold == null)
            {
                return true;
            }

            return Map(logLevel) >= threshold.MinimumLevel;
        }

        public void Log<TState>(
            MsLogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = formatter(state, exception);
            if (exception != null)
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    message = exception.ToString();
                }
                else
                {
                    StringBuilder builder = new StringBuilder(message);
                    builder.AppendLine();
                    builder.Append(exception);
                    message = builder.ToString();
                }
            }

            CoreLogLevel coreLevel = Map(logLevel);
            switch (coreLevel)
            {
                case CoreLogLevel.Trace:
                    _logger.Trace(message);
                    return;
                case CoreLogLevel.Debug:
                    _logger.Debug(message);
                    return;
                case CoreLogLevel.Info:
                    _logger.Info(message);
                    return;
                case CoreLogLevel.Success:
                    _logger.Success(message);
                    return;
                case CoreLogLevel.Warn:
                    _logger.Warning(message);
                    return;
                case CoreLogLevel.Error:
                    _logger.Error(message);
                    return;
                case CoreLogLevel.Fatal:
                    _logger.Fatal(message);
                    return;
                default:
                    return;
            }
        }

        private static CoreLogLevel Map(MsLogLevel logLevel)
        {
            switch (logLevel)
            {
                case MsLogLevel.Trace:
                    return CoreLogLevel.Trace;
                case MsLogLevel.Debug:
                    return CoreLogLevel.Debug;
                case MsLogLevel.Information:
                    return CoreLogLevel.Info;
                case MsLogLevel.Warning:
                    return CoreLogLevel.Warn;
                case MsLogLevel.Error:
                    return CoreLogLevel.Error;
                case MsLogLevel.Critical:
                    return CoreLogLevel.Fatal;
                case MsLogLevel.None:
                default:
                    return CoreLogLevel.Info;
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();

            public void Dispose()
            {
            }
        }
    }
}
