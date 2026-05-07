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
    [System.ComponentModel.DesignerCategory("Code")]
    internal class WpfTextHostForm : Form
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

        private readonly WpfLogTextPanelControl _wpfTextLogPanel;
        private readonly Button _sampleButton;
        private readonly Button _stressButton;
        private readonly Label _statusLabel;
        private readonly StressSummaryPanel _summaryPanel;
        private readonly CodeSamplePanel _codeSamplePanel;
        private readonly bool _autoRunStressTest;
        private readonly bool _closeAfterStressTest;
        private readonly int _stressLogCount;
        private readonly ILoggerOutput _logger;
        private bool _isStressRunning;

        internal WpfTextHostForm()
            : this(30000, false, false)
        {
        }

        internal WpfTextHostForm(int stressLogCount = 30000, bool autoRunStressTest = false, bool closeAfterStressTest = false)
        {
            _stressLogCount = stressLogCount > 0 ? stressLogCount : 30000;
            _autoRunStressTest = autoRunStressTest;
            _closeAfterStressTest = closeAfterStressTest;
            _logger = LogManager.Factory.CreateLogger("Logger.WinForms.Demo.WpfTextHost");

            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Text = "Logger.Wpf 文本宿主示例";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(880, 560);
            ClientSize = new Size(1020, 680);
            BackColor = Color.FromArgb(245, 247, 250);

            TableLayoutPanel rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(12)
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _sampleButton = new Button
            {
                AutoSize = true,
                Height = 34,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12, 0, 12, 0),
                Text = "写入文本宿主示例",
                UseVisualStyleBackColor = true
            };
            _sampleButton.Click += SampleButton_Click;

            _stressButton = new Button
            {
                AutoSize = true,
                Height = 34,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(12, 0, 12, 0),
                Text = "文本宿主压力测试",
                UseVisualStyleBackColor = true
            };
            _stressButton.Click += StressButton_Click;

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Margin = new Padding(0, 8, 0, 0),
                ForeColor = Color.FromArgb(29, 78, 216),
                TextAlign = ContentAlignment.MiddleRight,
                Text = "准备就绪"
            };

            Label infoLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = "这里演示的是封装在 WinForms 里的 WPF 文本日志宿主。"
            };

            _summaryPanel = new StressSummaryPanel();
            _summaryPanel.UpdateSummary(StressTestSummary.CreateIdle("WPF 文本宿主压力测试", "点击“文本宿主压力测试”开始批量写入。"));

            _codeSamplePanel = new CodeSamplePanel
            {
                CodeText =
@"using Logger.Core;
using Logger.WinForms.Controls;

ILoggerOutput logger = LogManager.Factory.CreateLogger(""MyApp.WpfTextHost"");

var hostPanel = new WpfLogTextPanelControl
{
    Dock = DockStyle.Fill,
    Header = ""WPF 文本日志演示"",
    Logger = logger,
    MaxLogEntries = 30000
};

Controls.Add(hostPanel);

logger.Info(""WPF text host ready"");
logger.Success(""Logger.Wpf 文本宿主已封装到 WinForms"");
logger.Error(""line1\r\nline2"");"
            };

            _wpfTextLogPanel = new WpfLogTextPanelControl
            {
                Dock = DockStyle.Fill,
                Header = "WPF 文本日志演示",
                MaxLogEntries = Math.Max(_stressLogCount + 1000, 8000),
                Margin = new Padding(0)
            };
            _wpfTextLogPanel.Logger = _logger;

            toolbar.Controls.Add(_sampleButton, 0, 0);
            toolbar.Controls.Add(_stressButton, 1, 0);
            toolbar.Controls.Add(_statusLabel, 2, 0);

            rootLayout.Controls.Add(toolbar, 0, 0);
            rootLayout.Controls.Add(infoLabel, 0, 1);
            rootLayout.Controls.Add(_summaryPanel, 0, 2);
            rootLayout.Controls.Add(_codeSamplePanel, 0, 3);
            rootLayout.Controls.Add(_wpfTextLogPanel, 0, 4);

            Controls.Add(rootLayout);

            Shown += WpfTextHostForm_Shown;
        }

        internal bool? AutoTestSucceeded { get; private set; }

        private async void WpfTextHostForm_Shown(object sender, EventArgs e)
        {
            _logger.Info("WPF 文本日志宿主已通过 Logger.WinForms 封装加载成功。");
            _logger.Info("当前窗体绑定的是独立 ILoggerOutput，不会和其他 Demo 共用日志。");
            _logger.Info("这个窗口直接调用 ILoggerOutput 写入，而不是调用控件自身的方法。");
            WriteLogFilePath();
            _logger.Info("点击“写入文本宿主示例”查看等级着色和滚动效果。");
            WriteLevelSamples();
            UpdateStatus("已加载文本宿主示例");

            if (_autoRunStressTest)
            {
                await RunStressTestAsync();

                if (_closeAfterStressTest && !IsDisposed)
                {
                    Close();
                }
            }
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

        private void SampleButton_Click(object sender, EventArgs e)
        {
            WriteLevelSamples();
            UpdateStatus("已写入一组文本宿主示例日志");
        }

        private async void StressButton_Click(object sender, EventArgs e)
        {
            await RunStressTestAsync();
        }

        private void WriteLevelSamples()
        {
            _logger.Trace("TRACE: 进入文本宿主示例流程。");
            _logger.Debug("DEBUG: 当前按钮和状态栏已初始化。");
            _logger.Info("INFO: WinForms 中的 WPF 文本日志宿主正在工作。");
            _logger.Success("SUCCESS: WpfLogTextPanelControl 已成功封装到 WinForms。");
            _logger.Warning("WARN: 压力测试会持续追加大量日志。");
            _logger.Error("ERROR: 这是一条示例错误日志，不代表真实故障。");
            _logger.Fatal("FATAL: 这是一条示例致命日志，用于观察高亮效果。");
        }

        private async Task RunStressTestAsync()
        {
            if (_isStressRunning)
            {
                return;
            }

            _isStressRunning = true;
            AutoTestSucceeded = null;
            SetToolbarState(false);

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                _summaryPanel.UpdateSummary(
                    StressTestSummary.CreateRunning(
                        "WPF 文本宿主压力测试",
                        _stressLogCount,
                        string.Format("正在写入 {0:N0} 条文本宿主日志，请稍候。", _stressLogCount)));
                _logger.Info(string.Format("开始文本宿主压力测试，本次计划写入 {0:N0} 条日志。", _stressLogCount));
                await Task.Run(() => ProduceStressLogs(_stressLogCount));

                stopwatch.Stop();
                _logger.Success(string.Format("文本宿主压力测试完成，共写入 {0:N0} 条日志，耗时 {1} ms。", _stressLogCount, stopwatch.ElapsedMilliseconds));
                _summaryPanel.UpdateSummary(
                    StressTestSummary.CreateSuccess(
                        "WPF 文本宿主压力测试",
                        _stressLogCount,
                        stopwatch.ElapsedMilliseconds,
                        "WPF 文本日志宿主已完成一轮批量写入。"));
                UpdateStatus(string.Format("压力测试完成，用时 {0} ms", stopwatch.ElapsedMilliseconds));
                AutoTestSucceeded = true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error("文本宿主压力测试失败：" + ex.Message);
                _summaryPanel.UpdateSummary(
                    StressTestSummary.CreateFailure(
                        "WPF 文本宿主压力测试",
                        _stressLogCount,
                        stopwatch.ElapsedMilliseconds,
                        ex.Message));
                UpdateStatus("压力测试失败");
                AutoTestSucceeded = false;
            }
            finally
            {
                SetToolbarState(true);
                _isStressRunning = false;
            }
        }

        private void ProduceStressLogs(int total)
        {
            const int batchSize = 200;
            List<LogEntry> batch = new List<LogEntry>(batchSize);

            for (int index = 1; index <= total; index++)
            {
                LogLevel level = DemoLevels[(index - 1) % DemoLevels.Length];
                batch.Add(new LogEntry(DateTime.Now, level, BuildStressMessage(level, index, total)));

                if (batch.Count == batchSize || index == total)
                {
                    _logger.AddLogs(batch);

                    if (index % 1000 == 0 || index == total)
                    {
                        PostStatus(string.Format("文本宿主压力测试进行中：{0:N0}/{1:N0}", index, total));
                    }

                    batch = new List<LogEntry>(batchSize);
                }
            }
        }

        private void SetToolbarState(bool enabled)
        {
            _sampleButton.Enabled = enabled;
            _stressButton.Enabled = enabled;
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
                "[{0:N0}/{1:N0}] {2} 级别文本宿主压力日志，用于验证 WPF 文本面板在 WinForms 中的滚动和追加表现。",
                index,
                total,
                level.ToString().ToUpperInvariant());
        }
    }
}
