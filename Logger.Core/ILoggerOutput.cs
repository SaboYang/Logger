using System.Collections.Generic;
using Logger.Core.Models;

namespace Logger.Core
{
    /// <summary>
    /// 业务层统一使用的日志写入接口。
    /// </summary>
    public interface ILoggerOutput
    {
        /// <summary>
        /// 设置当前 logger 的最低接收等级。
        /// 低于该等级的日志会在写入入口被过滤。
        /// </summary>
        /// <param name="minimumLevel">最低日志等级。</param>
        void SetMinimumLevel(LogLevel minimumLevel);

        /// <summary>
        /// 写入一条 <see cref="LogLevel.Trace"/> 等级日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        void Trace(string message);

        /// <summary>
        /// 写入一条 <see cref="LogLevel.Debug"/> 等级日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        void Debug(string message);

        /// <summary>
        /// 写入一条 <see cref="LogLevel.Info"/> 等级日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        void Info(string message);

        /// <summary>
        /// 写入一条 <see cref="LogLevel.Success"/> 等级日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        void Success(string message);

        /// <summary>
        /// 写入一条 <see cref="LogLevel.Warn"/> 等级日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        void Warning(string message);

        /// <summary>
        /// 写入一条 <see cref="LogLevel.Error"/> 等级日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        void Error(string message);

        /// <summary>
        /// 写入一条 <see cref="LogLevel.Fatal"/> 等级日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        void Fatal(string message);

        /// <summary>
        /// 按指定等级写入单条日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        void AddLog(LogLevel level, string message);

        /// <summary>
        /// 批量写入日志。
        /// </summary>
        /// <param name="entries">要写入的日志集合。</param>
        void AddLogs(IEnumerable<LogEntry> entries);
    }
}
