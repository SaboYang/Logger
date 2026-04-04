# Logger API 调用文档

## 1. 适用范围

本文档说明以下组件的调用方式：

- `Logger.Core`
- `Logger.Wpf`
- `Logger.WinForms`

目标是让调用方只关心两件事：

1. 怎么获取 logger
2. 怎么把 logger 绑定到 UI 控件

---

## 2. 核心接口

### 2.1 `ILoggerOutput`

业务代码主要依赖这个接口。

```csharp
public interface ILoggerOutput
{
    void SetMinimumLevel(LogLevel minimumLevel);

    void AddTrace(string message);
    void AddDebug(string message);
    void AddInfo(string message);
    void AddSuccess(string message);
    void AddWarning(string message);
    void AddError(string message);
    void AddFatal(string message);

    void AddLog(LogLevel level, string message);
    void AddLogs(IEnumerable<LogEntry> entries);
}
```

说明：

- `SetMinimumLevel(...)` 用来设置当前 logger 的入口最低等级
- 等级过滤发生在 `AddLog(...)` / `AddLogs(...)` 入口
- 下游会话、文件、自定义存储后端只消费通过入口过滤后的日志

### 2.2 `ILoggerService`

负责管理 logger 实例。

```csharp
public interface ILoggerService
{
    ILoggerFactory Factory { get; }
    ILoggerOutput Default { get; }
    ILoggerOutput GetLogger(string name);
    bool TryGetLogger(string name, out ILoggerOutput logger);
}
```

### 2.3 `ILoggerFactory`

负责按名称创建 logger。

```csharp
public interface ILoggerFactory
{
    ILoggerOutput CreateLogger(string name);
}
```

### 2.4 `LogManager`

全局入口。默认推荐优先通过它获取 logger。

```csharp
ILoggerOutput logger = LogManager.GetLogger("MyApp");
```

---

## 3. 同名 logger 规则

在同一个 `ILoggerService` 实例中：

- 相同名称获取到的是同一个 logger 实例
- 名称忽略大小写
- 名称前后空格会被裁剪
- 空名称会归一成 `Default`

示例：

```csharp
ILoggerOutput logger1 = LogManager.GetLogger("MyApp");
ILoggerOutput logger2 = LogManager.GetLogger(" myapp ");

bool sameInstance = ReferenceEquals(logger1, logger2); // true
```

注意：

- 如果调用了 `LogManager.Configure(...)` 切换到新的 `ILoggerService`
- 后续获取的同名 logger 会进入新的缓存空间

---

## 4. 快速开始

### 4.1 最简单的写法

```csharp
using Logger.Core;

ILoggerOutput logger = LogManager.GetLogger("MyApp");

logger.AddInfo("应用启动");
logger.AddSuccess("初始化完成");
logger.AddWarning("配置文件缺少可选项");
logger.AddError("连接失败");
```

### 4.2 批量写入

```csharp
using System;
using System.Collections.Generic;
using Logger.Core;
using Logger.Core.Models;

ILoggerOutput logger = LogManager.GetLogger("MyApp.Batch");

logger.AddLogs(new List<LogEntry>
{
    new LogEntry(DateTime.Now, LogLevel.Info, "批量日志 1"),
    new LogEntry(DateTime.Now, LogLevel.Success, "批量日志 2"),
    new LogEntry(DateTime.Now, LogLevel.Error, "批量日志 3")
});
```

---

## 5. 日志等级与过滤

当前日志等级如下：

```csharp
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Success = 3,
    Warn = 4,
    Error = 5,
    Fatal = 6
}
```

### 5.1 默认最低等级

默认最低等级是 `Trace`。

也就是说，默认情况下当前 logger 会接收全部等级日志。

### 5.2 修改最低等级

```csharp
using Logger.Core;
using Logger.Core.Models;

ILoggerOutput logger = LogManager.GetLogger("MyApp");
logger.SetMinimumLevel(LogLevel.Info);
```

常见用法：

- 开发环境打开 `Trace`
- 生产环境保持 `Info`
- 故障排查时临时切到 `Debug`

### 5.3 过滤职责

当前实现里，过滤职责在 logger 入口：

- `LogManager.GetLogger(...)` / `LogStoreLoggerFactory` 创建的 logger：在 logger 入口过滤
- 直接 `new LogStore()`：在 `LogStore` 自己的写入入口过滤

下游组件不再独立做等级判断：

- `ILogSessionSource` 看到的是已经通过入口过滤的日志
- `ILogStorageBackend` 收到的也是已经通过入口过滤的日志
- UI 控件显示的是绑定 logger 的当前可视日志集合

---

## 6. WPF 绑定方式

`Logger.Wpf.Controls.LogPanelControl` 支持直接绑定 `ILoggerOutput`。

### 6.1 XAML

```xml
<Window x:Class="WpfApp1.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:logger="clr-namespace:Logger.Wpf.Controls;assembly=Logger.Wpf">
    <Grid>
        <logger:LogPanelControl x:Name="LogViewer"
                                Header="运行日志"
                                MaxLogEntries="5000" />
    </Grid>
</Window>
```

### 6.2 Code-behind

```csharp
using Logger.Core;

public partial class MainWindow
{
    private readonly ILoggerOutput _logger = LogManager.GetLogger("WpfApp");

    public MainWindow()
    {
        InitializeComponent();
        LogViewer.Logger = _logger;

        _logger.AddInfo("WPF 日志控件绑定完成");
        _logger.AddError("多行示例\r\n第二行");
    }
}
```

### 6.3 WPF 控件常用属性

- `Header`：标题文本
- `MaxLogEntries`：显示层最大条数
- `Logger`：绑定的日志接口
- `LevelFilter`：UI 等级过滤
- `SearchText`：UI 搜索文本

说明：

- `ClearLogs()` 是 UI 视图清空，不是业务日志入口
- 业务代码应写 `ILoggerOutput`，而不是调用控件的 `AddInfo/AddError`

---

## 7. WinForms 绑定方式

`Logger.WinForms.Controls.LogPanelControl` 支持直接绑定 `ILoggerOutput`。

### 7.1 原生 WinForms 控件

```csharp
using System.Windows.Forms;
using Logger.Core;
using Logger.WinForms.Controls;

public partial class MainForm : Form
{
    private readonly ILoggerOutput _logger = LogManager.GetLogger("WinFormsApp");

    public MainForm()
    {
        InitializeComponent();

        var logPanel = new LogPanelControl
        {
            Dock = DockStyle.Fill,
            Header = "运行日志",
            MaxLogEntries = 5000,
            Logger = _logger
        };

        Controls.Add(logPanel);

        _logger.AddInfo("WinForms 日志控件绑定完成");
        _logger.AddSuccess("多行示例\r\n第二行\r\n第三行");
    }
}
```

### 7.2 WinForms 中承载 WPF 控件

`Logger.WinForms.Controls.WpfLogPanelControl` 内部使用 `ElementHost` 承载 WPF 控件。

```csharp
using System.Windows.Forms;
using Logger.Core;
using Logger.WinForms.Controls;

public partial class MainForm : Form
{
    private readonly ILoggerOutput _logger = LogManager.GetLogger("WinForms.WpfHost");

    public MainForm()
    {
        InitializeComponent();

        var logPanel = new WpfLogPanelControl
        {
            Dock = DockStyle.Fill,
            Header = "WPF 日志控件",
            MaxLogEntries = 5000,
            Logger = _logger
        };

        Controls.Add(logPanel);

        _logger.AddInfo("WinForms 中的 WPF 日志控件绑定完成");
    }
}
```

### 7.3 WinForms 控件常用属性

- `Header`：标题文本
- `MaxLogEntries`：显示层最大条数
- `Logger`：绑定的日志接口
- `LevelFilter`：UI 等级过滤
- `SearchText`：UI 搜索文本

---

## 8. 会话信息与文件信息

如果 logger 同时实现了以下接口，可以读取额外信息。

### 8.1 `ILogSessionSource`

用于读取本场日志信息。

```csharp
using Logger.Core;

ILoggerOutput logger = LogManager.GetLogger("MyApp");
ILogSessionSource session = logger as ILogSessionSource;

if (session != null)
{
    var sessionId = session.SessionId;
    var startedAt = session.SessionStartedAt;
    var count = session.SessionEntryCount;
    var entries = session.GetSessionEntriesSnapshot();
}
```

### 8.2 `ILogFileSource`

用于读取当前日志文件输出信息。

```csharp
using Logger.Core;

ILoggerOutput logger = LogManager.GetLogger("MyApp");
ILogFileSource fileSource = logger as ILogFileSource;

if (fileSource != null && fileSource.IsFileOutputEnabled)
{
    string filePath = fileSource.LogFilePath;
}
```

---

## 9. 工厂与全局配置

### 9.1 默认工厂

默认情况下，`LoggerService` 使用 `LogStoreLoggerFactory` 创建 logger。

```csharp
ILoggerService service = new LoggerService();
ILoggerOutput logger = service.GetLogger("MyApp");
```

### 9.2 自定义最低等级

```csharp
using Logger.Core;
using Logger.Core.Models;

ILoggerFactory factory = new LogStoreLoggerFactory(
    logRootDirectoryPath: null,
    minimumLevel: LogLevel.Warn);

ILoggerService service = new LoggerService(factory);
LogManager.Configure(service);

ILoggerOutput logger = LogManager.GetLogger("MyApp");
logger.AddInfo("这条不会写入 logger");
logger.AddError("这条会写入 logger");
```

说明：

- 这里的 `minimumLevel` 配置的是 factory 创建出来的 logger 入口等级
- 不是给存储后端单独再加一层过滤

### 9.3 自定义全局服务

```csharp
using Logger.Core;

ILoggerService service = new LoggerService(new LogStoreLoggerFactory());
LogManager.Configure(service);
```

说明：

- 调用 `LogManager.Configure(...)` 后，后续 `LogManager.GetLogger(...)` 会从新的服务实例取 logger
- 新旧服务之间的 logger 缓存互不共享

---

## 10. 可扩展存储后端

如果你想把日志存到文本文件、CSV、数据库或其他介质，需要实现以下接口：

- `ILogStorageBackend`
- `ILogStorageBackendFactory`

### 10.1 自定义数据库后端示例

```csharp
using System.Collections.Generic;
using Logger.Core;
using Logger.Core.Models;

public sealed class DbLogStorageBackendFactory : ILogStorageBackendFactory
{
    private readonly string _connectionString;

    public DbLogStorageBackendFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public ILogStorageBackend CreateBackend(LogStorageContext context)
    {
        return new DbLogStorageBackend(_connectionString);
    }
}

public sealed class DbLogStorageBackend : ILogStorageBackend
{
    private readonly string _connectionString;

    public DbLogStorageBackend(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void WriteBatch(IReadOnlyList<LogEntry> entries, LogStorageContext context)
    {
        // 在这里批量写数据库
    }
}
```

### 10.2 接入自定义后端

```csharp
using Logger.Core;
using Logger.Core.Models;

var factory = new LogStoreLoggerFactory(
    new DbLogStorageBackendFactory("Data Source=.;Initial Catalog=LoggerDb;Integrated Security=True"),
    minimumLevel: LogLevel.Trace);

LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = LogManager.GetLogger("OrderService");
logger.AddInfo("订单服务启动");
```

### 10.3 内置后端

当前内置后端包括：

- `TextFileLogStorageBackendFactory`
- `CsvFileLogStorageBackendFactory`
- `LogFileRollingMode.SingleFile / Year / Month / Week / Day`

默认文件滚动方式是 `LogFileRollingMode.Day`。

滚动分文件是按日志时间戳执行的：

- `AddLog(...)`：使用当前写入时间决定目标文件
- `AddLogs(...)`：使用每条 `LogEntry.Timestamp` 决定目标文件
- 如果一批日志跨越多个时间周期，后端会自动生成并写入多个文件
- 例如按日滚动时，一批同时包含 `2026-04-04` 和 `2026-04-05` 的日志，会分别进入 `20260404.xxx` 和 `20260405.xxx`

CSV 示例：

```csharp
using Logger.Core;
using Logger.Core.Models;

var factory = new LogStoreLoggerFactory(
    new CsvFileLogStorageBackendFactory(
        @"D:\Logs",
        LogFileRollingMode.Week),
    minimumLevel: LogLevel.Trace);

LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = LogManager.GetLogger("CsvDemo");
logger.AddInfo("当前日志会写入 CSV");
```

文本文件按月滚动示例：

```csharp
using Logger.Core;
using Logger.Core.Models;

var factory = new LogStoreLoggerFactory(
    logRootDirectoryPath: @"D:\Logs",
    minimumLevel: LogLevel.Trace,
    rollingMode: LogFileRollingMode.Month);

LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = LogManager.GetLogger("TextDemo");
logger.AddInfo("当前日志会按月写入文本文件");
```

### 10.4 存储后端的职责边界

建议按下面的边界实现自定义后端：

- logger：负责等级过滤、日志归一化、分发
- session/file/custom backend：负责消费已经通过过滤的日志
- backend：可以利用 `LogStorageContext` 中的 `LoggerName`、`SessionId`、`MinimumLevel` 做目录、表分区或元数据记录

不建议在 backend 里再重复做一遍等级过滤，除非你明确需要二次降采样。

---

## 11. 常见调用模式

### 11.1 推荐模式：业务只依赖接口

```csharp
using Logger.Core;

public sealed class OrderService
{
    private readonly ILoggerOutput _logger;

    public OrderService(ILoggerOutput logger)
    {
        _logger = logger;
    }

    public void Execute()
    {
        _logger.AddInfo("开始处理订单");
        _logger.AddSuccess("订单处理完成");
    }
}
```

### 11.2 UI 只负责绑定

```csharp
ILoggerOutput logger = LogManager.GetLogger("DesktopApp");
logPanel.Logger = logger;

logger.AddInfo("初始化 UI");
```

### 11.3 排查模式：临时打开 Trace

```csharp
ILoggerOutput logger = LogManager.GetLogger("DebugApp");
logger.SetMinimumLevel(LogLevel.Trace);

logger.AddTrace("进入方法 A");
logger.AddDebug("读取配置完成");
logger.AddInfo("执行完成");
```

---

## 12. FAQ

### 12.1 默认会不会丢掉 Trace / Debug？

不会。默认最低等级是 `Trace`。

### 12.2 为什么我改了最低等级，别的窗口也跟着变了？

因为它们绑定的是同一个 logger 实例。只要名字相同，且来自同一个 `ILoggerService`，最低等级配置也是共享的。

### 12.3 为什么不推荐直接调用控件的 `AddInfo`？

因为那样业务层会直接依赖 UI。推荐写法是：

```csharp
ILoggerOutput logger = LogManager.GetLogger("MyApp");
logPanel.Logger = logger;
logger.AddInfo("写业务日志");
```

不推荐：

```csharp
logPanel.AddInfo("写业务日志");
```

### 12.4 存储后端能不能自己再做过滤？

可以，但默认设计不是这样。当前框架把等级过滤放在 logger 入口，storage backend 默认只负责消费和持久化。

### 12.5 相同名称获取到的实例一定一样吗？

在同一个 `ILoggerService` 里是一样的。切换到新的 `LoggerService` 之后，会进入新的实例缓存空间。

---

## 13. 相关源码位置

- `Logger.Core\ILoggerOutput.cs`
- `Logger.Core\LogManager.cs`
- `Logger.Core\LoggerService.cs`
- `Logger.Core\LogStoreLoggerFactory.cs`
- `Logger.Core\LogStore.Memory.cs`
- `Logger.Core\ILogStorageBackend.cs`
- `Logger.Core\ILogStorageBackendFactory.cs`
- `Logger.Wpf\Controls\LogPanelControl.xaml.cs`
- `Logger.WinForms\Controls\LogPanelControl.Render.cs`
- `Logger.WinForms\Controls\WpfLogPanelControl.cs`
