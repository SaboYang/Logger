using System.Collections.Generic;
using Logger.Core.Models;

namespace Logger.Core
{
    public interface ILogStorageBackend
    {
        void WriteBatch(IReadOnlyList<LogEntry> entries, LogStorageContext context);
    }
}
