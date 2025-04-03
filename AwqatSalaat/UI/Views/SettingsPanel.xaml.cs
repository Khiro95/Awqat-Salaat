using AwqatSalaat.Helpers;
using AwqatSalaat.UI.Controls;
using AwqatSalaat.ViewModels;
using Microsoft.Win32;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for SettingsPanel.xaml
    /// </summary>
    public partial class SettingsPanel : UserControl
    {
        public static readonly string Version = typeof(SettingsPanel).Assembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?
            .Version;
        public static readonly string Architecture = Environment.Is64BitProcess ? "64-bit" : "32-bit";

        private OpenFileDialog openFileDialog;
        private CancellationTokenSource checkingUpdatesCancellationTokenSource;

        private WidgetSettingsViewModel ViewModel => DataContext as WidgetSettingsViewModel;

        public static DependencyProperty ParentPopupProperty = DependencyProperty.Register(
            nameof(ParentPopup),
            typeof(AcrylicPopup),
            typeof(SettingsPanel),
            new FrameworkPropertyMetadata(null));

        public AcrylicPopup ParentPopup
        {
            get => (AcrylicPopup)GetValue(ParentPopupProperty);
            set => SetValue(ParentPopupProperty, value);
        }

        public SettingsPanel()
        {
            InitializeComponent();
            IsVisibleChanged += SettingsPanel_IsVisibleChanged;
            version.Text = "v" + (Version ?? "{ERROR}");
            architecture.Text = Architecture;
        }

        private void SettingsPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;
            Log.Information("Settings panel became " + (isVisible ? "visible" : "invisible"));
            tabControl.SelectedIndex = isVisible ? 0 : -1;

            if (!isVisible)
            {
                CollapseExpanders(null);

                // cancel updates checking if it's going on
                checkingUpdatesCancellationTokenSource?.Cancel();
                checkingUpdatesCancellationTokenSource?.Dispose();
                checkingUpdatesCancellationTokenSource = null;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Log.Information($"Navigate to {e.Uri.AbsoluteUri}");
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        }

        private async void CheckForUpdatesClick(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Check for updates");
            // MessageBox will make the popup disappear so we have to force it to stay open temporarily
            var popup = Utils.GetOpenPopups().First();
            bool alteredPopup = false;

            if (popup != null && !popup.StaysOpen)
            {
                popup.StaysOpen = true;
                alteredPopup = true;
            }

            try
            {
                checkingUpdatesCancellationTokenSource = new CancellationTokenSource();
                var current = System.Version.Parse(Version);
#if DEBUG
                current = System.Version.Parse("1.0");
#endif
                var latest = await ViewModel.CheckForNewVersion(current, checkingUpdatesCancellationTokenSource.Token);

                if (latest is null)
                {
                    MessageBoxEx.Info(Properties.Resources.Dialog_WidgetUpToDate);
                }
                else
                {
                    var result = MessageBoxEx.Question(string.Format(Properties.Resources.Dialog_NewUpdateAvailableFormat, latest.Tag));

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(latest.HtmlUrl));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Checking updates has been canceled");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Checking for updates failed: {ex.Message}");
                MessageBoxEx.Error(Properties.Resources.Dialog_CheckingUpdatesFailed + $"\nError: {ex.Message}");
            }
            finally
            {
                checkingUpdatesCancellationTokenSource?.Dispose();
                checkingUpdatesCancellationTokenSource = null;

                if (alteredPopup)
                {
                    popup.StaysOpen = false;
                }
            }
        }

        private void BrowseNotificationSound_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Browse for notification sound");
            BrowseSoundFile(ViewModel.Realtime.NotificationSoundFilePath, (s, f) => s.NotificationSoundFile = f);
        }

        private void BrowseAdhanSound_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Browse for adhan sound");
            BrowseSoundFile(ViewModel.Realtime.AdhanSoundFilePath, (s, f) => s.AdhanSoundFile = f);
        }

        private void BrowseAdhanFajrSound_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Browse for adhan Fajr sound");
            BrowseSoundFile(ViewModel.Realtime.AdhanFajrSoundFilePath, (s, f) => s.AdhanFajrSoundFile = f);
        }

        private void BrowseSoundFile(string initialPath, Action<Properties.Settings, string> fileSetter)
        {
            if (openFileDialog is null)
            {
                openFileDialog = new OpenFileDialog()
                {
                    Filter = "Audio Files(*.wav;*.wma;*.mp3;*.aac)|*.wav;*.wma;*.mp3;*.aac;"
                };
            }

            ParentPopup.StaysOpen = true;
            ParentPopup.IsTopMost = false;

            try
            {
                openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(initialPath);

                if (openFileDialog.ShowDialog() == true)
                {
                    fileSetter(ViewModel.Realtime, openFileDialog.FileName);
                }
            }
            finally
            {
                ParentPopup.StaysOpen = false;
                ParentPopup.IsTopMost = true;
            }
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            CollapseExpanders(sender as Expander);
        }

        private void CollapseExpanders(Expander exception)
        {
            foreach (var item in timesPanel.Items)
            {
                var container = timesPanel.ItemContainerGenerator.ContainerFromItem(item);

                if (container != null)
                {
                    var expander = VisualTreeHelper.GetChild(container, 0) as Expander;

                    if (expander != exception)
                    {
                        expander.IsExpanded = false;
                    }
                }
            }
        }

        private void ShowLogsFileClick(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(LogManager.LogsPath))
            {
                MessageBoxEx.Info(Properties.Resources.Dialog_LogsFileNotFound);
                return;
            }

            Process.Start("explorer.exe", $"/select,\"{LogManager.LogsPath}\"");
        }
    }
}
