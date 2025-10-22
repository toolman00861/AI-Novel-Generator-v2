using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AINovelStudio.Commands;
using AINovelStudio.Models;
using AINovelStudio.Services;

namespace AINovelStudio.ViewModels
{
    /// <summary>
    /// 日志视图模型
    /// </summary>
    public class LoggerViewModel : BaseViewModel
    {
        private readonly ILoggerService _loggerService;
        private LoggerSettings _settings;
        private string _searchText = string.Empty;
        private LogLevel? _selectedLogLevel;
        private string _statusMessage = "就绪";

        /// <summary>
        /// 日志条目集合
        /// </summary>
        public ObservableCollection<LogEntry> LogEntries => _loggerService.LogEntries;

        /// <summary>
        /// 过滤后的日志条目集合
        /// </summary>
        public ObservableCollection<LogEntry> FilteredLogEntries { get; } = new ObservableCollection<LogEntry>();

        /// <summary>
        /// 搜索文本
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// 选中的日志级别
        /// </summary>
        public LogLevel? SelectedLogLevel
        {
            get => _selectedLogLevel;
            set
            {
                if (SetProperty(ref _selectedLogLevel, value))
                {
                    ApplyFilters();
                }
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 日志设置
        /// </summary>
        public LoggerSettings Settings
        {
            get => _settings;
            set
            {
                if (SetProperty(ref _settings, value))
                {
                    _loggerService.ApplySettings(_settings);
                }
            }
        }

        /// <summary>
        /// 清除日志命令
        /// </summary>
        public ICommand ClearLogsCommand { get; }

        /// <summary>
        /// 导出日志命令
        /// </summary>
        public ICommand ExportLogsCommand { get; }

        /// <summary>
        /// 保存设置命令
        /// </summary>
        public ICommand SaveSettingsCommand { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public LoggerViewModel()
        {
            _loggerService = LoggerService.Instance;
            _settings = new LoggerSettings
            {
                MinimumLogLevel = _loggerService.MinimumLogLevel,
                EnableFileLogging = _loggerService.EnableFileLogging,
                LogFilePath = _loggerService.LogFilePath,
                MaxLogEntries = _loggerService.MaxLogEntries,
                OutputToDebugConsole = _loggerService.OutputToDebugConsole,
                TimestampFormat = _loggerService.TimestampFormat,
                LogFormatTemplate = _loggerService.LogFormatTemplate
            };

            ClearLogsCommand = new RelayCommand(ClearLogs);
            ExportLogsCommand = new RelayCommand(ExportLogs);
            SaveSettingsCommand = new RelayCommand(SaveSettings);

            // 初始化过滤后的日志集合
            ApplyFilters();

            // 监听日志集合变化
            _loggerService.LogEntries.CollectionChanged += (sender, e) =>
            {
                ApplyFilters();
                UpdateStatusMessage();
            };
        }

        /// <summary>
        /// 应用过滤器
        /// </summary>
        private void ApplyFilters()
        {
            FilteredLogEntries.Clear();

            var filteredLogs = _loggerService.LogEntries.AsEnumerable();

            // 应用日志级别过滤
            if (_selectedLogLevel.HasValue)
            {
                filteredLogs = filteredLogs.Where(log => log.Level == _selectedLogLevel.Value);
            }

            // 应用搜索文本过滤
            if (!string.IsNullOrEmpty(_searchText))
            {
                var searchText = _searchText.ToLower();
                filteredLogs = filteredLogs.Where(log =>
                    log.Message.ToLower().Contains(searchText) ||
                    log.Source.ToLower().Contains(searchText) ||
                    log.Level.ToString().ToLower().Contains(searchText));
            }

            foreach (var log in filteredLogs)
            {
                FilteredLogEntries.Add(log);
            }

            UpdateStatusMessage();
        }

        /// <summary>
        /// 更新状态消息
        /// </summary>
        private void UpdateStatusMessage()
        {
            StatusMessage = $"显示 {FilteredLogEntries.Count} 条日志 (共 {_loggerService.LogEntries.Count} 条)";
        }

        /// <summary>
        /// 清除日志
        /// </summary>
        private void ClearLogs()
        {
            _loggerService.ClearLogs();
            FilteredLogEntries.Clear();
            UpdateStatusMessage();
        }

        /// <summary>
        /// 导出日志
        /// </summary>
        private void ExportLogs()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "日志文件 (*.log)|*.log|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                DefaultExt = ".log",
                FileName = $"AINovelStudio_Log_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using var writer = new StreamWriter(dialog.FileName);
                    foreach (var log in FilteredLogEntries)
                    {
                        writer.WriteLine(log.FormattedMessage);
                    }
                    StatusMessage = $"日志已导出到: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"导出失败: {ex.Message}";
                    _loggerService.LogException(ex, "导出日志失败", "LoggerViewModel");
                }
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                _loggerService.ApplySettings(_settings);
                StatusMessage = "设置已保存";
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存设置失败: {ex.Message}";
                _loggerService.LogException(ex, "保存日志设置失败", "LoggerViewModel");
            }
        }
    }
}