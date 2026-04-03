# Logger Structure Class Diagram

This document describes the current structure of the active solution:

- `Logger.Core`
- `Logger.Wpf`
- `Logger.WinForms`
- `Logger.WinForms.Demo`
- `WpfApp1`

Design notes:

- `ILoggerOutput` is the write-only logging interface used by application code.
- `ILogViewSource` is the UI-facing read model used by controls to render logs.
- `ClearLogs()` is a UI behavior and is intentionally not part of `ILoggerOutput`.

## Core And UI Layers

```mermaid
classDiagram
    direction LR

    class "Logger.Core.ILoggerOutput" as ILoggerOutput {
        <<interface>>
        +AddTrace(message)
        +AddDebug(message)
        +AddInfo(message)
        +AddSuccess(message)
        +AddWarning(message)
        +AddError(message)
        +AddFatal(message)
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
        +AddTrace(message)
        +AddDebug(message)
        +AddInfo(message)
        +AddSuccess(message)
        +AddWarning(message)
        +AddError(message)
        +AddFatal(message)
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
    LoggerService --> ILoggerOutput : caches and returns
    LogStoreLoggerFactory --> LogStore : creates
    LogManager ..> ILoggerService : delegates
    LogManager ..> ILoggerFactory
    LogManager ..> ILoggerOutput

    WpfLogPanelControl --> ILoggerOutput : binds
    WpfLogPanelControl --> ILogViewSource : reads
    WpfLogPanelControl --> LogEntry : displays
    LogLevelToBrushConverter --> LogLevel : maps

    WinFormsLogPanelControl --> ILoggerOutput : binds
    WinFormsLogPanelControl --> ILogViewSource : reads
    WinFormsLogPanelControl --> LogEntry : displays

    WinFormsWpfLogPanelControl *-- WpfLogPanelControl : hosts
    WinFormsWpfLogPanelControl --> ILoggerOutput : binds
```

## Demo And Host Relations

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
    LoggerFactoryIsolatedDemoForm --> WinFormsLogPanelControl2 : binds service and factory loggers
    LoggerFactoryIsolatedDemoForm ..> LogManager2 : GetLogger() and Factory.CreateLogger()

    StressSummaryPanel --> StressTestSummary : displays
    StressTestSummary --> StressTestState
```

## Summary

- `Logger.Core` defines the write interface, view interface, service layer, and default implementations.
- `Logger.Wpf` and `Logger.WinForms` both bind to `ILoggerOutput` instead of requiring callers to push text into the control.
- `Logger.WinForms.Controls.WpfLogPanelControl` hosts the WPF log control inside WinForms.
- `Logger.WinForms.Demo` and `WpfApp1` both follow the same pattern: acquire an `ILoggerOutput`, bind it to the control, then write logs through the interface.
