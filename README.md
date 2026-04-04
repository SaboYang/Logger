# Logger

轻量日志组件，分为三层：

- `Logger.Core`：日志接口、全局服务、存储扩展点
- `Logger.Wpf`：WPF 日志控件
- `Logger.WinForms`：WinForms 日志控件，以及 WinForms 承载 WPF 控件

目标是让业务代码只写 `ILoggerOutput`，UI 只负责绑定显示。

## 特性

- 统一日志接口：`Trace / Debug / Info / Success / Warn / Error / Fatal`
- 同名 logger 在同一个 `ILoggerService` 中复用同一个实例
- 默认最低等级是 `Trace`
- 等级过滤发生在 logger 写入入口，不在存储层重复过滤
- 支持 WPF / WinForms 绑定
- 支持文本文件、CSV、自定义存储后端
- UI 自带搜索、等级过滤、清空视图

## 1 分钟上手

### 获取 logger

```csharp
using Logger.Core;

ILoggerOutput logger = LogManager.GetLogger("MyApp");

logger.AddInfo("应用启动");
logger.AddSuccess("初始化完成");
logger.AddError("连接失败");
```

说明：

- `LogManager.GetLogger("MyApp")` 同名返回同一个实例
- `"MyApp"`、`" myapp "`、`"MYAPP"` 会归一成同一个 logger

### 设置最低等级

默认最低等级是 `Trace`，所以默认情况下全部等级都会被当前 logger 接收。

```csharp
using Logger.Core;
using Logger.Core.Models;

ILoggerOutput logger = LogManager.GetLogger("MyApp");
logger.SetMinimumLevel(LogLevel.Info);
```

说明：

- 等级过滤发生在 `ILoggerOutput.AddLog(...)` / `AddLogs(...)` 的入口
- 会话记录、文件写入、自定义存储后端只消费已经通过入口过滤的日志
- 如果多个调用方通过同一个名字拿到同一个 logger，那么它们共享同一个最低等级配置

## 绑定到 WPF

### XAML

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

### 代码绑定

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

## 绑定到 WinForms

### 原生 WinForms 控件

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

### WinForms 中承载 WPF 控件

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
    }
}
```

## 自定义存储后端

如果要把日志改成写文本、CSV、数据库或其他介质，实现这两个接口：

- `ILogStorageBackend`
- `ILogStorageBackendFactory`

示例：

```csharp
using System.Collections.Generic;
using Logger.Core;
using Logger.Core.Models;

public sealed class DbLogStorageBackendFactory : ILogStorageBackendFactory
{
    public ILogStorageBackend CreateBackend(LogStorageContext context)
    {
        return new DbLogStorageBackend();
    }
}

public sealed class DbLogStorageBackend : ILogStorageBackend
{
    public void WriteBatch(IReadOnlyList<LogEntry> entries, LogStorageContext context)
    {
        // 在这里批量写数据库
    }
}
```

接入方式：

```csharp
using Logger.Core;
using Logger.Core.Models;

var factory = new LogStoreLoggerFactory(
    new DbLogStorageBackendFactory(),
    minimumLevel: LogLevel.Trace);

LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = LogManager.GetLogger("OrderService");
logger.AddInfo("订单服务启动");
```

说明：

- `minimumLevel` 配置的是 factory 创建出来的 logger 入口等级
- 存储后端收到的是已经通过 logger 入口过滤的日志
- `LogStorageContext.MinimumLevel` 只用于让后端知道当前 logger 的配置，不建议后端再次做等级过滤

## 推荐用法

推荐：

```csharp
ILoggerOutput logger = LogManager.GetLogger("MyApp");
logPanel.Logger = logger;
logger.AddInfo("写业务日志");
```

不推荐：

```csharp
logPanel.AddInfo("写业务日志");
```

原因是前者业务层不依赖 UI，后续更换展示方式或存储方式更容易。

## 更多文档

- [完整 API 文档](docs/Logger-API.md)
- [类图](docs/Logger-ClassDiagram.md)
- [结构类图](docs/Logger-Structure-ClassDiagram.md)
