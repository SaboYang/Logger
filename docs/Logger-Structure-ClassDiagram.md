# Logger 结构类图

这份类图基于当前解决方案中的有效项目生成：

- `Logger.Core`
- `Logger.Wpf`
- `Logger.WinForms`
- `Logger.WinForms.Demo`
- `WpfApp1`

说明：

- `ILoggerOutput` 只负责“写日志”。
- `ILogViewSource` 负责“给 UI 展示日志”。
- `ClearLogs()` 属于控件侧 UI 行为，不属于 `ILoggerOutput`。

## 核心层与控件层

```mermaid
classDiagram
    direction LR

    class "Logger.Core.ILoggerOutput" as ILoggerOutput {
        <<interface>>
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
        +Configure(service)
    }

    class "Logger.Core.LoggerService" as LoggerService {
        +Shared : LoggerService
        +Factory : ILoggerFactory
        +Default : ILoggerOutput
        +GetLogger(name) ILoggerOutput
        +TryGetLogger(name, out logger) bool
    }

    class "Logger.Core.LogStoreLoggerFactory" as LogStoreLoggerFactory {
        +CreateLogger(name) ILoggerOutput
    }

    class "Logger.Core.LogStore" as LogStore {
        +SyncRoot : object
        +Entries : ObservableCollection~LogEntry~
        +MaxEntries : int
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

    class "Logger.Wpf.Controls.LogPanelControl" as WpfLogPanelControl {
        +Header : string
        +MaxLogEntries : int
        +Logger : ILoggerOutput
        +ClearLogs()
    }

    class "Logger.Wpf.Converters.LogLevelToBrushConverter" as LogLevelToBrushConverter {
        +Convert(value, targetType, parameter, culture) object
        +ConvertBack(value, targetType, parameter, culture) object
    }

    class "Logger.WinForms.Controls.LogPanelControl" as WinFormsLogPanelControl {
        +Header : string
        +AutoScrollToEnd : bool
        +MaxLogEntries : int
        +Logger : ILoggerOutput
        +ClearLogs()
    }

    class "Logger.WinForms.Controls.WpfLogPanelControl" as WinFormsWpfLogPanelControl {
        +Header : string
        +MaxLogEntries : int
        +Logger : ILoggerOutput
        +ClearLogs()
    }

    ILoggerService <|.. LoggerService
    ILoggerFactory <|.. LogStoreLoggerFactory
    ILoggerOutput <|.. LogStore
    ILogViewSource <|.. LogStore
    BulkObservableCollection --|> ObservableCollection
    LogStore *-- BulkObservableCollection : entries
    LogStore --> LogEntry : stores
    LogEntry --> LogLevel
    LoggerService --> ILoggerFactory : uses
    LoggerService --> ILoggerOutput : caches/returns
    LogStoreLoggerFactory --> LogStore : creates
    LogManager ..> ILoggerService : delegates
    LogManager ..> ILoggerFactory
    LogManager ..> ILoggerOutput

    WpfLogPanelControl --> ILoggerOutput : binds
    WpfLogPanelControl --> ILogViewSource : reads
    WpfLogPanelControl --> LogEntry : shows
    LogLevelToBrushConverter --> LogLevel : maps color

    WinFormsLogPanelControl --> ILoggerOutput : binds
    WinFormsLogPanelControl --> ILogViewSource : reads
    WinFormsLogPanelControl --> LogEntry : shows

    WinFormsWpfLogPanelControl *-- WpfLogPanelControl : hosts
    WinFormsWpfLogPanelControl --> ILoggerOutput : binds
```

## Demo 与宿主关系

```mermaid
classDiagram
    direction LR

    class "WpfApp1.MainWindow" as MainWindow {
        -_logger : ILoggerOutput
    }

    class "Logger.WinForms.Demo.MainForm" as MainForm {
        -_logger : ILoggerOutput
    }

    class "Logger.WinForms.Demo.WpfHostForm" as WpfHostForm {
        -_logger : ILoggerOutput
    }

    class "Logger.WinForms.Demo.LoggerFactoryIsolatedDemoForm" as LoggerFactoryIsolatedDemoForm {
        -_serviceLogger : ILoggerOutput
        -_factoryLogger : ILoggerOutput
    }

    class "Logger.WinForms.Demo.StressSummaryPanel" as StressSummaryPanel {
        +UpdateSummary(summary)
    }

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

    class "Logger.Wpf.Controls.LogPanelControl" as WpfLogPanelControl2
    class "Logger.WinForms.Controls.LogPanelControl" as WinFormsLogPanelControl2
    class "Logger.WinForms.Controls.WpfLogPanelControl" as WinFormsWpfLogPanelControl2
    class "Logger.Core.ILoggerOutput" as ILoggerOutput2
    class "Logger.Core.LogManager" as LogManager2

    MainWindow --> ILoggerOutput2 : writes
    MainWindow --> WpfLogPanelControl2 : binds Logger
    MainWindow ..> LogManager2 : GetLogger()

    MainForm --> ILoggerOutput2 : writes
    MainForm --> WinFormsLogPanelControl2 : binds Logger
    MainForm --> StressSummaryPanel
    MainForm ..> WpfHostForm : opens
    MainForm ..> LoggerFactoryIsolatedDemoForm : opens
    MainForm ..> LogManager2 : Factory.CreateLogger()

    WpfHostForm --> ILoggerOutput2 : writes
    WpfHostForm --> WinFormsWpfLogPanelControl2 : binds Logger
    WpfHostForm --> StressSummaryPanel
    WpfHostForm ..> LogManager2 : Factory.CreateLogger()

    LoggerFactoryIsolatedDemoForm --> ILoggerOutput2 : writes
    LoggerFactoryIsolatedDemoForm --> WinFormsLogPanelControl2 : binds service/factory logger
    LoggerFactoryIsolatedDemoForm ..> LogManager2 : GetLogger()/Factory.CreateLogger()

    StressSummaryPanel --> StressTestSummary : displays
    StressTestSummary --> StressTestState
```

## 当前结构总结

- `Logger.Core` 定义日志写入接口、日志展示接口、日志服务和默认实现。
- `Logger.Wpf` 与 `Logger.WinForms` 都通过绑定 `ILoggerOutput` 来显示日志，不要求业务代码直接调用控件方法。
- `Logger.WinForms.Controls.WpfLogPanelControl` 是对 WPF 日志控件的 WinForms 封装宿主。
- `Logger.WinForms.Demo` 和 `WpfApp1` 都采用“先拿 `ILoggerOutput`，再绑定到控件”的模式。
