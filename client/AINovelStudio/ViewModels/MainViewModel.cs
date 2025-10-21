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
            "AI生成", 
            "人物设计",
            "设置"
        };
        
        // 默认显示小说管理页面
        Navigate("小说管理");
    }

    /// <summary>
    /// 菜单项集合
    /// </summary>
    public ObservableCollection<string> MenuItems { get; }

    /// <summary>
    /// 当前显示的视图
    /// </summary>
    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    /// <summary>
    /// 当前选中的菜单项
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
            "AI生成" => new AIGenerationView { DataContext = new AIGenerationViewModel() },
            "人物设计" => new Views.CharacterDesignView { DataContext = new CharacterDesignViewModel() },
            "设置" => new SettingsView { DataContext = new SettingsViewModel() },
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