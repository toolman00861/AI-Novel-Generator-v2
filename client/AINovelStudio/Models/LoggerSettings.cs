using System;
using System.IO;
using AINovelStudio.Services;

namespace AINovelStudio.Models
{
    /// <summary>
    /// 日志配置类
    /// </summary>
    public class LoggerSettings
    {
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
        /// 获取默认日志文件路径
        /// </summary>
        /// <returns>默认日志文件路径</returns>
        private static string GetDefaultLogFilePath()
        {
            // 首先尝试使用项目根目录下的logs文件夹
            string? projectRoot = GetProjectRootDirectory();
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
        /// <returns>项目根目录路径，如果找不到则返回null</returns>
        private static string? GetProjectRootDirectory()
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
            }
            
            return null;
        }
    }
}