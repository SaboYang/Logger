using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Logger.Core;
using Logger.Core.Models;
using WpfLogPanel = Logger.Wpf.Controls.LogPanelControl;

namespace Logger.WinForms.Controls
{
    [DesignerCategory("Code")]
    public class WpfLogPanelControl : UserControl
    {
        private readonly ElementHost _elementHost;
        private readonly WpfLogPanel _wpfLogPanel;
        private string _header = "运行日志";
        private int _maxLogEntries = 500;
        private LogLevelFilter _levelFilter = LogLevelFilter.All;
        private string _searchText = string.Empty;

        public WpfLogPanelControl()
        {
            _wpfLogPanel = new WpfLogPanel
            {
                Header = _header,
                MaxLogEntries = _maxLogEntries,
                LevelFilter = _levelFilter,
                SearchText = _searchText
            };

            _elementHost = new ElementHost
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColorTransparent = true,
                Child = _wpfLogPanel
            };

            Controls.Add(_elementHost);
        }

        [Category("Appearance")]
        [DefaultValue("运行日志")]
        public string Header
        {
            get { return _header; }
            set
            {
                _header = value ?? string.Empty;
                ExecuteOnWpfThread(() => _wpfLogPanel.Header = _header);
            }
        }

        [Category("Behavior")]
        [DefaultValue(500)]
        public int MaxLogEntries
        {
            get { return _maxLogEntries; }
            set
            {
                _maxLogEntries = value;
                ExecuteOnWpfThread(() => _wpfLogPanel.MaxLogEntries = _maxLogEntries);
            }
        }

        [Category("Behavior")]
        [DefaultValue(LogLevelFilter.All)]
        public LogLevelFilter LevelFilter
        {
            get { return _levelFilter; }
            set
            {
                _levelFilter = value;
                ExecuteOnWpfThread(() => _wpfLogPanel.LevelFilter = _levelFilter);
            }
        }

        [Category("Behavior")]
        [DefaultValue("")]
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value ?? string.Empty;
                ExecuteOnWpfThread(() => _wpfLogPanel.SearchText = _searchText);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ILoggerOutput Logger
        {
            get { return _wpfLogPanel.Logger; }
            set { ExecuteOnWpfThread(() => _wpfLogPanel.Logger = value); }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LogStore LogStore
        {
            get { return _wpfLogPanel.LogStore; }
        }

        public void AddTrace(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.AddTrace(message));
        }

        public void AddDebug(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.AddDebug(message));
        }

        public void AddInfo(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.AddInfo(message));
        }

        public void AddSuccess(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.AddSuccess(message));
        }

        public void AddWarning(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.AddWarning(message));
        }

        public void AddError(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.AddError(message));
        }

        public void AddFatal(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.AddFatal(message));
        }

        public void AddLog(LogLevel level, string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.AddLog(level, message));
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            List<LogEntry> entryBatch = SnapshotEntries(entries);
            if (entryBatch.Count == 0)
            {
                return;
            }

            ExecuteOnWpfThread(() => _wpfLogPanel.AddLogs(entryBatch));
        }

        public void ClearLogs()
        {
            ExecuteOnWpfThread(_wpfLogPanel.ClearLogs);
        }

        public bool IsLevelVisible(LogLevel level)
        {
            return LevelFilter.Includes(level);
        }

        public void SetLevelVisible(LogLevel level, bool visible)
        {
            LogLevelFilter levelFlag = level.ToFilter();
            if (visible)
            {
                LevelFilter |= levelFlag;
                return;
            }

            LevelFilter &= ~levelFlag;
        }

        public void ResetLevelFilter()
        {
            LevelFilter = LogLevelFilter.All;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _elementHost != null)
            {
                _elementHost.Child = null;
                _elementHost.Dispose();
            }

            base.Dispose(disposing);
        }

        private void ExecuteOnWpfThread(Action action)
        {
            if (IsDisposed || Disposing || action == null)
            {
                return;
            }

            if (_wpfLogPanel.Dispatcher.CheckAccess())
            {
                action();
                return;
            }

            try
            {
                _wpfLogPanel.Dispatcher.BeginInvoke(action);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static List<LogEntry> SnapshotEntries(IEnumerable<LogEntry> entries)
        {
            List<LogEntry> snapshot = new List<LogEntry>();
            if (entries == null)
            {
                return snapshot;
            }

            foreach (LogEntry entry in entries)
            {
                if (entry != null)
                {
                    snapshot.Add(entry);
                }
            }

            return snapshot;
        }
    }
}
