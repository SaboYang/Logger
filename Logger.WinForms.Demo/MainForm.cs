using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Logger.Core;
using Logger.Core.Models;
using Logger.WinForms.Controls;

namespace Logger.WinForms.Demo
{
    public class MainForm : Form
    {
        private static readonly LogLevel[] DemoLevels =
        {
            LogLevel.Trace,
            LogLevel.Debug,
            LogLevel.Info,
            LogLevel.Success,
            LogLevel.Warn,
            LogLevel.Error,
            LogLevel.Fatal
        };

        private readonly LogPanelControl _logPanel;
        private readonly Button _sampleButton;
        private readonly Button _stressButton;
        private readonly Button _openWpfHostButton;
        private readonly Button _openFactoryDemoButton;
        private readonly Button _openFileDemoButton;
        private readonly NumericUpDown _stressCountInput;
        private readonly Label _statusLabel;
        private readonly StressSummaryPanel _summaryPanel;
        private readonly ILoggerOutput _logger;
        private bool _isStressTesting;

        public MainForm()
        {
            _logger = LogManager.Factory.CreateLogger("Logger.WinForms.Demo.MainForm");

            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Text = "Logger WinForms 示例";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(980, 640);
            ClientSize = new Size(1180, 760);
            BackColor = Color.FromArgb(245, 247, 250);

            TableLayoutPanel rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12)
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 9,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _sampleButton = new Button
            {
                AutoSize = true,
                Height = 34,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12, 0, 12, 0),
                Text = "写入等级示例",
                UseVisualStyleBackColor = true
            };
            _sampleButton.Click += SampleButton_Click;

            _stressButton = new Button
            {
                AutoSize = true,
                Height = 34,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(12, 0, 12, 0),
                Text = "日志压力测试",
                UseVisualStyleBackColor = true
            };
            _stressButton.Click += StressButton_Click;

            _openWpfHostButton = new Button
            {
                AutoSize = true,
                Height = 34,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(12, 0, 12, 0),
                Text = "打开 WPF 宿主",
                UseVisualStyleBackColor = true
            };
            _openWpfHostButton.Click += OpenWpfHostButton_Click;

            _openFactoryDemoButton = new Button
            {
                AutoSize = true,
                Height = 34,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(12, 0, 12, 0),
                Text = "打开工厂 Demo",
                UseVisualStyleBackColor = true
            };
            _openFactoryDemoButton.Click += OpenFactoryDemoButton_Click;

            _openFileDemoButton = new Button
            {
                AutoSize = true,
                Height = 34,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(12, 0, 12, 0),
                Text = "打开文件 Demo",
                UseVisualStyleBackColor = true
            };
            _openFileDemoButton.Click += OpenFileDemoButton_Click;

            Label countLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 8, 6, 0),
                ForeColor = Color.FromArgb(55, 65, 81),
                Text = "压力条数："
            };

            _stressCountInput = new NumericUpDown
            {
                Width = 100,
                Minimum = 100,
                Maximum = 50000,
                Increment = 100,
                Value = 5000,
                Margin = new Padding(0, 4, 16, 0),
                ThousandsSeparator = true
            };

            Label infoLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = "支持等级：TRACE / DEBUG / INFO / SUCCESS / WARN / ERROR / FATAL"
            };

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Margin = new Padding(0, 8, 0, 0),
                ForeColor = Color.FromArgb(29, 78, 216),
                TextAlign = ContentAlignment.MiddleRight,
                Text = "准备就绪"
            };

            _summaryPanel = new StressSummaryPanel();
            _summaryPanel.UpdateSummary(StressTestSummary.CreateIdle("WinForms 日志控件压测", "点击“日志压力测试”开始执行。"));

            _logPanel = new LogPanelControl
            {
                Dock = DockStyle.Fill,
                Header = "WinForms 日志演示",
                MaxLogEntries = 30000,
                Margin = new Padding(0)
            };
            _logPanel.Logger = _logger;

            toolbar.Controls.Add(_sampleButton, 0, 0);
            toolbar.Controls.Add(_stressButton, 1, 0);
            toolbar.Controls.Add(_openWpfHostButton, 2, 0);
            toolbar.Controls.Add(_openFactoryDemoButton, 3, 0);
            toolbar.Controls.Add(_openFileDemoButton, 4, 0);
            toolbar.Controls.Add(countLabel, 5, 0);
            toolbar.Controls.Add(_stressCountInput, 6, 0);
            toolbar.Controls.Add(_statusLabel, 8, 0);

            rootLayout.Controls.Add(toolbar, 0, 0);
            rootLayout.Controls.Add(infoLabel, 0, 1);
            rootLayout.Controls.Add(_summaryPanel, 0, 2);
            rootLayout.Controls.Add(_logPanel, 0, 3);

            Controls.Add(rootLayout);

            Shown += MainForm_Shown;
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            _logger.AddInfo("WinForms 示例程序已启动。");
            _logger.AddInfo("当前主窗体使用独立的 ILoggerOutput，不再与其他 Demo 窗口共享日志。");
            _logger.AddInfo("这个窗体不会调用日志控件的方法，而是直接向 ILoggerOutput 写入。");
            WriteLogFilePath();
            _logger.AddInfo("点击“写入等级示例”查看全部日志等级，点击“日志压力测试”执行批量写入。");
            _logger.AddInfo("点击“打开 WPF 宿主”可以在 WinForms 窗体中展示封装后的 WPF 日志控件。");
            _logger.AddInfo("点击“打开工厂 Demo”可以查看 ILoggerFactory 与 LoggerService 的接口用法演示。");
            _logger.AddInfo("点击“打开文件 Demo”可以同时验证本地文件落盘和文件内容预览。");
            _logger.AddInfo("如果要直接跑封装控件压测，可执行：Logger.WinForms.Demo.exe --wpf-host --stress --close-after-stress");
            WriteLevelSamples();
            UpdateStatus("已加载等级示例");
        }

        private void WriteLogFilePath()
        {
            ILogFileSource fileSource = _logger as ILogFileSource;
            if (fileSource == null || !fileSource.IsFileOutputEnabled || string.IsNullOrWhiteSpace(fileSource.LogFilePath))
            {
                return;
            }

            _logger.AddInfo("本地日志文件: " + fileSource.LogFilePath);
        }

        private void SampleButton_Click(object sender, EventArgs e)
        {
            WriteLevelSamples();
            UpdateStatus("已写入一组等级示例");
        }

        private async void StressButton_Click(object sender, EventArgs e)
        {
            await RunStressTestAsync();
        }

        private void OpenWpfHostButton_Click(object sender, EventArgs e)
        {
            WpfHostForm hostForm = new WpfHostForm();
            hostForm.Show(this);
            _logger.AddInfo("已打开一个承载封装版 WPF 日志控件的 WinForms 宿主窗体。");
            UpdateStatus("已打开 WPF 宿主窗体");
        }

        private void OpenFactoryDemoButton_Click(object sender, EventArgs e)
        {
            LoggerFactoryIsolatedDemoForm demoForm = new LoggerFactoryIsolatedDemoForm();
            demoForm.Show(this);
            _logger.AddInfo("已打开独立日志源模式的 ILoggerFactory / LoggerService Demo。");
            UpdateStatus("已打开工厂 Demo");
        }

        private void OpenFileDemoButton_Click(object sender, EventArgs e)
        {
            FileLogDemoForm demoForm = new FileLogDemoForm();
            demoForm.Show(this);
            _logger.AddInfo("已打开本地日志文件 Demo。");
            UpdateStatus("已打开文件 Demo");
        }

        private void WriteLevelSamples()
        {
            _logger.AddTrace("TRACE: 进入示例流程，记录细粒度跟踪信息。");
            _logger.AddDebug("DEBUG: 当前按钮和状态栏均已完成初始化。");
            _logger.AddInfo("INFO: WinForms 日志控件正在正常工作。");
            _logger.AddSuccess("SUCCESS: 日志控件已成功接入示例程序。");
            _logger.AddWarning("WARN: 压力测试条数过大时，界面刷新会更频繁。");
            _logger.AddError("ERROR: 这是演示错误日志，不代表真实故障。");
            _logger.AddFatal("FATAL: 这是演示致命日志，用于观察高亮效果。\r\nFATAL: 这是演示致命日志，用于观察高亮效果。\r\nFATAL: 这是演示致命日志，用于观察高亮效果。");
        }

        private async Task RunStressTestAsync()
        {
            if (_isStressTesting)
            {
                return;
            }

            _isStressTesting = true;
            SetToolbarState(false);

            int total = (int)_stressCountInput.Value;
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                _summaryPanel.UpdateSummary(
                    StressTestSummary.CreateRunning(
                        "WinForms 日志控件压测",
                        total,
                        string.Format("正在写入 {0:N0} 条日志，请稍候。", total)));
                _logger.AddInfo(string.Format("开始压力测试，本次计划写入 {0:N0} 条日志。", total));
                await Task.Run(() => ProduceStressLogs(total));

                stopwatch.Stop();
                _logger.AddSuccess(string.Format("压力测试完成，共写入 {0:N0} 条日志，耗时 {1} ms。", total, stopwatch.ElapsedMilliseconds));
                _summaryPanel.UpdateSummary(
                    StressTestSummary.CreateSuccess(
                        "WinForms 日志控件压测",
                        total,
                        stopwatch.ElapsedMilliseconds,
                        "批量写入完成，WinForms 日志控件保持正常响应。"));
                UpdateStatus(string.Format("压力测试完成，用时 {0} ms", stopwatch.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.AddError("压力测试失败：" + ex.Message);
                _summaryPanel.UpdateSummary(
                    StressTestSummary.CreateFailure(
                        "WinForms 日志控件压测",
                        total,
                        stopwatch.ElapsedMilliseconds,
                        ex.Message));
                UpdateStatus("压力测试失败");
            }
            finally
            {
                SetToolbarState(true);
                _isStressTesting = false;
            }
        }

        private void ProduceStressLogs(int total)
        {
            const int batchSize = 200;
            List<LogEntry> batch = new List<LogEntry>(batchSize);

            for (int i = 1; i <= total; i++)
            {
                LogLevel level = DemoLevels[(i - 1) % DemoLevels.Length];
                batch.Add(new LogEntry(DateTime.Now, level, BuildStressMessage(level, i, total)));

                if (batch.Count == batchSize || i == total)
                {
                    _logger.AddLogs(batch);

                    if (i % 1000 == 0 || i == total)
                    {
                        PostStatus(string.Format("压力测试进行中：{0:N0}/{1:N0}", i, total));
                    }

                    batch = new List<LogEntry>(batchSize);
                }
            }
        }

        private void SetToolbarState(bool enabled)
        {
            _sampleButton.Enabled = enabled;
            _stressButton.Enabled = enabled;
            _openWpfHostButton.Enabled = enabled;
            _openFactoryDemoButton.Enabled = enabled;
            _stressCountInput.Enabled = enabled;
        }

        private void PostStatus(string message)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            try
            {
                BeginInvoke(new Action<string>(UpdateStatus), message);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void UpdateStatus(string message)
        {
            _statusLabel.Text = message;
        }

        private static string BuildStressMessage(LogLevel level, int index, int total)
        {
            return string.Format(
                "[{0:N0}/{1:N0}] {2} 级别压力日志，用于验证滚动、颜色、清空和大批量写入场景。",
                index,
                total,
                level.ToString().ToUpperInvariant());
        }
    }
}
