using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Logger.Core;
using Logger.Core.Models;
using Logger.WinForms.Controls;

namespace Logger.WinForms.Demo
{
    public sealed class FileLogDemoForm : Form
    {
        private readonly ILoggerOutput _logger;
        private readonly ILogFileSource _fileSource;
        private readonly ILogSessionSource _sessionSource;
        private readonly LogPanelControl _logPanel;
        private readonly TextBox _filePathTextBox;
        private readonly TextBox _filePreviewTextBox;
        private readonly CodeSamplePanel _codeSamplePanel;
        private readonly Label _sessionInfoLabel;
        private readonly Label _statusLabel;
        private readonly Button _writeSampleButton;
        private readonly Button _writeBatchButton;
        private readonly Button _refreshPreviewButton;
        private readonly Timer _refreshTimer;
        private int _sequence;

        public FileLogDemoForm()
        {
            _logger = LogManager.Factory.CreateLogger("Logger.WinForms.Demo.File");
            _fileSource = _logger as ILogFileSource;
            _sessionSource = _logger as ILogSessionSource;

            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Text = "本地日志文件 Demo";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1200, 760);
            ClientSize = new Size(1380, 880);
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
                ColumnCount = 5,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _writeSampleButton = CreateToolbarButton("写入示例日志");
            _writeSampleButton.Click += WriteSampleButton_Click;

            _writeBatchButton = CreateToolbarButton("批量写入 100 条");
            _writeBatchButton.Click += WriteBatchButton_Click;

            _refreshPreviewButton = CreateToolbarButton("刷新文件预览");
            _refreshPreviewButton.Click += RefreshPreviewButton_Click;

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Margin = new Padding(0, 8, 0, 0),
                ForeColor = Color.FromArgb(29, 78, 216),
                TextAlign = ContentAlignment.MiddleRight,
                Text = "准备就绪"
            };

            toolbar.Controls.Add(_writeSampleButton, 0, 0);
            toolbar.Controls.Add(_writeBatchButton, 1, 0);
            toolbar.Controls.Add(_refreshPreviewButton, 2, 0);
            toolbar.Controls.Add(_statusLabel, 4, 0);

            GroupBox summaryGroup = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12),
                Text = "文件输出信息",
                Margin = new Padding(0, 0, 0, 10)
            };

            TableLayoutPanel summaryLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 3
            };

            Label pathLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6),
                ForeColor = Color.FromArgb(55, 65, 81),
                Text = "日志文件路径"
            };

            _filePathTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                ReadOnly = true,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = Color.White,
                Text = _fileSource != null ? _fileSource.LogFilePath : string.Empty
            };

            _sessionInfoLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = BuildSessionInfoText()
            };

            summaryLayout.Controls.Add(pathLabel, 0, 0);
            summaryLayout.Controls.Add(_filePathTextBox, 0, 1);
            summaryLayout.Controls.Add(_sessionInfoLabel, 0, 2);
            summaryGroup.Controls.Add(summaryLayout);

            Label infoLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = "左侧显示绑定到控件的 ILoggerOutput，右侧上方显示接入代码，下方预览当前日志文件内容。"
            };

            TableLayoutPanel contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));

            _logPanel = new LogPanelControl
            {
                Dock = DockStyle.Fill,
                Header = "日志接口输出",
                Logger = _logger,
                MaxLogEntries = 30000,
                Margin = new Padding(0, 0, 8, 0)
            };

            _codeSamplePanel = new CodeSamplePanel
            {
                Dock = DockStyle.Fill,
                Height = 230,
                Margin = new Padding(0, 0, 0, 8),
                CodeText =
@"using Logger.Core;
using Logger.WinForms.Controls;

ILoggerOutput logger = LogManager.Factory.CreateLogger(""MyApp.File"");

var logPanel = new LogPanelControl
{
    Dock = DockStyle.Fill,
    Header = ""文件日志输出"",
    Logger = logger
};

Controls.Add(logPanel);

logger.AddInfo(""write to ui and file"");
logger.AddError(""line1\r\nline2"");

ILogFileSource fileSource = logger as ILogFileSource;
string path = fileSource != null ? fileSource.LogFilePath : null;"
            };

            GroupBox previewGroup = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "文件内容预览",
                Padding = new Padding(10),
                Margin = new Padding(0)
            };

            _filePreviewTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                ReadOnly = true,
                Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
            previewGroup.Controls.Add(_filePreviewTextBox);

            TableLayoutPanel rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(8, 0, 0, 0)
            };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 230F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rightLayout.Controls.Add(_codeSamplePanel, 0, 0);
            rightLayout.Controls.Add(previewGroup, 0, 1);

            contentLayout.Controls.Add(_logPanel, 0, 0);
            contentLayout.Controls.Add(rightLayout, 1, 0);

            rootLayout.Controls.Add(toolbar, 0, 0);
            rootLayout.Controls.Add(summaryGroup, 0, 1);
            rootLayout.Controls.Add(infoLabel, 0, 2);
            rootLayout.Controls.Add(contentLayout, 0, 3);

            Controls.Add(rootLayout);

            _refreshTimer = new Timer
            {
                Interval = 250
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            Shown += FileLogDemoForm_Shown;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer.Tick -= RefreshTimer_Tick;
                _refreshTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void FileLogDemoForm_Shown(object sender, EventArgs e)
        {
            _logger.AddInfo("文件日志 Demo 已启动。");
            _logger.AddInfo("当前窗体通过 ILoggerOutput 写日志，同时将本场日志异步写入本地文件。");
            if (_fileSource != null && _fileSource.IsFileOutputEnabled)
            {
                _logger.AddSuccess("本地文件输出已启用。");
                _logger.AddInfo("日志文件: " + _fileSource.LogFilePath);
            }

            WriteSampleEntries();
            SchedulePreviewRefresh();
            UpdateStatus("已加载文件日志示例");
        }

        private async void WriteBatchButton_Click(object sender, EventArgs e)
        {
            SetButtonsEnabled(false);
            UpdateStatus("正在批量写入日志...");

            try
            {
                await Task.Run(() => WriteBatchEntries(100));
                UpdateStatus("已批量写入 100 条日志");
            }
            finally
            {
                SetButtonsEnabled(true);
                SchedulePreviewRefresh();
            }
        }

        private void WriteSampleButton_Click(object sender, EventArgs e)
        {
            WriteSampleEntries();
            SchedulePreviewRefresh();
            UpdateStatus("已写入一组文件日志示例");
        }

        private void RefreshPreviewButton_Click(object sender, EventArgs e)
        {
            RefreshPreviewNow();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            _refreshTimer.Stop();
            RefreshPreviewNow();
        }

        private void WriteSampleEntries()
        {
            _sequence++;
            _logger.AddTrace(string.Format("TRACE: 文件日志示例第 {0} 组开始。", _sequence));
            _logger.AddDebug(string.Format("DEBUG: 当前会话累计写入 {0} 条。", _sessionSource != null ? _sessionSource.SessionEntryCount : 0));
            _logger.AddInfo("INFO: 这条日志会同时进入 UI 和本地日志文件。");
            _logger.AddSuccess("SUCCESS: 文件输出链路正常。");
            _logger.AddWarning("WARN: 预览读取的是当前日志文件落盘后的内容。");
            _logger.AddError("ERROR: 这是文件日志演示用错误项。");
        }

        private void WriteBatchEntries(int count)
        {
            List<LogEntry> entries = new List<LogEntry>(count);
            Random random = new Random();

            for (int index = 1; index <= count; index++)
            {
                int current = System.Threading.Interlocked.Increment(ref _sequence);
                LogLevel level = GetLevelByIndex(index);
                entries.Add(new LogEntry(
                    DateTime.Now,
                    level,
                    string.Format(
                        "[{0:0000}] {1} 写入本地文件测试，随机值 {2}.",
                        current,
                        level.ToString().ToUpperInvariant(),
                        random.Next(1000, 9999))));
            }

            _logger.AddLogs(entries);
        }

        private void RefreshPreviewNow()
        {
            _sessionInfoLabel.Text = BuildSessionInfoText();

            if (_fileSource == null || !_fileSource.IsFileOutputEnabled || string.IsNullOrWhiteSpace(_fileSource.LogFilePath))
            {
                _filePreviewTextBox.Text = "当前日志实例未启用文件输出。";
                return;
            }

            try
            {
                string filePath = _fileSource.LogFilePath;
                if (!File.Exists(filePath))
                {
                    _filePreviewTextBox.Text = "日志文件尚未生成，请先写入日志。";
                    return;
                }

                _filePreviewTextBox.Text = File.ReadAllText(filePath);
                _filePreviewTextBox.SelectionStart = _filePreviewTextBox.TextLength;
                _filePreviewTextBox.ScrollToCaret();
                UpdateStatus("文件预览已刷新");
            }
            catch (Exception ex)
            {
                _filePreviewTextBox.Text = "读取日志文件失败: " + ex.Message;
                UpdateStatus("刷新文件预览失败");
            }
        }

        private void SchedulePreviewRefresh()
        {
            _refreshTimer.Stop();
            _refreshTimer.Start();
        }

        private string BuildSessionInfoText()
        {
            if (_sessionSource == null)
            {
                return "当前日志实例不支持会话信息。";
            }

            return string.Format(
                "SessionId: {0}    StartedAt: {1:yyyy-MM-dd HH:mm:ss.fff}    Entries: {2}",
                _sessionSource.SessionId.ToString("N"),
                _sessionSource.SessionStartedAt,
                _sessionSource.SessionEntryCount);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _writeSampleButton.Enabled = enabled;
            _writeBatchButton.Enabled = enabled;
            _refreshPreviewButton.Enabled = enabled;
        }

        private void UpdateStatus(string message)
        {
            _statusLabel.Text = message;
        }

        private static Button CreateToolbarButton(string text)
        {
            return new Button
            {
                AutoSize = true,
                Height = 34,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(12, 0, 12, 0),
                Text = text,
                UseVisualStyleBackColor = true
            };
        }

        private static LogLevel GetLevelByIndex(int index)
        {
            switch (index % 7)
            {
                case 1:
                    return LogLevel.Trace;
                case 2:
                    return LogLevel.Debug;
                case 3:
                    return LogLevel.Info;
                case 4:
                    return LogLevel.Success;
                case 5:
                    return LogLevel.Warn;
                case 6:
                    return LogLevel.Error;
                default:
                    return LogLevel.Fatal;
            }
        }
    }
}
