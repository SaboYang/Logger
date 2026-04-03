namespace Logger.Core
{
    public sealed class LogStoreLoggerFactory : ILoggerFactory
    {
        public ILoggerOutput CreateLogger(string name)
        {
            return new LogStore();
        }
    }
}
