using System.Collections.Generic;
using Logger.Core.Models;

namespace Logger.Core
{
    internal static class LogEntrySanitizer
    {
        public static string NormalizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            return message.Trim();
        }

        public static LogEntry NormalizeEntry(LogEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            string normalizedMessage = NormalizeMessage(entry.Message);
            if (normalizedMessage == null)
            {
                return null;
            }

            if (normalizedMessage == entry.Message)
            {
                return entry;
            }

            return new LogEntry(entry.Timestamp, entry.Level, normalizedMessage);
        }

        public static List<LogEntry> NormalizeEntries(IEnumerable<LogEntry> entries)
        {
            List<LogEntry> normalizedEntries = new List<LogEntry>();
            if (entries == null)
            {
                return normalizedEntries;
            }

            foreach (LogEntry entry in entries)
            {
                LogEntry normalizedEntry = NormalizeEntry(entry);
                if (normalizedEntry != null)
                {
                    normalizedEntries.Add(normalizedEntry);
                }
            }

            return normalizedEntries;
        }
    }
}
