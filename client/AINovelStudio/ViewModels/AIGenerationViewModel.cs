using AINovelStudio.Commands;
using AINovelStudio.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;

using AINovelStudio.Services;
using System.Threading.Tasks;
using System.Threading;
using System.Text; // 新增：用于流式缓冲
using System.Windows.Threading; // 新增：用于UI定时刷新
using System.IO;
using Microsoft.Win32;


namespace AINovelStudio.ViewModels;

/// <summary>
/// AI生成页面视图模型
/// </summary>
public class AIGenerationViewModel : BaseViewModel
{
    private bool _isContinueWritingSelected = true;
    private bool _isRewriteSelected;
    private bool _isOutlineSelected;
    private Novel? _selectedNovel;
    private Chapter? _selectedChapter;
    private string _inputText = string.Empty;
    private string _outputText = string.Empty;
    private int _wordLimit = 500;
    private double _creativity = 0.7;
    private string _generationStatus = "就绪";
    private bool _isGenerating;
    private ProviderSettings? _selectedProvider;

    private readonly AITextGenerationService _aiService;
    private readonly SettingsService _settingsService;

    // 新增：流式UI缓冲与刷新
    private readonly StringBuilder _streamBuffer = new StringBuilder();
    private readonly object _streamLock = new object();
    private DispatcherTimer? _flushTimer;
    private int _receivedCharCount;
    private readonly NovelStorageService _storageService = new NovelStorageService();

    public AIGenerationViewModel()
    {
        Novels = new ObservableCollection<Novel>();
        Chapters = new ObservableCollection<Chapter>();
        Providers = new ObservableCollection<ProviderSettings>();
        
        GenerateCommand = new RelayCommand(Generate, CanExecuteGenerate);
        CopyCommand = new RelayCommand(CopyOutput);
        SaveToChapterCommand = new RelayCommand(SaveToChapter);
        RegenerateCommand = new RelayCommand(Regenerate);

        _settingsService = new SettingsService();
        _aiService = new AITextGenerationService(_settingsService);

        var defaults = _settingsService.Load().GenerationDefaults;
        _wordLimit = defaults.WordLimit;
        _creativity = defaults.Temperature;

        LoadProviders();
        LoadSampleData();
    }

    #region 属性

    /// <summary>
    /// 小说列表
    /// </summary>
    public ObservableCollection<Novel> Novels { get; }

    /// <summary>
    /// 章节列表
    /// </summary>
    public ObservableCollection<Chapter> Chapters { get; }

    /// <summary>
    /// 供应商列表
    /// </summary>
    public ObservableCollection<ProviderSettings> Providers { get; }

    /// <summary>
    /// 选中的供应商
    /// </summary>
    public ProviderSettings? SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (SetProperty(ref _selectedProvider, value))
            {
                // 更新设置中的选中供应商
                if (value != null)
                {
                    var settings = _settingsService.Load();
                    settings.SelectedProviderName = value.Name;
                    _settingsService.Save(settings);
                }
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    /// <summary>
    /// 是否选择续写
    /// </summary>
    public bool IsContinueWritingSelected
    {
        get => _isContinueWritingSelected;
        set
        {
            if (SetProperty(ref _isContinueWritingSelected, value) && value)
            {
                IsRewriteSelected = false;
                IsOutlineSelected = false;
                UpdateInputTitle();
            }
        }
    }

    /// <summary>
    /// 是否选择改写
    /// </summary>
    public bool IsRewriteSelected
    {
        get => _isRewriteSelected;
        set
        {
            if (SetProperty(ref _isRewriteSelected, value) && value)
            {
                IsContinueWritingSelected = false;
                IsOutlineSelected = false;
                UpdateInputTitle();
            }
        }
    }

    /// <summary>
    /// 是否选择大纲生成
    /// </summary>
    public bool IsOutlineSelected
    {
        get => _isOutlineSelected;
        set
        {
            if (SetProperty(ref _isOutlineSelected, value) && value)
            {
                IsContinueWritingSelected = false;
                IsRewriteSelected = false;
                UpdateInputTitle();
            }
        }
    }

    /// <summary>
    /// 选中的小说
    /// </summary>
    public Novel? SelectedNovel
    {
        get => _selectedNovel;
        set
        {
            if (SetProperty(ref _selectedNovel, value))
            {
                LoadChapters();
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    /// <summary>
    /// 选中的章节
    /// </summary>
    public Chapter? SelectedChapter
    {
        get => _selectedChapter;
        set
        {
            if (SetProperty(ref _selectedChapter, value))
            {
                LoadChapterContent();
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    /// <summary>
    /// 是否显示章节选择
    /// </summary>
    public bool IsChapterSelectionVisible => IsContinueWritingSelected || IsRewriteSelected;

    /// <summary>
    /// 输入标题
    /// </summary>
    public string InputTitle { get; private set; } = "请输入内容";

    /// <summary>
    /// 输入文本
    /// </summary>
    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
            {
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    /// <summary>
    /// 输出文本
    /// </summary>
    public string OutputText
    {
        get => _outputText;
        set
        {
            if (SetProperty(ref _outputText, value))
            {
                OnPropertyChanged(nameof(HasOutput));
                OnPropertyChanged(nameof(CanRegenerate));
            }
        }
    }

    /// <summary>
    /// 字数限制
    /// </summary>
    public int WordLimit
    {
        get => _wordLimit;
        set => SetProperty(ref _wordLimit, value);
    }

    /// <summary>
    /// 创意度
    /// </summary>
    public double Creativity
    {
        get => _creativity;
        set => SetProperty(ref _creativity, value);
    }

    /// <summary>
    /// 生成状态
    /// </summary>
    public string GenerationStatus
    {
        get => _generationStatus;
        set
        {
            if (SetProperty(ref _generationStatus, value))
            {
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    /// <summary>
    /// 状态颜色
    /// </summary>
    public Brush StatusColor => GenerationStatus switch
    {
        "生成中..." => Brushes.Orange,
        "生成完成" => Brushes.Green,
        "生成失败" => Brushes.Red,
        _ => Brushes.Gray
    };

    /// <summary>
    /// 生成按钮文本
    /// </summary>
    public string GenerateButtonText => _isGenerating ? "生成中..." : "开始生成";

    /// <summary>
    /// 是否可以生成
    /// </summary>
    public bool CanGenerate => !_isGenerating && SelectedNovel != null && 
                              (!IsChapterSelectionVisible || SelectedChapter != null) &&
                              !string.IsNullOrWhiteSpace(InputText);

    /// <summary>
    /// 是否有输出
    /// </summary>
    public bool HasOutput => !string.IsNullOrWhiteSpace(OutputText);

    /// <summary>
    /// 是否可以重新生成
    /// </summary>
    public bool CanRegenerate => HasOutput && CanGenerate;

    #endregion

    #region 命令

    /// <summary>
    /// 生成命令
    /// </summary>
    public ICommand GenerateCommand { get; }

    /// <summary>
    /// 复制命令
    /// </summary>
    public ICommand CopyCommand { get; }

    /// <summary>
    /// 保存到章节命令
    /// </summary>
    public ICommand SaveToChapterCommand { get; }

    /// <summary>
    /// 重新生成命令
    /// </summary>
    public ICommand RegenerateCommand { get; }

    #endregion

    #region 私有方法

    /// <summary>
    /// 更新输入标题
    /// </summary>
    private void UpdateInputTitle()
    {
        InputTitle = IsContinueWritingSelected ? "请输入前文内容" :
                    IsRewriteSelected ? "请输入要改写的内容" :
                    IsOutlineSelected ? "请输入小说设定和要求" : "请输入内容";
        
        OnPropertyChanged(nameof(InputTitle));
        OnPropertyChanged(nameof(IsChapterSelectionVisible));
    }

    /// <summary>
    /// 加载章节
    /// </summary>
    private void LoadChapters()
    {
        Chapters.Clear();
        if (SelectedNovel?.Chapters != null)
        {
            foreach (var chapter in SelectedNovel.Chapters)
            {
                Chapters.Add(chapter);
            }
        }
        SelectedChapter = Chapters.FirstOrDefault();
    }

    /// <summary>
    /// 加载章节内容
    /// </summary>
    private void LoadChapterContent()
    {
        if (SelectedChapter != null && (IsContinueWritingSelected || IsRewriteSelected))
        {
            InputText = SelectedChapter.Content ?? string.Empty;
        }
    }

    /// <summary>
    /// 加载供应商列表
    /// </summary>
    private void LoadProviders()
    {
        var settings = _settingsService.Load();
        
        Providers.Clear();
        foreach (var provider in settings.Providers)
        {
            Providers.Add(provider);
        }
        
        // 设置当前选中的供应商
        SelectedProvider = Providers.FirstOrDefault(p => p.Name == settings.SelectedProviderName) 
                          ?? Providers.FirstOrDefault();
    }

    /// <summary>
    /// 加载示例数据
    /// </summary>
    private void LoadSampleData()
    {
        var novel1 = new Novel
        {
            Id = 1,
            Title = "修仙传奇",
            Description = "一个关于修仙的故事",
            Status = NovelStatus.InProgress,
            CreatedAt = DateTime.Now.AddDays(-10),
            Chapters = new List<Chapter>
            {
                new() { Id = 1, Title = "第一章：初入修仙界", Content = "在一个普通的小村庄里，住着一个名叫李明的少年...", Status = ChapterStatus.Completed },
                new() { Id = 2, Title = "第二章：奇遇", Content = "李明在山中遇到了一位神秘的老者...", Status = ChapterStatus.Draft }
            }
        };

        var novel2 = new Novel
        {
            Id = 2,
            Title = "都市重生",
            Description = "重生回到过去改变命运",
            Status = NovelStatus.InProgress,
            CreatedAt = DateTime.Now.AddDays(-5),
            Chapters = new List<Chapter>
            {
                new() { Id = 3, Title = "第一章：重生", Content = "睁开眼睛，我发现自己回到了十年前...", Status = ChapterStatus.Completed }
            }
        };

        Novels.Add(novel1);
        Novels.Add(novel2);
        
        SelectedNovel = Novels.FirstOrDefault();
    }

    /// <summary>
    /// 是否可以执行生成
    /// </summary>
    private bool CanExecuteGenerate()
    {
        return CanGenerate;
    }

    /// <summary>
    /// 生成内容
    /// </summary>
    private async void Generate()
    {
        _isGenerating = true;
        GenerationStatus = "生成中...";
        OutputText = ""; // 清空输出文本，准备接收流式响应
        _receivedCharCount = 0;
        lock (_streamLock) { _streamBuffer.Clear(); }
        OnPropertyChanged(nameof(GenerateButtonText));
        OnPropertyChanged(nameof(CanGenerate));

        try
        {
            // 构建提示词并调用真实 AI 服务
            var prompt = BuildPrompt();
            var settings = _settingsService.Load();
            var maxTokens = settings.GenerationDefaults.MaxTokens;

            // 根据设置选择使用流式生成或普通生成
            if (settings.GenerationDefaults.UseStreaming)
            {
                // 启动UI刷新定时器，节流更新
                _flushTimer?.Stop();
                _flushTimer = new DispatcherTimer
                {
                    Interval = System.TimeSpan.FromMilliseconds(60)
                };
                _flushTimer.Tick += (_, __) =>
                {
                    string pending;
                    lock (_streamLock)
                    {
                        if (_streamBuffer.Length == 0) return;
                        pending = _streamBuffer.ToString();
                        _streamBuffer.Clear();
                    }
                    // 在UI线程上批量追加并更新状态
                    var cleaned = CleanGeneratedText(pending);
                    OutputText += cleaned;
                    GenerationStatus = $"生成中...（{_receivedCharCount}字）";
                };
                _flushTimer.Start();

                // 使用流式生成（后台线程的回调仅写缓冲）
                await _aiService.GenerateStreamAsync(prompt, _creativity, maxTokens, 
                    chunk =>
                    {
                        // 仅在缓冲中累积，避免频繁切换到UI线程
                        if (string.IsNullOrEmpty(chunk)) return;
                        lock (_streamLock)
                        {
                            _streamBuffer.Append(chunk);
                            _receivedCharCount += chunk.Length;
                        }
                    }, 
                    CancellationToken.None).ConfigureAwait(false);

                // 结束后停止定时器并做最后一次刷新
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _flushTimer?.Stop();
                    _flushTimer = null;
                    string remaining;
                    lock (_streamLock)
                    {
                        remaining = _streamBuffer.ToString();
                        _streamBuffer.Clear();
                    }
                    if (!string.IsNullOrEmpty(remaining))
                    {
                        OutputText += CleanGeneratedText(remaining);
                    }
                });
            }
            else
            {
                // 使用普通生成
                var result = await _aiService.GenerateAsync(prompt, _creativity, maxTokens, CancellationToken.None).ConfigureAwait(false);
                OutputText = CleanGeneratedText(result);
            }

            if (string.IsNullOrWhiteSpace(OutputText))
            {
                OutputText = "（AI接口返回空结果，请检查模型与参数设置）";
            }
            GenerationStatus = "生成完成";
        }
        catch (Exception ex)
        {
            GenerationStatus = "生成失败";
            MessageBox.Show($"生成失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isGenerating = false;
            OnPropertyChanged(nameof(GenerateButtonText));
            OnPropertyChanged(nameof(CanGenerate));
        }
    }

    /// <summary>
    /// 构建不同模式的提示词
    /// </summary>
    private string BuildPrompt()
    {
        var header = IsContinueWritingSelected
            ? $"你是一位专业的小说作家。请直接续写下面的小说内容，保持文风一致，语言生动流畅，约{WordLimit}字。不要分析或解释，直接输出小说正文："
            : IsRewriteSelected
                ? $"你是一位专业的小说作家。请直接改写下面的小说内容，使语言更加生动流畅，保持原意，约{WordLimit}字。不要分析或解释，直接输出改写后的小说正文："
                : IsOutlineSelected
                    ? $"你是一位专业的小说作家。请根据以下设定直接生成详细的小说大纲，包含具体的情节发展与关键场景，约{WordLimit}字。不要分析或解释，直接输出大纲内容："
                    : "你是一位专业的小说作家。请直接生成小说内容，不要分析或解释：";

        var context = InputText ?? string.Empty;

        // 可附加章节与小说标题等上下文（如有选择章节）
        var chapterInfo = SelectedChapter != null ? $"\n\n章节：{SelectedChapter.Title}" : string.Empty;
        var novelInfo = SelectedNovel != null ? $"\n小说：{SelectedNovel.Title}" : string.Empty;

        return $"{header}\n\n{context}{novelInfo}{chapterInfo}";
    }

    // 新增：清理AI生成的文本（移除Markdown标记与多余空白）
    private string CleanGeneratedText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // 移除Markdown格式的粗体标记 **
        text = text.Replace("**", "");
        
        // 移除多余的空行（保留单个换行）
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n\s*\n", "\n\n");
        
        // 统一换行符为 \n 并按行处理
        var lines = text.Replace("\r\n", "\n").Split(new[] { "\n" }, System.StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Trim();
        }
        text = string.Join("\n", lines);
        
        // 移除开头和结尾的空白字符
        text = text.Trim();
        
        return text;
    }

    /// <summary>
    /// 复制输出内容
    /// </summary>
    private void CopyOutput()
    {
        if (!string.IsNullOrWhiteSpace(OutputText))
        {
            Clipboard.SetText(OutputText);
            MessageBox.Show("内容已复制到剪贴板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// 保存到章节
    /// </summary>
    private void SaveToChapter()
    {
        if (SelectedNovel == null || SelectedChapter == null || string.IsNullOrWhiteSpace(OutputText))
        {
            MessageBox.Show("请选择小说与章节，并且确保有生成内容。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
    
        SelectedChapter.Content = OutputText;
        var chapter = SelectedChapter ?? new Chapter { Title = "AI生成片段" };
        chapter.Content = OutputText;
        if (SelectedChapter == null)
        {
            SelectedChapter = chapter;
            SelectedNovel?.Chapters.Add(chapter);
        }
    
        try
        {
            _storageService.SaveGeneratedContent((SelectedNovel?.Title) ?? "未命名小说", (SelectedChapter?.Title) ?? "未命名章节", OutputText);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存到本地数据库失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    
        MessageBox.Show("已保存到章节并写入本地存储。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 重新生成（清空输出并再次生成）
    /// </summary>
    private void Regenerate()
    {
        if (!CanGenerate) return;
        OutputText = string.Empty;
        Generate();
    }

    #endregion
}
