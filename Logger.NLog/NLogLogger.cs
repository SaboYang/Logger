using System;
using System.Globalization;
using System.Text;
using Logger.Core;
using Logger.Core.Models;
using NLog;
using CoreLogLevel = Logger.Core.Models.LogLevel;
using NLogLogLevel = NLog.LogLevel;
using NLogLoggerType = NLog.Logger;

namespace Logger.NLog
{
    /// <summary>
    /// 将 Logger.Core 的输出转发到 NLog。
    /// </summary>
    public sealed class NLogLogger : ILoggerOutput
    {
        private readonly string _sourceLoggerName;
        private readonly string _nlogLoggerName;
        private readonly NLogLoggerOptions _options;
        private readonly NLogLoggerType _logger;
        private readonly object _syncRoot = new object();
        private CoreLogLevel _minimumLevel = CoreLogLevel.Trace;

        /// <summary>
        /// 初始化 NLog 日志适配器。
        /// </summary>
        /// <param name="sourceLoggerName">原始 logger 名称。</param>
        /// <param name="nlogLoggerName">映射后的 NLog logger 名称。</param>
        /// <param name="options">适配选项。</param>
        public NLogLogger(string sourceLoggerName, string nlogLoggerName, NLogLoggerOptions options)
            : this(
                sourceLoggerName,
                nlogLoggerName,
                options,
                global::NLog.LogManager.GetLogger(string.IsNullOrWhiteSpace(nlogLoggerName) ? "Default" : nlogLoggerName))
        {
        }

        internal NLogLogger(string sourceLoggerName, string nlogLoggerName, NLogLoggerOptions options, NLogLoggerType logger)
        {
            _sourceLoggerName = string.IsNullOrWhiteSpace(sourceLoggerName) ? "Default" : sourceLoggerName.Trim();
            _nlogLoggerName = string.IsNullOrWhiteSpace(nlogLoggerName) ? "Default" : nlogLoggerName.Trim();
            _options = options ?? new NLogLoggerOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SessionId = Guid.NewGuid();
            SessionStartedAt = DateTime.Now;
        }

        /// <summary>
        /// 获取原始 logger 名称。
        /// </summary>
        public string SourceLoggerName
        {
            get { return _sourceLoggerName; }
        }

        /// <summary>
        /// 获取映射后的 NLog logger 名称。
        /// </summary>
        public string LoggerName
        {
            get { return _nlogLoggerName; }
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
        public void Write(CoreLogLevel level, string message, Exception exception = null)
        {
            if (!ShouldWrite(level))
            {
                return;
            }

            NLogLogLevel nlogLevel = Map(level, _options);
            if (!_logger.IsEnabled(nlogLevel))
            {
                return;
            }

            string renderedMessage = RenderMessage(message, exception, _options);
            LogEventInfo eventInfo = new LogEventInfo(nlogLevel, _nlogLoggerName, renderedMessage);
            if (exception != null)
            {
                eventInfo.Exception = exception;
            }

            AttachEventProperties(eventInfo, level);
            _logger.Log(eventInfo);
        }

        /// <inheritdoc />
        public void SetMinimumLevel(CoreLogLevel minimumLevel)
        {
            lock (_syncRoot)
            {
                _minimumLevel = minimumLevel;
            }
        }

        /// <inheritdoc />
        public void Trace(string message)
        {
            Write(CoreLogLevel.Trace, message);
        }

        /// <inheritdoc />
        public void Debug(string message)
        {
            Write(CoreLogLevel.Debug, message);
        }

        /// <inheritdoc />
        public void Info(string message)
        {
            Write(CoreLogLevel.Info, message);
        }

        /// <inheritdoc />
        public void Success(string message)
        {
            Write(CoreLogLevel.Success, message);
        }

        /// <inheritdoc />
        public void Warning(string message)
        {
            Write(CoreLogLevel.Warn, message);
        }

        /// <inheritdoc />
        public void Error(string message)
        {
            Write(CoreLogLevel.Error, message);
        }

        /// <inheritdoc />
        public void Fatal(string message)
        {
            Write(CoreLogLevel.Fatal, message);
        }

        /// <inheritdoc />
        public void AddLog(CoreLogLevel level, string message)
        {
            Write(level, message);
        }

        /// <inheritdoc />
        public void AddLogs(System.Collections.Generic.IEnumerable<LogEntry> entries)
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

        private bool ShouldWrite(CoreLogLevel level)
        {
            lock (_syncRoot)
            {
                return level >= _minimumLevel;
            }
        }

        private void AttachEventProperties(LogEventInfo eventInfo, CoreLogLevel level)
        {
            if (eventInfo == null || _options == null)
            {
                return;
            }

            if (_options.AttachLoggerNameProperty && !string.IsNullOrWhiteSpace(_options.LoggerNamePropertyName))
            {
                eventInfo.Properties[_options.LoggerNamePropertyName] = _sourceLoggerName;
            }

            if (_options.AttachSessionProperties)
            {
                if (!string.IsNullOrWhiteSpace(_options.SessionIdPropertyName))
                {
                    eventInfo.Properties[_options.SessionIdPropertyName] = SessionId.ToString("D", CultureInfo.InvariantCulture);
                }

                if (!string.IsNullOrWhiteSpace(_options.SessionStartedAtPropertyName))
                {
                    eventInfo.Properties[_options.SessionStartedAtPropertyName] =
                        SessionStartedAt.ToString("o", CultureInfo.InvariantCulture);
                }
            }

            if (_options.AttachSuccessProperty && level == CoreLogLevel.Success && !string.IsNullOrWhiteSpace(_options.SuccessPropertyName))
            {
                eventInfo.Properties[_options.SuccessPropertyName] = "true";
            }
        }

        private static string RenderMessage(string message, Exception exception, NLogLoggerOptions options)
        {
            string normalizedMessage = message ?? string.Empty;
            if (exception == null)
            {
                return normalizedMessage;
            }

            if (options == null || !options.AppendExceptionText)
            {
                return normalizedMessage;
            }

            string exceptionText = exception.ToString();
            if (string.IsNullOrWhiteSpace(normalizedMessage))
            {
                return exceptionText;
            }

            StringBuilder builder = new StringBuilder(normalizedMessage);
            builder.AppendLine();
            builder.Append(exceptionText);
            return builder.ToString();
        }

        private static NLogLogLevel Map(CoreLogLevel level, NLogLoggerOptions options)
        {
            switch (level)
            {
                case CoreLogLevel.Trace:
                    return NLogLogLevel.Trace;
                case CoreLogLevel.Debug:
                    return NLogLogLevel.Debug;
                case CoreLogLevel.Info:
                    return NLogLogLevel.Info;
                case CoreLogLevel.Success:
                    return options != null ? options.SuccessTargetLevel : NLogLogLevel.Info;
                case CoreLogLevel.Warn:
                    return NLogLogLevel.Warn;
                case CoreLogLevel.Error:
                    return NLogLogLevel.Error;
                case CoreLogLevel.Fatal:
                    return NLogLogLevel.Fatal;
                default:
                    return NLogLogLevel.Info;
            }
        }
    }
}
