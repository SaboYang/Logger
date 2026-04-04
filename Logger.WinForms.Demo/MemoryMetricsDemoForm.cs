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
    public sealed class MemoryMetricsDemoForm : Form
    {
        private const int SessionBufferLimit = 200;
        private const int PendingQueueLimit = 120;
        private const int BackendDelayMilliseconds = 300;

        private readonly ILoggerOutput _logger;
        private readonly ILogSessionSource _sessionSource;
        private readonly ILogRuntimeMetricsSource _metricsSource;
        private readonly ILogFileSource _fileSource;
        private readonly LogPanelControl _logPanel;
        private readonly Label _sessionLabel;
        private readonly Label _metricsLabel;
        private readonly Label _statusLabel;
        private readonly TextBox _filePathTextBox;
        private readonly Button _writeSampleButton;
        private readonly Button _burstButton;
        private readonly Button _extremeButton;
        private readonly Button _refreshButton;
        private readonly CodeSamplePanel _codeSamplePanel;
        private readonly Timer _refreshTimer;
        private int _sequence;
        private bool _isWriting;

        public MemoryMetricsDemoForm()
        {
            string logRoot = Path.Combine(AppContext.BaseDirectory, "MemoryMetricsDemo", "Logs");
            ILoggerFactory factory = new LogStoreLoggerFactory(
                new SlowTextFileLogStorageBackendFactory(logRoot, BackendDelayMilliseconds),
                LogLevel.Trace,
                SessionBufferLimit,
                PendingQueueLimit);

            _logger = factory.CreateLogger("Logger.WinForms.Demo.MemoryMetrics");
            _sessionSource = _logger as ILogSessionSource;
            _metricsSource = _logger as ILogRuntimeMetricsSource;
            _fileSource = _logger as ILogFileSource;

            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Text = "Logger memory metrics demo";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1180, 760);
            ClientSize = new Size(1400, 860);
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
                ColumnCount = 6,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 10)
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _writeSampleButton = CreateToolbarButton("Write sample");
            _writeSampleButton.Click += WriteSampleButton_Click;

            _burstButton = CreateToolbarButton("Burst 5,000");
            _burstButton.Click += async (sender, e) => await RunBurstAsync(5000, 250);

            _extremeButton = CreateToolbarButton("Extreme 100,000");
            _extremeButton.Click += async (sender, e) => await RunBurstAsync(100000, 500);

            _refreshButton = CreateToolbarButton("Refresh metrics");
            _refreshButton.Click += RefreshButton_Click;

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                ForeColor = Color.FromArgb(29, 78, 216),
                Margin = new Padding(0, 8, 0, 0),
                TextAlign = ContentAlignment.MiddleRight,
                Text = "Ready"
            };

            toolbar.Controls.Add(_writeSampleButton, 0, 0);
            toolbar.Controls.Add(_burstButton, 1, 0);
            toolbar.Controls.Add(_extremeButton, 2, 0);
            toolbar.Controls.Add(_refreshButton, 3, 0);
            toolbar.Controls.Add(_statusLabel, 5, 0);

            GroupBox summaryGroup = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12),
                Text = "Runtime metrics",
                Margin = new Padding(0, 0, 0, 10)
            };

            TableLayoutPanel summaryLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 4
            };

            Label configLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(31, 41, 55),
                Margin = new Padding(0, 0, 0, 6),
                Text = string.Format(
                    "Session buffer limit = {0}, pending storage queue limit = {1}, backend delay = {2} ms",
                    SessionBufferLimit,
                    PendingQueueLimit,
                    BackendDelayMilliseconds)
            };

            _sessionLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(75, 85, 99),
                Margin = new Padding(0, 0, 0, 6)
            };

            _metricsLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(75, 85, 99),
                Margin = new Padding(0, 0, 0, 6)
            };

            _filePathTextBox = new TextBox
            {
                Dock = DockStyle.Top,
                ReadOnly = true,
                BackColor = Color.White,
                Text = _fileSource != null ? _fileSource.LogFilePath : string.Empty
            };

            summaryLayout.Controls.Add(configLabel, 0, 0);
            summaryLayout.Controls.Add(_sessionLabel, 0, 1);
            summaryLayout.Controls.Add(_metricsLabel, 0, 2);
            summaryLayout.Controls.Add(_filePathTextBox, 0, 3);
            summaryGroup.Controls.Add(summaryLayout);

            Label descriptionLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
                ForeColor = Color.FromArgb(75, 85, 99),
                Text = "This demo keeps UI binding unchanged and shows bounded session buffering plus non-dropping storage backpressure."
            };

            TableLayoutPanel contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));

            _logPanel = new LogPanelControl
            {
                Dock = DockStyle.Fill,
                Header = "Memory metrics log stream",
                Logger = _logger,
                MaxLogEntries = 30000,
                Margin = new Padding(0, 0, 8, 0)
            };

            _codeSamplePanel = new CodeSamplePanel
            {
                Dock = DockStyle.Fill,
                Height = 260,
                Margin = new Padding(8, 0, 0, 0),
                CodeText =
@"using Logger.Core;
using Logger.WinForms.Controls;

ILoggerFactory factory = new LogStoreLoggerFactory(
    new SlowTextFileLogStorageBackendFactory(@""D:\Logs"", 300),
    minimumLevel: LogLevel.Trace,
    maxBufferedSessionEntries: 200,
    maxPendingStorageEntries: 120);

ILoggerOutput logger = factory.CreateLogger(""MyApp.Memory"");
ILogRuntimeMetricsSource metrics = logger as ILogRuntimeMetricsSource;
ILogSessionSource session = logger as ILogSessionSource;

logPanel.Logger = logger;

logger.AddInfo(""runtime metrics demo ready"");

int buffered = metrics != null ? metrics.BufferedSessionEntryCount : 0;
int dropped = metrics != null ? metrics.DroppedPendingEntryCount : 0;
int total = session != null ? session.SessionEntryCount : 0;"
            };

            contentLayout.Controls.Add(_logPanel, 0, 0);
            contentLayout.Controls.Add(_codeSamplePanel, 1, 0);

            rootLayout.Controls.Add(toolbar, 0, 0);
            rootLayout.Controls.Add(summaryGroup, 0, 1);
            rootLayout.Controls.Add(descriptionLabel, 0, 2);
            rootLayout.Controls.Add(contentLayout, 0, 3);
            Controls.Add(rootLayout);

            _refreshTimer = new Timer
            {
                Interval = 250
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            Shown += MemoryMetricsDemoForm_Shown;
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

        private void MemoryMetricsDemoForm_Shown(object sender, EventArgs e)
        {
            _logger.AddInfo("Memory metrics demo started.");
            _logger.AddInfo("Write sample, burst, or extreme traffic to inspect buffered session entries and verify storage backpressure does not drop logs.");
            RefreshRuntimeState();
            _refreshTimer.Start();
        }

        private void WriteSampleButton_Click(object sender, EventArgs e)
        {
            WriteSampleEntries();
            RefreshRuntimeState();
            UpdateStatus("Sample entries written");
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshRuntimeState();
            UpdateStatus("Metrics refreshed");
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshRuntimeState();
        }

        private void WriteSampleEntries()
        {
            _sequence++;
            _logger.AddTrace(string.Format("TRACE sample #{0}", _sequence));
            _logger.AddDebug("DEBUG metrics inspection");
            _logger.AddInfo("INFO sample entry");
            _logger.AddSuccess("SUCCESS sample entry");
            _logger.AddWarning("WARN sample entry");
            _logger.AddError("ERROR sample entry");
            _logger.AddFatal("FATAL sample entry");
        }

        private async Task RunBurstAsync(int total, int batchSize)
        {
            if (_isWriting)
            {
                return;
            }

            _isWriting = true;
            SetToolbarState(false);
            UpdateStatus(string.Format("Writing {0:N0} entries...", total));

            try
            {
                await Task.Run(() => ProduceEntries(total, batchSize));
                UpdateStatus(string.Format("Finished writing {0:N0} entries", total));
            }
            finally
            {
                RefreshRuntimeState();
                SetToolbarState(true);
                _isWriting = false;
            }
        }

        private void ProduceEntries(int total, int batchSize)
        {
            List<LogEntry> batch = new List<LogEntry>(batchSize);

            for (int index = 1; index <= total; index++)
            {
                LogLevel level = GetLevelByIndex(index);
                batch.Add(new LogEntry(
                    DateTime.Now,
                    level,
                    string.Format(
                        "[{0:N0}/{1:N0}] {2} backpressure test message",
                        index,
                        total,
                        level.ToString().ToUpperInvariant())));

                if (batch.Count == batchSize || index == total)
                {
                    _logger.AddLogs(batch);
                    batch = new List<LogEntry>(batchSize);
                }
            }
        }

        private void RefreshRuntimeState()
        {
            if (_sessionSource != null)
            {
                _sessionLabel.Text = string.Format(
                    "SessionId: {0}    StartedAt: {1:yyyy-MM-dd HH:mm:ss.fff}    Total session entries: {2:N0}",
                    _sessionSource.SessionId.ToString("N"),
                    _sessionSource.SessionStartedAt,
                    _sessionSource.SessionEntryCount);
            }
            else
            {
                _sessionLabel.Text = "Session metrics are not available.";
            }

            if (_metricsSource != null)
            {
                _metricsLabel.Text = string.Format(
                    "BufferedSessionEntryCount: {0:N0}    DroppedPendingEntryCount: {1:N0}",
                    _metricsSource.BufferedSessionEntryCount,
                    _metricsSource.DroppedPendingEntryCount);
            }
            else
            {
                _metricsLabel.Text = "Runtime metrics are not available.";
            }

            _filePathTextBox.Text = _fileSource != null ? _fileSource.LogFilePath ?? string.Empty : string.Empty;
        }

        private void SetToolbarState(bool enabled)
        {
            _writeSampleButton.Enabled = enabled;
            _burstButton.Enabled = enabled;
            _extremeButton.Enabled = enabled;
            _refreshButton.Enabled = enabled;
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
