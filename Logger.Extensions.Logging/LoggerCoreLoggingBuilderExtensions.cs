using System;
using Logger.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Logger.Extensions.Logging
{
    public static class LoggerCoreLoggingBuilderExtensions
    {
        public static ILoggingBuilder AddLoggerCore(this ILoggingBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.AddSingleton<ILoggerProvider, LoggerCoreLoggerProvider>();
            return builder;
        }
    }
}
