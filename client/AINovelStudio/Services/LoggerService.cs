using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using AINovelStudio.Models;

namespace AINovelStudio.Services
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Fatal
    }

    /// <summary>
    /// 日志条目类
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 日志时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 日志源（类名或模块名）
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 获取日志级别对应的颜色
        /// </summary>
        public SolidColorBrush LevelColor
        {
            get
            {
                return Level switch
                {
                    LogLevel.Debug => new SolidColorBrush(Colors.Gray),
                    LogLevel.Info => new SolidColorBrush(Colors.Green),
                    LogLevel.Warning => new SolidColorBrush(Colors.Orange),
                    LogLevel.Error => new SolidColorBrush(Colors.Red),
                    LogLevel.Fatal => new SolidColorBrush(Colors.DarkRed),
                    _ => new SolidColorBrush(Colors.Black)
                };
            }
        }

        /// <summary>
        /// 格式化的日志消息
        /// </summary>
        public string FormattedMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 日志服务接口
    /// </summary>
    public interface ILoggerService
    {
        /// <summary>
        /// 记录调试级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        void Debug(string message, string source = "");

        /// <summary>
        /// 记录信息级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        void Info(string message, string source = "");

        /// <summary>
        /// 记录警告级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        void Warning(string message, string source = "");

        /// <summary>
        /// 记录错误级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        void Error(string message, string source = "");

        /// <summary>
        /// 记录致命错误级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        void Fatal(string message, string source = "");

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="message">附加消息</param>
        /// <param name="source">日志源</param>
        /// <param name="level">日志级别</param>
        void LogException(Exception ex, string message = "", string source = "", LogLevel level = LogLevel.Error);

        /// <summary>
        /// 获取日志条目集合
        /// </summary>
        ObservableCollection<LogEntry> LogEntries { get; }

        /// <summary>
        /// 清除日志
        /// </summary>
        void ClearLogs();

        /// <summary>
        /// 设置最低日志级别
        /// </summary>
        LogLevel MinimumLogLevel { get; set; }

        /// <summary>
        /// 是否启用文件日志
        /// </summary>
        bool EnableFileLogging { get; set; }

        /// <summary>
        /// 日志文件路径
        /// </summary>
        string LogFilePath { get; set; }
        
        /// <summary>
        /// 最大日志条目数
        /// </summary>
        int MaxLogEntries { get; set; }
        
        /// <summary>
        /// 是否在调试窗口输出日志
        /// </summary>
        bool OutputToDebugConsole { get; set; }
        
        /// <summary>
        /// 日志时间戳格式
        /// </summary>
        string TimestampFormat { get; set; }
        
        /// <summary>
        /// 日志格式模板
        /// </summary>
        string LogFormatTemplate { get; set; }
        
        /// <summary>
        /// 应用日志设置
        /// </summary>
        /// <param name="settings">日志设置</param>
        void ApplySettings(LoggerSettings settings);
    }

    /// <summary>
    /// 日志服务实现
    /// </summary>
    public class LoggerService : ILoggerService
    {
        private static LoggerService? _instance;
        private static readonly object _lock = new object();
        private readonly SynchronizationContext? _synchronizationContext;

        /// <summary>
        /// 获取日志服务单例
        /// </summary>
        public static LoggerService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LoggerService();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 日志条目集合
        /// </summary>
        public ObservableCollection<LogEntry> LogEntries { get; } = new ObservableCollection<LogEntry>();

        /// <summary>
        /// 最低日志级别
        /// </summary>
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        /// 是否启用文件日志
        /// </summary>
        public bool EnableFileLogging { get; set; } = true;

        /// <summary>
        /// 日志文件路径
        /// </summary>
        public string LogFilePath { get; set; } = GetDefaultLogFilePath();

        /// <summary>
        /// 最大日志条目数
        /// </summary>
        public int MaxLogEntries { get; set; } = 1000;
        
        /// <summary>
        /// 是否在调试窗口输出日志
        /// </summary>
        public bool OutputToDebugConsole { get; set; } = true;
        
        /// <summary>
        /// 日志时间戳格式
        /// </summary>
        public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
        
        /// <summary>
        /// 日志格式模板
        /// </summary>
        public string LogFormatTemplate { get; set; } = "[{timestamp}] [{level}] [{source}] {message}";

        /// <summary>
        /// 日志文件保留天数
        /// </summary>
        public int LogRetentionDays { get; set; } = 30;

        /// <summary>
        /// 构造函数
        /// </summary>
        private LoggerService()
        {
            _synchronizationContext = SynchronizationContext.Current;
            
            // 确保日志目录存在
            EnsureLogDirectoryExists();
            
            // 清理过期日志文件
            CleanupOldLogFiles();
        }

        /// <summary>
        /// 获取默认日志文件路径
        /// </summary>
        /// <returns>默认日志文件路径</returns>
        private static string GetDefaultLogFilePath()
        {
            // 首先尝试使用项目根目录下的logs文件夹
            string projectRoot = GetProjectRootDirectory();
            if (!string.IsNullOrEmpty(projectRoot))
            {
                return Path.Combine(projectRoot, "logs", $"log_{DateTime.Now:yyyyMMdd}.txt");
            }
            
            // 如果找不到项目根目录，使用用户本地应用数据目录
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AINovelStudio",
                "logs",
                $"log_{DateTime.Now:yyyyMMdd}.txt");
        }

        /// <summary>
        /// 获取项目根目录
        /// </summary>
        /// <returns>项目根目录路径，如果找不到则返回空字符串</returns>
        private static string GetProjectRootDirectory()
        {
            try
            {
                // 从当前执行目录开始向上查找
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                DirectoryInfo? dir = new DirectoryInfo(currentDir);
                
                while (dir != null)
                {
                    // 查找包含特定文件的目录作为项目根目录
                    if (File.Exists(Path.Combine(dir.FullName, "AINovelStudio.csproj")) ||
                        Directory.Exists(Path.Combine(dir.FullName, "client")) ||
                        File.Exists(Path.Combine(dir.FullName, "README.md")))
                    {
                        return dir.FullName;
                    }
                    dir = dir.Parent;
                }
            }
            catch
            {
                // 忽略异常，返回null
                return "";
            }
            
            return "";
        }

        /// <summary>
        /// 确保日志目录存在
        /// </summary>
        private void EnsureLogDirectoryExists()
        {
            try
            {
                string? logDirectory = Path.GetDirectoryName(LogFilePath);
                if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
            catch (Exception ex)
            {
                // 如果创建目录失败，记录到调试输出
                System.Diagnostics.Debug.WriteLine($"创建日志目录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用日志设置
        /// </summary>
        /// <param name="settings">日志设置</param>
        public void ApplySettings(LoggerSettings settings)
        {
            MinimumLogLevel = settings.MinimumLogLevel;
            EnableFileLogging = settings.EnableFileLogging;
            LogFilePath = settings.LogFilePath;
            MaxLogEntries = settings.MaxLogEntries;
            OutputToDebugConsole = settings.OutputToDebugConsole;
            TimestampFormat = settings.TimestampFormat;
            LogFormatTemplate = settings.LogFormatTemplate;
            LogRetentionDays = settings.LogRetentionDays;
            
            // 确保日志目录存在
            EnsureLogDirectoryExists();
            
            // 清理过期日志文件
            CleanupOldLogFiles();
        }

        /// <summary>
        /// 记录调试级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        public void Debug(string message, string source = "") => Log(LogLevel.Debug, message, source);

        /// <summary>
        /// 记录信息级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        public void Info(string message, string source = "") => Log(LogLevel.Info, message, source);

        /// <summary>
        /// 记录警告级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        public void Warning(string message, string source = "") => Log(LogLevel.Warning, message, source);

        /// <summary>
        /// 记录错误级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        public void Error(string message, string source = "") => Log(LogLevel.Error, message, source);

        /// <summary>
        /// 记录致命错误级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        public void Fatal(string message, string source = "") => Log(LogLevel.Fatal, message, source);

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="message">附加消息</param>
        /// <param name="source">日志源</param>
        /// <param name="level">日志级别</param>
        public void LogException(Exception ex, string message = "", string source = "", LogLevel level = LogLevel.Error)
        {
            string exceptionMessage = string.IsNullOrEmpty(message)
                ? $"异常: {ex.Message}"
                : $"{message} - 异常: {ex.Message}";

            if (ex.StackTrace != null)
            {
                exceptionMessage += $"\n堆栈跟踪: {ex.StackTrace}";
            }

            Log(level, exceptionMessage, source);

            // 记录内部异常
            if (ex.InnerException != null)
            {
                LogException(ex.InnerException, "内部异常", source, level);
            }
        }

        /// <summary>
        /// 清除日志
        /// </summary>
        public void ClearLogs()
        {
            if (_synchronizationContext != null)
            {
                _synchronizationContext.Post(_ => LogEntries.Clear(), null);
            }
            else
            {
                LogEntries.Clear();
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="source">日志源</param>
        private void Log(LogLevel level, string message, string source)
        {
            // 检查日志级别
            if (level < MinimumLogLevel)
                return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Source = source
            };

            // 格式化消息
            entry.FormattedMessage = FormatLogMessage(entry);

            // 添加到内存集合
            if (_synchronizationContext != null)
            {
                _synchronizationContext.Post(_ =>
                {
                    LogEntries.Add(entry);
                    
                    // 限制日志条目数量
                    while (LogEntries.Count > MaxLogEntries)
                    {
                        LogEntries.RemoveAt(0);
                    }
                }, null);
            }
            else
            {
                LogEntries.Add(entry);
                
                // 限制日志条目数量
                while (LogEntries.Count > MaxLogEntries)
                {
                    LogEntries.RemoveAt(0);
                }
            }

            // 写入文件
            if (EnableFileLogging)
            {
                try
                {
                    // 确保使用当天的日志文件
                    string currentLogFile = GetCurrentLogFilePath();
                    if (currentLogFile != LogFilePath)
                    {
                        LogFilePath = currentLogFile;
                        EnsureLogDirectoryExists();
                    }
                    
                    File.AppendAllText(LogFilePath, entry.FormattedMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // 避免在日志记录中产生递归
                    System.Diagnostics.Debug.WriteLine($"写入日志文件失败: {ex.Message}");
                }
            }

            // 同时输出到调试窗口
            if (OutputToDebugConsole)
            {
                System.Diagnostics.Debug.WriteLine(entry.FormattedMessage);
            }
        }

        /// <summary>
        /// 格式化日志消息
        /// </summary>
        /// <param name="entry">日志条目</param>
        /// <returns>格式化后的消息</returns>
        private string FormatLogMessage(LogEntry entry)
        {
            string formattedMessage = LogFormatTemplate;
            formattedMessage = formattedMessage.Replace("{timestamp}", entry.Timestamp.ToString(TimestampFormat));
            formattedMessage = formattedMessage.Replace("{level}", entry.Level.ToString());
            formattedMessage = formattedMessage.Replace("{source}", string.IsNullOrEmpty(entry.Source) ? "-" : entry.Source);
            formattedMessage = formattedMessage.Replace("{message}", entry.Message);
            return formattedMessage;
        }

        /// <summary>
        /// 获取当前日志文件路径
        /// </summary>
        /// <returns>当前日志文件路径</returns>
        private string GetCurrentLogFilePath()
        {
            string? logDirectory = Path.GetDirectoryName(LogFilePath);
            if (string.IsNullOrEmpty(logDirectory))
            {
                return GetDefaultLogFilePath();
            }
            
            return Path.Combine(logDirectory, $"log_{DateTime.Now:yyyyMMdd}.txt");
        }

        /// <summary>
        /// 清理过期的日志文件
        /// </summary>
        private void CleanupOldLogFiles()
        {
            try
            {
                string? logDirectory = Path.GetDirectoryName(LogFilePath);
                if (string.IsNullOrEmpty(logDirectory) || !Directory.Exists(logDirectory))
                {
                    return;
                }

                DateTime cutoffDate = DateTime.Now.AddDays(-LogRetentionDays);
                string[] logFiles = Directory.GetFiles(logDirectory, "log_*.txt");

                foreach (string logFile in logFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(logFile);
                        if (fileName.StartsWith("log_") && fileName.Length == 12) // log_yyyyMMdd
                        {
                            string dateString = fileName.Substring(4); // yyyyMMdd
                            if (DateTime.TryParseExact(dateString, "yyyyMMdd", null, 
                                System.Globalization.DateTimeStyles.None, out DateTime fileDate))
                            {
                                if (fileDate < cutoffDate)
                                {
                                    File.Delete(logFile);
                                    System.Diagnostics.Debug.WriteLine($"已删除过期日志文件: {logFile}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"删除日志文件失败 {logFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理日志文件失败: {ex.Message}");
            }
        }
    }
}