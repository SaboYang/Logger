using System;
using Logger.Core;
using Microsoft.Extensions.DependencyInjection;

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

            LoggerCoreRegistration.AddCoreServices(services, options);
            return services;
        }
    }
}
