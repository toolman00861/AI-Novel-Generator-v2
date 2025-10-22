using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class SimpleApiTest
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 智谱API连接测试 ===");
        
        // 读取配置文件
        var configPath = Path.Combine("bin", "Debug", "net8.0-windows", "appsettings.client.json");
        if (!File.Exists(configPath))
        {
            Console.WriteLine($"配置文件不存在: {configPath}");
            return;
        }
        
        var configJson = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<JsonElement>(configJson);
        
        // 获取智谱配置
        var providers = config.GetProperty("Providers").EnumerateArray();
        JsonElement? zhipuProvider = null;
        
        foreach (var provider in providers)
        {
            if (provider.GetProperty("Name").GetString() == "智谱")
            {
                zhipuProvider = provider;
                break;
            }
        }
        
        if (zhipuProvider == null)
        {
            Console.WriteLine("未找到智谱供应商配置");
            return;
        }
        
        var apiKey = zhipuProvider.Value.GetProperty("ApiKey").GetString();
        var baseUrl = zhipuProvider.Value.GetProperty("BaseUrl").GetString();
        var model = zhipuProvider.Value.GetProperty("DefaultModel").GetString();
        
        Console.WriteLine($"API密钥: {(string.IsNullOrEmpty(apiKey) ? "未设置" : "已设置")}");
        Console.WriteLine($"基础URL: {baseUrl}");
        Console.WriteLine($"模型: {model}");
        
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("API密钥未设置，无法测试");
            return;
        }
        
        // 测试API调用
        Console.WriteLine("\n开始测试API调用...");
        
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        
        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "user", content = "请说'Hello World'" }
            },
            temperature = 0.7,
            max_tokens = 100
        };
        
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        Console.WriteLine($"请求URL: {baseUrl}");
        Console.WriteLine($"请求体: {json}");
        
        try
        {
            var response = await client.PostAsync(baseUrl, content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"响应状态码: {response.StatusCode}");
            Console.WriteLine($"响应内容: {responseText}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✅ API连接测试成功!");
            }
            else
            {
                Console.WriteLine("❌ API连接测试失败!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ API调用异常: {ex.Message}");
            Console.WriteLine($"详细错误: {ex}");
        }
        
        Console.WriteLine("\n测试完成，按任意键退出...");
        Console.ReadKey();
    }
}