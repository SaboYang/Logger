using System.Collections.Generic;
using Logger.Core.Models;

namespace Logger.Core
{
    public interface ILoggerOutput
    {
        void AddTrace(string message);

        void AddDebug(string message);

        void AddInfo(string message);

        void AddSuccess(string message);

        void AddWarning(string message);

        void AddError(string message);

        void AddFatal(string message);

        void AddLog(LogLevel level, string message);

        void AddLogs(IEnumerable<LogEntry> entries);
    }
}
