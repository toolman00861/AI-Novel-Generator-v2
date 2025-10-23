using System;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using AINovelStudio.Models;
using AINovelStudio.Services.Persistence;

namespace AINovelStudio.Services
{
    /// <summary>
    /// 小说与章节的本地持久化服务（SQLite）。
    /// </summary>
    public class NovelStorageService
    {
        private readonly IPersistenceService _persistence;

        public NovelStorageService(IPersistenceService? persistence = null)
        {
            _persistence = persistence ?? new SqlitePersistenceService();
            _persistence.EnsureInitialized();
            EnsureSchema();
        }

        private void EnsureSchema()
        {
            using var conn = _persistence.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Novels (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL UNIQUE,
                    Description TEXT,
                    Author TEXT,
                    Status TEXT,
                    TagsJson TEXT,
                    CreatedAt TEXT,
                    UpdatedAt TEXT
                );";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Chapters (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    NovelId INTEGER NOT NULL,
                    Title TEXT NOT NULL,
                    Content TEXT,
                    OrderIndex INTEGER,
                    Status TEXT,
                    Summary TEXT,
                    CreatedAt TEXT,
                    UpdatedAt TEXT,
                    UNIQUE(NovelId, Title),
                    FOREIGN KEY(NovelId) REFERENCES Novels(Id) ON DELETE CASCADE
                );";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 根据小说标题获取或创建小说，返回小说Id。
        /// </summary>
        public int EnsureNovel(string title, Novel? source = null)
        {
            using var conn = _persistence.CreateConnection();
            conn.Open();

            using (var find = conn.CreateCommand())
            {
                find.CommandText = "SELECT Id FROM Novels WHERE Title = @Title LIMIT 1;";
                find.Parameters.AddWithValue("@Title", title);
                var id = find.ExecuteScalar() as long?;
                if (id.HasValue) return (int)id.Value;
            }

            using (var ins = conn.CreateCommand())
            {
                ins.CommandText = @"
                    INSERT INTO Novels (Title, Description, Author, Status, TagsJson, CreatedAt, UpdatedAt)
                    VALUES (@Title, @Description, @Author, @Status, @TagsJson, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();";
                ins.Parameters.AddWithValue("@Title", title);
                ins.Parameters.AddWithValue("@Description", source?.Description ?? "");
                ins.Parameters.AddWithValue("@Author", source?.Author ?? "");
                ins.Parameters.AddWithValue("@Status", (source?.Status ?? NovelStatus.InProgress).ToString());
                ins.Parameters.AddWithValue("@TagsJson", JsonSerializer.Serialize(source?.Tags ?? new System.Collections.Generic.List<string>()));
                var now = DateTime.Now.ToString("o");
                ins.Parameters.AddWithValue("@CreatedAt", now);
                ins.Parameters.AddWithValue("@UpdatedAt", now);
                var idObj = ins.ExecuteScalar() as long? ?? 0;
                return (int)idObj;
            }
        }

        /// <summary>
        /// 根据小说Id与章节标题获取或创建章节，返回章节Id。
        /// </summary>
        public int EnsureChapter(int novelId, string chapterTitle, Chapter? source = null)
        {
            using var conn = _persistence.CreateConnection();
            conn.Open();

            using (var find = conn.CreateCommand())
            {
                find.CommandText = "SELECT Id FROM Chapters WHERE NovelId = @NovelId AND Title = @Title LIMIT 1;";
                find.Parameters.AddWithValue("@NovelId", novelId);
                find.Parameters.AddWithValue("@Title", chapterTitle);
                var id = find.ExecuteScalar() as long?;
                if (id.HasValue) return (int)id.Value;
            }

            using (var ins = conn.CreateCommand())
            {
                ins.CommandText = @"
                    INSERT INTO Chapters (NovelId, Title, Content, OrderIndex, Status, Summary, CreatedAt, UpdatedAt)
                    VALUES (@NovelId, @Title, @Content, @OrderIndex, @Status, @Summary, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();";
                ins.Parameters.AddWithValue("@NovelId", novelId);
                ins.Parameters.AddWithValue("@Title", chapterTitle);
                ins.Parameters.AddWithValue("@Content", source?.Content ?? "");
                ins.Parameters.AddWithValue("@OrderIndex", source?.OrderIndex ?? 0);
                ins.Parameters.AddWithValue("@Status", (source?.Status ?? ChapterStatus.Draft).ToString());
                ins.Parameters.AddWithValue("@Summary", source?.Summary ?? "");
                var now = DateTime.Now.ToString("o");
                ins.Parameters.AddWithValue("@CreatedAt", now);
                ins.Parameters.AddWithValue("@UpdatedAt", now);
                var idObj = ins.ExecuteScalar() as long? ?? 0;
                return (int)idObj;
            }
        }

        /// <summary>
        /// 保存生成内容到指定小说与章节（按标题匹配，不存在则创建）。
        /// </summary>
        public void SaveGeneratedContent(string novelTitle, string chapterTitle, string content)
        {
            var novelId = EnsureNovel(novelTitle);
            var chapterId = EnsureChapter(novelId, chapterTitle);

            using var conn = _persistence.CreateConnection();
            conn.Open();
            using var upd = conn.CreateCommand();
            upd.CommandText = @"
                UPDATE Chapters
                SET Content = @Content,
                    Status = @Status,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id;";
            upd.Parameters.AddWithValue("@Content", content ?? "");
            upd.Parameters.AddWithValue("@Status", ChapterStatus.InProgress.ToString());
            upd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("o"));
            upd.Parameters.AddWithValue("@Id", chapterId);
            upd.ExecuteNonQuery();
        }
    }
}