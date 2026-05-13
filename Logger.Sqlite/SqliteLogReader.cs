using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using Logger.Core.Models;

namespace Logger.Sqlite
{
    /// <summary>
    /// 从 SQLite 日志库中读取最近的记录，用于预览和测试。
    /// </summary>
    public static class SqliteLogReader
    {
        /// <summary>
        /// 读取指定 SQLite 数据库中的最新日志记录。
        /// </summary>
        /// <param name="options">SQLite 存储配置。</param>
        /// <param name="maxCount">最多读取的记录数。</param>
        /// <returns>按时间倒序排列的日志记录集合。</returns>
        public static IReadOnlyList<SqliteLogRecord> ReadLatestRecords(
            SqliteLogStorageOptions options,
            int maxCount)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (maxCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            if (!File.Exists(options.DatabaseFilePath))
            {
                return new SqliteLogRecord[0];
            }

            List<SqliteLogRecord> records = new List<SqliteLogRecord>();
            using (SQLiteConnection connection = new SQLiteConnection(options.BuildConnectionString()))
            {
                connection.Open();
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandTimeout = options.CommandTimeoutSeconds;
                    command.CommandText = BuildQuerySql(options.TableName);
                    command.Parameters.Add("@MaxCount", System.Data.DbType.Int32).Value = maxCount;

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            records.Add(ReadRecord(reader));
                        }
                    }
                }
            }

            return records.ToArray();
        }

        private static string BuildQuerySql(string tableName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "SELECT [Id], [EntryHash], [LoggerName], [SessionId], [SessionStartedAtUtcTicks], [TimestampUtcTicks], [LevelValue], [Message] " +
                "FROM [{0}] ORDER BY [Id] DESC LIMIT @MaxCount;",
                tableName);
        }

        private static SqliteLogRecord ReadRecord(SQLiteDataReader reader)
        {
            long id = reader.GetInt64(0);
            string entryHash = reader.GetString(1);
            string loggerName = reader.GetString(2);
            Guid sessionId = Guid.Parse(reader.GetString(3));
            DateTime sessionStartedAt = FromUtcTicks(reader.GetInt64(4));
            DateTime timestamp = FromUtcTicks(reader.GetInt64(5));
            LogLevel level = (LogLevel)reader.GetInt32(6);
            string message = reader.IsDBNull(7) ? string.Empty : reader.GetString(7);

            return new SqliteLogRecord(
                id,
                entryHash,
                loggerName,
                sessionId,
                sessionStartedAt,
                timestamp,
                level,
                message);
        }

        private static DateTime FromUtcTicks(long utcTicks)
        {
            return new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime();
        }
    }
}
