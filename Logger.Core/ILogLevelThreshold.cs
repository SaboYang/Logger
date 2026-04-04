using Logger.Core.Models;

namespace Logger.Core
{
    public interface ILogLevelThreshold
    {
        LogLevel MinimumLevel { get; set; }
    }
}
