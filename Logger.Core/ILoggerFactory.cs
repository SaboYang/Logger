namespace Logger.Core
{
    public interface ILoggerFactory
    {
        ILoggerOutput CreateLogger(string name);
    }
}
