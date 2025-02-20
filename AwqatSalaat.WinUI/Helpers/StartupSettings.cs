using AwqatSalaat.Helpers;
using IWshRuntimeLibrary;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace AwqatSalaat.WinUI.Helpers
{
    public class StartupSettings : ObservableObject
    {
        private readonly Properties.Settings settings;
#if PACKAGED
        private bool launchOnStartup;
        private bool canSetLaunchOnStartup;

        public bool CanSetLaunchOnStartup { get => canSetLaunchOnStartup; private set => SetProperty(ref canSetLaunchOnStartup, value); }
        public bool LaunchOnStartup { get => canSetLaunchOnStartup && launchOnStartup; set => SetProperty(ref launchOnStartup, value); }
#else

        public bool CanSetLaunchOnStartup => true;
        public bool LaunchOnStartup
        {
            get => settings.LaunchOnWindowsStartup;
            set => settings.LaunchOnWindowsStartup = value;
        }
#endif

        public StartupSettings(Properties.Settings settings)
        {
            this.settings = settings;
#if !PACKAGED
            settings.PropertyChanged += Settings_PropertyChanged;
#endif
        }

#if PACKAGED
        public async Task VerifyStartupTask()
        {
            StartupTask startupTask = await StartupTask.GetAsync("AwqatSalaatStartupTask");
            CanSetLaunchOnStartup = startupTask.State is StartupTaskState.Enabled or StartupTaskState.Disabled;

            LaunchOnStartup = canSetLaunchOnStartup && startupTask.State == StartupTaskState.Enabled;
        }
#else
        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Properties.Settings.LaunchOnWindowsStartup))
            {
                OnPropertyChanged(nameof(LaunchOnStartup));
            }
        }

        ~StartupSettings()
        {
            settings.PropertyChanged -= Settings_PropertyChanged;
        }
#endif

        public async Task Commit()
        {
#if PACKAGED
            if (canSetLaunchOnStartup)
            {
                StartupTask startupTask = await StartupTask.GetAsync("AwqatSalaatStartupTask");

                if (launchOnStartup)
                {
                    await startupTask.RequestEnableAsync();
                }
                else
                {
                    startupTask.Disable();
                }
            }
#else
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var moduleInfo = process.MainModule.FileVersionInfo;
            var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), moduleInfo.ProductName + ".lnk");

            if (LaunchOnStartup)
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
#endif
        }
    }
}
