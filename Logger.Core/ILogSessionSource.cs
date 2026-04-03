using System;
using System.Collections.Generic;
using Logger.Core.Models;

namespace Logger.Core
{
    public interface ILogSessionSource
    {
        Guid SessionId { get; }

        DateTime SessionStartedAt { get; }

        int SessionEntryCount { get; }

        IReadOnlyList<LogEntry> GetSessionEntriesSnapshot();
    }
}
