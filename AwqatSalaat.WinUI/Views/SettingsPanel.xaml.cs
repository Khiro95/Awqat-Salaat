using AwqatSalaat.Interop;
using AwqatSalaat.Services.Nominatim;
using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Reflection;
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
        private static readonly string Version = typeof(SettingsPanel).Assembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?
            .Version;
        private static readonly string Architecture = Environment.Is64BitProcess ? "64-bit" : "32-bit";

#if PACKAGED
        static SettingsPanel()
        {
            var packageVersion = Windows.ApplicationModel.Package.Current.Id.Version;
            Version = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
        }
#endif

        private bool keepFlyoutOpen;
        private bool loadedFirstTime;

        private WidgetSettingsViewModel ViewModel => DataContext as WidgetSettingsViewModel;

        public Flyout ParentFlyout { get; set; }
        public StartupSettings StartupSettings { get; } = new StartupSettings();

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
        }

        private void SettingsPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (!loadedFirstTime)
            {
                loadedFirstTime = true;
                this.ViewModel.Updated += _ => StartupSettings.Commit();

                if (ParentFlyout is not null)
                {
                    ParentFlyout.Closing += (s, a) => a.Cancel = keepFlyoutOpen;
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
            var path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var file = await StorageFile.GetFileFromPathAsync(path);
            var iconThumbnail = await file.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 32);
            var bi = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
            bi.SetSource(iconThumbnail);
            icon.Source = bi;
        }
        
        private void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            // change selection when collapsed to hide the transition from previous tab to general tab
            if (Visibility == Visibility.Collapsed)
            {
                nav.SelectedItem = generalTab;

                // When the widget switch to/from compact mode when editing some setting,
                // a bug in ToggleSwitch makes it stuck at "Dragging" visual state hence when the flyout hide then show again
                // the toggle will not show up visually correct. This is well observed for On->Off transition.
                FixToggleSwitchVisualBug(countdownToggle);
                FixToggleSwitchVisualBug(compactModeToggle);
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
            // https://github.com/microsoft/microsoft-ui-xaml/issues/4438
            Windows.System.Launcher.LaunchUriAsync(new Uri("mailto:khiro95.gh@gmail.com"));
        }

        private async void CheckForUpdatesHyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            try
            {
                keepFlyoutOpen = true;
                var current = System.Version.Parse(Version);
#if DEBUG
                current = System.Version.Parse("1.0");
#endif
#if PACKAGED
                ViewModel.IsCheckingNewVersion = true;

                var storeContext = Windows.Services.Store.StoreContext.GetDefault();
                var updates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();

                ViewModel.IsCheckingNewVersion = false;

                if (updates.Count > 1)
                {
                    var version = updates[0].Package.Id.Version;
                    var versionString = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
                    var result = MessageBox.Question(string.Format(Properties.Resources.Dialog_NewUpdateAvailableFormat, versionString));

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
                var latest = await ViewModel.CheckForNewVersion(current);

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
            catch (Exception ex)
            {
                MessageBox.Error(Properties.Resources.Dialog_CheckingUpdatesFailed + $"\nError: {ex.Message}");
            }
            finally
            {
                keepFlyoutOpen = false;
#if PACKAGED
                // Just in case
                ViewModel.IsCheckingNewVersion = false;
#endif
            }
        }

        private async void BrowseSound_Click(object sender, RoutedEventArgs e)
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
                    ViewModel.Settings.NotificationSoundFile = file.Path;
                }
            }
            finally
            {
                keepFlyoutOpen = false;
                IsHitTestVisible = true;
            }
        }
    }
}
