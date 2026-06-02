using System;
using Logger.Core;
using Logger.Core.Models;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace Logger.NLog.Tests
{
    /// <summary>
    /// 验证 Logger.Core 到 NLog 的适配行为。
    /// </summary>
    public sealed class NLogLoggerTests : IDisposable
    {
        private readonly MemoryTarget _memoryTarget;

        public NLogLoggerTests()
        {
            _memoryTarget = new MemoryTarget("memory")
            {
                Layout = "${level}|${logger}|${message}|${event-properties:item=LoggerName}|${event-properties:item=SessionId}|${event-properties:item=SessionStartedAt}|${event-properties:item=LoggerSuccess}"
            };

            LoggingConfiguration configuration = new LoggingConfiguration();
            configuration.AddRule(global::NLog.LogLevel.Trace, global::NLog.LogLevel.Fatal, _memoryTarget);
            global::NLog.LogManager.Configuration = configuration;
            global::NLog.LogManager.ReconfigExistingLoggers();
        }

        [Fact]
        public void Write_MapsLevels_AndAttachesContextProperties()
        {
            NLogLoggerFactory factory = new NLogLoggerFactory(new NLogLoggerOptions
            {
                LoggerNamePrefix = "App.",
                AttachLoggerNameProperty = true,
                AttachSessionProperties = true,
                AttachSuccessProperty = true
            });

            NLogLogger logger = (NLogLogger)factory.CreateLogger("OrderService");
            Assert.Equal("OrderService", logger.SourceLoggerName);
            Assert.Equal("App.OrderService", logger.LoggerName);
            Assert.NotEqual(Guid.Empty, logger.SessionId);

            logger.Info("info");
            logger.Success("success");
            logger.Warning("warn");
            logger.Error("error");
            logger.Fatal("fatal");

            Assert.Equal(5, _memoryTarget.Logs.Count);
            Assert.Contains("Info|App.OrderService|info|OrderService|", _memoryTarget.Logs[0]);
            Assert.Contains("|true", _memoryTarget.Logs[1]);
            Assert.Contains("Warn|App.OrderService|warn|OrderService|", _memoryTarget.Logs[2]);
            Assert.Contains("Error|App.OrderService|error|OrderService|", _memoryTarget.Logs[3]);
            Assert.Contains("Fatal|App.OrderService|fatal|OrderService|", _memoryTarget.Logs[4]);
        }

        [Fact]
        public void Write_AppendsExceptionText_WhenEnabled()
        {
            NLogLoggerFactory factory = new NLogLoggerFactory(new NLogLoggerOptions
            {
                AppendExceptionText = true
            });

            NLogLogger logger = (NLogLogger)factory.CreateLogger("ExceptionLogger");
            InvalidOperationException exception = new InvalidOperationException("boom");

            logger.Write(Logger.Core.Models.LogLevel.Error, "failure", exception);

            Assert.Single(_memoryTarget.Logs);
            Assert.Contains("failure", _memoryTarget.Logs[0]);
            Assert.Contains("System.InvalidOperationException", _memoryTarget.Logs[0]);
            Assert.Contains("boom", _memoryTarget.Logs[0]);
        }

        public void Dispose()
        {
            global::NLog.LogManager.Shutdown();
        }
    }
}
