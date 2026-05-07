using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Logger.Core;
using Logger.Core.Models;
using WpfLogTextPanel = Logger.Wpf.Controls.LogTextPanelControl;

namespace Logger.WinForms.Controls
{
    /// <summary>
    /// 在 WinForms 中宿主 <see cref="Logger.Wpf.Controls.LogTextPanelControl"/>，并保持与 <see cref="WpfLogPanelControl"/> 相同的对外 API。
    /// </summary>
    [DesignerCategory("Code")]
    public class WpfLogTextPanelControl : UserControl
    {
        private const LogLevelFilter DefaultLevelFilter =
            LogLevelFilter.Info |
            LogLevelFilter.Success |
            LogLevelFilter.Warn |
            LogLevelFilter.Error |
            LogLevelFilter.Fatal;

        private readonly ElementHost _elementHost;
        private readonly WpfLogTextPanel _wpfLogPanel;
        private string _header = "运行日志";
        private int _maxLogEntries = 3000;
        private LogLevelFilter _levelFilter = DefaultLevelFilter;
        private string _searchText = string.Empty;
        private bool _searchBoxVisible;

        /// <summary>
        /// 初始化 <see cref="WpfLogTextPanelControl"/> 的新实例。
        /// </summary>
        public WpfLogTextPanelControl()
        {
            _wpfLogPanel = new WpfLogTextPanel
            {
                Header = _header,
                MaxLogEntries = _maxLogEntries,
                LevelFilter = _levelFilter,
                SearchText = _searchText,
                SearchBoxVisible = _searchBoxVisible
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

        /// <summary>
        /// 获取或设置日志面板标题。
        /// </summary>
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

        /// <summary>
        /// 获取或设置允许保留的最大日志条数。
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(3000)]
        public int MaxLogEntries
        {
            get { return _maxLogEntries; }
            set
            {
                _maxLogEntries = value;
                ExecuteOnWpfThread(() => _wpfLogPanel.MaxLogEntries = _maxLogEntries);
            }
        }

        /// <summary>
        /// 获取或设置日志等级过滤器。
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(DefaultLevelFilter)]
        public LogLevelFilter LevelFilter
        {
            get { return _levelFilter; }
            set
            {
                _levelFilter = value;
                ExecuteOnWpfThread(() => _wpfLogPanel.LevelFilter = _levelFilter);
            }
        }

        /// <summary>
        /// 获取或设置搜索文本。
        /// </summary>
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

        /// <summary>
        /// 获取或设置搜索框是否可见。
        /// </summary>
        [Category("Behavior")]
        [DefaultValue(false)]
        public bool SearchBoxVisible
        {
            get { return _searchBoxVisible; }
            set
            {
                _searchBoxVisible = value;
                ExecuteOnWpfThread(() => _wpfLogPanel.SearchBoxVisible = _searchBoxVisible);
            }
        }

        /// <summary>
        /// 获取或设置当前日志输出源。
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ILoggerOutput Logger
        {
            get { return _wpfLogPanel.Logger; }
            set { ExecuteOnWpfThread(() => _wpfLogPanel.Logger = value); }
        }

        /// <summary>
        /// 获取当前绑定的日志存储。
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LogStore LogStore
        {
            get { return _wpfLogPanel.LogStore; }
        }

        /// <summary>
        /// 追加一条 Trace 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Trace(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.Trace(message));
        }

        /// <summary>
        /// 追加一条 Debug 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Debug(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.Debug(message));
        }

        /// <summary>
        /// 追加一条 Info 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Info(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.Info(message));
        }

        /// <summary>
        /// 追加一条 Success 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Success(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.Success(message));
        }

        /// <summary>
        /// 追加一条 Warning 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Warning(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.Warning(message));
        }

        /// <summary>
        /// 追加一条 Error 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Error(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.Error(message));
        }

        /// <summary>
        /// 追加一条 Fatal 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Fatal(string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.Fatal(message));
        }

        /// <summary>
        /// 按指定等级和内容追加日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void AddLog(LogLevel level, string message)
        {
            ExecuteOnWpfThread(() => _wpfLogPanel.AddLog(level, message));
        }

        /// <summary>
        /// 批量追加日志。
        /// </summary>
        /// <param name="entries">日志集合。</param>
        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            List<LogEntry> entryBatch = SnapshotEntries(entries);
            if (entryBatch.Count == 0)
            {
                return;
            }

            ExecuteOnWpfThread(() => _wpfLogPanel.AddLogs(entryBatch));
        }

        /// <summary>
        /// 清空当前可见日志。
        /// </summary>
        public void ClearLogs()
        {
            ExecuteOnWpfThread(_wpfLogPanel.ClearLogs);
        }

        /// <summary>
        /// 将当前日志复制到剪贴板。
        /// </summary>
        public void CopyLogs()
        {
            ExecuteOnWpfThread(_wpfLogPanel.CopyLogs);
        }

        /// <summary>
        /// 判断指定等级是否可见。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <returns>可见返回 true，否则返回 false。</returns>
        public bool IsLevelVisible(LogLevel level)
        {
            return LevelFilter.Includes(level);
        }

        /// <summary>
        /// 设置指定等级是否可见。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="visible">是否可见。</param>
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

        /// <summary>
        /// 重置等级过滤器为全部可见。
        /// </summary>
        public void ResetLevelFilter()
        {
            LevelFilter = LogLevelFilter.All;
        }

        /// <summary>
        /// 释放宿主资源。
        /// </summary>
        /// <param name="disposing">是否释放托管资源。</param>
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
