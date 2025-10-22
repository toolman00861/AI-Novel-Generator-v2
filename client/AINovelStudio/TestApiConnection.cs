using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AINovelStudio.Services;

namespace AINovelStudio
{
    /// <summary>
    /// API连接测试工具
    /// </summary>
    public class TestApiConnection
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== AI Novel Studio API连接测试 ===");
            
            // 加载设置
            var settingsService = new SettingsService();
            var settings = settingsService.Load();
            
            Console.WriteLine($"当前选中的供应商: {settings.SelectedProviderName}");
            
            var selectedProvider = settings.Providers.FirstOrDefault(p => p.Name == settings.SelectedProviderName);
            if (selectedProvider == null)
            {
                Console.WriteLine("错误: 未找到选中的供应商配置");
                return;
            }
            
            Console.WriteLine($"供应商信息:");
            Console.WriteLine($"  名称: {selectedProvider.Name}");
            Console.WriteLine($"  类型: {selectedProvider.Vendor}");
            Console.WriteLine($"  基础URL: {selectedProvider.BaseUrl}");
            Console.WriteLine($"  API密钥: {(string.IsNullOrEmpty(selectedProvider.ApiKey) ? "未设置" : "已设置")}");
            Console.WriteLine($"  默认模型: {selectedProvider.DefaultModel}");
            
            if (string.IsNullOrEmpty(selectedProvider.ApiKey))
            {
                Console.WriteLine("错误: API密钥未设置，无法进行连接测试");
                return;
            }
            
            // 测试API连接
            Console.WriteLine("\n开始测试API连接...");
            
            try
            {
                var aiService = new AITextGenerationService();
                var testPrompt = "请说'Hello World'";
                
                Console.WriteLine($"测试提示词: {testPrompt}");
                
                var result = await aiService.GenerateAsync(testPrompt, 0.7, 100, System.Threading.CancellationToken.None);
                
                Console.WriteLine($"API响应成功!");
                Console.WriteLine($"生成结果: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API连接失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
            
            Console.WriteLine("\n测试完成，按任意键退出...");
            Console.ReadKey();
        }
    }
}