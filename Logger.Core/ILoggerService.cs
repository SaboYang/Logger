namespace Logger.Core
{
    /// <summary>
    /// 管理 logger 生命周期和实例缓存的服务接口。
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// 获取当前服务使用的 logger 工厂。
        /// </summary>
        ILoggerFactory Factory { get; }

        /// <summary>
        /// 获取默认 logger。
        /// </summary>
        ILoggerOutput Default { get; }

        /// <summary>
        /// 按名称获取 logger。
        /// 同名调用应返回同一个实例。
        /// </summary>
        /// <param name="name">日志名称。</param>
        /// <returns>对应名称的 logger。</returns>
        ILoggerOutput GetLogger(string name);

        /// <summary>
        /// 尝试按名称获取已存在的 logger。
        /// </summary>
        /// <param name="name">日志名称。</param>
        /// <param name="logger">获取到的 logger。</param>
        /// <returns>存在时返回 <c>true</c>；否则返回 <c>false</c>。</returns>
        bool TryGetLogger(string name, out ILoggerOutput logger);
    }
}
