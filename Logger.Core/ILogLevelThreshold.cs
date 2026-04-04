using Logger.Core.Models;

namespace Logger.Core
{
    /// <summary>
    /// 提供最低日志等级配置。
    /// </summary>
    public interface ILogLevelThreshold
    {
        /// <summary>
        /// 获取或设置当前 logger 的最低接收等级。
        /// </summary>
        LogLevel MinimumLevel { get; set; }
    }
}
