using System;
using System.IO;
using Logger.Core;
using Logger.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using CoreLoggerFactory = Logger.Core.ILoggerFactory;

namespace Logger.Extensions.Logging
{
    public static class LoggerCoreServiceCollectionExtensions
    {
        public static IServiceCollection AddLoggerCore(
            this IServiceCollection services,
            Action<LoggerCoreOptions> configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            LoggerCoreOptions options = new LoggerCoreOptions();
            if (configure != null)
            {
                configure(options);
            }

            services.AddSingleton(options);
            services.AddSingleton<CoreLoggerFactory>(sp => CreateLoggerFactory(sp.GetRequiredService<LoggerCoreOptions>()));
            services.AddSingleton<ILoggerService>(sp => new LoggerService(sp.GetRequiredService<CoreLoggerFactory>()));
            return services;
        }

        private static CoreLoggerFactory CreateLoggerFactory(LoggerCoreOptions options)
        {
            if (options == null)
            {
                options = new LoggerCoreOptions();
            }

            string logRootDirectoryPath = options.LogRootDirectoryPath;
            if (string.IsNullOrWhiteSpace(logRootDirectoryPath))
            {
                logRootDirectoryPath = Path.Combine(AppContext.BaseDirectory, "logs");
            }

            return new LogStoreLoggerFactory(
                logRootDirectoryPath: logRootDirectoryPath,
                minimumLevel: options.MinimumLevel,
                rollingMode: options.RollingMode,
                rollingRetentionDays: options.RollingRetentionDays,
                maxBufferedSessionEntries: options.MaxBufferedSessionEntries,
                maxPendingStorageEntries: options.MaxPendingStorageEntries,
                spoolRootDirectoryPath: options.SpoolRootDirectoryPath,
                spoolFlushMode: options.SpoolFlushMode);
        }
    }
}
