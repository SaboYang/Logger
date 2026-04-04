using System;
using System.Collections.Generic;
using System.Threading;

namespace Logger.Core
{
    public static class LogManager
    {
        private static ILoggerService _service = LoggerService.Shared;

        public static ILoggerService Service
        {
            get { return Volatile.Read(ref _service); }
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

        public static ILoggerOutput CreateMergedLogger(params ILoggerOutput[] loggers)
        {
            return new MergedLogger(loggers);
        }

        public static ILoggerOutput CreateMergedLogger(IEnumerable<ILoggerOutput> loggers)
        {
            return new MergedLogger(loggers);
        }

        public static void Configure(ILoggerService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Volatile.Write(ref _service, service);
        }
    }
}
