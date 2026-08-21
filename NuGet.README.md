# Logger 包说明

本仓库发布以下 NuGet 包：

- `ZH.Logger.Core`
- `ZH.Logger.Extensions.Logging`
- `ZH.Logger.Serilog`
- `ZH.Logger.Sqlite`
- `ZH.Logger.NLog`
- `ZH.Logger.Wpf`
- `ZH.Logger.WinForms`
- `ZH.Logger.WinForms.WpfHosts`

## 安装建议

- 需要日志接口、全局服务和存储扩展点时，安装 `ZH.Logger.Core`
- 需要接入 `Microsoft.Extensions.Logging` 和 `ILogger<T>` 适配时，安装 `ZH.Logger.Extensions.Logging`
- 需要接入 Serilog 适配时，安装 `ZH.Logger.Serilog`
- 需要 SQLite 持久化时，安装 `ZH.Logger.Sqlite`
- 需要接入 NLog 适配时，安装 `ZH.Logger.NLog`
- 需要 WPF 日志控件时，安装 `ZH.Logger.Wpf`
- 需要 WinForms 控件或在 WinForms 中承载 WPF 控件时，安装 `ZH.Logger.WinForms`
- 需要在 WinForms 中承载 WPF 控件时，也可以安装 `ZH.Logger.WinForms.WpfHosts`

## 许可证

这些包采用 MIT License 发布，详见仓库根目录 `LICENSE`。
