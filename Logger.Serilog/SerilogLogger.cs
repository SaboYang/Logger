using System;
using System.Collections.Generic;
using System.Globalization;
using Logger.Core;
using Logger.Core.Models;
using Serilog.Core;
using Serilog.Events;
using SerilogLoggerType = global::Serilog.ILogger;
using SerilogLogEventLevel = global::Serilog.Events.LogEventLevel;

namespace Logger.Serilog
{
    /// <summary>
    /// 将 <see cref="Logger.Core.ILoggerOutput"/> 的写入转发到 Serilog。
    /// </summary>
    public sealed class SerilogLogger : ILoggerOutput
    {
        private readonly string _sourceLoggerName;
        private readonly string _serilogLoggerName;
        private readonly SerilogLoggerOptions _options;
        private readonly SerilogLoggerType _baseLogger;
        private readonly object _syncRoot = new object();
        private LogLevel _minimumLevel = LogLevel.Trace;

        /// <summary>
        /// 初始化 Serilog 日志适配器。
        /// </summary>
        /// <param name="sourceLoggerName">原始 logger 名称。</param>
        /// <param name="serilogLoggerName">映射后的 Serilog logger 名称。</param>
        /// <param name="options">适配选项。</param>
        public SerilogLogger(string sourceLoggerName, string serilogLoggerName, SerilogLoggerOptions options)
            : this(sourceLoggerName, serilogLoggerName, options, global::Serilog.Log.Logger)
        {
        }

        internal SerilogLogger(
            string sourceLoggerName,
            string serilogLoggerName,
            SerilogLoggerOptions options,
            SerilogLoggerType logger)
        {
            _sourceLoggerName = NormalizeLoggerName(sourceLoggerName);
            _serilogLoggerName = NormalizeLoggerName(serilogLoggerName);
            _options = options ?? new SerilogLoggerOptions();
            SessionId = Guid.NewGuid();
            SessionStartedAt = DateTime.Now;
            _baseLogger = BuildLogger(
                logger,
                _sourceLoggerName,
                _serilogLoggerName,
                _options,
                SessionId,
                SessionStartedAt);
        }

        /// <summary>
        /// 获取原始 logger 名称。
        /// </summary>
        public string SourceLoggerName
        {
            get { return _sourceLoggerName; }
        }

        /// <summary>
        /// 获取映射后的 Serilog logger 名称。
        /// </summary>
        public string LoggerName
        {
            get { return _serilogLoggerName; }
        }

        /// <summary>
        /// 获取当前 logger 会话标识。
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// 获取当前 logger 会话开始时间。
        /// </summary>
        public DateTime SessionStartedAt { get; }

        /// <summary>
        /// 写入一条指定等级的日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        /// <param name="exception">可选异常。</param>
        public void Write(LogLevel level, string message, Exception exception = null)
        {
            if (!ShouldWrite(level))
            {
                return;
            }

            SerilogLogEventLevel serilogLevel = Map(level, _options);
            SerilogLoggerType logger = GetLoggerForLevel(level);
            if (!logger.IsEnabled(serilogLevel))
            {
                return;
            }

            logger.Write(serilogLevel, exception, EscapeMessageTemplate(message));
        }

        /// <inheritdoc />
        public void SetMinimumLevel(LogLevel minimumLevel)
        {
            lock (_syncRoot)
            {
                _minimumLevel = minimumLevel;
            }
        }

        /// <inheritdoc />
        public void Trace(string message)
        {
            Write(LogLevel.Trace, message);
        }

        /// <inheritdoc />
        public void Debug(string message)
        {
            Write(LogLevel.Debug, message);
        }

        /// <inheritdoc />
        public void Info(string message)
        {
            Write(LogLevel.Info, message);
        }

        /// <inheritdoc />
        public void Success(string message)
        {
            Write(LogLevel.Success, message);
        }

        /// <inheritdoc />
        public void Warning(string message)
        {
            Write(LogLevel.Warn, message);
        }

        /// <inheritdoc />
        public void Error(string message)
        {
            Write(LogLevel.Error, message);
        }

        /// <inheritdoc />
        public void Fatal(string message)
        {
            Write(LogLevel.Fatal, message);
        }

        /// <inheritdoc />
        public void AddLog(LogLevel level, string message)
        {
            Write(level, message);
        }

        /// <inheritdoc />
        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            if (entries == null)
            {
                return;
            }

            foreach (LogEntry entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                Write(entry.Level, entry.Message);
            }
        }

        private static SerilogLoggerType BuildLogger(
            SerilogLoggerType logger,
            string sourceLoggerName,
            string serilogLoggerName,
            SerilogLoggerOptions options,
            Guid sessionId,
            DateTime sessionStartedAt)
        {
            SerilogLoggerType configuredLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            configuredLogger = configuredLogger.ForContext(Constants.SourceContextPropertyName, serilogLoggerName);

            if (options.AttachLoggerNameProperty && !string.IsNullOrWhiteSpace(options.LoggerNamePropertyName))
            {
                configuredLogger = configuredLogger.ForContext(options.LoggerNamePropertyName, sourceLoggerName);
            }

            if (options.AttachSessionProperties)
            {
                if (!string.IsNullOrWhiteSpace(options.SessionIdPropertyName))
                {
                    configuredLogger = configuredLogger.ForContext(options.SessionIdPropertyName, sessionId);
                }

                if (!string.IsNullOrWhiteSpace(options.SessionStartedAtPropertyName))
                {
                    configuredLogger = configuredLogger.ForContext(options.SessionStartedAtPropertyName, sessionStartedAt);
                }
            }

            return configuredLogger;
        }

        private SerilogLoggerType GetLoggerForLevel(LogLevel level)
        {
            if (level != LogLevel.Success ||
                !_options.AttachSuccessProperty ||
                string.IsNullOrWhiteSpace(_options.SuccessPropertyName))
            {
                return _baseLogger;
            }

            return _baseLogger.ForContext(_options.SuccessPropertyName, true);
        }

        private bool ShouldWrite(LogLevel level)
        {
            lock (_syncRoot)
            {
                return level >= _minimumLevel;
            }
        }

        private static string EscapeMessageTemplate(string message)
        {
            string normalizedMessage = message ?? string.Empty;
            if (normalizedMessage.IndexOf('{') < 0 && normalizedMessage.IndexOf('}') < 0)
            {
                return normalizedMessage;
            }

            return normalizedMessage.Replace("{", "{{").Replace("}", "}}");
        }

        private static string NormalizeLoggerName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
        }

        private static SerilogLogEventLevel Map(LogLevel level, SerilogLoggerOptions options)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return SerilogLogEventLevel.Verbose;
                case LogLevel.Debug:
                    return SerilogLogEventLevel.Debug;
                case LogLevel.Info:
                    return SerilogLogEventLevel.Information;
                case LogLevel.Success:
                    return options != null ? options.SuccessTargetLevel : SerilogLogEventLevel.Information;
                case LogLevel.Warn:
                    return SerilogLogEventLevel.Warning;
                case LogLevel.Error:
                    return SerilogLogEventLevel.Error;
                case LogLevel.Fatal:
                    return SerilogLogEventLevel.Fatal;
                default:
                    return SerilogLogEventLevel.Information;
            }
        }
    }
}
