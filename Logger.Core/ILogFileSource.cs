namespace Logger.Core
{
    /// <summary>
    /// 提供日志文件输出状态和当前输出文件路径。
    /// </summary>
    public interface ILogFileSource
    {
        /// <summary>
        /// 获取一个值，指示当前 logger 是否启用了文件输出。
        /// </summary>
        bool IsFileOutputEnabled { get; }

        /// <summary>
        /// 获取当前写入中的日志文件路径。
        /// 未启用文件输出时可能为空。
        /// </summary>
        string LogFilePath { get; }
    }
}
