using System;
using System.IO;
using Logger.Core;
using Logger.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using CoreLoggerFactory = Logger.Core.ILoggerFactory;

namespace Logger.Extensions.Logging
{
    internal static class LoggerCoreRegistration
    {
        public static void AddCoreServices(IServiceCollection services, LoggerCoreOptions options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options == null)
            {
                options = new LoggerCoreOptions();
            }

            services.AddSingleton(options);
            services.AddSingleton<CoreLoggerFactory>(sp => CreateLoggerFactory(sp.GetRequiredService<LoggerCoreOptions>()));
            services.AddSingleton<ILoggerService>(sp => new LoggerService(sp.GetRequiredService<CoreLoggerFactory>()));
        }

        public static CoreLoggerFactory CreateLoggerFactory(LoggerCoreOptions options)
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
