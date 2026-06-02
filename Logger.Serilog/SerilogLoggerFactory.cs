using System;
using System.Collections.Concurrent;
using Logger.Core;

namespace Logger.Serilog
{
    /// <summary>
    /// 为 <see cref="Logger.Core.ILoggerFactory"/> 提供 Serilog 适配实现。
    /// </summary>
    public sealed class SerilogLoggerFactory : ILoggerFactory
    {
        private readonly SerilogLoggerOptions _options;
        private readonly ConcurrentDictionary<string, SerilogLogger> _loggers =
            new ConcurrentDictionary<string, SerilogLogger>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 初始化 Serilog 日志工厂。
        /// </summary>
        /// <param name="options">Serilog 适配选项；为空时使用默认值。</param>
        public SerilogLoggerFactory(SerilogLoggerOptions options = null)
        {
            _options = options ?? new SerilogLoggerOptions();
        }

        /// <summary>
        /// 获取当前工厂使用的配置。
        /// </summary>
        public SerilogLoggerOptions Options
        {
            get { return _options; }
        }

        /// <inheritdoc />
        public ILoggerOutput CreateLogger(string name)
        {
            string normalizedName = NormalizeLoggerName(name);
            return _loggers.GetOrAdd(
                normalizedName,
                key => new SerilogLogger(key, ResolveSerilogLoggerName(key, _options), _options));
        }

        private static string NormalizeLoggerName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
        }

        private static string ResolveSerilogLoggerName(string loggerName, SerilogLoggerOptions options)
        {
            string resolvedName = loggerName;
            if (options != null && options.LoggerNameResolver != null)
            {
                string mappedName = options.LoggerNameResolver(loggerName);
                if (!string.IsNullOrWhiteSpace(mappedName))
                {
                    resolvedName = mappedName.Trim();
                }
            }

            if (options != null && !string.IsNullOrWhiteSpace(options.LoggerNamePrefix))
            {
                resolvedName = options.LoggerNamePrefix + resolvedName;
            }

            return string.IsNullOrWhiteSpace(resolvedName) ? "Default" : resolvedName;
        }
    }
}
