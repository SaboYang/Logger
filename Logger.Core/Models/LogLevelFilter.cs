using System;

namespace Logger.Core.Models
{
    [Flags]
    public enum LogLevelFilter
    {
        None = 0,
        Trace = 1 << 0,
        Debug = 1 << 1,
        Info = 1 << 2,
        Success = 1 << 3,
        Warn = 1 << 4,
        Error = 1 << 5,
        Fatal = 1 << 6,
        All = Trace | Debug | Info | Success | Warn | Error | Fatal
    }

    public static class LogLevelFilterExtensions
    {
        public static bool Includes(this LogLevelFilter filter, LogLevel level)
        {
            LogLevelFilter levelFlag = level.ToFilter();
            return levelFlag != LogLevelFilter.None && (filter & levelFlag) == levelFlag;
        }

        public static LogLevelFilter ToFilter(this LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return LogLevelFilter.Trace;
                case LogLevel.Debug:
                    return LogLevelFilter.Debug;
                case LogLevel.Info:
                    return LogLevelFilter.Info;
                case LogLevel.Success:
                    return LogLevelFilter.Success;
                case LogLevel.Warn:
                    return LogLevelFilter.Warn;
                case LogLevel.Error:
                    return LogLevelFilter.Error;
                case LogLevel.Fatal:
                    return LogLevelFilter.Fatal;
                default:
                    return LogLevelFilter.None;
            }
        }
    }
}
