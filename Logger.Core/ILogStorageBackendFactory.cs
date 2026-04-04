namespace Logger.Core
{
    /// <summary>
    /// 为指定 logger 创建持久化后端实例。
    /// </summary>
    public interface ILogStorageBackendFactory
    {
        /// <summary>
        /// 根据上下文创建一个新的持久化后端。
        /// </summary>
        /// <param name="context">当前 logger 的存储上下文。</param>
        /// <returns>持久化后端实例。</returns>
        ILogStorageBackend CreateBackend(LogStorageContext context);
    }
}
