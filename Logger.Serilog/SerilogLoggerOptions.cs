using System;
using Serilog.Events;

namespace Logger.Serilog
{
    /// <summary>
    /// 定义 <see cref="Logger.Core"/> 到 Serilog 的适配选项。
    /// </summary>
    public sealed class SerilogLoggerOptions
    {
        /// <summary>
        /// 获取或设置用于重写 Serilog logger 名称的委托。
        /// 返回空值时会继续使用原始名称。
        /// </summary>
        public Func<string, string> LoggerNameResolver { get; set; }

        /// <summary>
        /// 获取或设置 Serilog logger 名称前缀。
        /// </summary>
        public string LoggerNamePrefix { get; set; }

        /// <summary>
        /// 获取或设置是否将原始 logger 名称写入 Serilog 属性。
        /// </summary>
        public bool AttachLoggerNameProperty { get; set; } = true;

        /// <summary>
        /// 获取或设置是否将会话信息写入 Serilog 属性。
        /// </summary>
        public bool AttachSessionProperties { get; set; } = true;

        /// <summary>
        /// 获取或设置是否将 Success 标记写入 Serilog 属性。
        /// </summary>
        public bool AttachSuccessProperty { get; set; } = true;

        /// <summary>
        /// 获取或设置 Success 等级映射到的 Serilog 等级。
        /// </summary>
        public LogEventLevel SuccessTargetLevel { get; set; } = LogEventLevel.Information;

        /// <summary>
        /// 获取或设置写入的 logger 名称属性名。
        /// </summary>
        public string LoggerNamePropertyName { get; set; } = "LoggerName";

        /// <summary>
        /// 获取或设置写入的会话标识属性名。
        /// </summary>
        public string SessionIdPropertyName { get; set; } = "SessionId";

        /// <summary>
        /// 获取或设置写入的会话开始时间属性名。
        /// </summary>
        public string SessionStartedAtPropertyName { get; set; } = "SessionStartedAt";

        /// <summary>
        /// 获取或设置 Success 标记属性名。
        /// </summary>
        public string SuccessPropertyName { get; set; } = "LoggerSuccess";
    }
}
