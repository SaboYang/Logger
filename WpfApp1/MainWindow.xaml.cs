using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Logger.Core;
using Logger.Core.Models;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int StressLogCount = 30000;

        private readonly ILoggerOutput _logger = LogManager.GetLogger("WpfApp1.MainWindow");
        private bool _isStressRunning;

        public MainWindow()
        {
            InitializeComponent();
            LogViewer.Logger = _logger;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetStatus("准备就绪");
            _logger.Success("日志控件已就绪。");
            _logger.Info("当前支持等级：TRACE / DEBUG / INFO / SUCCESS / WARN / ERROR / FATAL。");
            _logger.Info("当前窗口通过 Logger.Core 接口向 WPF 日志控件输出内容。");
            WriteLogFilePath();
            _logger.Info("点击“日志压力测试”可批量写入日志。");
        }

        private void WriteLogFilePath()
        {
            ILogFileSource fileSource = _logger as ILogFileSource;
            if (fileSource == null || !fileSource.IsFileOutputEnabled || string.IsNullOrWhiteSpace(fileSource.LogFilePath))
            {
                return;
            }

            _logger.Info("本地日志文件: " + fileSource.LogFilePath);
        }

        private async void BtnStressTest_Click(object sender, RoutedEventArgs e)
        {
            if (_isStressRunning)
            {
                _logger.Warning("压力测试正在进行中，请稍候。");
                return;
            }

            _isStressRunning = true;
            BtnStressTest.IsEnabled = false;
            SetStatus("压力测试进行中...");

            try
            {
                var st = Stopwatch.StartNew();
                _logger.Info($"开始压力测试，本次准备写入 {StressLogCount} 条日志。");
                await Task.Run(() => ProduceStressLogs(StressLogCount));
                _logger.Success("压力测试完成。");
                SetStatus($"压力测试完成，共写入 {StressLogCount} 条日志,完成时间：{st.Elapsed}");
            }
            catch (Exception ex)
            {
                _logger.Error($"压力测试失败：{ex.Message}");
                SetStatus("压力测试失败");
            }
            finally
            {
                _isStressRunning = false;
                BtnStressTest.IsEnabled = true;
            }
        }

        private void ProduceStressLogs(int totalCount)
        {
            Random random = new Random();
            const int batchSize = 200;
            var batch = new System.Collections.Generic.List<LogEntry>(batchSize);

            for (int index = 1; index <= totalCount; index++)
            {
                batch.Add(BuildStressEntry(index, random));

                if (batch.Count == batchSize || index == totalCount)
                {
                    _logger.AddLogs(batch);
                    batch = new System.Collections.Generic.List<LogEntry>(batchSize);
                }

                if (index % 1000 == 0 || index == totalCount)
                {
                    PostStatus($"压力测试进行中... {index}/{totalCount}");
                }
            }
        }

        private LogEntry BuildStressEntry(int index, Random random)
        {
            int remainder = index % 7;
            if (remainder == 1)
            {
                return new LogEntry(DateTime.Now, LogLevel.Trace, $"[#{index:00000}] TRACE 模拟链路追踪，游标位置 {random.Next(1, 9999)}。");
            }

            if (remainder == 2)
            {
                return new LogEntry(DateTime.Now, LogLevel.Debug, $"[#{index:00000}] DEBUG 模拟诊断数据，批次 {index / 25 + 1}。");
            }

            if (remainder == 3)
            {
                return new LogEntry(DateTime.Now, LogLevel.Info, $"[#{index:00000}] INFO 模拟业务消息，耗时 {random.Next(5, 60)} ms。");
            }

            if (remainder == 4)
            {
                return new LogEntry(DateTime.Now, LogLevel.Success, $"[#{index:00000}] SUCCESS 模拟操作完成，写入记录 {random.Next(1, 80)} 条。");
            }

            if (remainder == 5)
            {
                return new LogEntry(DateTime.Now, LogLevel.Warn, $"[#{index:00000}] WARN 模拟波动告警，队列长度 {random.Next(10, 500)}。");
            }

            if (remainder == 6)
            {
                return new LogEntry(DateTime.Now, LogLevel.Error, $"[#{index:00000}] ERROR 模拟异常告警，重试次数 {random.Next(1, 6)}。");
            }

            return new LogEntry(DateTime.Now, LogLevel.Fatal, $"[#{index:00000}] FATAL 模拟严重故障，任务已进入熔断。");
        }

        private void PostStatus(string text)
        {
            Dispatcher.BeginInvoke(new Action(() => SetStatus(text)));
        }

        private void SetStatus(string text)
        {
            TxtStatus.Text = text;
        }
    }
}

