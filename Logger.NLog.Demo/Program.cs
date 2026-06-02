using System;
using Logger.Core;
using Logger.NLog;
using NLog.Config;
using NLog.Targets;

namespace Logger.NLog.Demo
{
    internal static class Program
    {
        private static void Main()
        {
            MemoryTarget memoryTarget = new MemoryTarget("memory")
            {
                Layout = "${time}|${level}|${logger}|${message}|${event-properties:item=LoggerName}|${event-properties:item=SessionId}|${event-properties:item=LoggerSuccess}"
            };

            LoggingConfiguration configuration = new LoggingConfiguration();
            configuration.AddRule(global::NLog.LogLevel.Trace, global::NLog.LogLevel.Fatal, memoryTarget);
            global::NLog.LogManager.Configuration = configuration;
            global::NLog.LogManager.ReconfigExistingLoggers();

            NLogLoggerFactory factory = new NLogLoggerFactory(new NLogLoggerOptions
            {
                LoggerNamePrefix = "Demo."
            });
            LogManager.Configure(new LoggerService(factory));

            ILoggerOutput logger = LogManager.GetLogger("OrderService");
            logger.Info("NLog demo started");
            logger.Success("NLog demo success");
            logger.Warning("NLog demo warning");

            foreach (string line in memoryTarget.Logs)
            {
                Console.WriteLine(line);
            }

            global::NLog.LogManager.Shutdown();
        }
    }
}
