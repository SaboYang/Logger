using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Logger.Core;
using Logger.Core.Models;

namespace Logger.Wpf.Controls
{
    public partial class LogPanelControl : UserControl
    {
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

        private bool _autoScrollPending;
        private bool _refreshPending;
        private bool _updatingFilterMenu;
        private ILoggerOutput _currentLogger;
        private ILogViewSource _currentViewSource;
        private LogEntry _clearAnchorEntry;
        private IList<LogEntry> _visibleEntries = Array.Empty<LogEntry>();
        private readonly Dictionary<LogLevel, MenuItem> _filterMenuItems = new Dictionary<LogLevel, MenuItem>();
        private ContextMenu _filterMenu;

        public LogPanelControl()
        {
            InitializeComponent();
            InitializeFilterMenu();
            LogList.ItemsSource = _visibleEntries;
            UpdateFilterUi();
            Logger = new LogStore();
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(string),
                typeof(LogPanelControl),
                new PropertyMetadata("运行日志"));

        public static readonly DependencyProperty MaxLogEntriesProperty =
            DependencyProperty.Register(
                nameof(MaxLogEntries),
                typeof(int),
                typeof(LogPanelControl),
                new PropertyMetadata(500, OnMaxLogEntriesChanged));

        public static readonly DependencyProperty LoggerProperty =
            DependencyProperty.Register(
                nameof(Logger),
                typeof(ILoggerOutput),
                typeof(LogPanelControl),
                new PropertyMetadata(null, OnLoggerChanged));

        public static readonly DependencyProperty LevelFilterProperty =
            DependencyProperty.Register(
                nameof(LevelFilter),
                typeof(LogLevelFilter),
                typeof(LogPanelControl),
                new PropertyMetadata(LogLevelFilter.All, OnLevelFilterChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(
                nameof(SearchText),
                typeof(string),
                typeof(LogPanelControl),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public int MaxLogEntries
        {
            get { return (int)GetValue(MaxLogEntriesProperty); }
            set { SetValue(MaxLogEntriesProperty, value); }
        }

        public ILoggerOutput Logger
        {
            get { return (ILoggerOutput)GetValue(LoggerProperty); }
            set { SetValue(LoggerProperty, value); }
        }

        public LogLevelFilter LevelFilter
        {
            get { return (LogLevelFilter)GetValue(LevelFilterProperty); }
            set { SetValue(LevelFilterProperty, value); }
        }

        public string SearchText
        {
            get { return (string)GetValue(SearchTextProperty); }
            set { SetValue(SearchTextProperty, value); }
        }

        public LogStore LogStore
        {
            get { return Logger as LogStore; }
        }

        public void AddTrace(string message)
        {
            Logger?.AddTrace(message);
        }

        public void AddDebug(string message)
        {
            Logger?.AddDebug(message);
        }

        public void AddInfo(string message)
        {
            Logger?.AddInfo(message);
        }

        public void AddSuccess(string message)
        {
            Logger?.AddSuccess(message);
        }

        public void AddWarning(string message)
        {
            Logger?.AddWarning(message);
        }

        public void AddError(string message)
        {
            Logger?.AddError(message);
        }

        public void AddFatal(string message)
        {
            Logger?.AddFatal(message);
        }

        public void AddLog(LogLevel level, string message)
        {
            Logger?.AddLog(level, message);
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            Logger?.AddLogs(entries);
        }

        public void ClearLogs()
        {
            _clearAnchorEntry = GetLatestSourceEntry();
            RefreshVisibleEntries();
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

        private static void OnMaxLogEntriesChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LogPanelControl control = dependencyObject as LogPanelControl;
            if (control?._currentLogger == null)
            {
                return;
            }

            control._currentViewSource.MaxEntries = (int)e.NewValue;
            control.ScheduleRefreshFromLogger();
        }

        private static void OnLoggerChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LogPanelControl control = dependencyObject as LogPanelControl;
            if (control == null)
            {
                return;
            }

            control.AttachLogger(e.OldValue as ILoggerOutput, e.NewValue as ILoggerOutput);
        }

        private static void OnLevelFilterChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LogPanelControl control = dependencyObject as LogPanelControl;
            if (control == null)
            {
                return;
            }

            control.UpdateFilterUi();
            control.ScheduleRefreshFromLogger();
        }

        private static void OnSearchTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            LogPanelControl control = dependencyObject as LogPanelControl;
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
            ScheduleRefreshFromLogger();
        }

        private void Logs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add &&
                e.Action != NotifyCollectionChangedAction.Reset &&
                e.Action != NotifyCollectionChangedAction.Replace)
            {
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

        private void RefreshVisibleEntries()
        {
            _visibleEntries = SnapshotEntries();
            LogList.ItemsSource = _visibleEntries;
            ScheduleScrollToLatestEntry();
        }

        private void ScheduleScrollToLatestEntry()
        {
            LogEntry latestEntry = GetLatestVisibleEntry();
            if (latestEntry == null)
            {
                return;
            }

            if (_autoScrollPending)
            {
                return;
            }

            _autoScrollPending = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                _autoScrollPending = false;

                LogEntry entryToScroll = GetLatestVisibleEntry();
                if (entryToScroll == null)
                {
                    return;
                }

                LogList.ScrollIntoView(entryToScroll);
            }), DispatcherPriority.Background);
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
                    if (ShouldDisplayEntry(entry))
                    {
                        snapshot.Add(entry);
                    }
                }

                return snapshot;
            }
        }

        private bool ShouldDisplayEntry(LogEntry entry)
        {
            return entry != null && LevelFilter.Includes(entry.Level) && MatchesSearch(entry);
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
                   ContainsIgnoreCase(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"), searchText);
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

        private LogEntry GetLatestVisibleEntry()
        {
            if (_visibleEntries == null || _visibleEntries.Count == 0)
            {
                return null;
            }

            return _visibleEntries[_visibleEntries.Count - 1];
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearLogs();
        }

        private void SearchClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchText = string.Empty;
            SearchTextBox.Focus();
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

            _filterMenu.Items.Add(CreateCommandMenuItem("全选", SelectAllFilterMenuItem_Click));
            _filterMenu.Items.Add(CreateCommandMenuItem("全不选", ClearAllFilterMenuItem_Click));
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

        private static string NormalizeSearchText(string searchText)
        {
            return string.IsNullOrWhiteSpace(searchText) ? string.Empty : searchText.Trim();
        }

        private static bool ContainsIgnoreCase(string source, string searchText)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
