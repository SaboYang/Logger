using System;
using System.Collections.Generic;
using Logger.Core.Models;

namespace Logger.Core
{
    /// <summary>
    /// 提供当前日志会话的元数据和快照访问能力。
    /// </summary>
    public interface ILogSessionSource
    {
        /// <summary>
        /// 获取当前会话的唯一标识。
        /// </summary>
        Guid SessionId { get; }

        /// <summary>
        /// 获取当前会话开始时间。
        /// </summary>
        DateTime SessionStartedAt { get; }

        /// <summary>
        /// 获取当前会话累计接收的日志条数。
        /// </summary>
        int SessionEntryCount { get; }

        /// <summary>
        /// 获取当前会话日志的快照。
        /// </summary>
        /// <returns>当前会话日志快照。</returns>
        IReadOnlyList<LogEntry> GetSessionEntriesSnapshot();
    }
}
