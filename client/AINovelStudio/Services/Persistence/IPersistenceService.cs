using Microsoft.Data.Sqlite;

namespace AINovelStudio.Services.Persistence
{
    /// <summary>
    /// 抽象持久化存储服务，提供数据库连接与初始化。
    /// </summary>
    public interface IPersistenceService
    {
        /// <summary>
        /// 数据库文件绝对路径。
        /// </summary>
        string DatabasePath { get; }

        /// <summary>
        /// 创建一个新的 SQLite 连接（未打开）。
        /// </summary>
        SqliteConnection CreateConnection();

        /// <summary>
        /// 执行基础初始化（创建目录/文件等）。
        /// </summary>
        void EnsureInitialized();
    }
}