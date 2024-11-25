using AwqatSalaat.Interop;
using AwqatSalaat.Services.Nominatim;
using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
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

            nav.SelectionChanged += Nav_SelectionChanged;
        }

        private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            contentContainer.Content = args.SelectedItemContainer.Tag;
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
            if (Visibility == Visibility.Collapsed)
            {
                // change selection when collapsed to hide the transition from previous tab to general tab
                nav.SelectedItem = generalTab;

                // When the widget switch to/from compact mode when editing some setting,
                // a bug in ToggleSwitch makes it stuck at "Dragging" visual state hence when the flyout hide then show again
                // the toggle will not show up visually correct. This is well observed for On->Off transition.
                FixToggleSwitchVisualBug(countdownToggle);
                FixToggleSwitchVisualBug(compactModeToggle);

                CollapseExpanders(null);
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

        private async void BrowseNotificationSound_Click(object sender, RoutedEventArgs e)
        {
            await BrowseSoundFileAsync((s, f) => s.NotificationSoundFile = f);
        }

        private async void BrowseAdhanSound_Click(object sender, RoutedEventArgs e)
        {
            await BrowseSoundFileAsync((s, f) => s.AdhanSoundFile = f);
        }

        private async void BrowseAdhanFajrSound_Click(object sender, RoutedEventArgs e)
        {
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
                    fileSetter(ViewModel.Settings, file.Path);
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
    }
}
