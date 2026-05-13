using System;
using Logger.Core;

namespace Logger.Sqlite
{
    /// <summary>
    /// 为指定 logger 创建 SQLite 存储后端。
    /// </summary>
    public sealed class SqliteLogStorageBackendFactory : ILogStorageBackendFactory
    {
        /// <summary>
        /// 初始化 SQLite 存储后端工厂。
        /// </summary>
        /// <param name="options">SQLite 存储配置。</param>
        public SqliteLogStorageBackendFactory(SqliteLogStorageOptions options = null)
        {
            Options = options ?? SqliteLogStorageOptions.CreateDefault();
        }

        /// <summary>
        /// 获取当前工厂配置。
        /// </summary>
        public SqliteLogStorageOptions Options { get; }

        /// <inheritdoc />
        public ILogStorageBackend CreateBackend(LogStorageContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return new SqliteLogStorageBackend(Options);
        }
    }
}
