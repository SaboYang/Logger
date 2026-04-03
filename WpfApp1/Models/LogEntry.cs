using System;
using System.Windows.Media;

namespace WpfApp1.Models
{
    public class LogEntry
    {
        public LogEntry(DateTime timestamp, LogLevel level, string levelText, string message, Brush foreground)
        {
            Timestamp = timestamp;
            Level = level;
            LevelText = levelText;
            Message = message;
            Foreground = foreground;
        }

        public DateTime Timestamp { get; }

        public LogLevel Level { get; }

        public string LevelText { get; }

        public string Message { get; }

        public Brush Foreground { get; }
    }
}
