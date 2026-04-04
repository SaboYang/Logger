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
    public sealed class StorageBackendDemoForm : Form
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

        private readonly ComboBox _backendSelector;
        private readonly Button _recreateLoggerButton;
        private readonly Button _writeSampleButton;
        private readonly Button _writeBatchButton;
        private readonly Button _refreshPreviewButton;
        private readonly Label _statusLabel;
        private readonly Label _descriptionLabel;
        private readonly Label _targetLabel;
        private readonly Label _sessionLabel;
        private readonly CodeSamplePanel _codeSamplePanel;
        private readonly GroupBox _previewGroup;
        private readonly TextBox _filePreviewTextBox;
        private readonly DataGridView _tablePreviewGrid;
        private readonly LogPanelControl _logPanel;
        private readonly Timer _previewTimer;

        private ILoggerOutput _logger;
        private ILogFileSource _fileSource;
        private ILogSessionSource _sessionSource;
        private DemoTableLogStorageBackend _tableBackend;
        private int _sequence;
        private bool _isWritingBatch;

        public StorageBackendDemoForm()
        {
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Text = "可扩展存储 Demo";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1260, 760);
            ClientSize = new Size(1440, 860);
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
                ColumnCount = 7,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Label backendLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 8, 8, 0),
                ForeColor = Color.FromArgb(55, 65, 81),
                Text = "存储后端："
            };

            _backendSelector = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 220,
                Margin = new Padding(0, 2, 12, 0)
            };
            _backendSelector.Items.Add("文本文件后端");
            _backendSelector.Items.Add("CSV 文件后端");
            _backendSelector.Items.Add("自定义表后端（模拟数据库）");
            _backendSelector.SelectedIndex = 0;

            _recreateLoggerButton = CreateToolbarButton("重建日志接口");
            _recreateLoggerButton.Click += RecreateLoggerButton_Click;

            _writeSampleButton = CreateToolbarButton("写入示例");
            _writeSampleButton.Click += WriteSampleButton_Click;

            _writeBatchButton = CreateToolbarButton("批量写入 500 条");
            _writeBatchButton.Click += WriteBatchButton_Click;

            _refreshPreviewButton = CreateToolbarButton("刷新预览");
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

            toolbar.Controls.Add(backendLabel, 0, 0);
            toolbar.Controls.Add(_backendSelector, 1, 0);
            toolbar.Controls.Add(_recreateLoggerButton, 2, 0);
            toolbar.Controls.Add(_writeSampleButton, 3, 0);
            toolbar.Controls.Add(_writeBatchButton, 4, 0);
            toolbar.Controls.Add(_refreshPreviewButton, 5, 0);
            toolbar.Controls.Add(_statusLabel, 6, 0);

            _descriptionLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = "这个窗体演示 ILoggerOutput 绑定 UI 后，底层可以替换不同存储后端，业务写法不变。"
            };

            GroupBox summaryGroup = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12),
                Text = "当前后端信息",
                Margin = new Padding(0, 0, 0, 10)
            };

            TableLayoutPanel summaryLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 3
            };

            _targetLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6),
                ForeColor = Color.FromArgb(31, 41, 55)
            };

            _sessionLabel = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6),
                ForeColor = Color.FromArgb(75, 85, 99)
            };

            Label hintLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(107, 114, 128),
                Text = "右上角显示当前后端对应的接入代码。文本和 CSV 后端会落盘到本地文件，自定义后端会在右下角表格中模拟数据库记录。"
            };

            summaryLayout.Controls.Add(_targetLabel, 0, 0);
            summaryLayout.Controls.Add(_sessionLabel, 0, 1);
            summaryLayout.Controls.Add(hintLabel, 0, 2);
            summaryGroup.Controls.Add(summaryLayout);

            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.None,
                SplitterDistance = 760,
                BackColor = Color.FromArgb(229, 231, 235)
            };

            _logPanel = new LogPanelControl
            {
                Dock = DockStyle.Fill,
                Header = "可扩展存储日志输出",
                MaxLogEntries = 30000,
                Margin = new Padding(0)
            };

            TableLayoutPanel rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0)
            };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _codeSamplePanel = new CodeSamplePanel
            {
                Dock = DockStyle.Fill,
                Height = 250,
                Margin = new Padding(0, 0, 0, 8)
            };

            _previewGroup = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "后端预览",
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
                Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Visible = false
            };

            _tablePreviewGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Visible = false
            };
            _tablePreviewGrid.Columns.Add(CreateTextColumn("Timestamp", "时间", 165));
            _tablePreviewGrid.Columns.Add(CreateTextColumn("Level", "等级", 80));
            _tablePreviewGrid.Columns.Add(CreateTextColumn("LoggerName", "Logger", 260));
            _tablePreviewGrid.Columns.Add(CreateTextColumn("Message", "消息", 520));

            _previewGroup.Controls.Add(_filePreviewTextBox);
            _previewGroup.Controls.Add(_tablePreviewGrid);

            rightLayout.Controls.Add(_codeSamplePanel, 0, 0);
            rightLayout.Controls.Add(_previewGroup, 0, 1);

            splitContainer.Panel1.Controls.Add(_logPanel);
            splitContainer.Panel2.Controls.Add(rightLayout);

            rootLayout.Controls.Add(toolbar, 0, 0);
            rootLayout.Controls.Add(_descriptionLabel, 0, 1);
            rootLayout.Controls.Add(summaryGroup, 0, 2);
            rootLayout.Controls.Add(splitContainer, 0, 3);

            Controls.Add(rootLayout);

            _previewTimer = new Timer
            {
                Interval = 250
            };
            _previewTimer.Tick += PreviewTimer_Tick;

            Shown += StorageBackendDemoForm_Shown;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _previewTimer.Tick -= PreviewTimer_Tick;
                _previewTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void StorageBackendDemoForm_Shown(object sender, EventArgs e)
        {
            RecreateLogger();
        }

        private void RecreateLoggerButton_Click(object sender, EventArgs e)
        {
            RecreateLogger();
        }

        private void WriteSampleButton_Click(object sender, EventArgs e)
        {
            WriteSampleEntries();
            SchedulePreviewRefresh();
            UpdateStatus("已写入一组示例日志");
        }

        private async void WriteBatchButton_Click(object sender, EventArgs e)
        {
            if (_isWritingBatch || _logger == null)
            {
                return;
            }

            _isWritingBatch = true;
            SetToolbarState(false);
            UpdateStatus("正在批量写入 500 条日志...");

            try
            {
                await Task.Run(() => WriteBatchEntries(500));
                UpdateStatus("批量写入完成");
            }
            finally
            {
                _isWritingBatch = false;
                SetToolbarState(true);
                SchedulePreviewRefresh();
            }
        }

        private void RefreshPreviewButton_Click(object sender, EventArgs e)
        {
            RefreshPreviewNow();
        }

        private void PreviewTimer_Tick(object sender, EventArgs e)
        {
            _previewTimer.Stop();
            RefreshPreviewNow();
        }

        private void RecreateLogger()
        {
            StorageBackendKind backendKind = GetSelectedBackendKind();
            string loggerName = string.Format(
                "Logger.WinForms.Demo.Storage.{0}.{1}",
                backendKind,
                Guid.NewGuid().ToString("N"));

            _logger = CreateLoggerForBackend(backendKind, loggerName);
            _fileSource = _logger as ILogFileSource;
            _sessionSource = _logger as ILogSessionSource;
            _logPanel.Logger = _logger;

            UpdateBackendInfo(backendKind);
            UpdateCodeSample(backendKind);
            WriteStartupEntries(backendKind);
            SchedulePreviewRefresh();
            UpdateStatus("已创建新的日志接口实例");
        }

        private ILoggerOutput CreateLoggerForBackend(StorageBackendKind backendKind, string loggerName)
        {
            string rootDirectory = Path.Combine(AppContext.BaseDirectory, "StorageDemo");

            switch (backendKind)
            {
                case StorageBackendKind.CsvFile:
                    _tableBackend = null;
                    return new LogStoreLoggerFactory(
                        new CsvFileLogStorageBackendFactory(
                            Path.Combine(rootDirectory, "Csv")),
                        LogLevel.Trace).CreateLogger(loggerName);

                case StorageBackendKind.SimulatedDatabase:
                    DemoTableLogStorageBackendFactory tableFactory = new DemoTableLogStorageBackendFactory();
                    ILoggerOutput dbLogger = new LogStoreLoggerFactory(tableFactory, LogLevel.Trace).CreateLogger(loggerName);
                    _tableBackend = tableFactory.CurrentBackend;
                    return dbLogger;

                default:
                    _tableBackend = null;
                    return new LogStoreLoggerFactory(
                        new TextFileLogStorageBackendFactory(
                            Path.Combine(rootDirectory, "Text")),
                        LogLevel.Trace).CreateLogger(loggerName);
            }
        }

        private void UpdateBackendInfo(StorageBackendKind backendKind)
        {
            _descriptionLabel.Text = GetDescription(backendKind);

            if (_sessionSource != null)
            {
                _sessionLabel.Text = string.Format(
                    "SessionId: {0}    StartedAt: {1:yyyy-MM-dd HH:mm:ss.fff}    Entries: {2}",
                    _sessionSource.SessionId.ToString("N"),
                    _sessionSource.SessionStartedAt,
                    _sessionSource.SessionEntryCount);
            }
            else
            {
                _sessionLabel.Text = "当前日志接口未提供会话信息。";
            }

            if (_fileSource != null && _fileSource.IsFileOutputEnabled && !string.IsNullOrWhiteSpace(_fileSource.LogFilePath))
            {
                _targetLabel.Text = "输出目标: " + _fileSource.LogFilePath;
                _previewGroup.Text = "文件内容预览";
                _filePreviewTextBox.Visible = true;
                _tablePreviewGrid.Visible = false;
                return;
            }

            _targetLabel.Text = "输出目标: 模拟数据库表（内存预览）";
            _previewGroup.Text = "表记录预览";
            _filePreviewTextBox.Visible = false;
            _tablePreviewGrid.Visible = true;
        }

        private void UpdateCodeSample(StorageBackendKind backendKind)
        {
            _codeSamplePanel.CodeText = BuildCodeSample(backendKind);
        }

        private void WriteStartupEntries(StorageBackendKind backendKind)
        {
            if (_logger == null)
            {
                return;
            }

            _sequence = 0;
            _logger.Info("已创建新的 ILoggerOutput，并绑定到日志控件。");
            _logger.Info("当前存储后端: " + GetBackendName(backendKind));
            _logger.Info("业务层只向 ILoggerOutput 写日志，不直接调用控件显示方法。");

            if (_fileSource != null && _fileSource.IsFileOutputEnabled && !string.IsNullOrWhiteSpace(_fileSource.LogFilePath))
            {
                _logger.Success("当前后端会把日志异步写入本地文件。");
                _logger.Info("输出文件: " + _fileSource.LogFilePath);
            }
            else
            {
                _logger.Success("当前后端是自定义实现，右侧表格预览其接收到的批量写入结果。");
                _logger.Info("把这个自定义后端替换成 SQL Server、SQLite 或其他数据库实现即可接入真实数据库。");
            }
        }

        private void WriteSampleEntries()
        {
            if (_logger == null)
            {
                return;
            }

            _sequence++;
            _logger.Trace(string.Format("TRACE: 第 {0} 组示例日志开始。", _sequence));
            _logger.Debug("DEBUG: 当前窗体仍然只依赖 ILoggerOutput 接口。");
            _logger.Info("INFO: 可以随时切换到 CSV、文本文件或自定义后端。");
            _logger.Success("SUCCESS: 存储后端已经解耦，UI 绑定方式没有变化。");
            _logger.Warning("WARN: 如果换成真实数据库，建议在后端内部做批量插入。");
            _logger.Error("ERROR: 这是示例错误日志，用于验证多后端写入。");
            _logger.Fatal("FATAL: 这是示例致命日志，用于验证颜色和高亮。");
        }

        private void WriteBatchEntries(int count)
        {
            if (_logger == null)
            {
                return;
            }

            List<LogEntry> batch = new List<LogEntry>(count);

            for (int index = 1; index <= count; index++)
            {
                LogLevel level = DemoLevels[(index - 1) % DemoLevels.Length];
                batch.Add(new LogEntry(
                    DateTime.Now,
                    level,
                    string.Format(
                        "[{0:0000}] {1} 后端批量写入示例，说明 UI 可以保持不变，只有存储实现被替换。",
                        index,
                        level.ToString().ToUpperInvariant())));
            }

            _logger.AddLogs(batch);
        }

        private void RefreshPreviewNow()
        {
            if (_filePreviewTextBox.Visible)
            {
                RefreshFilePreview();
            }
            else
            {
                RefreshTablePreview();
            }

            if (_sessionSource != null)
            {
                _sessionLabel.Text = string.Format(
                    "SessionId: {0}    StartedAt: {1:yyyy-MM-dd HH:mm:ss.fff}    Entries: {2}",
                    _sessionSource.SessionId.ToString("N"),
                    _sessionSource.SessionStartedAt,
                    _sessionSource.SessionEntryCount);
            }
        }

        private void RefreshFilePreview()
        {
            if (_fileSource == null || !_fileSource.IsFileOutputEnabled || string.IsNullOrWhiteSpace(_fileSource.LogFilePath))
            {
                _filePreviewTextBox.Text = "当前日志接口没有文件输出。";
                return;
            }

            string filePath = _fileSource.LogFilePath;
            if (!File.Exists(filePath))
            {
                _filePreviewTextBox.Text = "日志文件尚未生成，请先写入日志。";
                return;
            }

            string text = File.ReadAllText(filePath);
            const int maxPreviewLength = 40000;
            if (text.Length > maxPreviewLength)
            {
                text = text.Substring(text.Length - maxPreviewLength, maxPreviewLength);
            }

            _filePreviewTextBox.Text = text;
            _filePreviewTextBox.SelectionStart = _filePreviewTextBox.TextLength;
            _filePreviewTextBox.ScrollToCaret();
            UpdateStatus("文件预览已刷新");
        }

        private void RefreshTablePreview()
        {
            _tablePreviewGrid.Rows.Clear();

            if (_tableBackend == null)
            {
                UpdateStatus("当前后端没有表预览");
                return;
            }

            IReadOnlyList<DemoTableLogRecord> records = _tableBackend.GetRecordsSnapshot();
            int startIndex = Math.Max(0, records.Count - 500);

            for (int index = startIndex; index < records.Count; index++)
            {
                DemoTableLogRecord record = records[index];
                _tablePreviewGrid.Rows.Add(
                    record.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    record.Level,
                    record.LoggerName,
                    NormalizeSingleLine(record.Message));
            }

            UpdateStatus(string.Format("表预览已刷新，当前共 {0:N0} 条记录", records.Count));
        }

        private void SchedulePreviewRefresh()
        {
            _previewTimer.Stop();
            _previewTimer.Start();
        }

        private void SetToolbarState(bool enabled)
        {
            _backendSelector.Enabled = enabled;
            _recreateLoggerButton.Enabled = enabled;
            _writeSampleButton.Enabled = enabled;
            _writeBatchButton.Enabled = enabled;
            _refreshPreviewButton.Enabled = enabled;
        }

        private void UpdateStatus(string message)
        {
            _statusLabel.Text = message;
        }

        private StorageBackendKind GetSelectedBackendKind()
        {
            switch (_backendSelector.SelectedIndex)
            {
                case 1:
                    return StorageBackendKind.CsvFile;
                case 2:
                    return StorageBackendKind.SimulatedDatabase;
                default:
                    return StorageBackendKind.TextFile;
            }
        }

        private static string GetBackendName(StorageBackendKind backendKind)
        {
            switch (backendKind)
            {
                case StorageBackendKind.CsvFile:
                    return "CSV 文件后端";
                case StorageBackendKind.SimulatedDatabase:
                    return "自定义表后端（模拟数据库）";
                default:
                    return "文本文件后端";
            }
        }

        private static string GetDescription(StorageBackendKind backendKind)
        {
            switch (backendKind)
            {
                case StorageBackendKind.CsvFile:
                    return "当前演示的是 CsvFileLogStorageBackendFactory。日志仍然写到 ILoggerOutput，存储格式改成 CSV。";
                case StorageBackendKind.SimulatedDatabase:
                    return "当前演示的是自定义 ILogStorageBackendFactory。右侧表格模拟数据库表，说明只换后端实现即可接数据库。";
                default:
                    return "当前演示的是 TextFileLogStorageBackendFactory。日志会异步写入标准文本文件。";
            }
        }

        private static string BuildCodeSample(StorageBackendKind backendKind)
        {
            switch (backendKind)
            {
                case StorageBackendKind.CsvFile:
                    return
@"using Logger.Core;

LogManager.Configure(
    new LoggerService(
        new LogStoreLoggerFactory(
            new CsvFileLogStorageBackendFactory(@""D:\Logs\Csv""),
            minimumLevel: LogLevel.Trace)));

ILoggerOutput logger = LogManager.GetLogger(""OrderService"");
logPanel.Logger = logger;

logger.Info(""CSV logger ready."");
logger.Error(""line1\r\nline2"");";

                case StorageBackendKind.SimulatedDatabase:
                    return
@"using Logger.Core;

ILogStorageBackendFactory storageFactory =
    new YourDbLogStorageBackendFactory(""Server=.;Database=Logger;..."");

ILoggerFactory loggerFactory =
    new LogStoreLoggerFactory(storageFactory, minimumLevel: LogLevel.Trace);

ILoggerOutput logger = loggerFactory.CreateLogger(""OrderService"");
logPanel.Logger = logger;

logger.Info(""Database logger ready."");
logger.Error(""line1\r\nline2"");

// Demo 中使用的是 DemoTableLogStorageBackendFactory，
// 真实项目里把它替换成你的数据库后端工厂即可。";

                default:
                    return
@"using Logger.Core;

LogManager.Configure(
    new LoggerService(
        new LogStoreLoggerFactory(
            new TextFileLogStorageBackendFactory(@""D:\Logs\Text""),
            minimumLevel: LogLevel.Trace)));

ILoggerOutput logger = LogManager.GetLogger(""OrderService"");
logPanel.Logger = logger;

logger.Info(""Text logger ready."");
logger.Error(""line1\r\nline2"");";
            }
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

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string headerText, int width)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                Width = width,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true
            };
        }

        private static string NormalizeSingleLine(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        }

        private enum StorageBackendKind
        {
            TextFile,
            CsvFile,
            SimulatedDatabase
        }
    }
}
