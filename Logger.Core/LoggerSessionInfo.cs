using System;

namespace Logger.Core
{
    internal sealed class LoggerSessionInfo
    {
        public LoggerSessionInfo(string loggerName)
        {
            LoggerName = string.IsNullOrWhiteSpace(loggerName) ? "Default" : loggerName.Trim();
            SessionId = Guid.NewGuid();
            StartedAt = DateTime.Now;
        }

        public string LoggerName { get; }

        public Guid SessionId { get; }

        public DateTime StartedAt { get; }
    }
}
