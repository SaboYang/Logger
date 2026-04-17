# Logger 包说明

本仓库发布四个 NuGet 包：

- `ZH.Logger.Core`
- `ZH.Logger.Extensions.Logging`
- `ZH.Logger.Wpf`
- `ZH.Logger.WinForms`

## 安装建议

- 需要日志接口、全局服务和存储扩展点时，安装 `ZH.Logger.Core`
- 需要接入 `Microsoft.Extensions.Logging` 和 `ILogger<T>` 适配时，安装 `ZH.Logger.Extensions.Logging`
- 需要 WPF 日志控件时，安装 `ZH.Logger.Wpf`
- 需要 WinForms 控件或在 WinForms 中承载 WPF 控件时，安装 `ZH.Logger.WinForms`

## 许可证

这些包采用 MIT License 发布，详见仓库根目录 `LICENSE`。
