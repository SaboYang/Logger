namespace Logger.Core
{
    public interface ILoggerService
    {
        ILoggerFactory Factory { get; }

        ILoggerOutput Default { get; }

        ILoggerOutput GetLogger(string name);

        bool TryGetLogger(string name, out ILoggerOutput logger);
    }
}
