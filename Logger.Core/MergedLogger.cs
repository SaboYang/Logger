using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Logger.Core.Models;

namespace Logger.Core
{
    public sealed class MergedLogger : ILoggerOutput, ILogViewSource, ILogLevelThreshold, ILogRuntimeMetricsSource, IDisposable
    {
        private readonly BulkObservableCollection<LogEntry> _entries = new BulkObservableCollection<LogEntry>();
        private readonly object _syncRoot = new object();
        private readonly ILoggerOutput[] _targets;
        private readonly ILogViewSource[] _viewSources;
        private readonly ViewSourceSubscription[] _subscriptions;
        private int _maxEntries = 500;
        private LogLevel _minimumLevel = LogLevel.Trace;
        private bool _disposed;

        public MergedLogger(params ILoggerOutput[] loggers)
            : this((IEnumerable<ILoggerOutput>)loggers)
        {
        }

        public MergedLogger(IEnumerable<ILoggerOutput> loggers)
        {
            _targets = NormalizeLoggers(loggers);
            _viewSources = ExtractViewSources(_targets);
            _subscriptions = CreateSubscriptions(_viewSources, this);

            RebuildEntries();
        }

        ~MergedLogger()
        {
            Dispose(false);
        }

        public object SyncRoot
        {
            get { return _syncRoot; }
        }

        public ObservableCollection<LogEntry> Entries
        {
            get { return _entries; }
        }

        public int MaxEntries
        {
            get
            {
                lock (_syncRoot)
                {
                    return _maxEntries;
                }
            }
            set
            {
                lock (_syncRoot)
                {
                    _maxEntries = Math.Max(1, value);
                }

                RebuildEntries();
            }
        }

        public LogLevel MinimumLevel
        {
            get
            {
                lock (_syncRoot)
                {
                    return _minimumLevel;
                }
            }
            set
            {
                lock (_syncRoot)
                {
                    _minimumLevel = value;
                }
            }
        }

        public int BufferedSessionEntryCount
        {
            get { return SumRuntimeMetric(metrics => metrics.BufferedSessionEntryCount); }
        }

        public int DroppedPendingEntryCount
        {
            get { return SumRuntimeMetric(metrics => metrics.DroppedPendingEntryCount); }
        }

        public void SetMinimumLevel(LogLevel minimumLevel)
        {
            MinimumLevel = minimumLevel;
        }

        public void Trace(string message)
        {
            AddLog(LogLevel.Trace, message);
        }

        public void Debug(string message)
        {
            AddLog(LogLevel.Debug, message);
        }

        public void Info(string message)
        {
            AddLog(LogLevel.Info, message);
        }

        public void Success(string message)
        {
            AddLog(LogLevel.Success, message);
        }

        public void Warning(string message)
        {
            AddLog(LogLevel.Warn, message);
        }

        public void Error(string message)
        {
            AddLog(LogLevel.Error, message);
        }

        public void Fatal(string message)
        {
            AddLog(LogLevel.Fatal, message);
        }

        public void AddLog(LogLevel level, string message)
        {
            if (!LogEntryFilter.MeetsMinimumLevel(level, MinimumLevel))
            {
                return;
            }

            for (int index = 0; index < _targets.Length; index++)
            {
                _targets[index]?.AddLog(level, message);
            }
        }

        public void AddLogs(IEnumerable<LogEntry> entries)
        {
            List<LogEntry> normalizedEntries = LogEntrySanitizer.NormalizeEntries(entries);
            if (normalizedEntries.Count == 0)
            {
                return;
            }

            List<LogEntry> filteredEntries = LogEntryFilter.FilterEntries(normalizedEntries, MinimumLevel);
            if (filteredEntries.Count == 0)
            {
                return;
            }

            for (int index = 0; index < _targets.Length; index++)
            {
                _targets[index]?.AddLogs(filteredEntries);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (int index = 0; index < _subscriptions.Length; index++)
            {
                _subscriptions[index].Dispose();
            }

            lock (_syncRoot)
            {
                _entries.ReplaceAll(Array.Empty<LogEntry>());
            }
        }

        private void RebuildEntries()
        {
            if (_disposed)
            {
                return;
            }

            List<LogEntry> mergedEntries = SnapshotEntries();
            lock (_syncRoot)
            {
                _entries.ReplaceAll(mergedEntries);
            }
        }

        private List<LogEntry> SnapshotEntries()
        {
            List<MergedEntryEnvelope> envelopes = new List<MergedEntryEnvelope>();
            int maxEntries = MaxEntries;

            for (int sourceIndex = 0; sourceIndex < _viewSources.Length; sourceIndex++)
            {
                ILogViewSource viewSource = _viewSources[sourceIndex];
                lock (viewSource.SyncRoot)
                {
                    for (int entryIndex = 0; entryIndex < viewSource.Entries.Count; entryIndex++)
                    {
                        LogEntry entry = viewSource.Entries[entryIndex];
                        if (entry != null)
                        {
                            envelopes.Add(new MergedEntryEnvelope(entry, sourceIndex, entryIndex));
                        }
                    }
                }
            }

            envelopes.Sort(CompareEnvelopes);

            if (envelopes.Count > maxEntries)
            {
                envelopes.RemoveRange(0, envelopes.Count - maxEntries);
            }

            List<LogEntry> mergedEntries = new List<LogEntry>(envelopes.Count);
            for (int index = 0; index < envelopes.Count; index++)
            {
                mergedEntries.Add(envelopes[index].Entry);
            }

            return mergedEntries;
        }

        private static int CompareEnvelopes(MergedEntryEnvelope left, MergedEntryEnvelope right)
        {
            int timestampCompare = left.Entry.Timestamp.CompareTo(right.Entry.Timestamp);
            if (timestampCompare != 0)
            {
                return timestampCompare;
            }

            int sourceCompare = left.SourceIndex.CompareTo(right.SourceIndex);
            if (sourceCompare != 0)
            {
                return sourceCompare;
            }

            return left.EntryIndex.CompareTo(right.EntryIndex);
        }

        private static ILoggerOutput[] NormalizeLoggers(IEnumerable<ILoggerOutput> loggers)
        {
            List<ILoggerOutput> normalizedLoggers = new List<ILoggerOutput>();
            if (loggers == null)
            {
                return normalizedLoggers.ToArray();
            }

            foreach (ILoggerOutput logger in loggers)
            {
                if (logger == null || ContainsReference(normalizedLoggers, logger))
                {
                    continue;
                }

                normalizedLoggers.Add(logger);
            }

            return normalizedLoggers.ToArray();
        }

        private static ILogViewSource[] ExtractViewSources(IEnumerable<ILoggerOutput> loggers)
        {
            List<ILogViewSource> viewSources = new List<ILogViewSource>();
            if (loggers == null)
            {
                return viewSources.ToArray();
            }

            foreach (ILoggerOutput logger in loggers)
            {
                ILogViewSource viewSource = logger as ILogViewSource;
                if (viewSource == null || ContainsReference(viewSources, viewSource))
                {
                    continue;
                }

                viewSources.Add(viewSource);
            }

            return viewSources.ToArray();
        }

        private static bool ContainsReference<T>(IList<T> items, T candidate)
            where T : class
        {
            for (int index = 0; index < items.Count; index++)
            {
                if (ReferenceEquals(items[index], candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private int SumRuntimeMetric(Func<ILogRuntimeMetricsSource, int> selector)
        {
            int sum = 0;
            for (int index = 0; index < _targets.Length; index++)
            {
                ILogRuntimeMetricsSource metrics = _targets[index] as ILogRuntimeMetricsSource;
                if (metrics != null)
                {
                    sum += selector(metrics);
                }
            }

            return sum;
        }

        private static ViewSourceSubscription[] CreateSubscriptions(IList<ILogViewSource> viewSources, MergedLogger owner)
        {
            ViewSourceSubscription[] subscriptions = new ViewSourceSubscription[viewSources.Count];
            for (int index = 0; index < viewSources.Count; index++)
            {
                subscriptions[index] = new ViewSourceSubscription(owner, viewSources[index]);
            }

            return subscriptions;
        }

        private struct MergedEntryEnvelope
        {
            public MergedEntryEnvelope(LogEntry entry, int sourceIndex, int entryIndex)
            {
                Entry = entry;
                SourceIndex = sourceIndex;
                EntryIndex = entryIndex;
            }

            public LogEntry Entry { get; }

            public int SourceIndex { get; }

            public int EntryIndex { get; }
        }

        private sealed class ViewSourceSubscription : IDisposable
        {
            private readonly WeakReference<MergedLogger> _owner;
            private readonly ILogViewSource _viewSource;
            private bool _disposed;

            public ViewSourceSubscription(MergedLogger owner, ILogViewSource viewSource)
            {
                _owner = new WeakReference<MergedLogger>(owner);
                _viewSource = viewSource;
                _viewSource.Entries.CollectionChanged += ViewEntries_CollectionChanged;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _viewSource.Entries.CollectionChanged -= ViewEntries_CollectionChanged;
            }

            private void ViewEntries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                MergedLogger owner;
                if (!_owner.TryGetTarget(out owner) || owner._disposed)
                {
                    Dispose();
                    return;
                }

                owner.RebuildEntries();
            }
        }
    }
}
