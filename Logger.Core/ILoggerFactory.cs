namespace Logger.Core
{
    /// <summary>
    /// 按名称创建日志实例。
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// 创建一个新的日志实例。
        /// </summary>
        /// <param name="name">日志名称。</param>
        /// <returns>新创建的日志实例。</returns>
        ILoggerOutput CreateLogger(string name);
    }
}
