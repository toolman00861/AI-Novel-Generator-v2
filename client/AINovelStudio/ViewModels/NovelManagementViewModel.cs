using AINovelStudio.Commands;
using AINovelStudio.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace AINovelStudio.ViewModels;

/// <summary>
/// 小说管理视图模型
/// </summary>
public class NovelManagementViewModel : BaseViewModel
{
    private ObservableCollection<Novel> _novels;

    public NovelManagementViewModel()
    {
        _novels = new ObservableCollection<Novel>();
        
        // 初始化命令
        CreateNovelCommand = new RelayCommand(CreateNovel);
        RefreshCommand = new RelayCommand(Refresh);
        EditNovelCommand = new RelayCommand<Novel>(EditNovel);
        DeleteNovelCommand = new RelayCommand<Novel>(DeleteNovel);
        ManageChaptersCommand = new RelayCommand<Novel>(ManageChapters);
        
        // 加载示例数据
        LoadSampleData();
    }

    /// <summary>
    /// 小说列表
    /// </summary>
    public ObservableCollection<Novel> Novels
    {
        get => _novels;
        set => SetProperty(ref _novels, value);
    }

    /// <summary>
    /// 小说数量
    /// </summary>
    public int NovelCount => Novels.Count;

    /// <summary>
    /// 创建新小说命令
    /// </summary>
    public ICommand CreateNovelCommand { get; }

    /// <summary>
    /// 刷新命令
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 编辑小说命令
    /// </summary>
    public ICommand EditNovelCommand { get; }

    /// <summary>
    /// 删除小说命令
    /// </summary>
    public ICommand DeleteNovelCommand { get; }

    /// <summary>
    /// 管理章节命令
    /// </summary>
    public ICommand ManageChaptersCommand { get; }

    /// <summary>
    /// 创建新小说
    /// </summary>
    private void CreateNovel()
    {
        var newNovel = new Novel
        {
            Id = Novels.Count + 1,
            Title = "新小说",
            Description = "请输入小说描述...",
            Author = "作者",
            Status = NovelStatus.Draft,
            Tags = new List<string> { "新作品" }
        };

        Novels.Add(newNovel);
        OnPropertyChanged(nameof(NovelCount));
        
        MessageBox.Show("新小说已创建！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 刷新数据
    /// </summary>
    private void Refresh()
    {
        // 这里可以从数据库或服务重新加载数据
        MessageBox.Show("数据已刷新！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 编辑小说
    /// </summary>
    /// <param name="novel">要编辑的小说</param>
    private void EditNovel(Novel? novel)
    {
        if (novel == null) return;
        
        MessageBox.Show($"编辑小说：{novel.Title}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 删除小说
    /// </summary>
    /// <param name="novel">要删除的小说</param>
    private void DeleteNovel(Novel? novel)
    {
        if (novel == null) return;

        var result = MessageBox.Show($"确定要删除小说《{novel.Title}》吗？", "确认删除", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            Novels.Remove(novel);
            OnPropertyChanged(nameof(NovelCount));
            MessageBox.Show("小说已删除！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// 管理章节
    /// </summary>
    /// <param name="novel">要管理章节的小说</param>
    private void ManageChapters(Novel? novel)
    {
        if (novel == null) return;
        
        MessageBox.Show($"管理《{novel.Title}》的章节", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// 加载示例数据
    /// </summary>
    private void LoadSampleData()
    {
        var sampleNovels = new List<Novel>
        {
            new Novel
            {
                Id = 1,
                Title = "星际穿越之旅",
                Description = "一个关于太空探索和人类命运的科幻故事，主人公在宇宙中寻找新的家园。",
                Author = "张三",
                Status = NovelStatus.InProgress,
                Tags = new List<string> { "科幻", "太空", "冒险" },
                Chapters = new List<Chapter>
                {
                    new Chapter { Id = 1, Title = "启程", OrderIndex = 1 },
                    new Chapter { Id = 2, Title = "遭遇", OrderIndex = 2 }
                }
            },
            new Novel
            {
                Id = 2,
                Title = "古代修仙传",
                Description = "一个普通少年意外获得修仙机缘，踏上了漫长的修仙之路。",
                Author = "李四",
                Status = NovelStatus.Draft,
                Tags = new List<string> { "修仙", "玄幻", "成长" },
                Chapters = new List<Chapter>
                {
                    new Chapter { Id = 3, Title = "觉醒", OrderIndex = 1 }
                }
            },
            new Novel
            {
                Id = 3,
                Title = "都市霸总日常",
                Description = "商界精英的爱情与事业双丰收的故事。",
                Author = "王五",
                Status = NovelStatus.Completed,
                Tags = new List<string> { "都市", "言情", "商战" },
                Chapters = new List<Chapter>
                {
                    new Chapter { Id = 4, Title = "初遇", OrderIndex = 1 },
                    new Chapter { Id = 5, Title = "合作", OrderIndex = 2 },
                    new Chapter { Id = 6, Title = "结局", OrderIndex = 3 }
                }
            }
        };

        foreach (var novel in sampleNovels)
        {
            Novels.Add(novel);
        }
        
        OnPropertyChanged(nameof(NovelCount));
    }
}