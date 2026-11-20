using AwqatSalaat.Configurations;
using AwqatSalaat.Helpers;
using AwqatSalaat.UI.Controls;
using AwqatSalaat.ViewModels;
using Microsoft.Win32;
using Serilog;
using System;
using System.Device.Location;
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
        private bool loadedFirstTime;

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
            Loaded += SettingsPanel_Loaded;
            Unloaded += SettingsPanel_Unloaded;
        }

        private void SettingsPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (!loadedFirstTime)
            {
                loadedFirstTime = true;
                ViewModel.SaveRejected += ViewModel_SaveRejected;
                
                if (!ViewModel.Settings.IsConfigured)
                {
                    TrySetGeolocation(false);
                }
            }

            ViewModel.Realtime.PropertyChanged -= Realtime_PropertyChanged;
            ViewModel.Realtime.PropertyChanged += Realtime_PropertyChanged;

            OnServiceChanged();
        }

        private void ViewModel_SaveRejected(string error)
        {
            ParentPopup.StaysOpen = true;
            ParentPopup.IsTopMost = false;
            var localizedError = LocaleManager.Default.Get("Dialog." + error);
            MessageBoxEx.Error(Properties.Resources.Dialog_InvalidServiceSettings + "\n" + localizedError);
            ParentPopup.StaysOpen = false;
            ParentPopup.IsTopMost = true;
        }

        private void SettingsPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Realtime.PropertyChanged -= Realtime_PropertyChanged;
        }

        private void Realtime_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Properties.Settings.Service))
            {
                OnServiceChanged();
            }
        }

        private void OnServiceChanged()
        {
            bool isQch = ViewModel.Realtime.Service == Data.PrayerTimesService.QCH;
            bool isCSV = ViewModel.Realtime.Service == Data.PrayerTimesService.CSV;

            if (isQch || isCSV)
            {
                Log.Information($"Adjusting Settings panel content to the selected service. (QCH={isQch}, CSV={isCSV})");
            }

            qchCitySetting.Visibility = isQch ? Visibility.Visible : Visibility.Collapsed;
            locationTab.Visibility = isQch || isCSV ? Visibility.Collapsed : Visibility.Visible;
            csvSettings.Visibility = isCSV ? Visibility.Visible : Visibility.Collapsed;

            // Waktu Solat serves official JAKIM times, so the school and calculation method don't apply
            bool isWaktuSolat = ViewModel.Realtime.Service == Data.PrayerTimesService.WaktuSolat;
            calculationSettings.Visibility = isQch || isCSV || isWaktuSolat ? Visibility.Collapsed : Visibility.Visible;
        }

        private void TrySetGeolocation(bool notifyIfNotAllowed)
        {
            try
            {
                Log.Information("Trying auto-geolocation...");
                var watcher = new GeoCoordinateWatcher();
                bool started = watcher.TryStart(false, TimeSpan.FromMilliseconds(2000));

                if (started && watcher.Permission == GeoPositionPermission.Granted)
                {
                    watcher.PositionChanged += Watcher_PositionChanged;
                }
                else
                {
                    if (notifyIfNotAllowed && started)
                    {
                        MessageBox.Show(LocaleManager.Default.Get("Dialog.LocationAccessDenied"), LocaleManager.Default.Get("Data.AppName"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    Log.Information("Auto-geolocation: " + (started ? "Access denied" : "Watcher not started"));
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Auto-geolocation failed: {ex.Message}");
            }
        }

        private void Watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            try
            {
                var watcher = (GeoCoordinateWatcher)sender;
                watcher.PositionChanged -= Watcher_PositionChanged;
                watcher.Stop();
                watcher.Dispose();

                ViewModel.Realtime.LocationDetection = Data.LocationDetectionMode.ByCoordinates;
                ViewModel.Realtime.Latitude = (decimal)e.Position.Location.Latitude;
                ViewModel.Realtime.Longitude = (decimal)e.Position.Location.Longitude;

                ViewModel.Locator.Check.Execute(null);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Auto-geolocation failed in PositionChanged: {ex.Message}");
            }
        }

        private void SettingsPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;
            Log.Information("Settings panel became " + (isVisible ? "visible" : "invisible"));
            tabControl.SelectedIndex = isVisible ? ViewModel.Settings.IsConfigured ? 0 : 3 : -1;

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
            if (sender is Button button && button.DataContext is PrayerConfig prayerConfig)
            {
                Log.Information($"Clicked on Browse for {prayerConfig.Key} adhan sound");

                if (prayerConfig.CanChangeAdhan && !prayerConfig.StandardAdhan)
                {
                    var path = BrowseSoundFile(prayerConfig.AdhanFile, null);

                    if (path != null)
                    {
                        prayerConfig.AdhanFile = path;
                    }
                }
            }
            else
            {
                Log.Information("Clicked on Browse for adhan sound");
                BrowseSoundFile(ViewModel.Realtime.AdhanSoundFilePath, (s, f) => s.AdhanSoundFile = f); 
            }
        }

        private void BrowseAdhanFajrSound_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Browse for adhan Fajr sound");
            BrowseSoundFile(ViewModel.Realtime.AdhanFajrSoundFilePath, (s, f) => s.AdhanFajrSoundFile = f);
        }

        private string BrowseSoundFile(string initialPath, Action<Properties.Settings, string> fileSetter)
        {
            if (openFileDialog is null)
            {
                openFileDialog = new OpenFileDialog()
                {
                    Filter = "Audio Files(*.wav;*.wma;*.mp3;*.aac)|*.wav;*.wma;*.mp3;*.aac;"
                };
            }

            if (initialPath == string.Empty)
            {
                initialPath = null;
            }

            ParentPopup.StaysOpen = true;
            ParentPopup.IsTopMost = false;

            try
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(initialPath);

                if (openFileDialog.ShowDialog() == true)
                {
                    fileSetter?.Invoke(ViewModel.Realtime, openFileDialog.FileName);

                    return openFileDialog.FileName;
                }
            }
            finally
            {
                ParentPopup.StaysOpen = false;
                ParentPopup.IsTopMost = true;
            }

            return null;
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

        private void BrowseCsvFile_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Browse for CSV file");

            if (openFileDialog is null)
            {
                openFileDialog = new OpenFileDialog()
                {
                    Filter = "CSV Files(*.csv)|*.csv;"
                };
            }

            ParentPopup.StaysOpen = true;
            ParentPopup.IsTopMost = false;

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    ViewModel.Realtime.CSV_FilePath = openFileDialog.FileName;
                }
            }
            finally
            {
                ParentPopup.StaysOpen = false;
                ParentPopup.IsTopMost = true;
            }
        }

        private void DetectLocation_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Auto-detect for location");

            TrySetGeolocation(true);
        }
    }
}
