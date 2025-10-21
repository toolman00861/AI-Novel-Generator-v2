using System;
using System.IO;
using System.Text.Json;
using AINovelStudio.Models;

namespace AINovelStudio.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public SettingsService(string? customPath = null)
        {
            _settingsPath = customPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.client.json");
        }

        public AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    return settings;
                }
            }
            catch
            {
                // ignore and return defaults
            }
            return new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
    }
}