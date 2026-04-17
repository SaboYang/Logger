using System;
using System.Windows;
using Logger.Core;
using Logger.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Logger.Extensions.Logging.Demo
{
#if NET8_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]
#endif
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ServiceCollection services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddLoggerCore();
            });
            services.AddSingleton<DemoActionService>();
            services.AddTransient<MainWindow>();

            _serviceProvider = services.BuildServiceProvider();
            LogManager.Configure(_serviceProvider.GetRequiredService<ILoggerService>());

            MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            IDisposable disposable = _serviceProvider as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }

            base.OnExit(e);
        }
    }
}
