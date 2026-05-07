using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Logger.Core;
using Logger.Core.Models;

namespace Logger.Wpf.Controls
{
    /// <summary>
    /// 使用只读富文本区域展示日志内容的 WPF 面板。
    /// </summary>
    public partial class LogTextPanelControl : UserControl
    {
        private const LogLevelFilter DefaultLevelFilter =
            LogLevelFilter.Info |
            LogLevelFilter.Success |
            LogLevelFilter.Warn |
            LogLevelFilter.Error |
            LogLevelFilter.Fatal;

        private static readonly LogLevel[] FilterLevels =
        {
            LogLevel.Trace,
            LogLevel.Debug,
            LogLevel.Info,
            LogLevel.Success,
            LogLevel.Warn,
            LogLevel.Error,
            LogLevel.Fatal
        };

        private const string TimestampFormat = "HH:mm:ss.fff";

        private static readonly Brush TraceBrush = CreateBrush(148, 163, 184);
        private static readonly Brush DebugBrush = CreateBrush(96, 165, 250);
        private static readonly Brush InfoBrush = CreateBrush(15, 23, 42);
        private static readonly Brush SuccessBrush = CreateBrush(22, 163, 74);
        private static readonly Brush WarnBrush = CreateBrush(217, 119, 6);
        private static readonly Brush ErrorBrush = CreateBrush(220, 38, 38);
        private static readonly Brush FatalBrush = CreateBrush(185, 28, 28);

        private ILoggerOutput _currentLogger;
        private ILogViewSource _currentViewSource;
        private LogEntry _clearAnchorEntry;
        private bool _refreshPending;
        private bool _scrollPending;
        private bool _scrollRequested;
        private bool _updatingFilterMenu;
        private readonly Dictionary<LogLevel, MenuItem> _filterMenuItems = new Dictionary<LogLevel, MenuItem>();
        private ContextMenu _filterMenu;

        /// <summary>
        /// 初始化 <see cref="LogTextPanelControl"/> 的新实例。
        /// </summary>
        public LogTextPanelControl()
        {
            InitializeComponent();
            InitializeFilterMenu();
            LogRichTextBox.Document = CreateDocument();
            Loaded += LogTextPanelControl_Loaded;
            UpdateFilterUi();
            Logger = new LogStore();
        }

        /// <summary>
        /// 获取或设置日志面板标题。
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(string),
                typeof(LogTextPanelControl),
                new PropertyMetadata("运行日志"));

        /// <summary>
        /// 获取或设置日志源允许保留的最大条数。
        /// </summary>
        public static readonly DependencyProperty MaxLogEntriesProperty =
            DependencyProperty.Register(
                nameof(MaxLogEntries),
                typeof(int),
                typeof(LogTextPanelControl),
                new PropertyMetadata(3000, OnMaxLogEntriesChanged));

        /// <summary>
        /// 获取或设置当前日志输出源。
        /// </summary>
        public static readonly DependencyProperty LoggerProperty =
            DependencyProperty.Register(
                nameof(Logger),
                typeof(ILoggerOutput),
                typeof(LogTextPanelControl),
                new PropertyMetadata(null, OnLoggerChanged));

        /// <summary>
        /// 获取或设置用于过滤显示的日志等级。
        /// </summary>
        public static readonly DependencyProperty LevelFilterProperty =
            DependencyProperty.Register(
                nameof(LevelFilter),
                typeof(LogLevelFilter),
                typeof(LogTextPanelControl),
                new PropertyMetadata(DefaultLevelFilter, OnLevelFilterChanged));

        /// <summary>
        /// 获取或设置用于搜索日志内容的文本。
        /// </summary>
        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(
                nameof(SearchText),
                typeof(string),
                typeof(LogTextPanelControl),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        /// <summary>
        /// 获取或设置搜索框是否可见。
        /// </summary>
        public static readonly DependencyProperty SearchBoxVisibleProperty =
            DependencyProperty.Register(
                nameof(SearchBoxVisible),
                typeof(bool),
                typeof(LogTextPanelControl),
                new PropertyMetadata(false, OnSearchBoxVisibleChanged));

        /// <summary>
        /// 获取或设置日志面板标题。
        /// </summary>
        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        /// 获取或设置日志源允许保留的最大条数。
        /// </summary>
        public int MaxLogEntries
        {
            get { return (int)GetValue(MaxLogEntriesProperty); }
            set { SetValue(MaxLogEntriesProperty, value); }
        }

        /// <summary>
        /// 获取或设置当前日志输出源。
        /// </summary>
        public ILoggerOutput Logger
        {
            get { return (ILoggerOutput)GetValue(LoggerProperty); }
            set { SetValue(LoggerProperty, value); }
        }

        /// <summary>
        /// 获取或设置当前等级过滤器。
        /// </summary>
        public LogLevelFilter LevelFilter
        {
            get { return (LogLevelFilter)GetValue(LevelFilterProperty); }
            set { SetValue(LevelFilterProperty, value); }
        }

        /// <summary>
        /// 获取或设置搜索文本。
        /// </summary>
        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        /// <summary>
        /// 获取或设置搜索框是否可见。
        /// </summary>
        public bool SearchBoxVisible
        {
            get { return (bool)GetValue(SearchBoxVisibleProperty); }
            set { SetValue(SearchBoxVisibleProperty, value); }
        }

        /// <summary>
        /// 获取当前绑定的日志存储对象。
        /// </summary>
        public LogStore LogStore
        {
            get { return _currentViewSource as LogStore; }
        }

        /// <summary>
        /// 追加一条 Trace 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Trace(string message)
        {
            Logger?.Trace(message);
        }

        /// <summary>
        /// 追加一条 Debug 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Debug(string message)
        {
            Logger?.Debug(message);
        }

        /// <summary>
        /// 追加一条 Info 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Info(string message)
        {
            Logger?.Info(message);
        }

        /// <summary>
        /// 追加一条 Success 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Success(string message)
        {
            Logger?.Success(message);
        }

        /// <summary>
        /// 追加一条 Warning 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Warning(string message)
        {
            Logger?.Warning(message);
        }

        /// <summary>
        /// 追加一条 Error 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Error(string message)
        {
            Logger?.Error(message);
        }

        /// <summary>
        /// 追加一条 Fatal 日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public void Fatal(string message)
        {
            Logger?.Fatal(message);
        }

        /// <summary>
        /// 按指定等级和内容追加日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void AddLog(LogLevel level, string message)
        {
            Logger?.AddLog(level, message);
        }

        /// <summary>
        /// 批量追加日志。
        /// </summary>
        /// <param name="entries">日志集合。</param>
        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            Logger?.AddLogs(entries);
        }

        /// <summary>
        /// 清空当前显示区域，但不清空底层日志源。
        /// </summary>
        public void ClearLogs()
        {
            _clearAnchorEntry = GetLatestSourceEntry();
            RefreshVisibleEntries();
        }

        /// <summary>
        /// 将当前显示内容复制到剪贴板。
        /// </summary>
        public void CopyLogs()
        {
            if (LogRichTextBox.Document == null)
            {
                return;
            }

            string text = new TextRange(LogRichTextBox.Document.ContentStart, LogRichTextBox.Document.ContentEnd).Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Clipboard.SetText(text);
        }

        /// <summary>
        /// 将日志滚动到最后一条。
        /// </summary>
        public void ScrollToLatest()
        {
            ScheduleScrollToLatestEntry();
        }

        /// <summary>
        /// 判断指定等级是否当前可见。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <returns>可见返回 true，否则返回 false。</returns>
        public bool IsLevelVisible(LogLevel level)
        {
            return LevelFilter.Includes(level);
        }

        /// <summary>
        /// 设置指定等级是否显示。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="visible">是否显示。</param>
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

        private static void OnSearchBoxVisibleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LogTextPanelControl control = dependencyObject as LogTextPanelControl;
            if (control == null || !(bool)e.NewValue || control.SearchTextBox == null)
            {
                return;
            }

            control.Dispatcher.BeginInvoke(new Action(() =>
            {
                control.SearchTextBox.Focus();
                control.SearchTextBox.SelectAll();
            }), DispatcherPriority.Input);
        }

        private static void OnMaxLogEntriesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LogTextPanelControl control = dependencyObject as LogTextPanelControl;
            if (control == null || control._currentViewSource == null)
            {
                return;
            }

            control._currentViewSource.MaxEntries = (int)e.NewValue;
            control.ScheduleRefreshFromLogger();
        }

        private static void OnLoggerChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LogTextPanelControl control = dependencyObject as LogTextPanelControl;
            if (control == null)
            {
                return;
            }

            control.AttachLogger(e.OldValue as ILoggerOutput, e.NewValue as ILoggerOutput);
        }

        private static void OnLevelFilterChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LogTextPanelControl control = dependencyObject as LogTextPanelControl;
            if (control == null)
            {
                return;
            }

            control.UpdateFilterUi();
            control.ScheduleRefreshFromLogger();
        }

        private static void OnSearchTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LogTextPanelControl control = dependencyObject as LogTextPanelControl;
            if (control == null)
            {
                return;
            }

            control.ScheduleRefreshFromLogger();
        }

        private void AttachLogger(ILoggerOutput oldLogger, ILoggerOutput newLogger)
        {
            ILogViewSource oldViewSource = oldLogger as ILogViewSource;
            if (oldViewSource != null)
            {
                oldViewSource.Entries.CollectionChanged -= Logs_CollectionChanged;
            }

            if (newLogger == null)
            {
                SetCurrentValue(LoggerProperty, new LogStore());
                return;
            }

            ILogViewSource newViewSource = newLogger as ILogViewSource;
            if (newViewSource == null)
            {
                throw new InvalidOperationException("The bound logger must implement ILogViewSource.");
            }

            _currentLogger = newLogger;
            _currentViewSource = newViewSource;
            _currentViewSource.MaxEntries = MaxLogEntries;
            _currentViewSource.Entries.CollectionChanged += Logs_CollectionChanged;
            _clearAnchorEntry = null;
            RefreshVisibleEntries();
        }

        private void Logs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
            {
                ScheduleAppend(ToLogEntries(e.NewItems));
                return;
            }

            ScheduleRefreshFromLogger();
        }

        private void ScheduleRefreshFromLogger()
        {
            if (_refreshPending)
            {
                return;
            }

            _refreshPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _refreshPending = false;
                RefreshVisibleEntries();
            }), DispatcherPriority.Background);
        }

        private void ScheduleAppend(IList<LogEntry> entries)
        {
            if (_refreshPending || entries == null || entries.Count == 0)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_refreshPending)
                {
                    return;
                }

                AppendEntries(entries);
            }), DispatcherPriority.Background);
        }

        private void RefreshVisibleEntries()
        {
            IList<LogEntry> snapshot = SnapshotEntries();
            LogRichTextBox.Document = BuildDocument(snapshot);

            if (snapshot.Count > 0)
            {
                ScheduleScrollToLatestEntry();
            }
        }

        private void AppendEntries(IList<LogEntry> entries)
        {
            if (_currentViewSource == null || entries == null || entries.Count == 0)
            {
                return;
            }

            if (LogRichTextBox.Document == null)
            {
                LogRichTextBox.Document = CreateDocument();
            }

            for (int index = 0; index < entries.Count; index++)
            {
                LogEntry entry = entries[index];
                if (!ShouldDisplayEntry(entry))
                {
                    continue;
                }

                Paragraph paragraph = CreateParagraph(entry);
                if (paragraph != null)
                {
                    LogRichTextBox.Document.Blocks.Add(paragraph);
                }
            }

            ScheduleScrollToLatestEntry();
        }

        private void ScheduleScrollToLatestEntry()
        {
            _scrollRequested = true;
            if (_scrollPending)
            {
                return;
            }

            _scrollPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _scrollPending = false;
                ScrollToLatestCore();
            }), DispatcherPriority.ContextIdle);
        }

        private void LogTextPanelControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_scrollRequested)
            {
                ScheduleScrollToLatestEntry();
            }
        }

        private void ScrollToLatestCore()
        {
            if (LogRichTextBox.Document == null || LogRichTextBox.Document.Blocks.Count == 0)
            {
                return;
            }

            LogRichTextBox.UpdateLayout();
            TextPointer end = LogRichTextBox.Document.ContentEnd;
            if (end != null)
            {
                LogRichTextBox.CaretPosition = end;
            }

            LogRichTextBox.ScrollToEnd();
        }

        private IList<LogEntry> SnapshotEntries()
        {
            if (_currentViewSource == null)
            {
                return Array.Empty<LogEntry>();
            }

            lock (_currentViewSource.SyncRoot)
            {
                if (_currentViewSource.Entries.Count == 0)
                {
                    return Array.Empty<LogEntry>();
                }

                int startIndex = GetVisibleStartIndex();
                int visibleCount = _currentViewSource.Entries.Count - startIndex;
                if (visibleCount <= 0)
                {
                    return Array.Empty<LogEntry>();
                }

                List<LogEntry> snapshot = new List<LogEntry>(visibleCount);
                for (int index = startIndex; index < _currentViewSource.Entries.Count; index++)
                {
                    LogEntry entry = _currentViewSource.Entries[index];
                    if (entry != null)
                    {
                        snapshot.Add(entry);
                    }
                }

                return snapshot;
            }
        }

        private FlowDocument BuildDocument(IList<LogEntry> entries)
        {
            FlowDocument document = CreateDocument();
            if (entries == null || entries.Count == 0)
            {
                return document;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                LogEntry entry = entries[index];
                if (!ShouldDisplayEntry(entry))
                {
                    continue;
                }

                Paragraph paragraph = CreateParagraph(entry);
                if (paragraph != null)
                {
                    document.Blocks.Add(paragraph);
                }
            }

            return document;
        }

        private static string FormatEntry(LogEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0} [{1}] {2}",
                entry.Timestamp.ToString(TimestampFormat, System.Globalization.CultureInfo.InvariantCulture),
                entry.LevelText,
                NormalizeDisplayMessage(entry.Message));
        }

        private static string NormalizeDisplayMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            return message.Replace("\r", " ").Replace("\n", " ");
        }

        private static FlowDocument CreateDocument()
        {
            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(0);
            document.ColumnWidth = double.PositiveInfinity;
            document.FontFamily = new FontFamily("Consolas");
            document.FontSize = 12;
            document.Foreground = InfoBrush;
            return document;
        }

        private bool ShouldDisplayEntry(LogEntry entry)
        {
            return entry != null &&
                   LevelFilter.Includes(entry.Level) &&
                   MatchesSearch(entry);
        }

        private bool MatchesSearch(LogEntry entry)
        {
            string searchText = NormalizeSearchText(SearchText);
            if (string.IsNullOrEmpty(searchText))
            {
                return true;
            }

            return ContainsIgnoreCase(entry.LevelText, searchText) ||
                   ContainsIgnoreCase(entry.Message, searchText) ||
                   ContainsIgnoreCase(entry.Timestamp.ToString(TimestampFormat, System.Globalization.CultureInfo.InvariantCulture), searchText);
        }

        private static Paragraph CreateParagraph(LogEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            Paragraph paragraph = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 2),
                Foreground = GetBrush(entry.Level)
            };

            paragraph.Inlines.Add(new Run(FormatEntry(entry)));
            return paragraph;
        }

        private static Brush GetBrush(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return TraceBrush;
                case LogLevel.Debug:
                    return DebugBrush;
                case LogLevel.Info:
                    return InfoBrush;
                case LogLevel.Success:
                    return SuccessBrush;
                case LogLevel.Warn:
                    return WarnBrush;
                case LogLevel.Error:
                    return ErrorBrush;
                case LogLevel.Fatal:
                    return FatalBrush;
                default:
                    return InfoBrush;
            }
        }

        private static Brush CreateBrush(byte red, byte green, byte blue)
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
            brush.Freeze();
            return brush;
        }

        private static string NormalizeSearchText(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return string.Empty;
            }

            return searchText.Trim();
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
            {
                return false;
            }

            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static List<LogEntry> ToLogEntries(System.Collections.IList items)
        {
            List<LogEntry> entries = new List<LogEntry>(items.Count);
            for (int index = 0; index < items.Count; index++)
            {
                LogEntry entry = items[index] as LogEntry;
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        private int GetVisibleStartIndex()
        {
            if (_clearAnchorEntry == null || _currentViewSource == null)
            {
                return 0;
            }

            for (int index = _currentViewSource.Entries.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(_currentViewSource.Entries[index], _clearAnchorEntry))
                {
                    return index + 1;
                }
            }

            return 0;
        }

        private LogEntry GetLatestSourceEntry()
        {
            if (_currentViewSource == null)
            {
                return null;
            }

            lock (_currentViewSource.SyncRoot)
            {
                if (_currentViewSource.Entries.Count == 0)
                {
                    return null;
                }

                return _currentViewSource.Entries[_currentViewSource.Entries.Count - 1];
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearLogs();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            CopyLogs();
        }

        private void SearchClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchText = string.Empty;
            if (SearchContainer.Visibility == Visibility.Visible)
            {
                SearchTextBox.Focus();
            }
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_filterMenu == null)
            {
                return;
            }

            UpdateFilterUi();

            if (_filterMenu.IsOpen)
            {
                _filterMenu.IsOpen = false;
                return;
            }

            _filterMenu.PlacementTarget = FilterButton;
            _filterMenu.Placement = PlacementMode.Bottom;
            _filterMenu.IsOpen = true;
        }

        private void SelectAllFilterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ResetLevelFilter();
        }

        private void ClearAllFilterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LevelFilter = LogLevelFilter.None;
        }

        private void DefaultFilterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LevelFilter = DefaultLevelFilter;
        }

        private void LevelFilterMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_updatingFilterMenu)
            {
                return;
            }

            MenuItem item = sender as MenuItem;
            if (item == null || !(item.Tag is LogLevel))
            {
                return;
            }

            LogLevel level = (LogLevel)item.Tag;
            SetLevelVisible(level, item.IsChecked);
        }

        private void InitializeFilterMenu()
        {
            _filterMenu = new ContextMenu
            {
                PlacementTarget = FilterButton,
                Placement = PlacementMode.Bottom,
                StaysOpen = true
            };
            _filterMenu.Opened += FilterMenu_Opened;
            _filterMenu.Closed += FilterMenu_Closed;

            _filterMenu.Items.Add(CreateCommandMenuItem("全选", SelectAllFilterMenuItem_Click));
            _filterMenu.Items.Add(CreateCommandMenuItem("全不选", ClearAllFilterMenuItem_Click));
            _filterMenu.Items.Add(CreateCommandMenuItem("默认", DefaultFilterMenuItem_Click));
            _filterMenu.Items.Add(new Separator());

            foreach (LogLevel level in FilterLevels)
            {
                MenuItem item = new MenuItem
                {
                    Header = GetLevelDisplayText(level),
                    IsCheckable = true,
                    StaysOpenOnClick = true,
                    Tag = level
                };
                item.Click += LevelFilterMenuItem_Click;
                _filterMenuItems[level] = item;
                _filterMenu.Items.Add(item);
            }

            FilterButton.ContextMenu = _filterMenu;
        }

        private static MenuItem CreateCommandMenuItem(string header, RoutedEventHandler onClick)
        {
            MenuItem item = new MenuItem
            {
                Header = header,
                StaysOpenOnClick = true
            };
            item.Click += onClick;
            return item;
        }

        private void UpdateFilterUi()
        {
            if (FilterButton == null)
            {
                return;
            }

            FilterButton.Content = GetFilterButtonText();
            FilterButton.Tag = IsFilterHighlighted();

            if (_filterMenuItems.Count == 0)
            {
                return;
            }

            _updatingFilterMenu = true;
            try
            {
                foreach (LogLevel level in FilterLevels)
                {
                    MenuItem item;
                    if (_filterMenuItems.TryGetValue(level, out item))
                    {
                        item.IsChecked = IsLevelVisible(level);
                    }
                }
            }
            finally
            {
                _updatingFilterMenu = false;
            }
        }

        private string GetFilterButtonText()
        {
            int selectedCount = GetSelectedLevelCount();
            if (selectedCount == FilterLevels.Length)
            {
                return "筛选(全部)";
            }

            if (selectedCount == 0)
            {
                return "筛选(无)";
            }

            return string.Format("筛选({0}/{1})", selectedCount, FilterLevels.Length);
        }

        private int GetSelectedLevelCount()
        {
            int count = 0;
            foreach (LogLevel level in FilterLevels)
            {
                if (IsLevelVisible(level))
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsFilterHighlighted()
        {
            int selectedCount = GetSelectedLevelCount();
            bool isFilterActive = selectedCount > 0 && selectedCount < FilterLevels.Length;
            return isFilterActive || (_filterMenu != null && _filterMenu.IsOpen);
        }

        private static string GetLevelDisplayText(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return "TRACE";
                case LogLevel.Debug:
                    return "DEBUG";
                case LogLevel.Info:
                    return "INFO";
                case LogLevel.Success:
                    return "SUCCESS";
                case LogLevel.Warn:
                    return "WARN";
                case LogLevel.Error:
                    return "ERROR";
                case LogLevel.Fatal:
                    return "FATAL";
                default:
                    return level.ToString().ToUpperInvariant();
            }
        }

        private void FilterMenu_Opened(object sender, RoutedEventArgs e)
        {
            UpdateFilterUi();
        }

        private void FilterMenu_Closed(object sender, RoutedEventArgs e)
        {
            UpdateFilterUi();
        }
    }
}
