using AwqatSalaat.Helpers;
using IWshRuntimeLibrary;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading;
using System.Windows.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static Mutex appMutex;

        public static event Action Quitting;

        public static ICommand Quit { get; }

        static App()
        {
            ExitIfOtherInstanceIsRunning();

            Quit = new RelayCommand(static o => QuitExecute());
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            if (SystemInfos.IsWindows10)
            {
                this.RequestedTheme = SystemInfos.IsLightThemeUsed() == true ? ApplicationTheme.Light : ApplicationTheme.Dark;
            }

            this.InitializeComponent();
            UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void ExitIfOtherInstanceIsRunning()
        {
            const string mutexId = @"C790179C-7492-4CCE-B377-5F48F394B2CB";

            appMutex = new Mutex(true, mutexId, out bool created);

            if (!created)
            {
                ShowError("The application is already running.");
                Environment.Exit(ExitCodes.AlreadyRunning);
            }
        }

        public static void SetLaunchOnWindowsStartup(bool launchOnWindowsStartup)
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var moduleInfo = process.MainModule.FileVersionInfo;
            var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), moduleInfo.ProductName + ".lnk");

            if (launchOnWindowsStartup)
            {
                WshShell wshShell = new WshShell();
                
                // Create the shortcut
                IWshShortcut shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = moduleInfo.FileName;
                shortcut.WorkingDirectory = Path.GetDirectoryName(moduleInfo.FileName);
                shortcut.Description = $"Launch {moduleInfo.ProductName}";
                shortcut.Save();
            }
            else
            {
                try
                {
                    System.IO.File.Delete(shortcutPath);
                }
                catch (Exception ex)
                {
#if DEBUG
                    throw;
#endif
                }
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            ShowUnhandledException(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            ShowUnhandledException(e.ExceptionObject);
        }

        private static void ShowUnhandledException(object exception)
        {
            string exceptionDump = exception?.ToString() ?? "(Empty)";
            ShowError("Unhandled Exception:\n" + exceptionDump);
            Environment.Exit(ExitCodes.UnhandledException);
        }

        private static void ShowError(string message)
        {
            string caption = LocaleManager.Default.Get("Data.AppName");
            Interop.User32.MessageBox(IntPtr.Zero, message, caption, Interop.MessageBoxButtons.MB_OK, Interop.MessageBoxIcon.MB_ICONERROR);
        }

        private static void QuitExecute()
        {
            Quitting?.Invoke();
            Current.Exit();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
#if DEBUG
            m_window = new MainWindow();
            m_window.Activate();
            m_window.Closed += (_, _) => QuitExecute();
#endif

            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            TaskBarManager.Initialize(dispatcher);
        }

#if DEBUG
        private Window m_window;
#endif

        private static class ExitCodes
        {
            public const int NoError = 0;
            // Not sure about using '1' since it can mean anything
            public const int AlreadyRunning = 2;
            public const int UnhandledException = 3;
        }
    }
}
