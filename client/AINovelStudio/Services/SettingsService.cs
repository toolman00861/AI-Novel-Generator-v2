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

        private void EnsureAppSettingsTable()
        {
            try
            {
                using var conn = _persistence.CreateConnection();
                conn.Open();

                // Create or migrate AppSettings table (remove provider fields, add SelectedProviderName)
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS AppSettingsNew (
                                        Id INTEGER PRIMARY KEY,
                                        ApiBase TEXT,
                                        SelectedProviderName TEXT,
                                        StreamingEnabled INTEGER,
                                        AutoSaveEnabled INTEGER,
                                        WordLimit INTEGER,
                                        Temperature REAL,
                                        MaxTokens INTEGER,
                                        UpdatedAt TEXT
                                    );";
                cmd.ExecuteNonQuery();

                // Migrate data from old table if exists
                cmd.CommandText = @"INSERT OR REPLACE INTO AppSettingsNew (Id, ApiBase, StreamingEnabled, AutoSaveEnabled, WordLimit, Temperature, MaxTokens, UpdatedAt)
                                    SELECT Id, ApiBase, StreamingEnabled, AutoSaveEnabled, WordLimit, Temperature, MaxTokens, UpdatedAt
                                    FROM AppSettings;";
                cmd.ExecuteNonQuery();

                // Drop old table
                cmd.CommandText = "DROP TABLE IF EXISTS AppSettings;";
                cmd.ExecuteNonQuery();

                // Rename new table
                cmd.CommandText = "ALTER TABLE AppSettingsNew RENAME TO AppSettings;";
                cmd.ExecuteNonQuery();

                // Create ProviderSettings table
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS ProviderSettings (
                                        Name TEXT PRIMARY KEY,
                                        Vendor TEXT,
                                        ApiKey TEXT,
                                        BaseUrl TEXT,
                                        DefaultModel TEXT
                                    );";
                cmd.ExecuteNonQuery();

                // Migrate old provider data if no providers exist
                cmd.CommandText = "SELECT COUNT(*) FROM ProviderSettings;";
                var count = cmd.ExecuteScalar() as long? ?? 0;
                if (count == 0)
                {
                    cmd.CommandText = @"INSERT INTO ProviderSettings (Name, Vendor, ApiKey, BaseUrl, DefaultModel)
                                        SELECT 'Default', Vendor, ApiKey, BaseUrl, DefaultModel
                                        FROM AppSettings WHERE Id = 1 AND Vendor IS NOT NULL LIMIT 1;";
                    cmd.ExecuteNonQuery();

                    // Set default selected
                    cmd.CommandText = "UPDATE AppSettings SET SelectedProviderName = 'Default' WHERE Id = 1;";
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // swallow init errors
            }
        }

        public AppSettings Load()
        {
            var settings = new AppSettings();

            using var conn = _persistence.CreateConnection();
            conn.Open();

            // Load main settings
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT ApiBase, SelectedProviderName, StreamingEnabled, AutoSaveEnabled, WordLimit, Temperature, MaxTokens
                                FROM AppSettings WHERE Id = 1 LIMIT 1;";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                settings.ApiBase = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                settings.SelectedProviderName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                settings.FeatureFlags.StreamingEnabled = !reader.IsDBNull(2) && reader.GetInt32(2) == 1;
                settings.FeatureFlags.AutoSaveEnabled = !reader.IsDBNull(3) && reader.GetInt32(3) == 1;
                settings.GenerationDefaults.WordLimit = reader.IsDBNull(4) ? 500 : reader.GetInt32(4);
                settings.GenerationDefaults.Temperature = reader.IsDBNull(5) ? 0.7 : reader.GetDouble(5);
                settings.GenerationDefaults.MaxTokens = reader.IsDBNull(6) ? 1024 : reader.GetInt32(6);
            }
            reader.Close(); // Explicitly close the reader

            // Load providers
            using var providerCmd = conn.CreateCommand(); // Use a new command
            providerCmd.CommandText = "SELECT Name, Vendor, ApiKey, BaseUrl, DefaultModel FROM ProviderSettings;";
            using var providerReader = providerCmd.ExecuteReader();
            while (providerReader.Read())
            {
                settings.Providers.Add(new ProviderSettings
                {
                    Name = providerReader.GetString(0),
                    Vendor = providerReader.IsDBNull(1) ? "openai" : providerReader.GetString(1),
                    ApiKey = providerReader.IsDBNull(2) ? string.Empty : providerReader.GetString(2),
                    BaseUrl = providerReader.IsDBNull(3) ? "https://api.openai.com/v1" : providerReader.GetString(3),
                    DefaultModel = providerReader.IsDBNull(4) ? "gpt-4o-mini" : providerReader.GetString(4)
                });
            }

            // JSON migration if needed
            if (settings.Providers.Count == 0 && File.Exists(_jsonPath))
            {
                try
                {
                    var json = File.ReadAllText(_jsonPath);
                    var jsonSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

                    var jsonDoc = JsonDocument.Parse(json);
                    if (jsonDoc.RootElement.TryGetProperty("Provider", out var providerElement))
                    {
                        var provider = new ProviderSettings
                        {
                            Name = "Default",
                            Vendor = providerElement.TryGetProperty("Vendor", out var v) ? v.GetString() ?? "openai" : "openai",
                            ApiKey = providerElement.TryGetProperty("ApiKey", out var k) ? k.GetString() ?? "" : "",
                            BaseUrl = providerElement.TryGetProperty("BaseUrl", out var b) ? b.GetString() ?? "https://api.openai.com/v1" : "https://api.openai.com/v1",
                            DefaultModel = providerElement.TryGetProperty("DefaultModel", out var m) ? m.GetString() ?? "gpt-4o-mini" : "gpt-4o-mini"
                        };
                        settings.Providers.Add(provider);
                        settings.SelectedProviderName = "Default";
                    }

                    settings.ApiBase = jsonSettings.ApiBase;
                    settings.FeatureFlags = jsonSettings.FeatureFlags;
                    settings.GenerationDefaults = jsonSettings.GenerationDefaults;
                    Save(settings);
                }
                catch { }
            }

            if (settings.Providers.Count == 0)
            {
                settings.Providers.Add(new ProviderSettings { Name = "Default", Vendor = "openai", BaseUrl = "https://api.openai.com/v1", DefaultModel = "gpt-4o-mini" });
                settings.SelectedProviderName = "Default";
            }

            // 当存在供应商但未设置选中的供应商时，回退到第一个
            if (string.IsNullOrWhiteSpace(settings.SelectedProviderName) && settings.Providers.Count > 0)
            {
                settings.SelectedProviderName = settings.Providers.First().Name;
                try { Save(settings); } catch { }
            }

            return settings;
        }

        public void Save(AppSettings settings)
        {
            using var conn = _persistence.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Save main settings
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"INSERT INTO AppSettings (Id, ApiBase, SelectedProviderName, StreamingEnabled, AutoSaveEnabled, WordLimit, Temperature, MaxTokens, UpdatedAt)
                                    VALUES (1, @ApiBase, @SelectedProviderName, @StreamingEnabled, @AutoSaveEnabled, @WordLimit, @Temperature, @MaxTokens, @UpdatedAt)
                                    ON CONFLICT(Id) DO UPDATE SET
                                        ApiBase = excluded.ApiBase,
                                        SelectedProviderName = excluded.SelectedProviderName,
                                        StreamingEnabled = excluded.StreamingEnabled,
                                        AutoSaveEnabled = excluded.AutoSaveEnabled,
                                        WordLimit = excluded.WordLimit,
                                        Temperature = excluded.Temperature,
                                        MaxTokens = excluded.MaxTokens,
                                        UpdatedAt = excluded.UpdatedAt;";
                cmd.Parameters.AddWithValue("@ApiBase", settings.ApiBase ?? string.Empty);
                cmd.Parameters.AddWithValue("@SelectedProviderName", settings.SelectedProviderName ?? string.Empty);
                cmd.Parameters.AddWithValue("@StreamingEnabled", settings.FeatureFlags.StreamingEnabled ? 1 : 0);
                cmd.Parameters.AddWithValue("@AutoSaveEnabled", settings.FeatureFlags.AutoSaveEnabled ? 1 : 0);
                cmd.Parameters.AddWithValue("@WordLimit", settings.GenerationDefaults.WordLimit);
                cmd.Parameters.AddWithValue("@Temperature", settings.GenerationDefaults.Temperature);
                cmd.Parameters.AddWithValue("@MaxTokens", settings.GenerationDefaults.MaxTokens);
                cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("o"));
                cmd.ExecuteNonQuery();

                // Clear existing providers
                cmd.CommandText = "DELETE FROM ProviderSettings;";
                cmd.ExecuteNonQuery();

                // Insert new providers
                foreach (var provider in settings.Providers)
                {
                    cmd.CommandText = @"INSERT INTO ProviderSettings (Name, Vendor, ApiKey, BaseUrl, DefaultModel)
                                        VALUES (@Name, @Vendor, @ApiKey, @BaseUrl, @DefaultModel);";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@Name", provider.Name);
                    cmd.Parameters.AddWithValue("@Vendor", provider.Vendor);
                    cmd.Parameters.AddWithValue("@ApiKey", provider.ApiKey ?? string.Empty);
                    cmd.Parameters.AddWithValue("@BaseUrl", provider.BaseUrl ?? string.Empty);
                    cmd.Parameters.AddWithValue("@DefaultModel", provider.DefaultModel ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();

                // Backup to JSON
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_jsonPath)!);
                    var json = JsonSerializer.Serialize(settings, JsonOptions);
                    File.WriteAllText(_jsonPath, json);
                }
                catch { }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}