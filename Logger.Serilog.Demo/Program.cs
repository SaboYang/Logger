using System;
using System.IO;
using Logger.Core;
using Logger.Core.Models;
using Logger.Serilog;
using Serilog;

namespace Logger.Serilog.Demo
{
    internal static class Program
    {
        private static void Main()
        {
            string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
            string logFilePath = Path.Combine(logDirectory, "serilog-demo.log");

            Directory.CreateDirectory(logDirectory);
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(
                    logFilePath,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj} LoggerName={LoggerName} SessionId={SessionId} StartedAt={SessionStartedAt} Success={LoggerSuccess}{NewLine}{Exception}",
                    shared: true)
                .CreateLogger();

            try
            {
                SerilogLoggerFactory factory = new SerilogLoggerFactory(new SerilogLoggerOptions
                {
                    LoggerNamePrefix = "Demo."
                });
                LogManager.Configure(new LoggerService(factory));

                ILoggerOutput logger = LogManager.GetLogger("OrderService");
                logger.Info("Serilog demo started");
                logger.Success("Serilog demo success");
                logger.Warning("Serilog demo warning");
                logger.Error("Serilog demo error");
                logger.Fatal("Serilog demo fatal");

                Log.CloseAndFlush();

                Console.WriteLine("本地文件路径:");
                Console.WriteLine(logFilePath);
                Console.WriteLine();
                Console.WriteLine("文件内容:");
                Console.WriteLine(File.ReadAllText(logFilePath));
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
