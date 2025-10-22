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
using System.Diagnostics;

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
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true // 美化输出的JSON，便于调试
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

            // 记录请求信息
            Debug.WriteLine($"[API请求] 供应商: {vendor}, 模型: {model}, BaseUrl: {baseUrl}");

            // 统一 Authorization 处理
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                Debug.WriteLine($"[API请求] 已设置Authorization头: Bearer {MaskApiKey(apiKey)}");
            }

            // 添加智谋大模型所需的特殊头部
            if (vendor == "zhipu" || (vendor == "custom" && baseUrl.Contains("bigmodel.cn")))
            {
                // 智谋API需要特殊处理
                Debug.WriteLine("[API请求] 检测到智谋API，添加特殊头部");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            var endpoint = BuildEndpoint(vendor, baseUrl);
            var requestBody = BuildChatCompletionsBody(vendor, model, prompt, temperature, maxTokens);
            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 记录完整请求信息
            Debug.WriteLine($"[API请求] 端点: {endpoint}");
            Debug.WriteLine($"[API请求] 请求体: {json}");

            try
            {
                var response = await client.PostAsync(endpoint, content, ct);
                var resText = await response.Content.ReadAsStringAsync(ct);
                
                // 记录响应信息
                Debug.WriteLine($"[API响应] 状态码: {response.StatusCode}");
                Debug.WriteLine($"[API响应] 响应头: {FormatHeaders(response.Headers)}");
                Debug.WriteLine($"[API响应] 响应体: {resText}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"AI接口错误：{response.StatusCode}\n{resText}");
                }

                return ExtractText(vendor, resText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[API错误] {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        private static string BuildEndpoint(string vendor, string baseUrl)
        {
            // 智谋API特殊处理
            if (vendor == "zhipu" || (vendor == "custom" && baseUrl.Contains("bigmodel.cn")))
            {
                // 智谋API的baseUrl已经是完整路径
                return baseUrl;
            }
            
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
            // 智谋API特殊处理
            if (vendor == "zhipu" || (vendor == "custom" && model.StartsWith("glm-")))
            {
                return new
                {
                    model = model,
                    messages = new object[]
                    {
                        new { role = "user", content = prompt }
                    },
                    temperature = Math.Clamp(temperature, 0, 2),
                    max_tokens = maxTokens > 0 ? maxTokens : 2048,
                    stream = true
                };
            }
            
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[JSON解析错误] {ex.Message}");
            }
            return string.Empty;
        }

        // 辅助方法：掩盖API密钥的大部分字符
        private string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey.Length <= 8)
                return "***";
            
            return apiKey.Substring(0, 4) + "..." + apiKey.Substring(apiKey.Length - 4);
        }

        // 辅助方法：格式化HTTP头部信息
        private string FormatHeaders(HttpResponseHeaders headers)
        {
            var sb = new StringBuilder();
            foreach (var header in headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            return sb.ToString();
        }
    }
}