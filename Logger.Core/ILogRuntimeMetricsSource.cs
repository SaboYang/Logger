namespace Logger.Core
{
    public interface ILogRuntimeMetricsSource
    {
        int BufferedSessionEntryCount { get; }

        int DroppedPendingEntryCount { get; }
    }
}
