namespace Logger.Core
{
    public interface ILogStorageBackendFactory
    {
        ILogStorageBackend CreateBackend(LogStorageContext context);
    }
}
