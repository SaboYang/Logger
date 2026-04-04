using System.Collections.ObjectModel;
using Logger.Core.Models;

namespace Logger.Core
{
    /// <summary>
    /// 为 UI 控件提供日志视图数据源。
    /// </summary>
    public interface ILogViewSource
    {
        /// <summary>
        /// 获取用于集合访问同步的锁对象。
        /// </summary>
        object SyncRoot { get; }

        /// <summary>
        /// 获取供 UI 显示的日志集合。
        /// </summary>
        ObservableCollection<LogEntry> Entries { get; }

        /// <summary>
        /// 获取或设置显示层保留的最大日志条数。
        /// </summary>
        int MaxEntries { get; set; }
    }
}
