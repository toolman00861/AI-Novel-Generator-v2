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
        public bool EnableFileLogging { get; set; } = false;

        /// <summary>
        /// 日志文件路径
        /// </summary>
        public string LogFilePath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AINovelStudio",
            "logs",
            $"log_{DateTime.Now:yyyyMMdd}.txt");

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
    }
}