using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace AINovelStudio.Services.Persistence
{
    /// <summary>
    /// SQLite 持久化服务实现：负责路径管理与基础初始化。
    /// </summary>
    public class SqlitePersistenceService : IPersistenceService
    {
        public string DatabasePath { get; }

        public SqlitePersistenceService(string? customDbPath = null)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            DatabasePath = customDbPath ?? Path.Combine(baseDir, "AINovelStudio.settings.db");
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection($"Data Source={DatabasePath}");
        }

        public void EnsureInitialized()
        {
            var dir = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            // SQLite 在首次连接时会自动创建文件，无需显式创建。
        }
    }
}