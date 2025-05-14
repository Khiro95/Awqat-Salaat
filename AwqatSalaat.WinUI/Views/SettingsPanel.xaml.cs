using AwqatSalaat.Helpers;
using AwqatSalaat.Interop;
using AwqatSalaat.Services.Nominatim;
using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    public sealed partial class SettingsPanel : UserControl
    {
        public static readonly string Version = typeof(SettingsPanel).Assembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?
            .Version;
        public static readonly string Architecture = Environment.Is64BitProcess ? "64-bit" : "32-bit";

#if PACKAGED
        static SettingsPanel()
        {
            var packageVersion = Windows.ApplicationModel.Package.Current.Id.Version;
            Version = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
        }
#endif

        private bool keepFlyoutOpen;
        private bool loadedFirstTime;
        private CancellationTokenSource checkingUpdatesCancellationTokenSource;

        private WidgetSettingsViewModel ViewModel => DataContext as WidgetSettingsViewModel;

        public Flyout ParentFlyout { get; set; }
        public StartupSettings StartupSettings { get; private set; }

        public SettingsPanel()
        {
            this.InitializeComponent();
            this.Loaded += SettingsPanel_Loaded;
            this.RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityChanged);

            // Workaround for a bug https://github.com/microsoft/microsoft-ui-xaml/issues/4035
            countryComboBox.RegisterPropertyChangedCallback(ComboBox.ItemsSourceProperty, OnItemsSourceChanged);

            version.Text = "v" + (Version ?? "{ERROR}");
            architecture.Text = Architecture;
            SetImageSource();

            nav.SelectionChanged += Nav_SelectionChanged;
        }

        private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            contentContainer.Content = args.SelectedItemContainer.Tag;
        }

        private void SettingsPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Settings panel loaded");

            if (!loadedFirstTime)
            {
                loadedFirstTime = true;
                StartupSettings = new StartupSettings(ViewModel.Realtime);
                Bindings.Update();

                this.ViewModel.Updated += _ => StartupSettings.Commit();

                if (ParentFlyout is not null)
                {
                    ParentFlyout.Closing += (s, a) => a.Cancel = keepFlyoutOpen;
                }

                if (!ViewModel.Settings.IsConfigured)
                {
                    nav.SelectedItem = locationTab;
                }
            }

#if PACKAGED
            StartupSettings.VerifyStartupTask();
#endif
        }

        // Workaround for a bug https://github.com/microsoft/microsoft-ui-xaml/issues/4035
        private static void OnItemsSourceChanged(DependencyObject sender, DependencyProperty dp)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox.ItemsSource is not null)
            {
                comboBox.SelectedValuePath = null;
                comboBox.SelectedValuePath = "Code";
            }
        }

        private async Task SetImageSource()
        {
#if PACKAGED
            var resourceContext = new Windows.ApplicationModel.Resources.Core.ResourceContext();
            resourceContext.QualifierValues["targetsize"] = "32";
            var namedResource = Windows.ApplicationModel.Resources.Core.ResourceManager.Current.MainResourceMap[@"Files/Images/applist.png"];
            var resourceCandidate = namedResource.Resolve(resourceContext);
            var imageFileStream = await resourceCandidate.GetValueAsStreamAsync();
            var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            bitmapImage.SetSourceAsync(imageFileStream);
            icon.Source = bitmapImage;
#else
            icon.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/app_icon_32.png"));
#endif
        }
        
        private void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            bool isVisible = Visibility == Visibility.Visible;
            Log.Information("Settings panel became " + (isVisible ? "visible" : "invisible"));

            if (!isVisible)
            {
                // change selection when collapsed to hide the transition from previous tab to general tab
                nav.SelectedItem = generalTab;

                // When the widget switch to/from compact mode when editing some setting,
                // a bug in ToggleSwitch makes it stuck at "Dragging" visual state hence when the flyout hide then show again
                // the toggle will not show up visually correct. This is well observed for On->Off transition.
                FixToggleSwitchVisualBug(countdownToggle);
                FixToggleSwitchVisualBug(compactModeToggle);

                CollapseExpanders(null);

                // cancel updates checking if it's going on
                checkingUpdatesCancellationTokenSource?.Cancel();
                checkingUpdatesCancellationTokenSource?.Dispose();
                checkingUpdatesCancellationTokenSource = null;
            }
        }

        private static void FixToggleSwitchVisualBug(ToggleSwitch toggleSwitch)
        {
            // Looks like there is a bug in ToggleSwitch visual states,
            // when it become invisible during a switch while in Dragging state it doesn't continue to On/Off state.
            // This make the ToggleSwitch remain visually ON in case the transition Dragging->On was interrupted
            // and then it get asked to go to Off
            if (!toggleSwitch.IsOn)
            {
                VisualStateManager.GoToState(toggleSwitch, "On", false);
                VisualStateManager.GoToState(toggleSwitch, "Off", false);
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs e)
        {
            if (e.SelectedItem is Place place)
            {
                ViewModel.Locator.SelectedPlace = place;
            }
        }

        private void ContactHyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            Log.Information("Clicked on Contact link");
            // https://github.com/microsoft/microsoft-ui-xaml/issues/4438
            Windows.System.Launcher.LaunchUriAsync(new Uri("mailto:khiro95.gh@gmail.com"));
        }

        private async void CheckForUpdatesHyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            try
            {
                Log.Information("Clicked on Check for updates");
                keepFlyoutOpen = true;
                checkingUpdatesCancellationTokenSource = new CancellationTokenSource();
                var current = System.Version.Parse(Version);
#if DEBUG
                current = System.Version.Parse("1.0");
#endif
#if PACKAGED
                ViewModel.IsCheckingNewVersion = true;

                var storeContext = Windows.Services.Store.StoreContext.GetDefault();
                InitializeWithWindow.Initialize(storeContext, App.MainHandle);
                var updates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();

                ViewModel.IsCheckingNewVersion = false;

                if (updates.Count > 0)
                {
                    // We can't get the version of the new package in Microsoft Store so we skip this info :(
                    var lines = Properties.Resources.Dialog_NewUpdateAvailableFormat.Split(Environment.NewLine);
                    var newMessage = Properties.Resources.Dialog_NewUpdateAvailableFormat.Replace(lines[1] + Environment.NewLine, "");
                    var result = MessageBox.Question(newMessage);

                    if (result == MessageBoxResult.IDYES)
                    {
                        Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?productid=9NHH4C81FZ0N")); ;
                    }
                }
                else
                {
                    MessageBox.Info(Properties.Resources.Dialog_WidgetUpToDate);
                }
#else
                var latest = await ViewModel.CheckForNewVersion(current, checkingUpdatesCancellationTokenSource.Token);

                if (latest is null)
                {
                    MessageBox.Info(Properties.Resources.Dialog_WidgetUpToDate);
                }
                else
                {
                    var result = MessageBox.Question(string.Format(Properties.Resources.Dialog_NewUpdateAvailableFormat, latest.Tag));

                    if (result == MessageBoxResult.IDYES)
                    {
                        Windows.System.Launcher.LaunchUriAsync(new Uri(latest.HtmlUrl));
                    }
                }
#endif
            }
            catch (OperationCanceledException)
            {
                Log.Debug("Checking updates has been canceled");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Checking for updates failed: {ex.Message}");
                MessageBox.Error(Properties.Resources.Dialog_CheckingUpdatesFailed + $"\nError: {ex.Message}");
            }
            finally
            {
                keepFlyoutOpen = false;
                checkingUpdatesCancellationTokenSource?.Dispose();
                checkingUpdatesCancellationTokenSource = null;
#if PACKAGED
                // Just in case
                ViewModel.IsCheckingNewVersion = false;
#endif
            }
        }

        private async void BrowseNotificationSound_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Browse for notification sound");
            await BrowseSoundFileAsync((s, f) => s.NotificationSoundFile = f);
        }

        private async void BrowseAdhanSound_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Browse for adhan sound");
            await BrowseSoundFileAsync((s, f) => s.AdhanSoundFile = f);
        }

        private async void BrowseAdhanFajrSound_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Browse for adhan Fajr sound");
            await BrowseSoundFileAsync((s, f) => s.AdhanFajrSoundFile = f);
        }

        private async Task BrowseSoundFileAsync(Action<Properties.Settings, string> fileSetter)
        {
            try
            {
                FileOpenPicker fileOpenPicker = new()
                {
                    FileTypeFilter = { ".wav", ".mp3", ".wma", ".aac" },
                };

                InitializeWithWindow.Initialize(fileOpenPicker, App.MainHandle);

                keepFlyoutOpen = true;
                IsHitTestVisible = false;

                StorageFile file = await fileOpenPicker.PickSingleFileAsync();

                if (file != null)
                {
                    fileSetter(ViewModel.Realtime, file.Path);
                }
            }
            finally
            {
                keepFlyoutOpen = false;
                IsHitTestVisible = true;
            }
        }

        private void Expander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
        {
            CollapseExpanders(sender);
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
                MessageBox.Info(Properties.Resources.Dialog_LogsFileNotFound);
                return;
            }

            Process.Start("explorer.exe", $"/select,\"{LogManager.LogsPath}\"");
        }
    }
}
