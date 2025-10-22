using System;
using System.IO;
using System.Windows;
using AINovelStudio.Services;

namespace AINovelStudio
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly LoggerService _logger = LoggerService.Instance;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化日志服务
            InitializeLogger();

            // 注册全局异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            _logger.Info("应用程序启动", "App");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logger.Info("应用程序退出", "App");
            base.OnExit(e);
        }

        private void InitializeLogger()
        {
            // 确保日志目录存在
            string logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AINovelStudio",
                "logs");

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // 配置日志服务
            _logger.EnableFileLogging = true;
            _logger.LogFilePath = Path.Combine(logDirectory, $"log_{DateTime.Now:yyyyMMdd}.txt");
            _logger.MinimumLogLevel = LogLevel.Debug;
            _logger.OutputToDebugConsole = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                _logger.Fatal($"未处理的应用程序异常: {ex.Message}", "AppDomain");
                _logger.LogException(ex, "未处理的应用程序异常", "AppDomain", LogLevel.Fatal);
            }
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.Fatal($"未处理的UI线程异常: {e.Exception.Message}", "Dispatcher");
            _logger.LogException(e.Exception, "未处理的UI线程异常", "Dispatcher", LogLevel.Fatal);
            
            // 标记为已处理，防止应用程序崩溃
            e.Handled = true;
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.Fatal($"未观察的任务异常: {e.Exception.Message}", "TaskScheduler");
            _logger.LogException(e.Exception, "未观察的任务异常", "TaskScheduler", LogLevel.Fatal);
            
            // 标记为已观察，防止进程终止
            e.SetObserved();
        }
    }
}

