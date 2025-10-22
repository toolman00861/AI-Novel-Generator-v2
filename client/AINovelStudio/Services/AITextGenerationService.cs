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
using System.IO;
using System.Collections.Generic;

namespace AINovelStudio.Services
{
    /// <summary>
    /// 基础 AI 文本生成服务，基于配置选择不同厂商的 Chat Completions 接口。
    /// </summary>
    public class AITextGenerationService
    {
        private readonly SettingsService _settingsService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILoggerService? _logger = LoggerService.Instance;

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
            client.Timeout = TimeSpan.FromSeconds(settings.GenerationDefaults.TimeoutSeconds > 0 ? settings.GenerationDefaults.TimeoutSeconds : 120);

            // 记录请求信息
            _logger?.Info($"[API请求] 供应商: {vendor}, 模型: {model}, BaseUrl: {baseUrl}", "AI生成");

            // 统一鉴权处理
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                if (vendor == "azure")
                {
                    // Azure 使用 api-key 头，而非 Bearer
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("api-key", apiKey);
                    _logger?.Debug($"[API请求] 已设置api-key头: {MaskApiKey(apiKey)}", "AI生成");
                }
                else
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    _logger?.Debug($"[API请求] 已设置Authorization头: Bearer {MaskApiKey(apiKey)}", "AI生成");
                }
            }

            // 供应商特殊头部
            if (vendor == "zhipu" || (vendor == "custom" && baseUrl.Contains("bigmodel.cn")))
            {
                _logger?.Info("[API请求] 检测到智谋API，添加Accept: application/json", "AI生成");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            var endpoint = BuildEndpoint(vendor, baseUrl);
            var requestBody = BuildChatCompletionsBody(vendor, model, prompt, temperature, maxTokens);
            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 记录完整请求信息
            _logger?.Info($"[API请求] 端点: {endpoint}", "AI生成");
            _logger?.Debug($"[API请求] 请求体: {json}", "AI生成");

            try
            {
                var response = await client.PostAsync(endpoint, content, ct);
                var resText = await response.Content.ReadAsStringAsync(ct);
                
                // 记录响应信息
                _logger?.Info($"[API响应] 状态码: {response.StatusCode}", "AI生成");
                _logger?.Debug($"[API响应] 响应头: {FormatHeaders(response.Headers)}", "AI生成");
                _logger?.Debug($"[API响应] 响应体: {resText}", "AI生成");

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.Error($"AI接口错误：{response.StatusCode}\n{resText}", "AI生成");
                    throw new InvalidOperationException($"AI接口错误：{response.StatusCode}\n{resText}");
                }

                return ExtractText(vendor, resText);
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "调用AI接口异常", "AI生成");
                throw;
            }
        }

        /// <summary>
        /// 流式生成文本，支持实时返回生成的内容
        /// </summary>
        /// <param name="prompt">提示词</param>
        /// <param name="temperature">创意度</param>
        /// <param name="maxTokens">最大令牌数</param>
        /// <param name="onChunkReceived">接收到文本块时的回调</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>完整的生成文本</returns>
        public async Task<string> GenerateStreamAsync(string prompt, double temperature, int maxTokens, 
            Action<string>? onChunkReceived = null, CancellationToken ct = default)
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
            client.Timeout = TimeSpan.FromSeconds(settings.GenerationDefaults.TimeoutSeconds > 0 ? settings.GenerationDefaults.TimeoutSeconds : 120);

            // 记录请求信息
            _logger?.Info($"[流式API请求] 供应商: {vendor}, 模型: {model}, BaseUrl: {baseUrl}", "AI生成");

            // 统一鉴权处理
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                if (vendor == "azure")
                {
                    client.DefaultRequestHeaders.Remove("Authorization");
                    client.DefaultRequestHeaders.Add("api-key", apiKey);
                    _logger?.Debug($"[流式API请求] 已设置api-key头: {MaskApiKey(apiKey)}", "AI生成");
                }
                else
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    _logger?.Debug($"[流式API请求] 已设置Authorization头: Bearer {MaskApiKey(apiKey)}", "AI生成");
                }
            }

            // 供应商特殊头部
            if (vendor == "zhipu" || (vendor == "custom" && baseUrl.Contains("bigmodel.cn")))
            {
                _logger?.Info("[流式API请求] 检测到智谋API，添加Accept: application/json", "AI生成");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }

            var endpoint = BuildEndpoint(vendor, baseUrl);
            var requestBody = BuildChatCompletionsBody(vendor, model, prompt, temperature, maxTokens, stream: true);
            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 记录完整请求信息
            _logger?.Info($"[流式API请求] 端点: {endpoint}", "AI生成");
            _logger?.Debug($"[流式API请求] 请求体: {json}", "AI生成");

            try
            {
                var response = await client.PostAsync(endpoint, content, HttpCompletionOption.ResponseHeadersRead, ct);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync(ct);
                    _logger?.Error($"流式AI接口错误：{response.StatusCode}\n{errorText}", "AI生成");
                    throw new InvalidOperationException($"流式AI接口错误：{response.StatusCode}\n{errorText}");
                }

                var fullText = new StringBuilder();
                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    ct.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                        continue;

                    var data = line.Substring(6); // 移除 "data: " 前缀
                    
                    if (data == "[DONE]")
                        break;

                    try
                    {
                        var chunk = ExtractStreamChunk(vendor, data);
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            fullText.Append(chunk);
                            onChunkReceived?.Invoke(chunk);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.Warning($"解析流式响应块失败: {ex.Message}, 数据: {data}", "AI生成");
                    }
                }

                var result = fullText.ToString();
                _logger?.Info($"[流式API响应] 完成，总长度: {result.Length} 字符", "AI生成");
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "调用流式AI接口异常", "AI生成");
                throw;
            }
        }

        private static string BuildEndpoint(string vendor, string baseUrl)
        {
            var trimmed = (baseUrl ?? string.Empty).TrimEnd('/');

            // 智谋API：若只给域名，自动补齐标准路径
            if (vendor == "zhipu" || (vendor == "custom" && trimmed.Contains("bigmodel.cn")))
            {
                if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.Contains("/api/paas/"))
                {
                    return baseUrl;
                }
                return trimmed + "/api/paas/v4/chat/completions";
            }

            // openai / openrouter
            if (vendor == "openai" || vendor == "openrouter")
            {
                if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                {
                    return baseUrl;
                }
                if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
                {
                    return trimmed + "/chat/completions";
                }
                return trimmed + "/v1/chat/completions";
            }

            // azure/custom：视为完整端点（请在设置中填写完整端点）
            return baseUrl;
        }

        private object BuildChatCompletionsBody(string vendor, string model, string prompt, double temperature, int maxTokens, bool stream = false)
        {
            // 智谱API特殊处理
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
                    stream = stream
                };
            }
            
            // OpenAI / OpenRouter / Azure 兼容 body
            var body = new
            {
                model = model,
                temperature = Math.Clamp(temperature, 0, 2),
                max_tokens = maxTokens > 0 ? (int?)maxTokens : null,
                stream = stream,
                messages = new object[]
                {
                    new { role = "system", content = "You are a helpful writing assistant." },
                    new { role = "user", content = prompt }
                }
            };

            return body;
        }

        private string ExtractText(string vendor, string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // OpenAI/OpenRouter/Azure: choices[0].message.content
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("message", out var message))
                    {
                        if (message.TryGetProperty("content", out var content))
                        {
                            return content.GetString() ?? string.Empty;
                        }
                    }
                    // 一些响应可能直接在choices[0].text
                    if (first.TryGetProperty("text", out var text))
                    {
                        return text.GetString() ?? string.Empty;
                    }
                }

                // 智谋或其他兼容字段
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
                _logger?.Error($"[JSON解析错误] {ex.Message}", "AI生成");
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

        /// <summary>
        /// 从流式响应中提取文本块
        /// </summary>
        private string ExtractStreamChunk(string vendor, string data)
        {
            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;

                // OpenAI/OpenRouter/Azure 格式: choices[0].delta.content
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("delta", out var delta))
                    {
                        if (delta.TryGetProperty("content", out var content))
                        {
                            return content.GetString() ?? string.Empty;
                        }
                    }
                }

                // 智谱API格式: choices[0].delta.content 或其他可能的格式
                if (root.TryGetProperty("content", out var contentProp))
                {
                    return contentProp.GetString() ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger?.Warning($"解析流式数据块失败: {ex.Message}", "AI生成");
                return string.Empty;
            }
        }
    }
}