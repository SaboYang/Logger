using System;
using System.Collections.Generic;
using System.IO;
using Logger.Core;
using Logger.Core.Models;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Logger.Serilog.Tests
{
    /// <summary>
    /// 验证 Logger.Core 到 Serilog 的适配行为。
    /// </summary>
    public sealed class SerilogLoggerTests : IDisposable
    {
        private readonly CollectingSink _sink;

        /// <summary>
        /// 初始化测试环境并替换全局 Serilog.Logger。
        /// </summary>
        public SerilogLoggerTests()
        {
            _sink = new CollectingSink();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(_sink)
                .CreateLogger();
        }

        /// <summary>
        /// 验证等级映射和上下文属性写入。
        /// </summary>
        [Fact]
        public void Write_MapsLevels_AndAttachesContextProperties()
        {
            SerilogLoggerFactory factory = new SerilogLoggerFactory(new SerilogLoggerOptions
            {
                LoggerNamePrefix = "App.",
                AttachLoggerNameProperty = true,
                AttachSessionProperties = true,
                AttachSuccessProperty = true
            });

            SerilogLogger logger = (SerilogLogger)factory.CreateLogger("OrderService");
            Assert.Equal("OrderService", logger.SourceLoggerName);
            Assert.Equal("App.OrderService", logger.LoggerName);
            Assert.NotEqual(Guid.Empty, logger.SessionId);
            Assert.NotEqual(default(DateTime), logger.SessionStartedAt);

            logger.Info("info");
            logger.Success("success");
            logger.Warning("warn");
            logger.Error("error");
            logger.Fatal("fatal");

            Assert.Equal(5, _sink.Events.Count);
            Assert.Equal("info", _sink.Events[0].RenderMessage());
            Assert.Equal("App.OrderService", GetScalarValue(_sink.Events[0], Constants.SourceContextPropertyName));
            Assert.Equal("OrderService", GetScalarValue(_sink.Events[0], "LoggerName"));
            Assert.Equal(logger.SessionId, GetScalarValue(_sink.Events[0], "SessionId"));
            Assert.Equal(logger.SessionStartedAt, GetScalarValue(_sink.Events[0], "SessionStartedAt"));
            Assert.Equal(true, GetScalarValue(_sink.Events[1], "LoggerSuccess"));
            Assert.Equal("warn", _sink.Events[2].RenderMessage());
            Assert.Equal("error", _sink.Events[3].RenderMessage());
            Assert.Equal("fatal", _sink.Events[4].RenderMessage());
            Assert.Equal(LogEventLevel.Information, _sink.Events[1].Level);
        }

        /// <summary>
        /// 验证异常会作为独立字段写入，而不是污染普通消息文本。
        /// </summary>
        [Fact]
        public void Write_PreservesExceptionAndEscapesLiteralBraces()
        {
            SerilogLoggerFactory factory = new SerilogLoggerFactory();
            SerilogLogger logger = (SerilogLogger)factory.CreateLogger("ExceptionLogger");
            InvalidOperationException exception = new InvalidOperationException("boom");

            logger.Write(LogLevel.Error, "failure {reason}", exception);

            Assert.Single(_sink.Events);
            Assert.Equal("failure {reason}", _sink.Events[0].RenderMessage());
            Assert.Same(exception, _sink.Events[0].Exception);
            Assert.Equal(LogEventLevel.Error, _sink.Events[0].Level);
        }

        /// <summary>
        /// 验证 Serilog 可以将日志写入本地文件。
        /// </summary>
        [Fact]
        public void Write_CanPersistToLocalFile()
        {
            string logDirectory = Path.Combine(Path.GetTempPath(), "Logger.Serilog.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(logDirectory);
            string logFilePath = Path.Combine(logDirectory, "app.log");

            Log.CloseAndFlush();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(logFilePath, shared: true)
                .CreateLogger();

            SerilogLoggerFactory factory = new SerilogLoggerFactory();
            ILoggerOutput logger = factory.CreateLogger("LocalFile");

            logger.Info("persisted to file");
            Log.CloseAndFlush();

            Assert.True(File.Exists(logFilePath));
            string fileContent = File.ReadAllText(logFilePath);
            Assert.Contains("persisted to file", fileContent);
        }

        /// <summary>
        /// 清理全局 Serilog.Logger。
        /// </summary>
        public void Dispose()
        {
            Log.CloseAndFlush();
        }

        private static object GetScalarValue(LogEvent logEvent, string propertyName)
        {
            LogEventPropertyValue value;
            if (!logEvent.Properties.TryGetValue(propertyName, out value))
            {
                return null;
            }

            ScalarValue scalarValue = value as ScalarValue;
            return scalarValue != null ? scalarValue.Value : null;
        }

        private sealed class CollectingSink : ILogEventSink
        {
            private readonly object _syncRoot = new object();
            private readonly List<LogEvent> _events = new List<LogEvent>();

            public IReadOnlyList<LogEvent> Events
            {
                get { return _events; }
            }

            public void Emit(LogEvent logEvent)
            {
                if (logEvent == null)
                {
                    return;
                }

                lock (_syncRoot)
                {
                    _events.Add(logEvent);
                }
            }
        }
    }
}
