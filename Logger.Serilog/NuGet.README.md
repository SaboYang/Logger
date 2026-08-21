# Logger.Serilog

`Logger.Serilog` 提供 `Logger.Core <-> Serilog` 的双向适配层。

## 用法

```csharp
using Logger.Core;
using Logger.Serilog;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/app.log", shared: true)
    .CreateLogger();

var factory = new SerilogLoggerFactory(new SerilogLoggerOptions
{
    LoggerNamePrefix = "MyApp."
});

LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = LogManager.GetLogger("OrderService");
logger.Info("Serilog 接入完成");
```

## Serilog 主日志，ILoggerOutput 辅助显示

如果业务代码以 Serilog 为主，同时需要把同一条日志显示到 WPF 或 WinForms 控件，可以注册 `LoggerOutputSink`：

```csharp
using Logger.Core;
using Logger.Serilog;
using Serilog;

ILoggerOutput panelLogger = new LogStoreLoggerFactory(
    logRootDirectoryPath: "Logs",
    minimumLevel: Logger.Core.Models.LogLevel.Trace)
    .CreateLogger("SerilogPanel");

Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/app.log", shared: true)
    .WriteTo.Sink(new LoggerOutputSink(panelLogger))
    .CreateLogger();

Log.Information("这条日志会同时写入 app.log 和 ILoggerOutput");
```

`panelLogger` 必须使用非 Serilog 实现，例如 `LogStoreLoggerFactory` 创建的 logger，避免形成 `Serilog -> ILoggerOutput -> Serilog` 循环。

## 说明

- `SourceContext` 默认映射为适配后的 logger 名称
- `LoggerName`、`SessionId` 和 `SessionStartedAt` 可按需写入 Serilog event properties
- `Success` 等级默认映射到 `Information`
- 安装 `ZH.Logger.Serilog` 后可以直接使用 `Serilog.Sinks.File` 将日志写到本地文件
- `LoggerOutputSink` 会把 Serilog 的 `Verbose / Debug / Information / Warning / Error / Fatal` 映射到 Logger.Core 等级
- 如果事件包含异常，异常文本会追加到 `ILoggerOutput` 的消息中
- 普通文本消息会自动转义为 Serilog message template，避免 `{}` 被当成占位符
