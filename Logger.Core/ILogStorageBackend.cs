using System.Collections.Generic;
using Logger.Core.Models;

namespace Logger.Core
{
    /// <summary>
    /// 定义日志持久化后端。
    /// </summary>
    public interface ILogStorageBackend
    {
        /// <summary>
        /// 批量写入日志到目标存储介质。
        /// </summary>
        /// <param name="entries">待写入的日志集合。</param>
        /// <param name="context">当前 logger 的存储上下文。</param>
        void WriteBatch(IReadOnlyList<LogEntry> entries, LogStorageContext context);
    }
}
