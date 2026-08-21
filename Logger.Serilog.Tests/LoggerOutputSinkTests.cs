using System;
using System.Collections.Generic;
using Logger.Core;
using Logger.Core.Models;
using Serilog;
using Xunit;

namespace Logger.Serilog.Tests
{
    /// <summary>
    /// 验证 Serilog 到 ILoggerOutput 的转发行为。
    /// </summary>
    public sealed class LoggerOutputSinkTests : IDisposable
    {
        private readonly RecordingLogger _logger = new RecordingLogger();

        /// <summary>
        /// 初始化 Serilog，并注册 LoggerOutputSink。
        /// </summary>
        public LoggerOutputSinkTests()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(new LoggerOutputSink(_logger))
                .CreateLogger();
        }

        /// <summary>
        /// 验证各 Serilog 等级可以映射到 Logger.Core 等级。
        /// </summary>
        [Fact]
        public void Emit_MapsLevelsAndRendersMessage()
        {
            Log.Verbose("trace {Value}", 1);
            Log.Debug("debug");
            Log.Information("info");
            Log.Warning("warn");
            Log.Error("error");
            Log.Fatal("fatal");

            Assert.Equal(6, _logger.Entries.Count);
            Assert.Equal(LogLevel.Trace, _logger.Entries[0].Level);
            Assert.Equal("trace 1", _logger.Entries[0].Message);
            Assert.Equal(LogLevel.Debug, _logger.Entries[1].Level);
            Assert.Equal(LogLevel.Info, _logger.Entries[2].Level);
            Assert.Equal(LogLevel.Warn, _logger.Entries[3].Level);
            Assert.Equal(LogLevel.Error, _logger.Entries[4].Level);
            Assert.Equal(LogLevel.Fatal, _logger.Entries[5].Level);
        }

        /// <summary>
        /// 验证异常会转发到 Logger.Core 日志消息中。
        /// </summary>
        [Fact]
        public void Emit_AppendsExceptionText()
        {
            InvalidOperationException exception = new InvalidOperationException("boom");

            Log.Error(exception, "operation failed");

            Assert.Single(_logger.Entries);
            Assert.Equal(LogLevel.Error, _logger.Entries[0].Level);
            Assert.Contains("operation failed", _logger.Entries[0].Message);
            Assert.Contains("InvalidOperationException", _logger.Entries[0].Message);
            Assert.Contains("boom", _logger.Entries[0].Message);
        }

        /// <summary>
        /// 释放全局 Serilog 日志器。
        /// </summary>
        public void Dispose()
        {
            Log.CloseAndFlush();
        }

        private sealed class RecordingLogger : ILoggerOutput
        {
            public List<LogEntry> Entries { get; } = new List<LogEntry>();

            public void SetMinimumLevel(LogLevel minimumLevel)
            {
            }

            public void Trace(string message)
            {
                AddLog(LogLevel.Trace, message);
            }

            public void Debug(string message)
            {
                AddLog(LogLevel.Debug, message);
            }

            public void Info(string message)
            {
                AddLog(LogLevel.Info, message);
            }

            public void Success(string message)
            {
                AddLog(LogLevel.Success, message);
            }

            public void Warning(string message)
            {
                AddLog(LogLevel.Warn, message);
            }

            public void Error(string message)
            {
                AddLog(LogLevel.Error, message);
            }

            public void Fatal(string message)
            {
                AddLog(LogLevel.Fatal, message);
            }

            public void AddLog(LogLevel level, string message)
            {
                Entries.Add(new LogEntry(DateTime.Now, level, message));
            }

            public void AddLogs(IEnumerable<LogEntry> entries)
            {
                if (entries == null)
                {
                    return;
                }

                Entries.AddRange(entries);
            }
        }
    }
}
