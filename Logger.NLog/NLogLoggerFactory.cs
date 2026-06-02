using System;
using System.Collections.Concurrent;
using Logger.Core;

namespace Logger.NLog
{
    /// <summary>
    /// 为 Logger.Core 创建 NLog 日志输出适配器。
    /// </summary>
    public sealed class NLogLoggerFactory : ILoggerFactory
    {
        private readonly NLogLoggerOptions _options;
        private readonly ConcurrentDictionary<string, NLogLogger> _loggers =
            new ConcurrentDictionary<string, NLogLogger>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 初始化 NLog 日志工厂。
        /// </summary>
        /// <param name="options">NLog 适配选项；为空时使用默认值。</param>
        public NLogLoggerFactory(NLogLoggerOptions options = null)
        {
            _options = options ?? new NLogLoggerOptions();
        }

        /// <summary>
        /// 获取当前工厂使用的配置。
        /// </summary>
        public NLogLoggerOptions Options
        {
            get { return _options; }
        }

        /// <inheritdoc />
        public ILoggerOutput CreateLogger(string name)
        {
            string normalizedName = NormalizeLoggerName(name);
            return _loggers.GetOrAdd(
                normalizedName,
                key => new NLogLogger(key, ResolveNLogLoggerName(key, _options), _options));
        }

        private static string NormalizeLoggerName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
        }

        private static string ResolveNLogLoggerName(string loggerName, NLogLoggerOptions options)
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
