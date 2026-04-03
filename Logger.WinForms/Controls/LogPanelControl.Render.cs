using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Logger.Core;
using Logger.Core.Models;

namespace Logger.WinForms.Controls
{
    [DesignerCategory("Code")]
    public class LogPanelControl : UserControl
    {
        private const int SearchRefreshDelayMilliseconds = 180;

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

        private readonly Panel _headerPanel;
        private readonly FlowLayoutPanel _actionPanel;
        private readonly Label _headerLabel;
        private readonly TextBox _searchTextBox;
        private readonly Button _filterButton;
        private readonly Button _clearButton;
        private readonly ContextMenuStrip _filterMenu;
        private readonly System.Windows.Forms.Timer _searchRefreshTimer;
        private readonly LogGridView _logGrid;
        private readonly Font _levelFont;
        private bool _refreshPending;
        private int _refreshWorkerRunning;
        private int _refreshVersion;
        private int _appliedRefreshVersion;
        private bool _updatingFilterMenu;
        private bool _updatingSearchText;
        private string _header = "\u8fd0\u884c\u65e5\u5fd7";
        private string _searchText = string.Empty;
        private bool _autoScrollToEnd = true;
        private int _maxLogEntries = 500;
        private LogLevelFilter _levelFilter = LogLevelFilter.All;
        private ILoggerOutput _currentLogger;
        private ILogViewSource _currentViewSource;
        private LogEntry _clearAnchorEntry;
        private IList<LogEntry> _visibleEntries = Array.Empty<LogEntry>();
        private readonly Dictionary<LogLevel, ToolStripMenuItem> _filterMenuItems =
            new Dictionary<LogLevel, ToolStripMenuItem>();

        public LogPanelControl()
        {
            BackColor = Color.FromArgb(248, 250, 252);
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(8);

            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.Transparent
            };

            _actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            _filterMenu = BuildFilterMenu();
            _searchRefreshTimer = new System.Windows.Forms.Timer
            {
                Interval = SearchRefreshDelayMilliseconds
            };
            _searchRefreshTimer.Tick += SearchRefreshTimer_Tick;

            _searchTextBox = new TextBox
            {
                Width = 200,
                Margin = new Padding(0, 3, 8, 0),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };
            _searchTextBox.TextChanged += SearchTextBox_TextChanged;

            _filterButton = new Button
            {
                Text = "\u7b5b\u9009",
                Width = 96,
                Height = 28,
                Margin = new Padding(0, 0, 8, 0),
                FlatStyle = FlatStyle.System,
                UseVisualStyleBackColor = true
            };
            _filterButton.Click += FilterButton_Click;

            _clearButton = new Button
            {
                Text = "\u6e05\u7a7a\u65e5\u5fd7",
                Width = 86,
                Height = 28,
                Margin = new Padding(0),
                FlatStyle = FlatStyle.System,
                UseVisualStyleBackColor = true
            };
            _clearButton.Click += ClearButton_Click;

            _headerLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(31, 41, 55),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                AutoEllipsis = true
            };

            _logGrid = BuildLogGrid();
            _levelFont = new Font("Consolas", 9F, FontStyle.Bold, GraphicsUnit.Point);
            _logGrid.CellFormatting += LogGrid_CellFormatting;
            _logGrid.CellValueNeeded += LogGrid_CellValueNeeded;

            _actionPanel.Controls.Add(_searchTextBox);
            _actionPanel.Controls.Add(_filterButton);
            _actionPanel.Controls.Add(_clearButton);
            _headerPanel.Controls.Add(_headerLabel);
            _headerPanel.Controls.Add(_actionPanel);

            Controls.Add(_logGrid);
            Controls.Add(_headerPanel);

            Header = _header;
            UpdateFilterUi();
            UpdateSearchUi();
            AttachLogger(new LogStore());
        }

        [Category("Appearance")]
        [DefaultValue("\u8fd0\u884c\u65e5\u5fd7")]
        public string Header
        {
            get { return _header; }
            set
            {
                _header = value ?? string.Empty;
                _headerLabel.Text = _header;
            }
        }

        [Category("Behavior")]
        [DefaultValue("")]
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                ExecuteOnUiThread(() =>
                {
                    string nextText = value ?? string.Empty;
                    if (string.Equals(_searchText, nextText, StringComparison.Ordinal))
                    {
                        return;
                    }

                    _searchText = nextText;
                    UpdateSearchUi();
                    ScheduleSearchRefresh();
                });
            }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool AutoScrollToEnd
        {
            get { return _autoScrollToEnd; }
            set { _autoScrollToEnd = value; }
        }

        [Category("Behavior")]
        [DefaultValue(500)]
        public int MaxLogEntries
        {
            get { return _maxLogEntries; }
            set
            {
                ExecuteOnUiThread(() =>
                {
                    _maxLogEntries = Math.Max(1, value);

                    if (_currentViewSource != null)
                    {
                        _currentViewSource.MaxEntries = _maxLogEntries;
                    }

                    ScheduleRefresh();
                });
            }
        }

        [Category("Behavior")]
        [DefaultValue(LogLevelFilter.All)]
        public LogLevelFilter LevelFilter
        {
            get { return _levelFilter; }
            set
            {
                ExecuteOnUiThread(() =>
                {
                    if (_levelFilter == value)
                    {
                        return;
                    }

                    _levelFilter = value;
                    UpdateFilterUi();
                    ScheduleRefresh();
                });
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ILoggerOutput Logger
        {
            get { return _currentLogger; }
            set { ExecuteOnUiThread(() => AttachLogger(value)); }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LogStore LogStore
        {
            get { return _currentViewSource as LogStore; }
        }

        public void AddTrace(string message)
        {
            ExecuteOnUiThread(() => _currentLogger?.AddTrace(message));
        }

        public void AddDebug(string message)
        {
            ExecuteOnUiThread(() => _currentLogger?.AddDebug(message));
        }

        public void AddInfo(string message)
        {
            ExecuteOnUiThread(() => _currentLogger?.AddInfo(message));
        }

        public void AddSuccess(string message)
        {
            ExecuteOnUiThread(() => _currentLogger?.AddSuccess(message));
        }

        public void AddWarning(string message)
        {
            ExecuteOnUiThread(() => _currentLogger?.AddWarning(message));
        }

        public void AddError(string message)
        {
            ExecuteOnUiThread(() => _currentLogger?.AddError(message));
        }

        public void AddFatal(string message)
        {
            ExecuteOnUiThread(() => _currentLogger?.AddFatal(message));
        }

        public void AddLog(LogLevel level, string message)
        {
            ExecuteOnUiThread(() => _currentLogger?.AddLog(level, message));
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            List<LogEntry> entryBatch = SnapshotEntries(entries);
            if (entryBatch.Count == 0)
            {
                return;
            }

            ExecuteOnUiThread(() => _currentLogger?.AddLogs(entryBatch));
        }

        public void ClearLogs()
        {
            ExecuteOnUiThread(() =>
            {
                _clearAnchorEntry = GetLatestSourceEntry();
                ScheduleRefresh();
            });
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

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (_refreshPending || GetCurrentEntryCount() > 0)
            {
                _refreshPending = false;
                ScheduleRefresh();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_currentViewSource != null)
                {
                    _currentViewSource.Entries.CollectionChanged -= Logs_CollectionChanged;
                }

                _searchTextBox.TextChanged -= SearchTextBox_TextChanged;
                _searchRefreshTimer.Tick -= SearchRefreshTimer_Tick;
                _searchRefreshTimer.Stop();
                _filterButton.Click -= FilterButton_Click;
                _clearButton.Click -= ClearButton_Click;
                _logGrid.CellFormatting -= LogGrid_CellFormatting;
                _logGrid.CellValueNeeded -= LogGrid_CellValueNeeded;
                foreach (ToolStripMenuItem item in _filterMenuItems.Values)
                {
                    item.Click -= FilterMenuItem_Click;
                }
                _filterMenu.Dispose();
                _searchRefreshTimer.Dispose();
                _levelFont.Dispose();
                _searchTextBox.Font.Dispose();
                _headerLabel.Font.Dispose();
            }

            base.Dispose(disposing);
        }

        private static LogGridView BuildLogGrid()
        {
            LogGridView grid = new LogGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable,
                BackgroundColor = Color.FromArgb(17, 24, 39),
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(31, 41, 55),
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                ColumnHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                VirtualMode = true,
                EditMode = DataGridViewEditMode.EditProgrammatically,
                ScrollBars = ScrollBars.Vertical,
                StandardTab = true
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(17, 24, 39),
                ForeColor = Color.FromArgb(249, 250, 251),
                SelectionBackColor = Color.FromArgb(17, 24, 39),
                SelectionForeColor = Color.FromArgb(249, 250, 251),
                Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point),
                WrapMode = DataGridViewTriState.False,
                Padding = new Padding(0, 2, 0, 2)
            };

            grid.RowsDefaultCellStyle = grid.DefaultCellStyle;
            grid.RowTemplate.Height = 24;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders;

            DataGridViewTextBoxColumn timestampColumn = new DataGridViewTextBoxColumn
            {
                Name = "Timestamp",
                Width = 92,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true,
                Resizable = DataGridViewTriState.False
            };

            DataGridViewTextBoxColumn levelColumn = new DataGridViewTextBoxColumn
            {
                Name = "Level",
                Width = 72,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true,
                Resizable = DataGridViewTriState.False
            };

            DataGridViewTextBoxColumn messageColumn = new DataGridViewTextBoxColumn
            {
                Name = "Message",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true,
                Resizable = DataGridViewTriState.False,
                MinimumWidth = 120
            };
            messageColumn.DefaultCellStyle = new DataGridViewCellStyle(grid.DefaultCellStyle)
            {
                WrapMode = DataGridViewTriState.True,
                Alignment = DataGridViewContentAlignment.TopLeft
            };

            grid.Columns.Add(timestampColumn);
            grid.Columns.Add(levelColumn);
            grid.Columns.Add(messageColumn);
            grid.ClearSelection();

            return grid;
        }

        private ContextMenuStrip BuildFilterMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                ShowCheckMargin = true,
                AutoClose = false
            };

            ToolStripMenuItem selectAllItem = new ToolStripMenuItem("全选");
            selectAllItem.Click += SelectAllFilterMenuItem_Click;
            menu.Items.Add(selectAllItem);

            ToolStripMenuItem clearAllItem = new ToolStripMenuItem("全不选");
            clearAllItem.Click += ClearAllFilterMenuItem_Click;
            menu.Items.Add(clearAllItem);

            menu.Items.Add(new ToolStripSeparator());

            foreach (LogLevel level in FilterLevels)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(GetLevelDisplayText(level))
                {
                    CheckOnClick = true,
                    Checked = true,
                    Tag = level
                };
                item.Click += FilterMenuItem_Click;
                _filterMenuItems[level] = item;
                menu.Items.Add(item);
            }

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem closeItem = new ToolStripMenuItem("取消")
            {
                AutoToolTip = false
            };
            closeItem.Click += CloseFilterMenuItem_Click;
            menu.Items.Add(closeItem);

            return menu;
        }

        private void LogGrid_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            LogEntry entry = GetVisibleEntry(e.RowIndex);
            if (entry == null)
            {
                return;
            }

            if (e.ColumnIndex == 0)
            {
                e.Value = entry.Timestamp.ToString("HH:mm:ss.fff");
                return;
            }

            if (e.ColumnIndex == 1)
            {
                e.Value = entry.LevelText;
                return;
            }

            e.Value = NormalizeDisplayMessage(entry.Message);
        }

        private void LogGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            LogEntry entry = GetVisibleEntry(e.RowIndex);
            if (entry == null)
            {
                return;
            }

            e.CellStyle.BackColor = Color.FromArgb(17, 24, 39);
            e.CellStyle.SelectionBackColor = Color.FromArgb(17, 24, 39);

            if (e.ColumnIndex == 0)
            {
                e.CellStyle.ForeColor = Color.FromArgb(156, 163, 175);
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                e.CellStyle.Font = _logGrid.DefaultCellStyle.Font;
                e.CellStyle.WrapMode = DataGridViewTriState.False;
            }
            else if (e.ColumnIndex == 1)
            {
                e.CellStyle.ForeColor = GetLevelColor(entry.Level);
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                e.CellStyle.Font = _levelFont;
                e.CellStyle.WrapMode = DataGridViewTriState.False;
            }
            else
            {
                e.CellStyle.ForeColor = Color.FromArgb(249, 250, 251);
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                e.CellStyle.Font = _logGrid.DefaultCellStyle.Font;
                e.CellStyle.WrapMode = DataGridViewTriState.True;
                e.CellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
            }
        }

        private void Logs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ScheduleRefresh();
        }

        private void ScheduleRefresh()
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            Interlocked.Increment(ref _refreshVersion);

            if (!IsHandleCreated)
            {
                _refreshPending = true;
                return;
            }

            StartRefreshWorker();
        }

        private void StartRefreshWorker()
        {
            if (Interlocked.CompareExchange(ref _refreshWorkerRunning, 1, 0) != 0)
            {
                return;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    RunRefreshWorker();
                }
                finally
                {
                    Interlocked.Exchange(ref _refreshWorkerRunning, 0);

                    if (!IsDisposed &&
                        !Disposing &&
                        IsHandleCreated &&
                        Volatile.Read(ref _appliedRefreshVersion) != Volatile.Read(ref _refreshVersion))
                    {
                        StartRefreshWorker();
                    }
                }
            });
        }

        private void RunRefreshWorker()
        {
            while (!IsDisposed && !Disposing)
            {
                int requestedVersion = Volatile.Read(ref _refreshVersion);
                IList<LogEntry> visibleEntries = SnapshotCurrentEntries();
                int latestVersion = Volatile.Read(ref _refreshVersion);

                if (IsDisposed || Disposing)
                {
                    return;
                }

                if (requestedVersion != latestVersion)
                {
                    continue;
                }

                try
                {
                    BeginInvoke(new Action(() => ApplyVisibleEntries(visibleEntries, requestedVersion)));
                }
                catch (ObjectDisposedException)
                {
                    _refreshPending = true;
                    return;
                }
                catch (InvalidOperationException)
                {
                    _refreshPending = true;
                    return;
                }

                if (requestedVersion == Volatile.Read(ref _refreshVersion))
                {
                    return;
                }
            }
        }

        private void ApplyVisibleEntries(IList<LogEntry> visibleEntries, int refreshVersion)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
            {
                _refreshPending = true;
                return;
            }

            int latestVersion = Volatile.Read(ref _refreshVersion);
            if (refreshVersion != latestVersion ||
                refreshVersion < Volatile.Read(ref _appliedRefreshVersion))
            {
                return;
            }

            _refreshPending = false;
            Volatile.Write(ref _appliedRefreshVersion, refreshVersion);
            _visibleEntries = visibleEntries ?? Array.Empty<LogEntry>();
            _logGrid.RowCount = _visibleEntries.Count;
            _logGrid.Invalidate();
            _logGrid.ClearSelection();

            if (AutoScrollToEnd)
            {
                ScrollToEnd();
            }
        }

        private void AttachLogger(ILoggerOutput logger)
        {
            ILoggerOutput nextLogger = logger ?? new LogStore();
            ILogViewSource nextViewSource = nextLogger as ILogViewSource;
            if (nextViewSource == null)
            {
                throw new InvalidOperationException("The bound logger must implement ILogViewSource.");
            }

            if (ReferenceEquals(_currentLogger, nextLogger))
            {
                _currentViewSource.MaxEntries = _maxLogEntries;
                ScheduleRefresh();
                return;
            }

            if (_currentViewSource != null)
            {
                _currentViewSource.Entries.CollectionChanged -= Logs_CollectionChanged;
            }

            _currentLogger = nextLogger;
            _currentViewSource = nextViewSource;
            _currentViewSource.MaxEntries = _maxLogEntries;
            _currentViewSource.Entries.CollectionChanged += Logs_CollectionChanged;
            _clearAnchorEntry = null;

            ScheduleRefresh();
        }

        private void ScrollToEnd()
        {
            if (_logGrid.RowCount == 0)
            {
                return;
            }

            try
            {
                _logGrid.FirstDisplayedScrollingRowIndex = _logGrid.RowCount - 1;
            }
            catch (InvalidOperationException)
            {
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        private void FilterButton_Click(object sender, EventArgs e)
        {
            if (_filterMenu == null)
            {
                return;
            }

            UpdateFilterUi();

            if (_filterMenu.Visible)
            {
                _filterMenu.Close();
                return;
            }

            Point location = _filterButton.PointToScreen(new Point(0, _filterButton.Height));
            _filterMenu.Show(location);
        }

        private void SelectAllFilterMenuItem_Click(object sender, EventArgs e)
        {
            ResetLevelFilter();
        }

        private void ClearAllFilterMenuItem_Click(object sender, EventArgs e)
        {
            LevelFilter = LogLevelFilter.None;
        }

        private void FilterMenuItem_Click(object sender, EventArgs e)
        {
            if (_updatingFilterMenu)
            {
                return;
            }

            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null || !(item.Tag is LogLevel))
            {
                return;
            }

            LogLevel level = (LogLevel)item.Tag;
            SetLevelVisible(level, item.Checked);
        }

        private void CloseFilterMenuItem_Click(object sender, EventArgs e)
        {
            if (_filterMenu != null && _filterMenu.Visible)
            {
                _filterMenu.Close();
            }
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_updatingSearchText)
            {
                return;
            }

            SearchText = _searchTextBox.Text;
        }

        private void SearchRefreshTimer_Tick(object sender, EventArgs e)
        {
            _searchRefreshTimer.Stop();
            ScheduleRefresh();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            ClearLogs();
        }

        private void ExecuteOnUiThread(Action action)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            if (!IsHandleCreated)
            {
                action();
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(action);
                }
                catch (ObjectDisposedException)
                {
                }
                catch (InvalidOperationException)
                {
                }

                return;
            }

            action();
        }

        private int GetCurrentEntryCount()
        {
            if (_currentViewSource == null)
            {
                return 0;
            }

            lock (_currentViewSource.SyncRoot)
            {
                return GetVisibleEntryCount();
            }
        }

        private IList<LogEntry> SnapshotCurrentEntries()
        {
            ILogViewSource viewSource = _currentViewSource;
            if (viewSource == null)
            {
                return Array.Empty<LogEntry>();
            }

            LogEntry clearAnchorEntry = _clearAnchorEntry;
            LogLevelFilter levelFilter = _levelFilter;
            string searchText = NormalizeSearchText(_searchText);

            lock (viewSource.SyncRoot)
            {
                int startIndex = GetVisibleStartIndex(viewSource, clearAnchorEntry);
                int visibleCount = viewSource.Entries.Count - startIndex;
                if (visibleCount <= 0)
                {
                    return Array.Empty<LogEntry>();
                }

                List<LogEntry> snapshot = new List<LogEntry>(visibleCount);
                for (int index = startIndex; index < viewSource.Entries.Count; index++)
                {
                    LogEntry entry = viewSource.Entries[index];
                    if (ShouldDisplayEntry(entry, levelFilter, searchText))
                    {
                        snapshot.Add(entry);
                    }
                }

                return snapshot;
            }
        }

        private int GetVisibleEntryCount()
        {
            ILogViewSource viewSource = _currentViewSource;
            if (viewSource == null)
            {
                return 0;
            }

            LogEntry clearAnchorEntry = _clearAnchorEntry;
            LogLevelFilter levelFilter = _levelFilter;
            string searchText = NormalizeSearchText(_searchText);
            int startIndex = GetVisibleStartIndex(viewSource, clearAnchorEntry);
            int visibleCount = 0;
            for (int index = startIndex; index < viewSource.Entries.Count; index++)
            {
                if (ShouldDisplayEntry(viewSource.Entries[index], levelFilter, searchText))
                {
                    visibleCount++;
                }
            }

            return visibleCount;
        }

        private static int GetVisibleStartIndex(ILogViewSource viewSource, LogEntry clearAnchorEntry)
        {
            if (clearAnchorEntry == null || viewSource == null)
            {
                return 0;
            }

            for (int index = viewSource.Entries.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(viewSource.Entries[index], clearAnchorEntry))
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

        private LogEntry GetVisibleEntry(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _visibleEntries.Count)
            {
                return null;
            }

            return _visibleEntries[rowIndex];
        }

        private void ScheduleSearchRefresh()
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            if (!IsHandleCreated)
            {
                _refreshPending = true;
                return;
            }

            _searchRefreshTimer.Stop();
            _searchRefreshTimer.Start();
        }

        private static bool ShouldDisplayEntry(LogEntry entry, LogLevelFilter levelFilter, string searchText)
        {
            return entry != null && levelFilter.Includes(entry.Level) && MatchesSearch(entry, searchText);
        }

        private static bool MatchesSearch(LogEntry entry, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return true;
            }

            return ContainsIgnoreCase(entry.LevelText, searchText) ||
                   ContainsIgnoreCase(entry.Message, searchText) ||
                   ContainsIgnoreCase(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"), searchText);
        }

        private void UpdateFilterUi()
        {
            if (_filterButton != null)
            {
                _filterButton.Text = GetFilterButtonText();
            }

            if (_filterMenuItems.Count == 0)
            {
                return;
            }

            _updatingFilterMenu = true;
            try
            {
                foreach (LogLevel level in FilterLevels)
                {
                    ToolStripMenuItem item;
                    if (_filterMenuItems.TryGetValue(level, out item))
                    {
                        item.Checked = IsLevelVisible(level);
                    }
                }
            }
            finally
            {
                _updatingFilterMenu = false;
            }
        }

        private void UpdateSearchUi()
        {
            if (_searchTextBox == null)
            {
                return;
            }

            if (string.Equals(_searchTextBox.Text, _searchText, StringComparison.Ordinal))
            {
                return;
            }

            _updatingSearchText = true;
            try
            {
                _searchTextBox.Text = _searchText;
            }
            finally
            {
                _updatingSearchText = false;
            }
        }

        private string GetFilterButtonText()
        {
            int selectedCount = GetSelectedLevelCount();
            if (selectedCount == FilterLevels.Length)
            {
                return "\u7b5b\u9009(\u5168\u90e8)";
            }

            if (selectedCount == 0)
            {
                return "\u7b5b\u9009(\u65e0)";
            }

            return string.Format("\u7b5b\u9009({0}/{1})", selectedCount, FilterLevels.Length);
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

        private static string NormalizeDisplayMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            string normalized = message.Replace("\r\n", "\n").Replace('\r', '\n');
            normalized = normalized.Replace("\n", Environment.NewLine);
            normalized = normalized.Replace("\t", "    ");
            return normalized;
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

        private static Color GetLevelColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return Color.FromArgb(148, 163, 184);
                case LogLevel.Debug:
                    return Color.FromArgb(96, 165, 250);
                case LogLevel.Info:
                    return Color.FromArgb(34, 211, 238);
                case LogLevel.Success:
                    return Color.FromArgb(74, 222, 128);
                case LogLevel.Warn:
                    return Color.FromArgb(250, 204, 21);
                case LogLevel.Error:
                    return Color.FromArgb(248, 113, 113);
                case LogLevel.Fatal:
                    return Color.FromArgb(244, 63, 94);
                default:
                    return Color.FromArgb(249, 250, 251);
            }
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

        private sealed class LogGridView : DataGridView
        {
            public LogGridView()
            {
                DoubleBuffered = true;
            }
        }
    }
}
