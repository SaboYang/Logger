using System.Collections.Generic;
using Logger.Core.Models;

namespace Logger.Core
{
    internal static class LogEntryFilter
    {
        public static bool MeetsMinimumLevel(LogLevel level, LogLevel minimumLevel)
        {
            return level >= minimumLevel;
        }

        public static List<LogEntry> FilterEntries(IEnumerable<LogEntry> entries, LogLevel minimumLevel)
        {
            List<LogEntry> filteredEntries = new List<LogEntry>();
            if (entries == null)
            {
                return filteredEntries;
            }

            foreach (LogEntry entry in entries)
            {
                if (entry != null && MeetsMinimumLevel(entry.Level, minimumLevel))
                {
                    filteredEntries.Add(entry);
                }
            }

            return filteredEntries;
        }
    }
}
