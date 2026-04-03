using System;

namespace Logger.Core
{
    public static class LogManager
    {
        private static readonly object SyncRoot = new object();
        private static ILoggerService _service = LoggerService.Shared;

        public static ILoggerService Service
        {
            get
            {
                lock (SyncRoot)
                {
                    return _service;
                }
            }
        }

        public static ILoggerFactory Factory
        {
            get { return Service.Factory; }
        }

        public static ILoggerOutput Default
        {
            get { return Service.Default; }
        }

        public static ILoggerOutput GetLogger(string name)
        {
            return Service.GetLogger(name);
        }

        public static bool TryGetLogger(string name, out ILoggerOutput logger)
        {
            return Service.TryGetLogger(name, out logger);
        }

        public static void Configure(ILoggerService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            lock (SyncRoot)
            {
                _service = service;
            }
        }
    }
}
