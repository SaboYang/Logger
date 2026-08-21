using System;
using Logger.Core;
using Logger.Core.Models;
using Serilog.Core;
using Serilog.Events;

namespace Logger.Serilog
{
    /// <summary>
    /// 将 Serilog 日志事件转发到指定的 <see cref="ILoggerOutput"/>。
    /// </summary>
    public sealed class LoggerOutputSink : ILogEventSink
    {
        private readonly ILoggerOutput _logger;

        /// <summary>
        /// 初始化日志转发 Sink。
        /// </summary>
        /// <param name="logger">接收转发日志的输出实例。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="logger"/> 为空时抛出。</exception>
        public LoggerOutputSink(ILoggerOutput logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 接收一条 Serilog 日志事件并转发到 <see cref="ILoggerOutput"/>。
        /// </summary>
        /// <param name="logEvent">Serilog 日志事件。</param>
        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                return;
            }

            string message = logEvent.RenderMessage();
            if (logEvent.Exception != null)
            {
                message += Environment.NewLine + logEvent.Exception;
            }

            _logger.AddLog(Map(logEvent.Level), message);
        }

        private static LogLevel Map(LogEventLevel level)
        {
            switch (level)
            {
                case LogEventLevel.Verbose:
                    return LogLevel.Trace;
                case LogEventLevel.Debug:
                    return LogLevel.Debug;
                case LogEventLevel.Information:
                    return LogLevel.Info;
                case LogEventLevel.Warning:
                    return LogLevel.Warn;
                case LogEventLevel.Error:
                    return LogLevel.Error;
                case LogEventLevel.Fatal:
                    return LogLevel.Fatal;
                default:
                    return LogLevel.Info;
            }
        }
    }
}
