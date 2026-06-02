# Logger.NLog

`Logger.NLog` 提供 `Logger.Core -> NLog` 的适配层。

## 用法

```csharp
using Logger.Core;
using Logger.NLog;

var factory = new NLogLoggerFactory(new NLogLoggerOptions
{
    LoggerNamePrefix = "MyApp."
});

LogManager.Configure(new LoggerService(factory));

ILoggerOutput logger = LogManager.GetLogger("OrderService");
logger.Info("NLog 接入完成");
```

## 说明

- `LoggerName` 默认会映射到 NLog 的 logger 名称
- `SessionId` 和 `SessionStartedAt` 会写入 NLog event properties
- `Success` 等级默认映射到 `Info`
