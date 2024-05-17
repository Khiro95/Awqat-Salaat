using AwqatSalaat.Services.Nominatim;
using AwqatSalaat.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;
using System;

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

        private void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            // change selection when collapsed to hide the transition from previous tab to general tab
            if (Visibility == Visibility.Collapsed)
            {
                nav.SelectedItem = generalTab;
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
