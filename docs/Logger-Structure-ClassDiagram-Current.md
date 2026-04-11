# Logger 结构类图

这份结构类图按当前代码库整理，重点覆盖三层：
- 核心运行时与持久化管线
- WPF / WinForms 控件层
- Demo 宿主与样例窗口

说明：
- `ILoggerOutput` 只负责写日志。
- `ILogViewSource` 负责给 UI 展示。
- `LoggerConfiguration` 会先读取本地 `Logger.config`。
- 如果文件不存在，直接走默认配置。
- 如果文件存在，先按 logger 名称索引，找不到再回退 `<default>`。

## 核心运行时

```mermaid
classDiagram
    direction LR

    class "Logger.Core.ILoggerOutput" as ILoggerOutput {
        <<interface>>
        +SetMinimumLevel(minimumLevel)
        +Trace(message)
        +Debug(message)
        +Info(message)
        +Success(message)
        +Warning(message)
        +Error(message)
        +Fatal(message)
        +AddLog(level, message)
        +AddLogs(entries)
    }

    class "Logger.Core.ILogViewSource" as ILogViewSource {
        <<interface>>
        +SyncRoot : object
        +Entries : ObservableCollection~LogEntry~
        +MaxEntries : int
    }

    class "Logger.Core.ILogSessionSource" as ILogSessionSource {
        <<interface>>
        +SessionId : Guid
        +SessionStartedAt : DateTime
        +SessionEntryCount : int
        +GetSessionEntriesSnapshot()
    }

    class "Logger.Core.ILogFileSource" as ILogFileSource {
        <<interface>>
        +IsFileOutputEnabled : bool
        +LogFilePath : string
    }

    class "Logger.Core.ILogLevelThreshold" as ILogLevelThreshold {
        <<interface>>
        +MinimumLevel : LogLevel
    }

    class "Logger.Core.ILogRuntimeMetricsSource" as ILogRuntimeMetricsSource {
        <<interface>>
        +BufferedSessionEntryCount : int
        +DroppedPendingEntryCount : int
    }

    class "Logger.Core.ILoggerFactory" as ILoggerFactory {
        <<interface>>
        +CreateLogger(name) ILoggerOutput
    }

    class "Logger.Core.ILoggerService" as ILoggerService {
        <<interface>>
        +Factory : ILoggerFactory
        +Default : ILoggerOutput
        +GetLogger(name) ILoggerOutput
        +TryGetLogger(name, out logger) bool
    }

    class "Logger.Core.LogManager" as LogManager {
        <<static>>
        +Service : ILoggerService
        +Factory : ILoggerFactory
        +Default : ILoggerOutput
        +GetLogger(name) ILoggerOutput
        +TryGetLogger(name, out logger) bool
        +CreateMergedLogger(loggers) ILoggerOutput
        +ReleaseLogger(logger) bool
    }

    class "Logger.Core.LoggerService" as LoggerService {
        +Shared : LoggerService
        +Factory : ILoggerFactory
        +Default : ILoggerOutput
        +GetLogger(name) ILoggerOutput
        +TryGetLogger(name, out logger) bool
    }

    class "Logger.Core.LoggerConfiguration" as LoggerConfiguration {
        <<static>>
        +CreateDefaultFactory() ILoggerFactory
        +ResolveRuntimeSettings(loggerName)
    }

    class "Logger.Core.ConfigurableLogStoreLoggerFactory" as ConfigurableLogStoreLoggerFactory {
        +CreateLogger(name) ILoggerOutput
    }

    class "Logger.Core.LogStoreLoggerFactory" as LogStoreLoggerFactory {
        +CreateLogger(name) ILoggerOutput
    }

    class "Logger.Core.MergedLogger" as MergedLogger {
        +SyncRoot : object
        +Entries : ObservableCollection~LogEntry~
        +MaxEntries : int
        +MinimumLevel : LogLevel
        +SetMinimumLevel(minimumLevel)
        +AddLog(level, message)
        +AddLogs(entries)
    }

    class "Logger.Core.CompositeLogger" as CompositeLogger {
        +MinimumLevel : LogLevel
        +SetMinimumLevel(minimumLevel)
        +AddLog(level, message)
        +AddLogs(entries)
    }

    class "Logger.Core.LogStore" as LogStore {
        +SyncRoot : object
        +Entries : ObservableCollection~LogEntry~
        +MaxEntries : int
        +MinimumLevel : LogLevel
        +SetMinimumLevel(minimumLevel)
        +AddLog(level, message)
        +AddLogs(entries)
    }

    class "Logger.Core.LogSessionBuffer" as LogSessionBuffer {
        +SessionId : Guid
        +SessionStartedAt : DateTime
        +SessionEntryCount : int
        +SetMinimumLevel(minimumLevel)
        +GetSessionEntriesSnapshot()
    }

    class "Logger.Core.StorageLogWriter" as StorageLogWriter {
        +BufferedSessionEntryCount : int
        +DroppedPendingEntryCount : int
        +SetMinimumLevel(minimumLevel)
    }

    class "Logger.Core.LogStorageContext" as LogStorageContext {
        +LoggerName : string
        +SessionId : Guid
        +SessionStartedAt : DateTime
        +MinimumLevel : LogLevel
        +MaxBufferedSessionEntries : int
        +MaxPendingStorageEntries : int
        +SpoolRootDirectoryPath : string
        +SpoolFlushMode : LogSpoolFlushMode
    }

    class "Logger.Core.ILogStorageBackendFactory" as ILogStorageBackendFactory {
        <<interface>>
        +CreateBackend(context) ILogStorageBackend
    }

    class "Logger.Core.ILogStorageBackend" as ILogStorageBackend {
        <<interface>>
        +WriteBatch(entries, context)
    }

    class "Logger.Core.TextFileLogStorageBackendFactory" as TextFileLogStorageBackendFactory {
        +CreateBackend(context) ILogStorageBackend
    }

    class "Logger.Core.CsvFileLogStorageBackendFactory" as CsvFileLogStorageBackendFactory {
        +CreateBackend(context) ILogStorageBackend
    }

    class "Logger.Core.TextFileLogStorageBackend" as TextFileLogStorageBackend {
        +IsFileOutputEnabled : bool
        +LogFilePath : string
    }

    class "Logger.Core.CsvFileLogStorageBackend" as CsvFileLogStorageBackend {
        +IsFileOutputEnabled : bool
        +LogFilePath : string
    }

    class "Logger.Core.FileLogWalSpool" as FileLogWalSpool {
        +HasPendingEntries : bool
        +AppendEntries(entries, requireDurableFlush)
        +TryReadNextBatch(maxCount, out entries, out commitOffset)
        +MarkCommitted(commitOffset)
        +Dispose()
    }

    class "Logger.Core.LogFileRetentionCleaner" as LogFileRetentionCleaner {
        +CleanupExpiredDailyLogFiles(pathProvider, now)
    }

    class "Logger.Core.LoggerPathUtility" as LoggerPathUtility {
        <<static>>
        +NormalizeLoggerName(loggerName) string
        +ResolveLogRootDirectory(path) string
        +ResolveSpoolRootDirectory(path) string
        +BuildSpoolDirectoryPath(loggerName, path) string
    }

    class "Logger.Core.BulkObservableCollection<T>" as BulkObservableCollection {
        +AddRange(items)
        +RemoveRange(index, count)
    }

    class "Logger.Core.Models.LogEntry" as LogEntry {
        +Timestamp : DateTime
        +Level : LogLevel
        +LevelText : string
        +Message : string
    }

    class "Logger.Core.Models.LogLevel" as LogLevel {
        <<enumeration>>
        Trace
        Debug
        Info
        Success
        Warn
        Error
        Fatal
    }

    class "Logger.Core.LogLevelFilter" as LogLevelFilter {
        <<enumeration>>
        None
        Trace
        Debug
        Info
        Success
        Warn
        Error
        Fatal
        All
    }

    class "Logger.Core.LogFileRollingMode" as LogFileRollingMode {
        <<enumeration>>
        SingleFile
        Year
        Month
        Week
        Day
        DayWithRetention
    }

    class "Logger.Core.LogSpoolFlushMode" as LogSpoolFlushMode {
        <<enumeration>>
        Buffered
        Durable
    }

    ILoggerService <|.. LoggerService
    ILoggerFactory <|.. ConfigurableLogStoreLoggerFactory
    ILoggerFactory <|.. LogStoreLoggerFactory

    ILoggerOutput <|.. LogStore
    ILoggerOutput <|.. MergedLogger
    ILoggerOutput <|.. CompositeLogger
    ILoggerOutput <|.. LogSessionBuffer
    ILoggerOutput <|.. StorageLogWriter
    ILoggerOutput <|.. TextFileLogStorageBackend
    ILoggerOutput <|.. CsvFileLogStorageBackend

    ILogViewSource <|.. LogStore
    ILogViewSource <|.. MergedLogger
    ILogViewSource <|.. CompositeLogger

    ILogSessionSource <|.. LogSessionBuffer
    ILogSessionSource <|.. CompositeLogger

    ILogFileSource <|.. TextFileLogStorageBackend
    ILogFileSource <|.. CsvFileLogStorageBackend
    ILogFileSource <|.. CompositeLogger

    ILogLevelThreshold <|.. LogStore
    ILogLevelThreshold <|.. MergedLogger
    ILogLevelThreshold <|.. CompositeLogger

    ILogRuntimeMetricsSource <|.. MergedLogger
    ILogRuntimeMetricsSource <|.. CompositeLogger
    ILogRuntimeMetricsSource <|.. LogSessionBuffer
    ILogRuntimeMetricsSource <|.. StorageLogWriter

    LogManager ..> ILoggerService : facade
    LogManager ..> MergedLogger : CreateMergedLogger()
    LoggerService --> ILoggerFactory : uses
    LoggerService --> ILoggerOutput : caches/returns

    LoggerConfiguration ..> ILoggerFactory : create default factory
    LoggerConfiguration ..> ConfigurableLogStoreLoggerFactory : wraps config
    LoggerConfiguration ..> LoggerPathUtility : normalize names

    ConfigurableLogStoreLoggerFactory --> LogStoreLoggerFactory : delegates
    LogStoreLoggerFactory --> LogStorageContext : builds
    LogStoreLoggerFactory --> LogStore : view source
    LogStoreLoggerFactory --> LogSessionBuffer : session source
    LogStoreLoggerFactory --> ILogStorageBackendFactory : optional backend
    LogStoreLoggerFactory --> StorageLogWriter : storage writer
    LogStoreLoggerFactory --> CompositeLogger : returns

    CompositeLogger *-- LogStore : view source
    CompositeLogger *-- LogSessionBuffer : session source
    CompositeLogger *-- StorageLogWriter : storage writer

    LogStore *-- BulkObservableCollection : entries
    LogStore --> LogEntry : stores
    LogEntry --> LogLevel

    MergedLogger *-- BulkObservableCollection : merged entries
    MergedLogger --> LogEntry : merges

    LogSessionBuffer --> LogStorageContext : context
    StorageLogWriter --> FileLogWalSpool : WAL
    StorageLogWriter --> ILogStorageBackend : writes batch
    FileLogWalSpool --> LogStorageContext : spool paths

    TextFileLogStorageBackendFactory --> LogFileRollingMode : rolling
    CsvFileLogStorageBackendFactory --> LogFileRollingMode : rolling
    TextFileLogStorageBackend --> LogFileRetentionCleaner : retention
    CsvFileLogStorageBackend --> LogFileRetentionCleaner : retention
    TextFileLogStorageBackend --> LoggerPathUtility : paths
    CsvFileLogStorageBackend --> LoggerPathUtility : paths
```

`LogStoreLoggerFactory` 是当前最主要的运行时工厂。它会把 `LogStore`、`LogSessionBuffer` 和可选的 `StorageLogWriter` 组装到 `CompositeLogger` 里。

`LoggerConfiguration` 则负责把本地 `Logger.config` 映射为按名称索引的运行时配置。

## UI 与 Demo

```mermaid
classDiagram
    direction LR

    class "Logger.Wpf.Controls.LogPanelControl" as WpfLogPanelControl {
        +Header : string
        +MaxLogEntries : int
        +Logger : ILoggerOutput
        +LevelFilter : LogLevelFilter
        +SearchText : string
        +SearchBoxVisible : bool
        +ClearLogs()
        +CopyLogs()
        +ResetLevelFilter()
    }

    class "Logger.WinForms.Controls.LogPanelControl" as WinFormsLogPanelControl {
        +Header : string
        +MaxLogEntries : int
        +Logger : ILoggerOutput
        +AutoScrollToEnd : bool
        +HighThroughputMode : bool
        +LevelFilter : LogLevelFilter
        +SearchText : string
        +SearchBoxVisible : bool
        +ClearLogs()
        +CopyLogs()
        +ResetLevelFilter()
    }

    class "Logger.WinForms.Controls.WpfLogPanelControl" as WinFormsWpfLogPanelControl {
        +Header : string
        +MaxLogEntries : int
        +Logger : ILoggerOutput
        +LevelFilter : LogLevelFilter
        +SearchText : string
        +SearchBoxVisible : bool
        +ClearLogs()
        +CopyLogs()
        +ResetLevelFilter()
    }

    class "WpfApp1.MainWindow" as MainWindow {
        -_logger : ILoggerOutput
    }

    class "Logger.WinForms.Demo.MainForm" as MainForm {
        -_logPanel : LogPanelControl
        -_logger : ILoggerOutput
    }

    class "Logger.WinForms.Demo.WpfHostForm" as WpfHostForm {
        -_logger : ILoggerOutput
    }

    class "Logger.WinForms.Demo.LoggerFactoryIsolatedDemoForm" as LoggerFactoryIsolatedDemoForm {
        -_serviceLogger : ILoggerOutput
        -_factoryLogger : ILoggerOutput
    }

    class "Logger.WinForms.Demo.LoggerConfigDemoForm" as LoggerConfigDemoForm
    class "Logger.WinForms.Demo.FileLogDemoForm" as FileLogDemoForm
    class "Logger.WinForms.Demo.StorageBackendDemoForm" as StorageBackendDemoForm
    class "Logger.WinForms.Demo.MemoryMetricsDemoForm" as MemoryMetricsDemoForm

    class "Logger.WinForms.Demo.StressSummaryPanel" as StressSummaryPanel {
        +UpdateSummary(summary)
    }

    class "Logger.WinForms.Demo.CodeSamplePanel" as CodeSamplePanel

    class "Logger.WinForms.Demo.StressTestSummary" as StressTestSummary {
        +Scenario : string
        +State : StressTestState
        +LogCount : int
        +DurationMs : long
        +Timestamp : DateTime
        +Details : string
        +Throughput : double
    }

    class "Logger.WinForms.Demo.StressTestState" as StressTestState {
        <<enumeration>>
        NotRun
        Running
        Succeeded
        Failed
    }

    class "Logger.WinForms.Demo.DemoTableLogStorageBackend" as DemoTableLogStorageBackend
    class "Logger.WinForms.Demo.DemoTableLogStorageBackendFactory" as DemoTableLogStorageBackendFactory
    class "Logger.WinForms.Demo.SlowTextFileLogStorageBackendFactory" as SlowTextFileLogStorageBackendFactory

    class "Logger.Core.LogManager" as LogManager2
    class "Logger.Core.ILoggerOutput" as ILoggerOutput2

    MainWindow --> WpfLogPanelControl : binds Logger
    MainWindow --> ILoggerOutput2 : writes
    MainWindow ..> LogManager2 : GetLogger()

    MainForm --> WinFormsLogPanelControl : binds Logger
    MainForm --> StressSummaryPanel
    MainForm --> CodeSamplePanel
    MainForm ..> WpfHostForm : opens
    MainForm ..> LoggerFactoryIsolatedDemoForm : opens
    MainForm ..> LoggerConfigDemoForm : opens
    MainForm ..> FileLogDemoForm : opens
    MainForm ..> StorageBackendDemoForm : opens
    MainForm ..> MemoryMetricsDemoForm : opens
    MainForm ..> LogManager2 : Factory.CreateLogger()

    WpfHostForm --> WinFormsWpfLogPanelControl : binds Logger
    WpfHostForm ..> LogManager2 : Factory.CreateLogger()

    LoggerFactoryIsolatedDemoForm --> WinFormsLogPanelControl : binds logger
    LoggerFactoryIsolatedDemoForm ..> LogManager2 : GetLogger()/Factory.CreateLogger()

    StressSummaryPanel --> StressTestSummary : displays
    StressTestSummary --> StressTestState

    StorageBackendDemoForm --> DemoTableLogStorageBackendFactory : demo backend
    StorageBackendDemoForm --> DemoTableLogStorageBackend : demo backend
    StorageBackendDemoForm --> SlowTextFileLogStorageBackendFactory : demo backend

    WinFormsWpfLogPanelControl *-- WpfLogPanelControl : hosts via ElementHost
```

## 读图提示

- `Logger.Core` 是所有层的基础。
- `Logger.Wpf` 和 `Logger.WinForms` 只负责展示与交互，不承载业务写日志逻辑。
- `Logger.WinForms.Controls.WpfLogPanelControl` 是 WinForms 对 WPF 日志控件的封装宿主。
- `Logger.WinForms.Demo` 和 `WpfApp1` 都采用“先拿 `ILoggerOutput`，再绑定到控件”的方式。

更多说明可以继续参考：
- [README.md](../README.md)
- [Logger-API.md](Logger-API.md)
