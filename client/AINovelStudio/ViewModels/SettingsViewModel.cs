using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AINovelStudio.Commands;
using AINovelStudio.Models;
using AINovelStudio.Services;

namespace AINovelStudio.ViewModels
{
    /// <summary>
    /// 设置页面视图模型
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsService _service;
        private AppSettings _settings;

        public SettingsViewModel() : this(new SettingsService()) { }

        public SettingsViewModel(SettingsService service)
        {
            _service = service;
            _settings = _service.Load();

            SaveCommand = new RelayCommand(Save);
            ResetCommand = new RelayCommand(Reset);
            TestCommand = new RelayCommand(async () => await TestAsync());
        }

        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand TestCommand { get; }

        // 常规配置
        public string ApiBase
        {
            get => _settings.ApiBase;
            set { _settings.ApiBase = value; OnPropertyChanged(); }
        }

        // Provider 配置
        public string Vendor
        {
            get => _settings.Provider.Vendor;
            set { _settings.Provider.Vendor = value; OnPropertyChanged(); }
        }

        public string ApiKey
        {
            get => _settings.Provider.ApiKey;
            set { _settings.Provider.ApiKey = value; OnPropertyChanged(); }
        }

        public string BaseUrl
        {
            get => _settings.Provider.BaseUrl;
            set { _settings.Provider.BaseUrl = value; OnPropertyChanged(); }
        }

        public string DefaultModel
        {
            get => _settings.Provider.DefaultModel;
            set { _settings.Provider.DefaultModel = value; OnPropertyChanged(); }
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

        private void Save()
        {
            try
            {
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
            MessageBox.Show("已重置为默认值（未保存）", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task TestAsync()
        {
            try
            {
                // 基础校验
                if (string.IsNullOrWhiteSpace(Vendor))
                {
                    MessageBox.Show("请先设置 Vendor（openai/azure/openrouter/custom）", "校验", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
                {
                    MessageBox.Show("BaseUrl 不是有效的绝对地址", "校验", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 简单网络探测（可选）
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var requestUrl = string.IsNullOrWhiteSpace(ApiBase) ? BaseUrl : ApiBase;

                var head = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                var response = await client.SendAsync(head);
                MessageBox.Show($"网络连通：{(int)response.StatusCode}", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}