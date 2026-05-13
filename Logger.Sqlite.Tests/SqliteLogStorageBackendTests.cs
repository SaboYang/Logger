using System;
using System.Collections.Generic;
using System.IO;
using Logger.Core;
using Logger.Core.Models;
using Logger.Sqlite;
using Xunit;

namespace Logger.Sqlite.Tests
{
    /// <summary>
    /// 验证 SQLite 存储后端的写入与读取行为。
    /// </summary>
    public sealed class SqliteLogStorageBackendTests
    {
        [Fact]
        public void WriteBatch_CreatesTable_AndPersistsRecords()
        {
            string rootDirectory = CreateTempDirectory();
            try
            {
                SqliteLogStorageOptions options = new SqliteLogStorageOptions(
                    Path.Combine(rootDirectory, "logger.db"));
                SqliteLogStorageBackend backend = new SqliteLogStorageBackend(options);
                LogStorageContext context = CreateContext("OrderService");
                IReadOnlyList<LogEntry> entries = CreateEntries();

                backend.WriteBatch(entries, context);

                IReadOnlyList<SqliteLogRecord> records = SqliteLogReader.ReadLatestRecords(options, 10);

                Assert.Equal(3, records.Count);
                Assert.Equal("OrderService", records[0].LoggerName);
                Assert.Equal("DEBUG", records[0].LevelText);
                Assert.Equal(new string('x', 1024), records[0].Message);
                Assert.Equal("Error line 1\r\nError line 2", records[1].Message);
                Assert.Equal("Startup complete", records[2].Message);
            }
            finally
            {
                DeleteDirectory(rootDirectory);
            }
        }

        [Fact]
        public void WriteBatch_IsIdempotent_ForSameReplay()
        {
            string rootDirectory = CreateTempDirectory();
            try
            {
                SqliteLogStorageOptions options = new SqliteLogStorageOptions(
                    Path.Combine(rootDirectory, "logger.db"));
                SqliteLogStorageBackend backend = new SqliteLogStorageBackend(options);
                LogStorageContext context = CreateContext("ReplayService");
                IReadOnlyList<LogEntry> entries = CreateEntries();

                backend.WriteBatch(entries, context);
                backend.WriteBatch(entries, context);

                IReadOnlyList<SqliteLogRecord> records = SqliteLogReader.ReadLatestRecords(options, 50);

                Assert.Equal(entries.Count, records.Count);
            }
            finally
            {
                DeleteDirectory(rootDirectory);
            }
        }

        [Fact]
        public void WriteBatch_Throws_WhenDatabasePathIsDirectory()
        {
            string rootDirectory = CreateTempDirectory();
            try
            {
                SqliteLogStorageOptions options = new SqliteLogStorageOptions(rootDirectory);
                SqliteLogStorageBackend backend = new SqliteLogStorageBackend(options);
                LogStorageContext context = CreateContext("BrokenService");

                Assert.ThrowsAny<Exception>(() => backend.WriteBatch(CreateEntries(), context));
            }
            finally
            {
                DeleteDirectory(rootDirectory);
            }
        }

        [Fact]
        public void SqliteLogStorageOptions_RejectsInvalidTableName()
        {
            Assert.Throws<ArgumentException>(
                () => new SqliteLogStorageOptions(tableName: "1bad-name"));
        }

        private static IReadOnlyList<LogEntry> CreateEntries()
        {
            return new[]
            {
                new LogEntry(
                    new DateTime(2026, 5, 13, 10, 0, 0, DateTimeKind.Local),
                    LogLevel.Info,
                    "Startup complete"),
                new LogEntry(
                    new DateTime(2026, 5, 13, 10, 0, 1, DateTimeKind.Local),
                    LogLevel.Error,
                    "Error line 1\r\nError line 2"),
                new LogEntry(
                    new DateTime(2026, 5, 13, 10, 0, 2, DateTimeKind.Local),
                    LogLevel.Debug,
                    new string('x', 1024))
            };
        }

        private static LogStorageContext CreateContext(string loggerName)
        {
            return new LogStorageContext(
                loggerName,
                Guid.Parse("7d88b93f-4c0d-4f74-bf52-5f0d4c0c4f2f"),
                new DateTime(2026, 5, 13, 9, 59, 0, DateTimeKind.Local),
                LogLevel.Trace,
                5000,
                5000,
                null,
                LogSpoolFlushMode.Buffered);
        }

        private static string CreateTempDirectory()
        {
            string rootDirectory = Path.Combine(Path.GetTempPath(), "Logger.Sqlite.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootDirectory);
            return rootDirectory;
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
            }
        }
    }
}
