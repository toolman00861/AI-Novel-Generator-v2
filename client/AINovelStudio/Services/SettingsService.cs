using System;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using AINovelStudio.Models;
using AINovelStudio.Services.Persistence;

namespace AINovelStudio.Services
{
    public class SettingsService
    {
        private readonly IPersistenceService _persistence;
        private readonly string _jsonPath;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public SettingsService() : this(new SqlitePersistenceService()) { }

        public SettingsService(IPersistenceService persistence)
        {
            _persistence = persistence;
            _jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.client.json");

            _persistence.EnsureInitialized();
            EnsureAppSettingsTable();
        }

        public AppSettings Load()
        {
            using var conn = _persistence.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT ApiBase, Vendor, ApiKey, BaseUrl, DefaultModel,
                                        StreamingEnabled, AutoSaveEnabled,
                                        WordLimit, Temperature, MaxTokens
                                 FROM AppSettings WHERE Id = 1 LIMIT 1";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new AppSettings
                {
                    ApiBase = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    Provider = new ProviderSettings
                    {
                        Vendor = reader.IsDBNull(1) ? "openai" : reader.GetString(1),
                        ApiKey = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        BaseUrl = reader.IsDBNull(3) ? "https://api.openai.com/v1" : reader.GetString(3),
                        DefaultModel = reader.IsDBNull(4) ? "gpt-4o-mini" : reader.GetString(4)
                    },
                    FeatureFlags = new FeatureFlags
                    {
                        StreamingEnabled = !reader.IsDBNull(5) && reader.GetInt32(5) == 1,
                        AutoSaveEnabled = !reader.IsDBNull(6) && reader.GetInt32(6) == 1
                    },
                    GenerationDefaults = new GenerationDefaults
                    {
                        WordLimit = reader.IsDBNull(7) ? 500 : reader.GetInt32(7),
                        Temperature = reader.IsDBNull(8) ? 0.7 : reader.GetDouble(8),
                        MaxTokens = reader.IsDBNull(9) ? 1024 : reader.GetInt32(9)
                    }
                };
            }

            // 如果数据库尚无记录，尝试从旧的 JSON 文件迁移一次
            try
            {
                if (File.Exists(_jsonPath))
                {
                    var json = File.ReadAllText(_jsonPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    Save(settings);
                    return settings;
                }
            }
            catch
            {
                // ignore migration errors and return defaults
            }

            return new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            using var conn = _persistence.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO AppSettings (
                                    Id, ApiBase, Vendor, ApiKey, BaseUrl, DefaultModel,
                                    StreamingEnabled, AutoSaveEnabled, WordLimit, Temperature, MaxTokens, UpdatedAt)
                                VALUES (
                                    1, @ApiBase, @Vendor, @ApiKey, @BaseUrl, @DefaultModel,
                                    @StreamingEnabled, @AutoSaveEnabled, @WordLimit, @Temperature, @MaxTokens, @UpdatedAt)
                                ON CONFLICT(Id) DO UPDATE SET
                                    ApiBase=excluded.ApiBase,
                                    Vendor=excluded.Vendor,
                                    ApiKey=excluded.ApiKey,
                                    BaseUrl=excluded.BaseUrl,
                                    DefaultModel=excluded.DefaultModel,
                                    StreamingEnabled=excluded.StreamingEnabled,
                                    AutoSaveEnabled=excluded.AutoSaveEnabled,
                                    WordLimit=excluded.WordLimit,
                                    Temperature=excluded.Temperature,
                                    MaxTokens=excluded.MaxTokens,
                                    UpdatedAt=excluded.UpdatedAt";

            cmd.Parameters.AddWithValue("@ApiBase", settings.ApiBase ?? string.Empty);
            cmd.Parameters.AddWithValue("@Vendor", settings.Provider?.Vendor ?? "openai");
            cmd.Parameters.AddWithValue("@ApiKey", settings.Provider?.ApiKey ?? string.Empty);
            cmd.Parameters.AddWithValue("@BaseUrl", settings.Provider?.BaseUrl ?? "https://api.openai.com/v1");
            cmd.Parameters.AddWithValue("@DefaultModel", settings.Provider?.DefaultModel ?? "gpt-4o-mini");
            cmd.Parameters.AddWithValue("@StreamingEnabled", settings.FeatureFlags?.StreamingEnabled == true ? 1 : 0);
            cmd.Parameters.AddWithValue("@AutoSaveEnabled", settings.FeatureFlags?.AutoSaveEnabled == true ? 1 : 0);
            cmd.Parameters.AddWithValue("@WordLimit", settings.GenerationDefaults?.WordLimit ?? 500);
            cmd.Parameters.AddWithValue("@Temperature", settings.GenerationDefaults?.Temperature ?? 0.7);
            cmd.Parameters.AddWithValue("@MaxTokens", settings.GenerationDefaults?.MaxTokens ?? 1024);
            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));

            cmd.ExecuteNonQuery();

            // 同步更新 JSON 文件（可选，作为备份）
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_jsonPath)!);
                var json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(_jsonPath, json);
            }
            catch
            {
                // ignore backup errors
            }
        }

        private void EnsureAppSettingsTable()
        {
            try
            {
                using var conn = _persistence.CreateConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS AppSettings (
                                        Id INTEGER PRIMARY KEY,
                                        ApiBase TEXT,
                                        Vendor TEXT,
                                        ApiKey TEXT,
                                        BaseUrl TEXT,
                                        DefaultModel TEXT,
                                        StreamingEnabled INTEGER,
                                        AutoSaveEnabled INTEGER,
                                        WordLimit INTEGER,
                                        Temperature REAL,
                                        MaxTokens INTEGER,
                                        UpdatedAt TEXT
                                    );";
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // swallow init errors; will fallback to defaults on load
            }
        }
    }
}