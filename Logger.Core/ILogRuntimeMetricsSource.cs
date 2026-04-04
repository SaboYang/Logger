namespace Logger.Core
{
    /// <summary>
    /// 提供运行时缓冲和丢弃统计信息。
    /// </summary>
    public interface ILogRuntimeMetricsSource
    {
        /// <summary>
        /// 获取当前保留在会话缓冲中的日志条数。
        /// </summary>
        int BufferedSessionEntryCount { get; }

        /// <summary>
        /// 获取后台待处理队列中被丢弃的日志条数。
        /// 当前默认可靠模式下通常为 0。
        /// </summary>
        int DroppedPendingEntryCount { get; }
    }
}
