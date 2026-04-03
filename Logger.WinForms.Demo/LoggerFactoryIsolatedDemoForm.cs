using System;
using System.Drawing;
using System.Windows.Forms;
using Logger.Core;
using Logger.WinForms.Controls;

namespace Logger.WinForms.Demo
{
    public class LoggerFactoryIsolatedDemoForm : Form
    {
        private readonly ILoggerOutput _serviceLogger;
        private readonly ILoggerOutput _factoryLogger;
        private readonly LogPanelControl _serviceLogPanel;
        private readonly LogPanelControl _factoryLogPanel;
        private readonly Button _writeServiceButton;
        private readonly Button _writeFactoryButton;
        private readonly Button _writeBothButton;
        private readonly Button _clearButton;
        private readonly Label _statusLabel;
        private int _serviceSequence;
        private int _factorySequence;

        public LoggerFactoryIsolatedDemoForm()
        {
            _serviceLogger = LogManager.GetLogger("Logger.WinForms.Demo.Factory.Service." + Guid.NewGuid().ToString("N"));
            _factoryLogger = LogManager.Factory.CreateLogger("Logger.WinForms.Demo.Factory.Standalone");

            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Text = "ILoggerFactory / LoggerService Demo";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1180, 700);
            ClientSize = new Size(1320, 820);
            BackColor = Color.FromArgb(245, 247, 250);

            TableLayoutPanel rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
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

            _writeServiceButton = CreateToolbarButton("写入服务日志");
            _writeServiceButton.Click += WriteServiceButton_Click;

            _writeFactoryButton = CreateToolbarButton("写入工厂日志");
            _writeFactoryButton.Click += WriteFactoryButton_Click;

            _writeBothButton = CreateToolbarButton("同时写入两边");
            _writeBothButton.Click += WriteBothButton_Click;

            _clearButton = CreateToolbarButton("清空演示日志");
            _clearButton.Click += ClearButton_Click;

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
                Text = "左侧通过 LoggerService 获取接口，右侧通过 ILoggerFactory 创建接口。本 Demo 中两组日志源都是独立实例，不会与其他窗口共享。"
            };

            TableLayoutPanel panelLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            panelLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panelLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            _serviceLogPanel = new LogPanelControl
            {
                Dock = DockStyle.Fill,
                Header = "LoggerService 日志源",
                MaxLogEntries = 30000,
                Margin = new Padding(0, 0, 6, 0),
                Logger = _serviceLogger
            };

            _factoryLogPanel = new LogPanelControl
            {
                Dock = DockStyle.Fill,
                Header = "ILoggerFactory 日志源",
                MaxLogEntries = 10000,
                Margin = new Padding(6, 0, 0, 0),
                Logger = _factoryLogger
            };

            panelLayout.Controls.Add(_serviceLogPanel, 0, 0);
            panelLayout.Controls.Add(_factoryLogPanel, 1, 0);

            toolbar.Controls.Add(_writeServiceButton, 0, 0);
            toolbar.Controls.Add(_writeFactoryButton, 1, 0);
            toolbar.Controls.Add(_writeBothButton, 2, 0);
            toolbar.Controls.Add(_clearButton, 3, 0);
            toolbar.Controls.Add(_statusLabel, 4, 0);

            rootLayout.Controls.Add(toolbar, 0, 0);
            rootLayout.Controls.Add(infoLabel, 0, 1);
            rootLayout.Controls.Add(panelLayout, 0, 2);

            Controls.Add(rootLayout);

            Shown += LoggerFactoryIsolatedDemoForm_Shown;
        }

        private void LoggerFactoryIsolatedDemoForm_Shown(object sender, EventArgs e)
        {
            _serviceLogger.AddInfo("左侧日志接口通过 LoggerService 获取。");
            _serviceLogger.AddInfo("这里使用了独立名称，因此不会与其他 Demo 窗口共享。");

            _factoryLogger.AddInfo("右侧日志接口通过 ILoggerFactory.CreateLogger 创建。");
            _factoryLogger.AddInfo("它同样只服务当前窗体。");

            UpdateStatus("Demo 已就绪");
        }

        private void WriteServiceButton_Click(object sender, EventArgs e)
        {
            _serviceSequence++;
            _serviceLogger.AddInfo(string.Format("服务日志源写入第 {0} 条演示消息。", _serviceSequence));
            UpdateStatus("已写入一条服务日志");
        }

        private void WriteFactoryButton_Click(object sender, EventArgs e)
        {
            _factorySequence++;
            _factoryLogger.AddDebug(string.Format("工厂日志源写入第 {0} 条演示消息。", _factorySequence));
            UpdateStatus("已写入一条工厂日志");
        }

        private void WriteBothButton_Click(object sender, EventArgs e)
        {
            _serviceSequence++;
            _factorySequence++;

            _serviceLogger.AddSuccess(string.Format("服务日志源写入第 {0} 条联动消息。", _serviceSequence));
            _factoryLogger.AddWarning(string.Format("工厂日志源写入第 {0} 条联动消息。", _factorySequence));

            UpdateStatus("两边都已写入一条日志");
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            _serviceLogPanel.ClearLogs();
            _factoryLogPanel.ClearLogs();
            _serviceSequence = 0;
            _factorySequence = 0;
            UpdateStatus("已清空演示日志");
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
    }
}
