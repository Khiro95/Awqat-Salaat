using AwqatSalaat.Helpers;
using AwqatSalaat.WinUI.Views;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.Threading;
using System.Windows.Input;
using WinRT.Interop;

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

#if DEBUG
        public static IntPtr MainHandle { get; private set; }
#else
        public static IntPtr MainHandle => TaskBarManager.CurrentWidgetHandle;
#endif
        public static ICommand Quit { get; }

        static App()
        {
            InitLogger();

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
                Log.Information("Widget is already running");
                ShowError(Properties.Resources.Dialog_AppAlreadyRunning);
                Environment.Exit(ExitCodes.AlreadyRunning);
            }
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, e.Exception.Message);
            ShowUnhandledException(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.ExceptionObject as Exception, (e.ExceptionObject as Exception)?.Message);
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
            Helpers.MessageBox.Error(message);
        }

        private static void QuitExecute()
        {
            Log.Information("Quitting");
            Quitting?.Invoke();
            Current.Exit();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
#if DEBUG
            m_window = new MainWindow();
            m_window.Activate();
            m_window.Closed += (_, _) => QuitExecute();
            MainHandle = WindowNative.GetWindowHandle(m_window);
#endif

            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

            InitializeWidget(dispatcher);
        }

        private void InitializeWidget(Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue)
        {
            try
            {
                Log.Information("Initializing taskbar manager");
                TaskBarManager.Initialize(dispatcherQueue);
            }
            catch (WidgetNotInjectedException ex)
            {
                Log.Warning(ex, ex.Message);
                var result = Helpers.MessageBox.Error(ex.Message, Interop.MessageBoxButtons.MB_RETRYCANCEL);

                if (result == Interop.MessageBoxResult.IDRETRY)
                {
                    InitializeWidget(dispatcherQueue);
                    return;
                }

                // Calling Environment.Exit(ExitCodes.CouldNotInjectWidget) directly make the widget crash for some reason.
                // The workaround is to call Exit() then asynchonously call Environment.Exit(ExitCodes.CouldNotInjectWidget)
                // to override exit code; otherwise exit code will be 0x0
                Exit();
                System.Threading.Tasks.Task.Delay(50).ContinueWith(_ => Environment.Exit(ExitCodes.CouldNotInjectWidget));
            }
        }

        private static void InitLogger()
        {
            string identity = $"Awqat Salaat WinUI v{SettingsPanel.Version} ({SettingsPanel.Architecture})";
#if PACKAGED
            identity += " (Packaged)";
#endif
            LogManager.WidgetIdentity = identity;
            //Serilog doesn't serialize fields so we need to transform the object to props-only object
            LogManager.LoggerConfigurationHandler = config => config
                .Destructure.ByTransforming<Interop.RECT>(r => new { r.top, r.right, r.bottom, r.left })
                .Destructure.ByTransforming<UIAutomationClient.tagRECT>(r => new { r.top, r.right, r.bottom, r.left });

            LogManager.InvalidateLogger();
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
            public const int CouldNotInjectWidget = 4;
        }
    }
}
