using AINovelStudio.Commands;
using AINovelStudio.Views;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AINovelStudio.ViewModels;

/// <summary>
/// 主窗口视图模型
/// </summary>
public class MainViewModel : BaseViewModel
{
    private object? _currentView;
    private string _selectedMenuItem = "小说管理";

    public MainViewModel()
    {
        NavigateCommand = new RelayCommand<string>(Navigate);
        
        // 初始化菜单项
        MenuItems = new ObservableCollection<string>
        {
            "小说管理",
            "人物设计",
            "AI生成",
            "设置",
            "日志"
        };
        
        // 默认导航到小说管理页面
        Navigate(_selectedMenuItem);
    }

    /// <summary>
    /// 当前视图
    /// </summary>
    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    /// <summary>
    /// 菜单项集合
    /// </summary>
    public ObservableCollection<string> MenuItems { get; }

    /// <summary>
    /// 选中的菜单项
    /// </summary>
    public string SelectedMenuItem
    {
        get => _selectedMenuItem;
        set => SetProperty(ref _selectedMenuItem, value);
    }

    /// <summary>
    /// 导航命令
    /// </summary>
    public ICommand NavigateCommand { get; }

    /// <summary>
    /// 导航到指定页面
    /// </summary>
    /// <param name="viewName">页面名称</param>
    private void Navigate(string? viewName)
    {
        if (string.IsNullOrEmpty(viewName))
            return;

        SelectedMenuItem = viewName;

        CurrentView = viewName switch
        {
            "小说管理" => new NovelManagementView { DataContext = new NovelManagementViewModel() },
            "人物设计" => new CharacterDesignView { DataContext = new CharacterDesignViewModel() },
            "AI生成" => new AIGenerationView { DataContext = new AIGenerationViewModel() },
            "设置" => new SettingsView { DataContext = new SettingsViewModel() },
            "日志" => new LoggerView { DataContext = new LoggerViewModel() },
            _ => CurrentView
        };
    }

    /// <summary>
    /// 创建占位符视图
    /// </summary>
    /// <param name="title">标题</param>
    /// <returns>占位符视图模型</returns>
    private PlaceholderViewModel CreatePlaceholderView(string title)
    {
        return new PlaceholderViewModel { Title = title };
    }
}

/// <summary>
/// 占位符视图模型
/// </summary>
public class PlaceholderViewModel : BaseViewModel
{
    public string Title { get; set; } = string.Empty;
}