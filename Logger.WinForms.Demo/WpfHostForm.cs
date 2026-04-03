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
    public class WpfHostForm : Form
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

        private readonly WpfLogPanelControl _wpfLogPanel;
        private readonly Button _sampleButton;
        private readonly Button _stressButton;
        private readonly Label _statusLabel;
        private readonly StressSummaryPanel _summaryPanel;
        private readonly bool _autoRunStressTest;
        private readonly bool _closeAfterStressTest;
        private readonly int _stressLogCount;
        private readonly ILoggerOutput _logger;
        private readonly string _panelHeader;
        private bool _isStressRunning;

        public WpfHostForm()
            : this(null, null, null, 30000, false, false)
        {
        }

        public WpfHostForm(int stressLogCount = 30000, bool autoRunStressTest = false, bool closeAfterStressTest = false)
            : this(null, null, null, stressLogCount, autoRunStressTest, closeAfterStressTest)
        {
        }

        public WpfHostForm(
            ILoggerOutput logger,
            string windowTitle,
            string panelHeader,
            int stressLogCount = 30000,
            bool autoRunStressTest = false,
            bool closeAfterStressTest = false)
        {
            _stressLogCount = stressLogCount > 0 ? stressLogCount : 30000;
            _autoRunStressTest = autoRunStressTest;
            _closeAfterStressTest = closeAfterStressTest;
            _logger = logger ?? LogManager.Factory.CreateLogger("Logger.WinForms.Demo.WpfHost");
            _panelHeader = string.IsNullOrWhiteSpace(panelHeader) ? "WPF 日志演示" : panelHeader;

            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Text = string.IsNullOrWhiteSpace(windowTitle) ? "Logger.Wpf 宿主窗体" : windowTitle;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(880, 560);
            ClientSize = new Size(1020, 680);
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
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
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
                Text = "写入 WPF 示例",
                UseVisualStyleBackColor = true
            };
            _sampleButton.Click += SampleButton_Click;

            _stressButton = new Button
            {
                AutoSize = true,
                Height = 34,
                Margin = new Padding(0, 0, 12, 0),
                Padding = new Padding(12, 0, 12, 0),
                Text = "WPF 压力测试",
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
                Text = "当前窗体展示的是封装在 Logger.WinForms 中的 WPF 日志控件。"
            };

            _summaryPanel = new StressSummaryPanel();
            _summaryPanel.UpdateSummary(StressTestSummary.CreateIdle("WPF 宿主控件压测", "点击“WPF 压力测试”开始执行。"));

            _wpfLogPanel = new WpfLogPanelControl
            {
                Dock = DockStyle.Fill,
                Header = _panelHeader,
                MaxLogEntries = Math.Max(_stressLogCount + 1000, 8000),
                Margin = new Padding(0)
            };
            _wpfLogPanel.Logger = _logger;

            toolbar.Controls.Add(_sampleButton, 0, 0);
            toolbar.Controls.Add(_stressButton, 1, 0);
            toolbar.Controls.Add(_statusLabel, 3, 0);

            rootLayout.Controls.Add(toolbar, 0, 0);
            rootLayout.Controls.Add(infoLabel, 0, 1);
            rootLayout.Controls.Add(_summaryPanel, 0, 2);
            rootLayout.Controls.Add(_wpfLogPanel, 0, 3);

            Controls.Add(rootLayout);

            Shown += WpfHostForm_Shown;
        }

        public bool? AutoTestSucceeded { get; private set; }

        private async void WpfHostForm_Shown(object sender, EventArgs e)
        {
            _logger.AddInfo("WPF 日志控件已经通过 Logger.WinForms 封装成功挂载。");
            _logger.AddInfo("当前宿主窗体绑定了独立的 ILoggerOutput，默认不会与其他 Demo 窗口共享日志。");
            _logger.AddInfo("这个窗体不会调用日志控件的方法，而是直接向 ILoggerOutput 写入。");
            _logger.AddInfo("点击“写入 WPF 示例”查看等级显示，点击“WPF 压力测试”执行批量写入。");
            WriteLevelSamples();
            UpdateStatus("WPF 宿主窗体已就绪");

            if (_autoRunStressTest)
            {
                await RunStressTestAsync();

                if (_closeAfterStressTest && !IsDisposed)
                {
                    Close();
                }
            }
        }

        private void SampleButton_Click(object sender, EventArgs e)
        {
            WriteLevelSamples();
            UpdateStatus("已写入一组 WPF 示例日志");
        }

        private async void StressButton_Click(object sender, EventArgs e)
        {
            await RunStressTestAsync();
        }

        private void WriteLevelSamples()
        {
            _logger.AddTrace("TRACE: WPF 控件已接入 WinForms 宿主。");
            _logger.AddDebug("DEBUG: 当前正在验证 WPF 宿主封装路径。");
            _logger.AddInfo("INFO: WinForms 与 WPF 日志控件可以并存运行。");
            _logger.AddSuccess("SUCCESS: Logger.Wpf 宿主窗体初始化完成。");
            _logger.AddWarning("WARN: 压力测试时会使用分批写入，避免界面假死。");
            _logger.AddError("ERROR: 这是演示错误日志，用于确认颜色和样式。");
            _logger.AddFatal("FATAL: 这是演示致命日志，用于确认高亮显示。\r\nFATAL: 这是演示致命日志，用于确认高亮显示。\r\nFATAL: 这是演示致命日志，用于确认高亮显示。\r\nFATAL: 这是演示致命日志，用于确认高亮显示。");
        }

        private async Task RunStressTestAsync()
        {
            if (_isStressRunning)
            {
                _logger.AddWarning("WPF 压力测试正在进行中，请稍候。");
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
                        "WPF 宿主控件压测",
                        _stressLogCount,
                        string.Format("正在写入 {0:N0} 条 WPF 压测日志。", _stressLogCount)));
                _logger.AddInfo(string.Format("开始 WPF 压力测试，本次计划写入 {0:N0} 条日志。", _stressLogCount));
                await Task.Run(() => ProduceStressLogs(_stressLogCount));

                stopwatch.Stop();
                _logger.AddSuccess(string.Format("WPF 压力测试完成，共写入 {0:N0} 条日志，耗时 {1} ms。", _stressLogCount, stopwatch.ElapsedMilliseconds));
                _summaryPanel.UpdateSummary(
                    StressTestSummary.CreateSuccess(
                        "WPF 宿主控件压测",
                        _stressLogCount,
                        stopwatch.ElapsedMilliseconds,
                        "封装后的 WPF 日志控件在 WinForms 宿主中完成了整轮批量写入。"));
                UpdateStatus(string.Format("WPF 压力测试完成，用时 {0} ms", stopwatch.ElapsedMilliseconds));
                AutoTestSucceeded = true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.AddError("WPF 压力测试失败：" + ex.Message);
                _summaryPanel.UpdateSummary(
                    StressTestSummary.CreateFailure(
                        "WPF 宿主控件压测",
                        _stressLogCount,
                        stopwatch.ElapsedMilliseconds,
                        ex.Message));
                UpdateStatus("WPF 压力测试失败");
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
                        PostStatus(string.Format("WPF 压力测试进行中：{0:N0}/{1:N0}", index, total));
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
                "[{0:N0}/{1:N0}] {2} 级别 WPF 压力日志，用于验证封装控件在 WinForms 中的滚动和写入表现。",
                index,
                total,
                level.ToString().ToUpperInvariant());
        }
    }
}
