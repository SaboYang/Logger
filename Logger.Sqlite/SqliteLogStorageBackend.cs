using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Logger.Core;
using Logger.Core.Models;

namespace Logger.Sqlite
{
    /// <summary>
    /// 将日志批量写入 SQLite 数据库。
    /// </summary>
    public sealed class SqliteLogStorageBackend : ILogStorageBackend
    {
        private readonly SqliteLogStorageOptions _options;
        private readonly object _initializeSyncRoot = new object();
        private bool _schemaInitialized;

        /// <summary>
        /// 初始化 SQLite 日志后端。
        /// </summary>
        /// <param name="options">SQLite 存储配置。</param>
        public SqliteLogStorageBackend(SqliteLogStorageOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public void WriteBatch(IReadOnlyList<LogEntry> entries, LogStorageContext context)
        {
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            EnsureDatabaseDirectory();

            using (SQLiteConnection connection = new SQLiteConnection(_options.BuildConnectionString()))
            {
                connection.Open();
                EnsureSchema(connection);

                using (SQLiteTransaction transaction = connection.BeginTransaction())
                using (SQLiteCommand command = CreateInsertCommand(connection, transaction))
                {
                    WriteEntries(entries, context, command);
                    transaction.Commit();
                }
            }
        }

        private void EnsureSchema(SQLiteConnection connection)
        {
            if (_schemaInitialized)
            {
                return;
            }

            lock (_initializeSyncRoot)
            {
                if (_schemaInitialized)
                {
                    return;
                }

                if (_options.AutoCreateTable)
                {
                    ExecuteNonQuery(connection, BuildCreateTableSql(_options.TableName));
                    ExecuteNonQuery(connection, BuildCreateLoggerIndexSql(_options.TableName));
                    ExecuteNonQuery(connection, BuildCreateSessionIndexSql(_options.TableName));
                }

                _schemaInitialized = true;
            }
        }

        private void WriteEntries(
            IReadOnlyList<LogEntry> entries,
            LogStorageContext context,
            SQLiteCommand command)
        {
            string loggerName = context.LoggerName ?? string.Empty;
            string sessionId = context.SessionId.ToString("D");
            long sessionStartedAtUtcTicks = ToUtcTicks(context.SessionStartedAt);

            for (int index = 0; index < entries.Count; index++)
            {
                LogEntry entry = entries[index];
                if (entry == null)
                {
                    continue;
                }

                string message = entry.Message ?? string.Empty;
                long timestampUtcTicks = ToUtcTicks(entry.Timestamp);
                LogLevel level = entry.Level;
                string entryHash = BuildEntryHash(
                    loggerName,
                    sessionId,
                    sessionStartedAtUtcTicks,
                    timestampUtcTicks,
                    level,
                    message);

                command.Parameters["@EntryHash"].Value = entryHash;
                command.Parameters["@LoggerName"].Value = loggerName;
                command.Parameters["@SessionId"].Value = sessionId;
                command.Parameters["@SessionStartedAtUtcTicks"].Value = sessionStartedAtUtcTicks;
                command.Parameters["@TimestampUtcTicks"].Value = timestampUtcTicks;
                command.Parameters["@LevelValue"].Value = (int)level;
                command.Parameters["@LevelText"].Value = level.ToString().ToUpperInvariant();
                command.Parameters["@Message"].Value = message;
                command.ExecuteNonQuery();
            }
        }

        private SQLiteCommand CreateInsertCommand(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            SQLiteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandTimeout = _options.CommandTimeoutSeconds;
            command.CommandText = BuildInsertSql(_options.TableName);
            command.Parameters.Add("@EntryHash", DbType.String);
            command.Parameters.Add("@LoggerName", DbType.String);
            command.Parameters.Add("@SessionId", DbType.String);
            command.Parameters.Add("@SessionStartedAtUtcTicks", DbType.Int64);
            command.Parameters.Add("@TimestampUtcTicks", DbType.Int64);
            command.Parameters.Add("@LevelValue", DbType.Int32);
            command.Parameters.Add("@LevelText", DbType.String);
            command.Parameters.Add("@Message", DbType.String);
            command.Prepare();
            return command;
        }

        private void EnsureDatabaseDirectory()
        {
            string directoryPath = Path.GetDirectoryName(_options.DatabaseFilePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private static void ExecuteNonQuery(SQLiteConnection connection, string sql)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        private static string BuildCreateTableSql(string tableName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "CREATE TABLE IF NOT EXISTS [{0}] (" +
                "[Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "[EntryHash] TEXT NOT NULL UNIQUE, " +
                "[LoggerName] TEXT NOT NULL, " +
                "[SessionId] TEXT NOT NULL, " +
                "[SessionStartedAtUtcTicks] INTEGER NOT NULL, " +
                "[TimestampUtcTicks] INTEGER NOT NULL, " +
                "[LevelValue] INTEGER NOT NULL, " +
                "[LevelText] TEXT NOT NULL, " +
                "[Message] TEXT NOT NULL);",
                tableName);
        }

        private static string BuildCreateLoggerIndexSql(string tableName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "CREATE INDEX IF NOT EXISTS [IX_{0}_LoggerName_Timestamp] ON [{0}] ([LoggerName], [TimestampUtcTicks]);",
                tableName);
        }

        private static string BuildCreateSessionIndexSql(string tableName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "CREATE INDEX IF NOT EXISTS [IX_{0}_SessionId] ON [{0}] ([SessionId]);",
                tableName);
        }

        private string BuildInsertSql(string tableName)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "INSERT OR IGNORE INTO [{0}] (" +
                "[EntryHash], [LoggerName], [SessionId], [SessionStartedAtUtcTicks], [TimestampUtcTicks], [LevelValue], [LevelText], [Message]) " +
                "VALUES (@EntryHash, @LoggerName, @SessionId, @SessionStartedAtUtcTicks, @TimestampUtcTicks, @LevelValue, @LevelText, @Message);",
                tableName);
        }

        private static long ToUtcTicks(DateTime value)
        {
            return value.ToUniversalTime().Ticks;
        }

        private static string BuildEntryHash(
            string loggerName,
            string sessionId,
            long sessionStartedAtUtcTicks,
            long timestampUtcTicks,
            LogLevel level,
            string message)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string payload = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}\u001f{1}\u001f{2}\u001f{3}\u001f{4}\u001f{5}",
                    loggerName ?? string.Empty,
                    sessionId ?? string.Empty,
                    sessionStartedAtUtcTicks,
                    timestampUtcTicks,
                    (int)level,
                    message ?? string.Empty);

                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                byte[] hash = sha256.ComputeHash(bytes);
                return ToHex(hash);
            }
        }

        private static string ToHex(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);
            for (int index = 0; index < bytes.Length; index++)
            {
                builder.Append(bytes[index].ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }
    }
}
