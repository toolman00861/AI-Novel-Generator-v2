using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AINovelStudio.Commands;
using AINovelStudio.Models;
using AINovelStudio.Services;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace AINovelStudio.ViewModels
{
    /// <summary>
    /// 设置页面视图模型
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsService _service;
        private readonly ILoggerService? _logger;
        private AppSettings _settings;
        
        public SettingsViewModel(ILoggerService logger)
        {
            _logger = logger;
            _service = new SettingsService();
            InitializeViewModel();
        }

        public ObservableCollection<ProviderSettings> Providers { get; } = new ObservableCollection<ProviderSettings>();

        private ProviderSettings? _selectedProvider;
        public ProviderSettings? SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                _selectedProvider = value;
                // 立即更新设置中的选中供应商名称
                _settings.SelectedProviderName = value?.Name ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Vendor));
                OnPropertyChanged(nameof(ApiKey));
                OnPropertyChanged(nameof(BaseUrl));
                OnPropertyChanged(nameof(DefaultModel));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand ResetCommand { get; private set; }
        public ICommand TestCommand { get; private set; }
        public ICommand AddProviderCommand { get; private set; }
        public ICommand RemoveProviderCommand { get; private set; }

        // 默认构造函数
        public SettingsViewModel() 
        {
            _service = new SettingsService();
            InitializeViewModel();
        }
        
        private void InitializeViewModel()
        {
            _settings = _service.Load();
            
            foreach (var provider in _settings.Providers)
            {
                Providers.Add(provider);
            }
            SelectedProvider = Providers.FirstOrDefault(p => p.Name == _settings.SelectedProviderName) ?? Providers.FirstOrDefault();
            
            SaveCommand = new RelayCommand(Save);
            ResetCommand = new RelayCommand(Reset);
            TestCommand = new RelayCommand(async () => await TestAsync());
            
            AddProviderCommand = new RelayCommand(AddProvider);
            RemoveProviderCommand = new RelayCommand(RemoveProvider, () => SelectedProvider != null && Providers.Count > 1);
        }

        // 常规配置
        public string ApiBase
        {
            get => _settings.ApiBase;
            set { _settings.ApiBase = value; OnPropertyChanged(); }
        }

        // Provider 配置 (proxied to SelectedProvider)
        public string Vendor
        {
            get => SelectedProvider?.Vendor ?? "openai";
            set
            {
                if (SelectedProvider != null)
                {
                    SelectedProvider.Vendor = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ApiKey
        {
            get => SelectedProvider?.ApiKey ?? string.Empty;
            set
            {
                if (SelectedProvider != null)
                {
                    SelectedProvider.ApiKey = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BaseUrl
        {
            get => SelectedProvider?.BaseUrl ?? "https://api.openai.com/v1";
            set
            {
                if (SelectedProvider != null)
                {
                    SelectedProvider.BaseUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DefaultModel
        {
            get => SelectedProvider?.DefaultModel ?? "gpt-4o-mini";
            set
            {
                if (SelectedProvider != null)
                {
                    SelectedProvider.DefaultModel = value;
                    OnPropertyChanged();
                }
            }
        }

        // 功能开关
        public bool StreamingEnabled
        {
            get => _settings.FeatureFlags.StreamingEnabled;
            set { _settings.FeatureFlags.StreamingEnabled = value; OnPropertyChanged(); }
        }

        public bool AutoSaveEnabled
        {
            get => _settings.FeatureFlags.AutoSaveEnabled;
            set { _settings.FeatureFlags.AutoSaveEnabled = value; OnPropertyChanged(); }
        }

        // 生成默认参数
        public int WordLimit
        {
            get => _settings.GenerationDefaults.WordLimit;
            set { _settings.GenerationDefaults.WordLimit = value; OnPropertyChanged(); }
        }

        public double Temperature
        {
            get => _settings.GenerationDefaults.Temperature;
            set { _settings.GenerationDefaults.Temperature = value; OnPropertyChanged(); }
        }

        public int MaxTokens
        {
            get => _settings.GenerationDefaults.MaxTokens;
            set { _settings.GenerationDefaults.MaxTokens = value; OnPropertyChanged(); }
        }

        public int TimeoutSeconds
        {
            get => _settings.GenerationDefaults.TimeoutSeconds;
            set { _settings.GenerationDefaults.TimeoutSeconds = value; OnPropertyChanged(); }
        }

        public bool UseStreaming
        {
            get => _settings.GenerationDefaults.UseStreaming;
            set { _settings.GenerationDefaults.UseStreaming = value; OnPropertyChanged(); }
        }

        private void Save()
        {
            try
            {
                _settings.Providers = Providers.ToList();
                _settings.SelectedProviderName = SelectedProvider?.Name ?? string.Empty;
                _service.Save(_settings);
                MessageBox.Show("设置已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Reset()
        {
            _settings = new AppSettings();
            // 重置 Providers 列表为默认
            Providers.Clear();
            var defaultProvider = new ProviderSettings
            {
                Name = "Default",
                Vendor = "openai",
                BaseUrl = "https://api.openai.com/v1",
                DefaultModel = "gpt-4o-mini"
            };
            Providers.Add(defaultProvider);
            SelectedProvider = defaultProvider;

            OnPropertyChanged(nameof(ApiBase));
            OnPropertyChanged(nameof(Vendor));
            OnPropertyChanged(nameof(ApiKey));
            OnPropertyChanged(nameof(BaseUrl));
            OnPropertyChanged(nameof(DefaultModel));
            OnPropertyChanged(nameof(StreamingEnabled));
            OnPropertyChanged(nameof(AutoSaveEnabled));
            OnPropertyChanged(nameof(WordLimit));
            OnPropertyChanged(nameof(Temperature));
            OnPropertyChanged(nameof(MaxTokens));
            OnPropertyChanged(nameof(TimeoutSeconds));
            OnPropertyChanged(nameof(UseStreaming));
            CommandManager.InvalidateRequerySuggested();
            MessageBox.Show("已重置为默认值（未保存）", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        

        private void AddProvider()
        {
            var newName = $"Provider{Providers.Count + 1}";
            var newProvider = new ProviderSettings
            {
                Name = newName,
                Vendor = "openai",
                BaseUrl = "https://api.openai.com/v1",
                DefaultModel = "gpt-4o-mini"
            };
            Providers.Add(newProvider);
            SelectedProvider = newProvider;
            CommandManager.InvalidateRequerySuggested();
        }

        private void RemoveProvider()
        {
            if (SelectedProvider != null && Providers.Count > 1)
            {
                var index = Providers.IndexOf(SelectedProvider);
                Providers.Remove(SelectedProvider);
                SelectedProvider = Providers[Math.Max(0, index - 1)];
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private async Task TestAsync()
        {
            try
            {
                if (SelectedProvider == null)
                {
                    MessageBox.Show("请先选择一个提供商", "校验", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _logger?.Warning("测试连接失败：未选择提供商", "设置");
                    return;
                }

                // 基础校验
                if (string.IsNullOrWhiteSpace(SelectedProvider.Vendor))
                {
                    MessageBox.Show("请先设置 Vendor（openai/azure/openrouter/zhipu/custom）", "校验", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _logger?.Warning($"测试连接失败：未设置Vendor，提供商ID：{SelectedProvider.Id}", "设置");
                    return;
                }
                if (!Uri.TryCreate(SelectedProvider.BaseUrl, UriKind.Absolute, out _))
                {
                    MessageBox.Show("BaseUrl 不是有效的绝对地址", "校验", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _logger?.Warning($"测试连接失败：BaseUrl不是有效的绝对地址，URL：{SelectedProvider.BaseUrl}", "设置");
                    return;
                }
                if (string.IsNullOrWhiteSpace(SelectedProvider.ApiKey))
                {
                    MessageBox.Show("请先设置 ApiKey", "校验", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _logger?.Warning($"测试连接失败：未设置ApiKey，提供商：{SelectedProvider.Name}", "设置");
                    return;
                }

                // 简单网络探测（可选）
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var requestUrl = string.IsNullOrWhiteSpace(ApiBase) ? SelectedProvider.BaseUrl : ApiBase;

                // 添加详细日志
                _logger?.Info($"开始测试连接 - 供应商: {SelectedProvider.Vendor}, URL: {requestUrl}", "设置");
                
                // 智谋API特殊处理
                bool isZhipu = SelectedProvider.Vendor == "zhipu" || 
                               (SelectedProvider.Vendor == "custom" && 
                                (requestUrl.Contains("bigmodel.cn") || 
                                 (SelectedProvider.DefaultModel?.StartsWith("glm-") == true)));
                
                if (isZhipu)
                {
                    _logger?.Info("检测到智谋API，使用POST方法测试", "设置");
                    
                    // 智谋API需要使用POST方法和特定的请求体
                    var testRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                    testRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    
                    if (!string.IsNullOrWhiteSpace(SelectedProvider.ApiKey))
                    {
                        testRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SelectedProvider.ApiKey);
                        _logger?.Debug("已添加授权头", "设置");
                    }
                    
                    // 构建一个最小化的请求体
                    var testBody = new
                    {
                        model = SelectedProvider.DefaultModel ?? "glm-4.6",
                        messages = new[] { new { role = "user", content = "hello" } },
                        stream = false,
                        max_tokens = 10
                    };
                    
                    var json = JsonSerializer.Serialize(testBody);
                    testRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    // 记录请求详情
                    _logger?.Debug($"请求体: {json}", "设置");
                    
                    var response = await client.SendAsync(testRequest);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    _logger?.Info($"智谋API测试结果 - 状态码: {(int)response.StatusCode}, 是否成功: {response.IsSuccessStatusCode}", "设置");
                    _logger?.Debug($"响应内容: {responseContent}", "设置");
                    
                    MessageBox.Show($"智谋API测试结果：{(int)response.StatusCode}\n\n响应内容：{(responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent)}", 
                                   "测试", MessageBoxButton.OK, 
                                   response.IsSuccessStatusCode ? MessageBoxImage.Information : MessageBoxImage.Warning);
                }
                else
                {
                    // 常规API测试
                    var head = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                    var response = await client.SendAsync(head);
                    _logger?.Info($"API测试结果 - 状态码: {(int)response.StatusCode}, 是否成功: {response.IsSuccessStatusCode}", "设置");
                    MessageBox.Show($"网络连通：{(int)response.StatusCode}", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"测试连接异常: {ex.Message}", "设置");
                _logger?.Debug($"异常详情: {ex}", "设置");
                MessageBox.Show($"测试失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}