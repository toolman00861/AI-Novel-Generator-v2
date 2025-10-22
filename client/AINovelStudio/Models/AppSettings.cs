using System;

namespace AINovelStudio.Models
{
    public class AppSettings
    {
        public string ApiBase { get; set; } = string.Empty;
        public List<ProviderSettings> Providers { get; set; } = new List<ProviderSettings>();
        public string SelectedProviderName { get; set; } = string.Empty;
        public FeatureFlags FeatureFlags { get; set; } = new FeatureFlags();
        public GenerationDefaults GenerationDefaults { get; set; } = new GenerationDefaults();
    }

    public class ProviderSettings
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Vendor { get; set; } = "openai"; // openai|azure|openrouter|custom
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string DefaultModel { get; set; } = "gpt-4o-mini";
    }

    public class FeatureFlags
    {
        public bool StreamingEnabled { get; set; } = true;
        public bool AutoSaveEnabled { get; set; } = true;
    }

    public class GenerationDefaults
    {
        public int WordLimit { get; set; } = 500;
        public double Temperature { get; set; } = 0.7;
        public int MaxTokens { get; set; } = 1024;
        public int TimeoutSeconds { get; set; } = 120;
    }
}