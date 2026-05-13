using System;
using Logger.Core.Models;

namespace Logger.Sqlite
{
    /// <summary>
    /// 表示一条从 SQLite 读取出的日志记录。
    /// </summary>
    public sealed class SqliteLogRecord
    {
        /// <summary>
        /// 初始化 SQLite 日志记录。
        /// </summary>
        /// <param name="id">数据库行号。</param>
        /// <param name="entryHash">稳定去重键。</param>
        /// <param name="loggerName">logger 名称。</param>
        /// <param name="sessionId">会话标识。</param>
        /// <param name="sessionStartedAt">会话开始时间。</param>
        /// <param name="timestamp">日志时间。</param>
        /// <param name="level">日志级别。</param>
        /// <param name="message">日志内容。</param>
        public SqliteLogRecord(
            long id,
            string entryHash,
            string loggerName,
            Guid sessionId,
            DateTime sessionStartedAt,
            DateTime timestamp,
            LogLevel level,
            string message)
        {
            Id = id;
            EntryHash = entryHash;
            LoggerName = loggerName;
            SessionId = sessionId;
            SessionStartedAt = sessionStartedAt;
            Timestamp = timestamp;
            Level = level;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// 获取数据库行号。
        /// </summary>
        public long Id { get; }

        /// <summary>
        /// 获取稳定去重键。
        /// </summary>
        public string EntryHash { get; }

        /// <summary>
        /// 获取 logger 名称。
        /// </summary>
        public string LoggerName { get; }

        /// <summary>
        /// 获取会话标识。
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// 获取会话开始时间。
        /// </summary>
        public DateTime SessionStartedAt { get; }

        /// <summary>
        /// 获取日志时间。
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 获取日志级别。
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// 获取日志内容。
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 获取日志级别的文本展示形式。
        /// </summary>
        public string LevelText
        {
            get { return Level.ToString().ToUpperInvariant(); }
        }
    }
}
