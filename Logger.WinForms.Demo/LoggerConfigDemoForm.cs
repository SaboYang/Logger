using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Logger.Core;
using Logger.WinForms.Controls;

namespace Logger.WinForms.Demo
{
    public sealed class LoggerConfigDemoForm : Form
    {
        private const string ConfiguredLoggerName = "Logger.WinForms.Demo.ConfigDemo.Configured";
        private const string FallbackLoggerName = "Logger.WinForms.Demo.ConfigDemo.Fallback";

        private readonly ILoggerOutput _configuredLogger;
        private readonly ILoggerOutput _fallbackLogger;
        private readonly ILogFileSource _configuredFileSource;
        private readonly ILogFileSource _fallbackFileSource;
        private readonly LogPanelControl _configuredLogPanel;
        private readonly LogPanelControl _fallbackLogPanel;
        private readonly TextBox _configFilePathTextBox;
        private readonly Label _configStateLabel;
        private readonly TextBox _configuredPathTextBox;
        private readonly TextBox _fallbackPathTextBox;
        private readonly Label _statusLabel;
        private readonly Button _writeConfiguredButton;
        private readonly Button _writeFallbackButton;
        private readonly Button _writeBothButton;
        private readonly Button _refreshButton;
        private readonly CodeSamplePanel _codeSamplePanel;
        private int _sequence;

        public LoggerConfigDemoForm()
        {
            _configuredLogger = LogManager.GetLogger(ConfiguredLoggerName);
            _fallbackLogger = LogManager.GetLogger(FallbackLoggerName);
            _configuredFileSource = _configuredLogger as ILogFileSource;
            _fallbackFileSource = _fallbackLogger as ILogFileSource;

            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Text = "Logger.config 演示";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1220, 760);
            ClientSize = new Size(1420, 860);
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
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            _writeConfiguredButton = CreateToolbarButton("写入命名 logger");
            _writeConfiguredButton.Click += WriteConfiguredButton_Click;

            _writeFallbackButton = CreateToolbarButton("写入默认 logger");
            _writeFallbackButton.Click += WriteFallbackButton_Click;

            _writeBothButton = CreateToolbarButton("同时写入");
            _writeBothButton.Click += WriteBothButton_Click;

            _refreshButton = CreateToolbarButton("刷新摘要");
            _refreshButton.Click += RefreshButton_Click;

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Margin = new Padding(0, 8, 0, 0),
                ForeColor = Color.FromArgb(29, 78, 216),
                TextAlign = ContentAlignment.MiddleRight,
                Text = "准备就绪"
            };

            toolbar.Controls.Add(_writeConfiguredButton, 0, 0);
            toolbar.Controls.Add(_writeFallbackButton, 1, 0);
            toolbar.Controls.Add(_writeBothButton, 2, 0);
            toolbar.Controls.Add(_refreshButton, 3, 0);
            toolbar.Controls.Add(_statusLabel, 4, 0);

            GroupBox summaryGroup = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12),
                Text = "配置解析结果",
                Margin = new Padding(0, 0, 0, 10)
            };

            TableLayoutPanel summaryLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 8
            };

            Label configFileLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6),
                ForeColor = Color.FromArgb(55, 65, 81),
                Text = "本地配置文件路径"
            };

            _configFilePathTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                ReadOnly = true,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 8),
                Text = GetConfigurationFilePath()
            };

            _configStateLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8),
                ForeColor = Color.FromArgb(75, 85, 99)
            };

            Label configuredLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6),
                ForeColor = Color.FromArgb(55, 65, 81),
                Text = "命名 logger 输出路径"
            };

            _configuredPathTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                ReadOnly = true,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 8)
            };

            Label fallbackLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6),
                ForeColor = Color.FromArgb(55, 65, 81),
                Text = "默认回退 logger 输出路径"
            };

            _fallbackPathTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                ReadOnly = true,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 8)
            };

            Label noteLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(107, 114, 128),
                Text = "命名 logger 命中本地 Logger.config 的同名 <logger> 节点；未命中时回退到 <default>。"
            };

            summaryLayout.Controls.Add(configFileLabel, 0, 0);
            summaryLayout.Controls.Add(_configFilePathTextBox, 0, 1);
            summaryLayout.Controls.Add(_configStateLabel, 0, 2);
            summaryLayout.Controls.Add(configuredLabel, 0, 3);
            summaryLayout.Controls.Add(_configuredPathTextBox, 0, 4);
            summaryLayout.Controls.Add(fallbackLabel, 0, 5);
            summaryLayout.Controls.Add(_fallbackPathTextBox, 0, 6);
            summaryLayout.Controls.Add(noteLabel, 0, 7);
            summaryGroup.Controls.Add(summaryLayout);

            Label infoLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = "左侧对照两个 logger：一个命中本地配置，一个没有对应名称而回退到默认配置。右侧展示可直接复制的 Logger.config 和调用代码。"
            };

            TableLayoutPanel contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));

            TableLayoutPanel logPanelsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            logPanelsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            logPanelsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            _configuredLogPanel = new LogPanelControl
            {
                Dock = DockStyle.Fill,
                Header = "命名配置 logger",
                Logger = _configuredLogger,
                MaxLogEntries = 25000,
                Margin = new Padding(0, 0, 0, 8)
            };

            _fallbackLogPanel = new LogPanelControl
            {
                Dock = DockStyle.Fill,
                Header = "默认回退 logger",
                Logger = _fallbackLogger,
                MaxLogEntries = 25000,
                Margin = new Padding(0, 8, 0, 0)
            };

            logPanelsLayout.Controls.Add(_configuredLogPanel, 0, 0);
            logPanelsLayout.Controls.Add(_fallbackLogPanel, 0, 1);

            _codeSamplePanel = new CodeSamplePanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 0, 0, 0),
                CodeText = BuildCodeSampleText()
            };

            contentLayout.Controls.Add(logPanelsLayout, 0, 0);
            contentLayout.Controls.Add(_codeSamplePanel, 1, 0);

            rootLayout.Controls.Add(toolbar, 0, 0);
            rootLayout.Controls.Add(summaryGroup, 0, 1);
            rootLayout.Controls.Add(infoLabel, 0, 2);
            rootLayout.Controls.Add(contentLayout, 0, 3);

            Controls.Add(rootLayout);

            Shown += LoggerConfigDemoForm_Shown;
        }

        private void LoggerConfigDemoForm_Shown(object sender, EventArgs e)
        {
            _configuredLogger.Info("命名 logger 演示已启动。");
            _configuredLogger.Info("如果本地 Logger.config 包含同名配置，这条日志会进入命名路径。");

            _fallbackLogger.Info("默认回退 logger 演示已启动。");
            _fallbackLogger.Info("如果找不到同名配置，这个 logger 会回退到 <default>。");

            RefreshSummary();
            UpdateStatus("演示已就绪");
        }

        private void WriteConfiguredButton_Click(object sender, EventArgs e)
        {
            WriteConfiguredSample();
            UpdateStatus("已写入命名 logger");
        }

        private void WriteFallbackButton_Click(object sender, EventArgs e)
        {
            WriteFallbackSample();
            UpdateStatus("已写入默认回退 logger");
        }

        private void WriteBothButton_Click(object sender, EventArgs e)
        {
            WriteConfiguredSample();
            WriteFallbackSample();
            UpdateStatus("已同时写入命名与默认 logger");
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshSummary();
            UpdateStatus("摘要已刷新");
        }

        private void WriteConfiguredSample()
        {
            _sequence++;
            _configuredLogger.Info(string.Format("命名 logger 第 {0} 次写入。", _sequence));
            _configuredLogger.Success("这条日志会落到 Logger.config 中的命名配置路径。");
            _configuredLogger.Warning("示例：rollingMode 和 rollingRetentionDays 都可以在本地文件中覆盖。");
        }

        private void WriteFallbackSample()
        {
            _sequence++;
            _fallbackLogger.Info(string.Format("默认回退 logger 第 {0} 次写入。", _sequence));
            _fallbackLogger.Warning("这个 logger 没有同名配置，应该回退到 <default>。");
            _fallbackLogger.Error("示例：如果删除本地 Logger.config，两个 logger 都会走默认配置。");
        }

        private void RefreshSummary()
        {
            _configFilePathTextBox.Text = GetConfigurationFilePath();

            if (File.Exists(GetConfigurationFilePath()))
            {
                _configStateLabel.Text = "状态：已找到本地 Logger.config。命名 logger 命中同名节点，未命中时回退到 <default>。";
            }
            else
            {
                _configStateLabel.Text = "状态：未找到本地 Logger.config。当前两个 logger 都会使用代码里的默认配置。";
            }

            _configuredPathTextBox.Text = GetFilePathText(_configuredFileSource);
            _fallbackPathTextBox.Text = GetFilePathText(_fallbackFileSource);
        }

        private static string GetFilePathText(ILogFileSource fileSource)
        {
            if (fileSource == null || !fileSource.IsFileOutputEnabled)
            {
                return "文件输出未启用";
            }

            string path = fileSource.LogFilePath;
            return string.IsNullOrWhiteSpace(path) ? "未生成路径" : path;
        }

        private static string GetConfigurationFilePath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Logger.config");
        }

        private static string BuildCodeSampleText()
        {
            return
@"<?xml version=""1.0"" encoding=""utf-8""?>
<loggerConfiguration>
  <default logRootDirectoryPath=""Logs"" rollingMode=""DayWithRetention"" rollingRetentionDays=""30"" />
  <logger name=""Logger.WinForms.Demo.ConfigDemo.Configured""
          logRootDirectoryPath=""Logs\ConfigDemo\Named""
          rollingMode=""DayWithRetention""
          rollingRetentionDays=""7"" />
</loggerConfiguration>

using Logger.Core;
using Logger.WinForms.Controls;

ILoggerOutput configuredLogger =
    LogManager.GetLogger(""Logger.WinForms.Demo.ConfigDemo.Configured"");

ILoggerOutput fallbackLogger =
    LogManager.GetLogger(""Logger.WinForms.Demo.ConfigDemo.Fallback"");

configuredPanel.Logger = configuredLogger;
fallbackPanel.Logger = fallbackLogger;

configuredLogger.Info(""命名 logger 命中本地配置"");
fallbackLogger.Warning(""未命中名称，回退到 default"");";
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

        private void UpdateStatus(string text)
        {
            _statusLabel.Text = text;
        }
    }
}
