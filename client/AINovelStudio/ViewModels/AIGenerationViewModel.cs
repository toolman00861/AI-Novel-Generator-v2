using AINovelStudio.Commands;
using AINovelStudio.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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

    public AIGenerationViewModel()
    {
        Novels = new ObservableCollection<Novel>();
        Chapters = new ObservableCollection<Chapter>();
        
        GenerateCommand = new RelayCommand(Generate, CanExecuteGenerate);
        CopyCommand = new RelayCommand(CopyOutput);
        SaveToChapterCommand = new RelayCommand(SaveToChapter);
        RegenerateCommand = new RelayCommand(Regenerate);

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
        OnPropertyChanged(nameof(GenerateButtonText));
        OnPropertyChanged(nameof(CanGenerate));

        try
        {
            // 模拟AI生成过程
            await Task.Delay(2000);

            // 根据生成类型生成不同的内容
            string generatedContent = IsContinueWritingSelected ? GenerateContinuation() :
                                    IsRewriteSelected ? GenerateRewrite() :
                                    IsOutlineSelected ? GenerateOutline() : string.Empty;

            OutputText = generatedContent;
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
    /// 生成续写内容
    /// </summary>
    private string GenerateContinuation()
    {
        return $"基于前文内容，AI续写如下：\n\n" +
               $"故事继续发展，主人公面临新的挑战。在经历了前面的种种困难后，" +
               $"他逐渐成长起来，开始展现出不同寻常的能力。\n\n" +
               $"这时，一个神秘的人物出现了，为故事带来了新的转折...\n\n" +
               $"（这是AI生成的示例内容，实际使用时会调用真实的AI接口）";
    }

    /// <summary>
    /// 生成改写内容
    /// </summary>
    private string GenerateRewrite()
    {
        return $"AI改写版本：\n\n" +
               $"原文经过AI优化后，语言更加流畅，情节更加紧凑。" +
               $"人物形象更加鲜明，对话更加生动自然。\n\n" +
               $"改写后的内容保持了原文的核心思想，但在表达方式上更加精炼有力...\n\n" +
               $"（这是AI生成的示例内容，实际使用时会调用真实的AI接口）";
    }

    /// <summary>
    /// 生成大纲内容
    /// </summary>
    private string GenerateOutline()
    {
        return $"小说大纲：\n\n" +
               $"第一部分：开端\n" +
               $"- 主人公背景介绍\n" +
               $"- 引发事件\n" +
               $"- 初步冲突\n\n" +
               $"第二部分：发展\n" +
               $"- 主要矛盾展开\n" +
               $"- 人物关系复杂化\n" +
               $"- 多重困难叠加\n\n" +
               $"第三部分：高潮\n" +
               $"- 矛盾激化\n" +
               $"- 关键选择\n" +
               $"- 转折点\n\n" +
               $"第四部分：结局\n" +
               $"- 问题解决\n" +
               $"- 人物成长\n" +
               $"- 故事收尾\n\n" +
               $"（这是AI生成的示例大纲，实际使用时会根据具体设定生成）";
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
        if (string.IsNullOrWhiteSpace(OutputText) || SelectedNovel == null)
            return;

        var result = MessageBox.Show("是否将生成的内容保存为新章节？", "确认", 
                                   MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            var newChapter = new Chapter
            {
                Id = (SelectedNovel.Chapters?.Count ?? 0) + 1,
                Title = $"第{(SelectedNovel.Chapters?.Count ?? 0) + 1}章：AI生成章节",
                Content = OutputText,
                Status = ChapterStatus.Draft,
                CreatedAt = DateTime.Now
            };

            SelectedNovel.Chapters ??= new List<Chapter>();
            SelectedNovel.Chapters.Add(newChapter);
            
            LoadChapters();
            MessageBox.Show("内容已保存为新章节", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// 重新生成
    /// </summary>
    private void Regenerate()
    {
        Generate();
    }

    #endregion
}