using System;

namespace Logger.Core.Models
{
    public class LogEntry
    {
        public LogEntry(DateTime timestamp, LogLevel level, string message)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message;
        }

        public DateTime Timestamp { get; }

        public LogLevel Level { get; }

        public string LevelText
        {
            get
            {
                switch (Level)
                {
                    case LogLevel.Trace:
                        return "TRACE";
                    case LogLevel.Debug:
                        return "DEBUG";
                    case LogLevel.Info:
                        return "INFO";
                    case LogLevel.Success:
                        return "SUCCESS";
                    case LogLevel.Warn:
                        return "WARN";
                    case LogLevel.Error:
                        return "ERROR";
                    case LogLevel.Fatal:
                        return "FATAL";
                    default:
                        return "INFO";
                }
            }
        }

        public string Message { get; }
    }
}
