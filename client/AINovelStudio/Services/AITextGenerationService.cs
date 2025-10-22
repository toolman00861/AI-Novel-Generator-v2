using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AINovelStudio.Models;
using System.Linq;

namespace AINovelStudio.Services
{
    /// <summary>
    /// 基础 AI 文本生成服务，基于配置选择不同厂商的 Chat Completions 接口。
    /// </summary>
    public class AITextGenerationService
    {
        private readonly SettingsService _settingsService;
        private readonly JsonSerializerOptions _jsonOptions;

        public AITextGenerationService(SettingsService? settingsService = null)
        {
            _settingsService = settingsService ?? new SettingsService();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<string> GenerateAsync(string prompt, double temperature, int maxTokens, CancellationToken ct = default)
        {
            var settings = _settingsService.Load();
            if (string.IsNullOrEmpty(settings.SelectedProviderName) || !settings.Providers.Any(p => p.Name == settings.SelectedProviderName))
            {
                throw new InvalidOperationException("No selected provider configured.");
            }
            var selectedProvider = settings.Providers.First(p => p.Name == settings.SelectedProviderName);
            var vendor = (selectedProvider.Vendor ?? "openai").Trim().ToLowerInvariant();
            var apiKey = selectedProvider.ApiKey;
            var baseUrl = selectedProvider.BaseUrl;
            var model = string.IsNullOrWhiteSpace(selectedProvider.DefaultModel) ? "gpt-4o-mini" : selectedProvider.DefaultModel;

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("BaseUrl 未配置");

            if (string.IsNullOrWhiteSpace(apiKey) && vendor != "custom")
                throw new InvalidOperationException("ApiKey 未配置（custom 可不填）");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(30);

            // 统一 Authorization 处理
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            var endpoint = BuildEndpoint(vendor, baseUrl);
            var requestBody = BuildChatCompletionsBody(vendor, model, prompt, temperature, maxTokens);
            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(endpoint, content, ct);
            var resText = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"AI接口错误：{response.StatusCode}\n{resText}");
            }

            return ExtractText(vendor, resText);
        }

        private static string BuildEndpoint(string vendor, string baseUrl)
        {
            // openai / openrouter 统一走 /chat/completions
            if (vendor == "openai" || vendor == "openrouter")
            {
                // 若 BaseUrl 已包含 /chat/completions 则直接使用
                if (baseUrl.TrimEnd('/').EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                {
                    return baseUrl;
                }
                return baseUrl.TrimEnd('/') + "/chat/completions";
            }

            // azure/custom：BaseUrl 视为完整端点
            return baseUrl;
        }

        private static object BuildChatCompletionsBody(string vendor, string model, string prompt, double temperature, int maxTokens)
        {
            // OpenAI / OpenRouter 兼容 body
            var body = new
            {
                model = model,
                temperature = Math.Clamp(temperature, 0, 2),
                max_tokens = maxTokens > 0 ? (int?)maxTokens : null,
                messages = new object[]
                {
                    new { role = "system", content = "You are a helpful writing assistant." },
                    new { role = "user", content = prompt }
                }
            };

            // Azure 可能需要不同字段；这里假设 azure 也兼容（实际部署时请根据 API 版本调整）
            return body;
        }

        private string ExtractText(string vendor, string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // OpenAI/OpenRouter: choices[0].message.content
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var message = choices[0].GetProperty("message");
                    if (message.TryGetProperty("content", out var content))
                    {
                        return content.GetString() ?? string.Empty;
                    }
                }

                // Fallback: try top-level "content" or "text"
                if (root.TryGetProperty("content", out var contentProp))
                {
                    return contentProp.GetString() ?? string.Empty;
                }
                if (root.TryGetProperty("text", out var textProp))
                {
                    return textProp.GetString() ?? string.Empty;
                }
            }
            catch
            {
                // ignore parse errors
            }
            return string.Empty;
        }
    }
}