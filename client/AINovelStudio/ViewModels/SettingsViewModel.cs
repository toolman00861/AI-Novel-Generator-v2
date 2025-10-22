using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AINovelStudio.Commands;
using AINovelStudio.Models;
using AINovelStudio.Services;
using System.Collections.ObjectModel;

namespace AINovelStudio.ViewModels
{
    /// <summary>
    /// 设置页面视图模型
    /// </summary>
    public class SettingsViewModel : BaseViewModel
    {
        private readonly SettingsService _service;
        private AppSettings _settings;

        public ObservableCollection<ProviderSettings> Providers { get; } = new ObservableCollection<ProviderSettings>();

        private ProviderSettings? _selectedProvider;
        public ProviderSettings? SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                _selectedProvider = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Vendor));
                OnPropertyChanged(nameof(ApiKey));
                OnPropertyChanged(nameof(BaseUrl));
                OnPropertyChanged(nameof(DefaultModel));
            }
        }

        public ICommand AddProviderCommand { get; }
        public ICommand RemoveProviderCommand { get; }

        public SettingsViewModel() : this(new SettingsService()) { }

        public SettingsViewModel(SettingsService service)
        {
            _service = service;
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

        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand TestCommand { get; }

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
        }

        private void RemoveProvider()
        {
            if (SelectedProvider != null && Providers.Count > 1)
            {
                var index = Providers.IndexOf(SelectedProvider);
                Providers.Remove(SelectedProvider);
                SelectedProvider = Providers[Math.Max(0, index - 1)];
            }
        }

        private async Task TestAsync()
        {
            try
            {
                if (SelectedProvider == null)
                {
                    MessageBox.Show("请先选择一个提供商", "校验", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 基础校验
                if (string.IsNullOrWhiteSpace(SelectedProvider.Vendor))
                {
                    MessageBox.Show("请先设置 Vendor（openai/azure/openrouter/custom）", "校验", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!Uri.TryCreate(SelectedProvider.BaseUrl, UriKind.Absolute, out _))
                {
                    MessageBox.Show("BaseUrl 不是有效的绝对地址", "校验", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 简单网络探测（可选）
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var requestUrl = string.IsNullOrWhiteSpace(ApiBase) ? SelectedProvider.BaseUrl : ApiBase;

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