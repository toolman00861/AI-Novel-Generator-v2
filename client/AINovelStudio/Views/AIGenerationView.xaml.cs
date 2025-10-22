using System.Windows.Controls;

namespace AINovelStudio.Views;

/// <summary>
/// AIGenerationView.xaml 的交互逻辑
/// </summary>
public partial class AIGenerationView : UserControl
{
    public AIGenerationView()
    {
        InitializeComponent();
    }

    // 新增：输出文本变更时自动滚动到底部，便于观看流式内容
    private void OutputTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.ScrollToEnd();
        }
    }
}