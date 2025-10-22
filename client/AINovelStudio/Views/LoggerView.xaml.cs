using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;
using AINovelStudio.Services;

namespace AINovelStudio.Views
{
    /// <summary>
    /// LoggerView.xaml 的交互逻辑
    /// </summary>
    public partial class LoggerView : UserControl, INotifyPropertyChanged
    {
        private readonly ILoggerService _loggerService;
        private string _statusMessage = "就绪";
        private ObservableCollection<LogEntry> _filteredLogs;
        private CollectionViewSource _viewSource;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 过滤后的日志集合
        /// </summary>
        public ObservableCollection<LogEntry> FilteredLogs
        {
            get => _filteredLogs;
            set
            {
                _filteredLogs = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public LoggerView()
        {
            InitializeComponent();
            DataContext = this;
            
            _loggerService = LoggerService.Instance;
            _filteredLogs = new ObservableCollection<LogEntry>(_loggerService.LogEntries);
            
            // 设置过滤器
            _viewSource = new CollectionViewSource { Source = _filteredLogs };
            LogListView.ItemsSource = _viewSource.View;
            
            // 监听日志集合变化
            _loggerService.LogEntries.CollectionChanged += (sender, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (LogEntry item in e.NewItems)
                    {
                        if (ShouldIncludeLogEntry(item))
                        {
                            _filteredLogs.Add(item);
                        }
                    }
                }
                
                if (e.OldItems != null)
                {
                    foreach (LogEntry item in e.OldItems)
                    {
                        var itemToRemove = _filteredLogs.FirstOrDefault(x => 
                            x.Timestamp == item.Timestamp && 
                            x.Level == item.Level && 
                            x.Message == item.Message);
                            
                        if (itemToRemove != null)
                        {
                            _filteredLogs.Remove(itemToRemove);
                        }
                    }
                }
                
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                {
                    _filteredLogs.Clear();
                }
                
                UpdateStatusMessage();
            };
            
            // 设置日志级别过滤器变更事件
            LogLevelFilter.SelectionChanged += (sender, e) => ApplyFilters();
            
            // 初始状态消息
            UpdateStatusMessage();
        }

        /// <summary>
        /// 属性变更通知
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 搜索按钮点击事件
        /// </summary>
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 清除按钮点击事件
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _loggerService.ClearLogs();
            _filteredLogs.Clear();
            UpdateStatusMessage();
        }

        /// <summary>
        /// 导出按钮点击事件
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "日志文件 (*.log)|*.log|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                DefaultExt = ".log",
                FileName = $"AINovelStudio_Log_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    foreach (var log in _filteredLogs)
                    {
                        sb.AppendLine(log.FormattedMessage);
                    }
                    
                    File.WriteAllText(saveFileDialog.FileName, sb.ToString());
                    StatusMessage = $"日志已导出到: {saveFileDialog.FileName}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"导出失败: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// 应用过滤器
        /// </summary>
        private void ApplyFilters()
        {
            _filteredLogs.Clear();
            
            foreach (var log in _loggerService.LogEntries)
            {
                if (ShouldIncludeLogEntry(log))
                {
                    _filteredLogs.Add(log);
                }
            }
            
            UpdateStatusMessage();
        }

        /// <summary>
        /// 判断日志条目是否应该包含在过滤结果中
        /// </summary>
        /// <param name="log">日志条目</param>
        /// <returns>是否包含</returns>
        private bool ShouldIncludeLogEntry(LogEntry log)
        {
            // 应用日志级别过滤
            if (LogLevelFilter.SelectedIndex > 0)
            {
                var selectedLevel = (LogLevel)(LogLevelFilter.SelectedIndex - 1);
                if (log.Level != selectedLevel)
                {
                    return false;
                }
            }
            
            // 应用搜索过滤
            if (!string.IsNullOrEmpty(SearchBox?.Text))
            {
                var searchText = SearchBox.Text.ToLower();
                return log.Message.ToLower().Contains(searchText) || 
                       log.Source.ToLower().Contains(searchText) ||
                       log.Level.ToString().ToLower().Contains(searchText);
            }
            
            return true;
        }

        /// <summary>
        /// 更新状态消息
        /// </summary>
        private void UpdateStatusMessage()
        {
            StatusMessage = $"显示 {_filteredLogs.Count} 条日志 (共 {_loggerService.LogEntries.Count} 条)";
        }
    }
}