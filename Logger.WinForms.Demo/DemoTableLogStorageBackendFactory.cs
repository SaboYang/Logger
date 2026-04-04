using Logger.Core;

namespace Logger.WinForms.Demo
{
    internal sealed class DemoTableLogStorageBackendFactory : ILogStorageBackendFactory
    {
        public DemoTableLogStorageBackend CurrentBackend { get; private set; }

        public ILogStorageBackend CreateBackend(LogStorageContext context)
        {
            CurrentBackend = new DemoTableLogStorageBackend();
            return CurrentBackend;
        }
    }
}
