using AINovelStudio.Commands;
using AINovelStudio.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using AINovelStudio.Services;
using System.Threading;
using System.Threading.Tasks;

namespace AINovelStudio.ViewModels;

/// <summary>
/// 人物设计页面视图模型
/// </summary>
public class CharacterDesignViewModel : BaseViewModel
{
    private Novel? _selectedNovel;
    private Character? _selectedCharacter;
    private string _characterTagsText = string.Empty;
    private bool _isGeneratePersonality = true;
    private bool _isGenerateBackground;
    private bool _isGenerateDialogue;
    private string _aiPrompt = string.Empty;
    private string _aiGeneratedContent = string.Empty;
    private bool _isGenerating;

    private readonly AITextGenerationService _aiService;
    private readonly SettingsService _settingsService;

    public CharacterDesignViewModel()
    {
        Novels = new ObservableCollection<Novel>();
        Characters = new ObservableCollection<Character>();

        _settingsService = new SettingsService();
        _aiService = new AITextGenerationService(_settingsService);

        CreateCharacterCommand = new RelayCommand(CreateCharacter);
        RefreshCommand = new RelayCommand(Refresh);
        SelectCharacterCommand = new RelayCommand<Character>(SelectCharacter);
        SaveCharacterCommand = new RelayCommand(SaveCharacter);
        DeleteCharacterCommand = new RelayCommand(DeleteCharacter);
        OptimizeCharacterCommand = new RelayCommand(OptimizeCharacter);
        GenerateWithAICommand = new RelayCommand(GenerateWithAI);
        ApplyAIContentCommand = new RelayCommand(ApplyAIContent);
        CopyAIContentCommand = new RelayCommand(CopyAIContent);

        LoadSampleData();
    }

    #region 属性

    /// <summary>
    /// 小说列表
    /// </summary>
    public ObservableCollection<Novel> Novels { get; }

    /// <summary>
    /// 角色列表
    /// </summary>
    public ObservableCollection<Character> Characters { get; }

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
                LoadCharacters();
            }
        }
    }

    /// <summary>
    /// 选中的角色
    /// </summary>
    public Character? SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (SetProperty(ref _selectedCharacter, value))
            {
                UpdateCharacterTagsText();
                OnPropertyChanged(nameof(IsCharacterSelected));
            }
        }
    }

    /// <summary>
    /// 是否选中了角色
    /// </summary>
    public bool IsCharacterSelected => SelectedCharacter != null;

    /// <summary>
    /// 角色标签文本
    /// </summary>
    public string CharacterTagsText
    {
        get => _characterTagsText;
        set
        {
            if (SetProperty(ref _characterTagsText, value))
            {
                UpdateCharacterTags();
            }
        }
    }

    /// <summary>
    /// 是否生成性格
    /// </summary>
    public bool IsGeneratePersonality
    {
        get => _isGeneratePersonality;
        set
        {
            if (SetProperty(ref _isGeneratePersonality, value) && value)
            {
                IsGenerateBackground = false;
                IsGenerateDialogue = false;
            }
        }
    }

    /// <summary>
    /// 是否生成背景
    /// </summary>
    public bool IsGenerateBackground
    {
        get => _isGenerateBackground;
        set
        {
            if (SetProperty(ref _isGenerateBackground, value) && value)
            {
                IsGeneratePersonality = false;
                IsGenerateDialogue = false;
            }
        }
    }

    /// <summary>
    /// 是否生成对话
    /// </summary>
    public bool IsGenerateDialogue
    {
        get => _isGenerateDialogue;
        set
        {
            if (SetProperty(ref _isGenerateDialogue, value) && value)
            {
                IsGeneratePersonality = false;
                IsGenerateBackground = false;
            }
        }
    }

    /// <summary>
    /// AI提示
    /// </summary>
    public string AIPrompt
    {
        get => _aiPrompt;
        set
        {
            if (SetProperty(ref _aiPrompt, value))
            {
                OnPropertyChanged(nameof(CanGenerateWithAI));
            }
        }
    }

    /// <summary>
    /// AI生成的内容
    /// </summary>
    public string AIGeneratedContent
    {
        get => _aiGeneratedContent;
        set
        {
            if (SetProperty(ref _aiGeneratedContent, value))
            {
                OnPropertyChanged(nameof(HasAIContent));
            }
        }
    }

    /// <summary>
    /// 是否有AI生成的内容
    /// </summary>
    public bool HasAIContent => !string.IsNullOrWhiteSpace(AIGeneratedContent);

    /// <summary>
    /// 是否可以使用AI生成
    /// </summary>
    public bool CanGenerateWithAI => !_isGenerating && !string.IsNullOrWhiteSpace(AIPrompt);

    #endregion

    #region 命令

    /// <summary>
    /// 创建角色命令
    /// </summary>
    public ICommand CreateCharacterCommand { get; }

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 选择角色命令
    /// </summary>
    public ICommand SelectCharacterCommand { get; }

    /// <summary>
    /// 保存角色命令
    /// </summary>
    public ICommand SaveCharacterCommand { get; }

    /// <summary>
    /// 删除角色命令
    /// </summary>
    public ICommand DeleteCharacterCommand { get; }

    /// <summary>
    /// 优化角色命令
    /// </summary>
    public ICommand OptimizeCharacterCommand { get; }

    /// <summary>
    /// AI生成命令
    /// </summary>
    public ICommand GenerateWithAICommand { get; }

    /// <summary>
    /// 应用AI内容命令
    /// </summary>
    public ICommand ApplyAIContentCommand { get; }

    /// <summary>
    /// 复制AI内容命令
    /// </summary>
    public ICommand CopyAIContentCommand { get; }

    #endregion

    #region 私有方法

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
            Characters = new List<Character>
            {
                new()
                {
                    Id = 1,
                    NovelId = 1,
                    Name = "李明",
                    Description = "主角，普通少年",
                    Personality = "勇敢、善良、有正义感",
                    Background = "出生在小村庄，父母早逝",
                    SpeakingStyle = "朴实直接，不善言辞",
                    Tags = new List<string> { "主角", "修仙者", "少年" }
                },
                new()
                {
                    Id = 2,
                    NovelId = 1,
                    Name = "玄机老人",
                    Description = "神秘的修仙高手",
                    Personality = "睿智、神秘、慈祥",
                    Background = "隐居山林的修仙大能",
                    SpeakingStyle = "言简意赅，富含哲理",
                    Tags = new List<string> { "师父", "高手", "老者" }
                }
            }
        };

        var novel2 = new Novel
        {
            Id = 2,
            Title = "都市重生",
            Description = "重生回到过去改变命运",
            Status = NovelStatus.InProgress,
            CreatedAt = DateTime.Now.AddDays(-5),
            Characters = new List<Character>
            {
                new()
                {
                    Id = 3,
                    NovelId = 2,
                    Name = "张伟",
                    Description = "重生的主角",
                    Personality = "聪明、果断、有野心",
                    Background = "前世失败的商人，重生后决心改变命运",
                    SpeakingStyle = "自信从容，言辞犀利",
                    Tags = new List<string> { "主角", "重生者", "商人" }
                }
            }
        };

        Novels.Add(novel1);
        Novels.Add(novel2);

        SelectedNovel = Novels.FirstOrDefault();
    }

    /// <summary>
    /// 加载角色
    /// </summary>
    private void LoadCharacters()
    {
        Characters.Clear();
        if (SelectedNovel?.Characters != null)
        {
            foreach (var character in SelectedNovel.Characters)
            {
                Characters.Add(character);
            }
        }
        SelectedCharacter = null;
    }

    /// <summary>
    /// 更新角色标签文本
    /// </summary>
    private void UpdateCharacterTagsText()
    {
        if (SelectedCharacter?.Tags != null)
        {
            _characterTagsText = string.Join(", ", SelectedCharacter.Tags);
            OnPropertyChanged(nameof(CharacterTagsText));
        }
        else
        {
            _characterTagsText = string.Empty;
            OnPropertyChanged(nameof(CharacterTagsText));
        }
    }

    /// <summary>
    /// 更新角色标签
    /// </summary>
    private void UpdateCharacterTags()
    {
        if (SelectedCharacter != null)
        {
            SelectedCharacter.Tags = CharacterTagsText
                .Split(',')
                .Select(tag => tag.Trim())
                .Where(tag => !string.IsNullOrEmpty(tag))
                .ToList();
        }
    }

    /// <summary>
    /// 创建角色
    /// </summary>
    private void CreateCharacter()
    {
        if (SelectedNovel == null)
        {
            MessageBox.Show("请先选择一个小说", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var newCharacter = new Character
        {
            Id = (SelectedNovel.Characters?.Count ?? 0) + 1,
            NovelId = SelectedNovel.Id,
            Name = "新角色",
            Description = "请输入角色描述",
            Personality = "请输入性格特点",
            Background = "请输入背景故事",
            SpeakingStyle = "请输入说话风格",
            Tags = new List<string>()
        };

        SelectedNovel.Characters ??= new List<Character>();
        SelectedNovel.Characters.Add(newCharacter);

        LoadCharacters();
        SelectedCharacter = newCharacter;
    }

    /// <summary>
    /// 刷新
    /// </summary>
    private void Refresh()
    {
        LoadCharacters();
    }

    /// <summary>
    /// 选择角色
    /// </summary>
    private void SelectCharacter(Character? character)
    {
        SelectedCharacter = character;
    }

    /// <summary>
    /// 保存角色
    /// </summary>
    private void SaveCharacter()
    {
        if (SelectedCharacter == null)
            return;

        MessageBox.Show("角色信息已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 删除角色
    /// </summary>
    private void DeleteCharacter()
    {
        if (SelectedCharacter == null || SelectedNovel == null)
            return;

        var result = MessageBox.Show($"确定要删除角色 '{SelectedCharacter.Name}' 吗？", "确认删除",
                                   MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            SelectedNovel.Characters?.Remove(SelectedCharacter);
            LoadCharacters();
            MessageBox.Show("角色已删除", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// 优化角色
    /// </summary>
    private void OptimizeCharacter()
    {
        if (SelectedCharacter == null)
            return;

        AIPrompt = $"请优化以下角色设定：\n角色名：{SelectedCharacter.Name}\n描述：{SelectedCharacter.Description}\n性格：{SelectedCharacter.Personality}";
        IsGeneratePersonality = true;
        GenerateWithAI();
    }

    /// <summary>
    /// AI生成
    /// </summary>
    private async void GenerateWithAI()
    {
        _isGenerating = true;
        AIGeneratedContent = ""; // 清空输出文本，准备接收流式响应
        OnPropertyChanged(nameof(CanGenerateWithAI));

        try
        {
            var settings = _settingsService.Load();
            var maxTokens = settings.GenerationDefaults.MaxTokens;
            var temperature = settings.GenerationDefaults.Temperature;

            // 根据设置选择使用流式生成或普通生成
            if (settings.GenerationDefaults.UseStreaming)
            {
                // 使用流式生成
                await _aiService.GenerateStreamAsync(AIPrompt, temperature, maxTokens, 
                    chunk =>
                    {
                        // 在UI线程上更新输出文本，并清理格式
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var cleanedChunk = CleanGeneratedText(chunk);
                            AIGeneratedContent += cleanedChunk;
                        });
                    }, 
                    CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                // 使用普通生成
                var result = await _aiService.GenerateAsync(AIPrompt, temperature, maxTokens, CancellationToken.None).ConfigureAwait(false);
                AIGeneratedContent = CleanGeneratedText(result);
            }

            if (string.IsNullOrWhiteSpace(AIGeneratedContent))
            {
                AIGeneratedContent = "（AI接口返回空结果，请检查模型与参数设置）";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"AI生成失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isGenerating = false;
            OnPropertyChanged(nameof(CanGenerateWithAI));
        }
    }

    /// <summary>
    /// 生成性格
    /// </summary>
    private string GeneratePersonality()
    {
        return $"基于提示：{AIPrompt}\n\n" +
               $"AI生成的性格特点：\n\n" +
               $"• 核心性格：坚韧不拔，面对困难从不轻易放弃\n" +
               $"• 情感特质：内心善良，对朋友忠诚，但有时过于固执\n" +
               $"• 行为模式：喜欢独立思考，不盲从他人意见\n" +
               $"• 社交风格：话不多但言之有物，更愿意用行动证明自己\n" +
               $"• 价值观念：重视正义和公平，愿意为弱者发声\n\n" +
               $"（这是AI生成的示例内容，实际使用时会调用真实的AI接口）";
    }

    /// <summary>
    /// 生成背景
    /// </summary>
    private string GenerateBackground()
    {
        return $"基于提示：{AIPrompt}\n\n" +
               $"AI生成的背景故事：\n\n" +
               $"出生在一个普通的中产家庭，父亲是工程师，母亲是教师。从小就展现出超乎常人的观察力和学习能力。\n\n" +
               $"十岁时经历了一次意外事件，这次经历让他对世界有了更深刻的认识，也塑造了他坚强的性格。\n\n" +
               $"青少年时期在学校表现优异，但因为性格内向，朋友不多。直到遇到了现在的挚友，才逐渐打开心扉。\n\n" +
               $"成年后选择了一条与众不同的道路，虽然充满挑战，但他从未后悔过自己的选择。\n\n" +
               $"（这是AI生成的示例内容，实际使用时会调用真实的AI接口）";
    }

    /// <summary>
    /// 生成对话
    /// </summary>
    private string GenerateDialogue()
    {
        return $"基于提示：{AIPrompt}\n\n" +
               $"AI生成的角色对话示例：\n\n" +
               $"【日常对话】\n" +
               $"\"这件事我需要仔细考虑一下，不能匆忙做决定。\"\n\n" +
               $"【面对困难】\n" +
               $"\"困难只是成长路上的垫脚石，我不会被它击倒。\"\n\n" +
               $"【与朋友交流】\n" +
               $"\"你知道的，我不善于表达，但你的友谊对我很重要。\"\n\n" +
               $"【面对敌人】\n" +
               $"\"我不会主动挑起争端，但也绝不会退缩。\"\n\n" +
               $"【内心独白】\n" +
               $"\"也许别人不理解我的选择，但我必须走自己认为正确的路。\"\n\n" +
               $"（这是AI生成的示例内容，实际使用时会调用真实的AI接口）";
    }

    /// <summary>
    /// 应用AI内容
    /// </summary>
    private void ApplyAIContent()
    {
        if (SelectedCharacter == null || string.IsNullOrWhiteSpace(AIGeneratedContent))
            return;

        var result = MessageBox.Show("是否将AI生成的内容应用到当前角色？", "确认",
                                   MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            if (IsGeneratePersonality)
            {
                SelectedCharacter.Personality = AIGeneratedContent;
            }
            else if (IsGenerateBackground)
            {
                SelectedCharacter.Background = AIGeneratedContent;
            }
            else if (IsGenerateDialogue)
            {
                SelectedCharacter.SpeakingStyle = AIGeneratedContent;
            }

            OnPropertyChanged(nameof(SelectedCharacter));
            MessageBox.Show("AI内容已应用到角色", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// 复制AI内容
    /// </summary>
    private void CopyAIContent()
    {
        if (!string.IsNullOrWhiteSpace(AIGeneratedContent))
        {
            Clipboard.SetText(AIGeneratedContent);
            MessageBox.Show("内容已复制到剪贴板", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// 清理生成的文本，移除多余的格式标记和空格
    /// </summary>
    private string CleanGeneratedText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // 移除Markdown格式的粗体标记 **
        text = text.Replace("**", "");
        
        // 移除多余的空行（保留单个换行）
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n\s*\n", "\n\n");
        
        // 移除行首和行尾的多余空格
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Trim();
        }
        text = string.Join("\n", lines);
        
        // 移除开头和结尾的空白字符
        text = text.Trim();
        
        return text;
    }

    #endregion
}