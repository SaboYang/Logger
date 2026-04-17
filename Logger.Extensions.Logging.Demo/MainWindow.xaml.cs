using System;
using System.Windows;
using Logger.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Logger.Extensions.Logging.Demo
{
#if NET8_0_OR_GREATER
    [System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]
#endif
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly ILoggerService _loggerService;
        private readonly DemoActionService _demoActionService;
        private readonly ILoggerOutput _panelLogger;

        public MainWindow()
            : this(
                  NullLogger<MainWindow>.Instance,
                  LoggerService.Shared,
                  new DemoActionService(NullLogger<DemoActionService>.Instance))
        {
        }

        public MainWindow(
            ILogger<MainWindow> logger,
            ILoggerService loggerService,
            DemoActionService demoActionService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerService = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
            _demoActionService = demoActionService ?? throw new ArgumentNullException(nameof(demoActionService));

            InitializeComponent();

            string windowLoggerName = typeof(MainWindow).FullName ?? typeof(MainWindow).Name;
            string serviceLoggerName = typeof(DemoActionService).FullName ?? typeof(DemoActionService).Name;

            _panelLogger = LogManager.CreateMergedLogger(
                _loggerService.GetLogger(windowLoggerName),
                _loggerService.GetLogger(serviceLoggerName));
            LogPanel.Logger = _panelLogger;

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string windowLoggerName = typeof(MainWindow).FullName ?? typeof(MainWindow).Name;
            string serviceLoggerName = typeof(DemoActionService).FullName ?? typeof(DemoActionService).Name;

            _logger.LogInformation("MainWindow 已通过容器创建，ILogger<MainWindow> 已注入。");
            _logger.LogInformation("MainWindow category: {Category}", windowLoggerName);
            _logger.LogInformation("DemoActionService category: {Category}", serviceLoggerName);
            _logger.LogInformation(
                "日志文件路径: {Path}",
                ((ILogFileSource)_loggerService.GetLogger(windowLoggerName))?.LogFilePath ?? "N/A");
            _demoActionService.EmitGreeting();
        }

        private void WriteWindowLog_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("窗口按钮触发写入，时间: {Time}", DateTime.Now);
            _logger.LogWarning("这是一个窗口级别的警告示例。");
        }

        private void WriteServiceLog_Click(object sender, RoutedEventArgs e)
        {
            _demoActionService.EmitGreeting();
        }

        private void WriteBatchLog_Click(object sender, RoutedEventArgs e)
        {
            _demoActionService.EmitSampleBatch(12);
        }

        private void WriteExceptionLog_Click(object sender, RoutedEventArgs e)
        {
            _demoActionService.EmitException();
        }

        private void ClearPanelLog_Click(object sender, RoutedEventArgs e)
        {
            LogPanel.ClearLogs();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            IDisposable disposable = _panelLogger as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
