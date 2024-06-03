using AwqatSalaat.Services.Nominatim;
using AwqatSalaat.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;
using System;
using System.Threading.Tasks;

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

        private WidgetSettingsViewModel ViewModel => DataContext as WidgetSettingsViewModel;

        public SettingsPanel()
        {
            this.InitializeComponent();
            this.RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityChanged);

            // Workaround for a bug https://github.com/microsoft/microsoft-ui-xaml/issues/4035
            countryComboBox.RegisterPropertyChangedCallback(ComboBox.ItemsSourceProperty, OnItemsSourceChanged);

            version.Text = "v" + (Version ?? "{ERROR}");
            architecture.Text = Architecture;
            SetImageSource();
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
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
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

        private void ContactHyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            //https://github.com/microsoft/microsoft-ui-xaml/issues/4438
            Windows.System.Launcher.LaunchUriAsync(new Uri("mailto:khiro95.gh@gmail.com"));
        }
    }
}
