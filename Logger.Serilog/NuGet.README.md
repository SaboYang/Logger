# Logger.Serilog

`Logger.Serilog` 提供 `Logger.Core -> Serilog` 的适配层。

## 用法

```csharp
using Logger.Core;
using Logger.Serilog;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var factory = new SerilogLoggerFactory(new SerilogLoggerOptions
{
    LoggerNamePrefix = "MyApp."
});

LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = LogManager.GetLogger("OrderService");
logger.Info("Serilog 接入完成");
```

## 说明

- `SourceContext` 默认映射为适配后的 logger 名称
- `LoggerName`、`SessionId` 和 `SessionStartedAt` 可按需写入 Serilog event properties
- `Success` 等级默认映射到 `Information`
- 普通文本消息会自动转义为 Serilog message template，避免 `{}` 被当成占位符
