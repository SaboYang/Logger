# ILoggerOutput 实现说明

## 1. 文档目的

`ILoggerOutput` 是业务层统一使用的日志写入接口。仓库中的不同实现分别承担内存展示、组合分发、会话缓存、持久化以及外部日志框架适配等职责。

本文档用于说明各实现的功能边界、数据流和适用场景。

## 2. 实现分类

| 分类 | 实现 | 主要职责 |
| --- | --- | --- |
| 核心存储 | `LogStore` | 在内存中保存日志，提供 UI 绑定数据 |
| 组合分发 | `CompositeLogger` | 将一条日志分发到多个内部输出 |
| 聚合展示 | `MergedLogger` | 合并多个 logger 的日志视图，并广播写入 |
| 会话缓存 | `LogSessionBuffer` | 保存当前会话日志和会话统计 |
| 持久化 | `StorageLogWriter` | 通过 WAL 和存储后端异步持久化 |
| 外部适配 | `NLogLogger` | 将 Logger.Core 日志转发到 NLog |
| 外部适配 | `SerilogLogger` | 将 Logger.Core 日志转发到 Serilog |
| 反向 Sink | `LoggerOutputSink` | 将 Serilog 日志转发到 `ILoggerOutput` |

## 3. `LogStore`

文件：`Logger.Core/LogStore.Memory.cs`

`LogStore` 是最基础的内存日志实现，也是 UI 控件最直接使用的实现。

主要功能：

- 使用 `ObservableCollection<LogEntry>` 保存日志。
- 实现 `ILogViewSource`，可绑定到 WPF / WinForms 日志控件。
- 支持 `MaxEntries`，超出容量后移除最早日志。
- 支持 `MinimumLevel` 等级过滤。
- 支持单条和批量写入。
- 支持释放后停止接收日志。

数据流：

```text
ILoggerOutput
    -> LogStore
        -> ObservableCollection<LogEntry>
            -> WPF / WinForms 日志控件
```

适用场景：

- UI 实时日志展示。
- 单元测试中的内存日志。
- 不需要持久化的简单场景。

## 4. `CompositeLogger`

文件：`Logger.Core/CompositeLogger.cs`

`CompositeLogger` 是 `Logger.Core` 默认存储链路中的核心组合实现。它本身不负责具体存储，而是将一条日志分发给多个 `ILoggerOutput`。

默认情况下，`LogStoreLoggerFactory` 会组合以下输出：

```text
CompositeLogger
    +-> LogStore
    +-> LogSessionBuffer
    +-> StorageLogWriter
```

主要功能：

- 一个日志入口，多路输出。
- 统一执行最低等级过滤。
- 向 UI 暴露 `Entries`。
- 暴露会话标识和会话统计。
- 暴露文件输出状态和文件路径。
- 汇总内部输出的运行时指标。
- 统一释放内部输出资源。

这是通过 `Logger.Core` 获取普通 logger 时最常见的实现：

```csharp
ILoggerOutput logger = LogManager.GetLogger("OrderService");
```

## 5. `MergedLogger`

文件：`Logger.Core/MergedLogger.cs`

`MergedLogger` 用于把多个已有 logger 合并成一个新的 logger。它同时提供写入广播和 UI 视图聚合。

示例：

```csharp
ILoggerOutput appLogger = LogManager.GetLogger("App");
ILoggerOutput deviceLogger = LogManager.GetLogger("Device");

ILoggerOutput mergedLogger = LogManager.CreateMergedLogger(
    appLogger,
    deviceLogger);
```

主要功能：

- 写入时广播到所有目标 logger。
- 读取时合并目标 logger 的日志视图。
- 按时间排序。
- 支持最大显示条数。
- 监听目标视图变化并刷新聚合结果。
- 避免重复添加同一个 logger 实例。

数据流：

```text
MergedLogger
    +-> App logger
    +-> Device logger
    +-> Network logger
```

适用场景：

- 一个 UI 面板显示多个模块日志。
- 聚合多个设备、服务或业务模块的日志。
- 同一条日志需要同时写入多个目标。

## 6. `LogSessionBuffer`

文件：`Logger.Core/LogSessionBuffer.cs`

`LogSessionBuffer` 是内部会话缓存实现，不直接负责 UI 展示，也不负责最终持久化。

主要功能：

- 保存当前 logger 会话的日志。
- 提供 `SessionId` 和 `SessionStartedAt`。
- 统计当前会话的总日志数。
- 保存最近一部分会话日志。
- 通过最大缓存条数限制内存占用。
- 为会话查询、恢复和运行时指标提供数据。

`LogStore` 和 `LogSessionBuffer` 的区别：

| 实现 | 重点 |
| --- | --- |
| `LogStore` | UI 可观察集合和界面展示 |
| `LogSessionBuffer` | 会话数据、会话统计和有限缓存 |

## 7. `StorageLogWriter`

文件：`Logger.Core/StorageLogWriter.cs`

`StorageLogWriter` 是内部持久化实现，负责把日志先写入 WAL，再由后台转存到实际存储后端。

数据流：

```text
StorageLogWriter
    -> FileLogWalSpool
        -> ILogStorageBackend
            +-> 文本文件
            +-> CSV
            +-> SQLite
            +-> 自定义数据库
```

主要功能：

- 异步写入 WAL。
- 后台批量转存到存储后端。
- 支持背压和待处理容量限制。
- 支持进程异常后的 WAL 恢复。
- `Error` 和 `Fatal` 日志使用更强的持久化策略。
- 支持 `Buffered` 和 `Durable` 刷新模式。
- 提供待处理数量和丢弃数量等运行时指标。

它的重点是可靠持久化，不是 UI 展示。

## 8. `NLogLogger`

文件：`Logger.NLog/NLogLogger.cs`

`NLogLogger` 是 Logger.Core 到 NLog 的适配器，数据方向为：

```text
ILoggerOutput -> NLog
```

主要功能：

- 映射 Logger.Core 的日志等级。
- `Success` 默认映射为 NLog `Info`。
- 支持 logger 名称前缀和名称转换。
- 支持写入 `LoggerName`、`SessionId`、`SessionStartedAt`。
- 支持写入 `LoggerSuccess` 标记。
- 支持异常转发。
- 尊重 Logger.Core logger 的最低等级。

它适合已经使用 `Logger.Core` 接口，但底层希望由 NLog 负责输出的场景。

## 9. `SerilogLogger`

文件：`Logger.Serilog/SerilogLogger.cs`

`SerilogLogger` 是 Logger.Core 到 Serilog 的适配器，数据方向为：

```text
ILoggerOutput -> Serilog
```

主要功能：

- `Trace` 映射为 Serilog `Verbose`。
- `Info` 映射为 Serilog `Information`。
- `Warn` 映射为 Serilog `Warning`。
- `Success` 默认映射为 Serilog `Information`。
- `Error` 和 `Fatal` 映射到对应 Serilog 等级。
- 设置 `SourceContext`。
- 支持 `LoggerName`、`SessionId`、`SessionStartedAt` 和 `LoggerSuccess` 属性。
- 保留异常对象。
- 转义普通消息中的 `{}`，避免被误解析成 message template。

典型用法：

```csharp
var factory = new SerilogLoggerFactory(new SerilogLoggerOptions
{
    LoggerNamePrefix = "MyApp."
});

LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = LogManager.GetLogger("OrderService");
logger.Info("订单服务启动");
```

## 10. `LoggerOutputSink`

文件：`Logger.Serilog/LoggerOutputSink.cs`

`LoggerOutputSink` 是 Serilog 到 Logger.Core 的反向适配器。它不是 `ILoggerOutput` 的实现，而是 Serilog 的 `ILogEventSink`。

数据方向为：

```text
Serilog -> LoggerOutputSink -> ILoggerOutput
```

主要功能：

- 接收 Serilog 日志事件。
- 映射 Serilog 等级到 Logger.Core 等级。
- 使用 `RenderMessage()` 渲染消息模板。
- 将异常文本追加到 `ILoggerOutput` 消息中。
- 让 Serilog 继续作为主日志系统，同时把日志同步到 UI。

示例：

```csharp
ILoggerOutput panelLogger = new LogStoreLoggerFactory(
    logRootDirectoryPath: "Logs",
    minimumLevel: LogLevel.Trace)
    .CreateLogger("SerilogPanel");

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/app.log", shared: true)
    .WriteTo.Sink(new LoggerOutputSink(panelLogger))
    .CreateLogger();

Log.Information("这条日志会同时写入文件和 UI 日志控件");
```

注意：`panelLogger` 必须使用非 Serilog 实现，例如 `LogStoreLoggerFactory` 创建的 logger。不能使用 `SerilogLoggerFactory` 创建，否则会形成循环：

```text
Serilog
    -> LoggerOutputSink
        -> SerilogLogger
            -> Serilog
```

## 11. 工厂和服务关系

### 11.1 `LoggerService`

文件：`Logger.Core/LoggerService.cs`

`LoggerService` 负责：

- 按名称缓存 logger。
- 通过 `ILoggerFactory` 创建 logger。
- 归一化 logger 名称。
- 复用同名 logger。
- 提供 `Default` 和 `GetLogger(...)` 入口。
- 释放和清理 logger。

### 11.2 `LogStoreLoggerFactory`

文件：`Logger.Core/LogStoreLoggerFactory.cs`

`LogStoreLoggerFactory` 负责组装默认的 `CompositeLogger`：

```text
LogStoreLoggerFactory
    -> LogStore
    -> LogSessionBuffer
    -> StorageLogWriter
    -> CompositeLogger
```

因此，通常不需要手动创建 `CompositeLogger`、`LogSessionBuffer` 或 `StorageLogWriter`。

## 12. 两种推荐架构

### 12.1 Logger.Core 为主

适合业务层统一依赖 `ILoggerOutput`，并且需要内置 UI、WAL 和文件存储能力的项目。

```text
业务代码
    -> ILoggerOutput
        -> CompositeLogger
            +-> LogStore
            +-> LogSessionBuffer
            +-> StorageLogWriter
```

### 12.2 Serilog 为主

适合业务代码已经统一使用 Serilog，同时需要把日志显示到 WPF / WinForms 控件的项目。

```text
业务代码
    -> Serilog
        +-> File / Console / Database
        +-> LoggerOutputSink
            -> ILoggerOutput
                -> UI 日志控件
```

## 13. 应用方式

### 13.1 Logger.Core 默认日志链路

这是最完整的应用方式，日志会同时进入内存 UI、会话缓存和持久化存储。

```csharp
using Logger.Core;
using Logger.Core.Models;

var factory = new LogStoreLoggerFactory(
    logRootDirectoryPath: @"D:\Logs",
    minimumLevel: LogLevel.Trace,
    rollingMode: LogFileRollingMode.DayWithRetention,
    rollingRetentionDays: 30);

LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = LogManager.GetLogger("OrderService");
logger.Info("订单服务启动");
logger.Success("订单模块初始化完成");
logger.Error("订单保存失败");
```

如果需要绑定 UI 控件：

```csharp
LogPanel.Logger = logger;
```

### 13.2 只使用内存日志

适合测试、临时预览或不需要文件落盘的场景。

```csharp
using Logger.Core;

var logger = new LogStore
{
    MaxEntries = 5000
};

logger.Info("这条日志只保存在内存中");
LogPanel.Logger = logger;
```

应用退出时，如果 logger 由调用方创建，应释放它：

```csharp
using (logger)
{
    logger.Info("开始执行任务");
}
```

### 13.3 合并多个模块日志

适合一个 UI 面板展示多个模块或设备的日志。

```csharp
ILoggerOutput appLogger = LogManager.GetLogger("App");
ILoggerOutput deviceLogger = LogManager.GetLogger("Device");

ILoggerOutput mergedLogger = LogManager.CreateMergedLogger(appLogger, deviceLogger);
try
{
    LogPanel.Logger = mergedLogger;
    appLogger.Info("应用启动");
    deviceLogger.Info("设备连接成功");
}
finally
{
    var disposable = mergedLogger as IDisposable;
    if (disposable != null)
    {
        disposable.Dispose();
    }
}
```

`MergedLogger` 的写入会广播到所有目标 logger，视图会合并目标 logger 的已有日志。

### 13.4 Logger.Core 接入 NLog

```csharp
using Logger.Core;
using Logger.NLog;
using NLog;
using NLog.Config;
using NLog.Targets;

var target = new FileTarget("file")
{
    FileName = "Logs/nlog-${shortdate}.log"
};

LogManager.Setup().LoadConfiguration(builder =>
    builder.ForLogger().WriteTo(target));

var factory = new NLogLoggerFactory(new NLogLoggerOptions
{
    LoggerNamePrefix = "MyApp."
});

Logger.Core.LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = Logger.Core.LogManager.GetLogger("OrderService");
logger.Info("NLog 接入完成");
```

这里的业务代码仍然调用 `ILoggerOutput`，实际输出由 NLog 配置决定。

### 13.5 Logger.Core 接入 Serilog

```csharp
using Logger.Core;
using Logger.Serilog;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .WriteTo.File("Logs/serilog.log", shared: true)
    .CreateLogger();

var factory = new SerilogLoggerFactory(new SerilogLoggerOptions
{
    LoggerNamePrefix = "MyApp."
});

Logger.Core.LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = Logger.Core.LogManager.GetLogger("OrderService");
logger.Info("Serilog 接入完成");

Log.CloseAndFlush();
```

### 13.6 Serilog 为主日志，ILoggerOutput 为 UI 辅助输出

这是 Serilog 主导项目推荐的应用方式。业务代码调用 Serilog，文件、控制台等由 Serilog 负责，UI 通过 `LoggerOutputSink` 接收副本。

```csharp
using Logger.Core;
using Logger.Core.Models;
using Logger.Serilog;
using Serilog;

ILoggerOutput panelLogger = new LogStoreLoggerFactory(
    logRootDirectoryPath: "Logs",
    minimumLevel: LogLevel.Trace)
    .CreateLogger("SerilogPanel");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File("Logs/app.log", shared: true)
    .WriteTo.Sink(new LoggerOutputSink(panelLogger))
    .CreateLogger();

Log.Information("应用启动");
Log.Warning("库存不足");
Log.Error("订单保存失败");

LogPanel.Logger = panelLogger;
```

应用退出时，应按创建顺序释放资源：

```csharp
Log.CloseAndFlush();

var disposable = panelLogger as IDisposable;
if (disposable != null)
{
    disposable.Dispose();
}
```

`panelLogger` 必须使用 `LogStoreLoggerFactory` 或其他非 Serilog 实现创建，不能使用 `SerilogLoggerFactory`，否则会形成递归转发。

### 13.7 在 Microsoft.Extensions.Logging 中使用

如果业务代码依赖 `Microsoft.Extensions.Logging.ILogger<T>`，可以使用 `Logger.Extensions.Logging`，让 Microsoft logger 最终写入 Logger.Core：

```csharp
using Logger.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddLoggerCore();
});

using (var serviceProvider = services.BuildServiceProvider())
{
    ILogger<OrderService> logger =
        serviceProvider.GetRequiredService<ILogger<OrderService>>();

    logger.LogInformation("订单服务启动");
}
```

## 14. 选型建议

| 需求 | 推荐实现 |
| --- | --- |
| 只在内存显示日志 | `LogStore` |
| UI、会话、文件同时工作 | `CompositeLogger`，通常由 `LogStoreLoggerFactory` 创建 |
| 一个面板显示多个 logger | `MergedLogger` |
| 需要 WAL 和可靠持久化 | `StorageLogWriter`，通常由 `LogStoreLoggerFactory` 创建 |
| Logger.Core 接入 NLog | `NLogLoggerFactory` |
| Logger.Core 接入 Serilog | `SerilogLoggerFactory` |
| Serilog 主输出同步到 UI | `LoggerOutputSink` |
