using System;
using System.Data.SQLite;
using System.IO;

namespace Logger.Sqlite
{
    /// <summary>
    /// 定义 SQLite 日志存储后端的基础配置。
    /// </summary>
    public sealed class SqliteLogStorageOptions
    {
        private const string DefaultDatabaseFileName = "Logger.sqlite";
        private const string DefaultTableName = "LogEntries";
        private const int DefaultCommandTimeoutSeconds = 30;

        /// <summary>
        /// 创建默认 SQLite 配置。
        /// </summary>
        /// <returns>默认配置实例。</returns>
        public static SqliteLogStorageOptions CreateDefault()
        {
            return new SqliteLogStorageOptions();
        }

        /// <summary>
        /// 初始化 SQLite 存储配置。
        /// </summary>
        /// <param name="databaseFilePath">数据库文件路径；为空时使用启动目录下的默认文件。</param>
        /// <param name="tableName">日志表名称。</param>
        /// <param name="commandTimeoutSeconds">命令超时时间，单位为秒。</param>
        /// <param name="autoCreateTable">是否在首次写入时自动创建表结构。</param>
        public SqliteLogStorageOptions(
            string databaseFilePath = null,
            string tableName = DefaultTableName,
            int commandTimeoutSeconds = DefaultCommandTimeoutSeconds,
            bool autoCreateTable = true)
        {
            DatabaseFilePath = ResolveDatabaseFilePath(databaseFilePath);
            TableName = NormalizeTableName(tableName);
            CommandTimeoutSeconds = Math.Max(1, commandTimeoutSeconds);
            AutoCreateTable = autoCreateTable;
        }

        /// <summary>
        /// 获取 SQLite 数据库文件路径。
        /// </summary>
        public string DatabaseFilePath { get; }

        /// <summary>
        /// 获取日志表名称。
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// 获取命令超时时间，单位为秒。
        /// </summary>
        public int CommandTimeoutSeconds { get; }

        /// <summary>
        /// 获取是否自动创建表结构。
        /// </summary>
        public bool AutoCreateTable { get; }

        internal string BuildConnectionString()
        {
            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder();
            builder.DataSource = DatabaseFilePath;
            builder.Version = 3;
            builder.Pooling = true;
            builder.BinaryGUID = false;
            builder.DefaultTimeout = CommandTimeoutSeconds;
            builder.BusyTimeout = CommandTimeoutSeconds * 1000;
            builder.FailIfMissing = false;
            builder.SyncMode = SynchronizationModes.Normal;
            builder.JournalMode = SQLiteJournalModeEnum.Wal;
            builder.DateTimeKind = DateTimeKind.Utc;
            builder.ForeignKeys = true;
            return builder.ConnectionString;
        }

        private static string ResolveDatabaseFilePath(string databaseFilePath)
        {
            if (string.IsNullOrWhiteSpace(databaseFilePath))
            {
                return Path.Combine(AppContext.BaseDirectory, DefaultDatabaseFileName);
            }

            string trimmedPath = databaseFilePath.Trim();
            if (Path.IsPathRooted(trimmedPath))
            {
                return trimmedPath;
            }

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, trimmedPath));
        }

        private static string NormalizeTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException("表名称不能为空。", nameof(tableName));
            }

            string trimmedName = tableName.Trim();
            if (trimmedName.Length == 0)
            {
                throw new ArgumentException("表名称不能为空。", nameof(tableName));
            }

            if (!IsValidIdentifier(trimmedName))
            {
                throw new ArgumentException("表名称只能包含字母、数字和下划线，且不能以数字开头。", nameof(tableName));
            }

            return trimmedName;
        }

        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!(char.IsLetter(value[0]) || value[0] == '_'))
            {
                return false;
            }

            for (int index = 1; index < value.Length; index++)
            {
                char character = value[index];
                if (!(char.IsLetterOrDigit(character) || character == '_'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
