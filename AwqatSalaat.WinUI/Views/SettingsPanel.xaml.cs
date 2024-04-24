using AwqatSalaat.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    public sealed partial class SettingsPanel : UserControl
    {
        private WidgetSettingsViewModel ViewModel => DataContext as WidgetSettingsViewModel;

        public SettingsPanel()
        {
            this.InitializeComponent();
            this.RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityChanged);
        }

        private void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
        {
            // change selection when collapsed to hide the transition from previous tab to general tab
            if (Visibility == Visibility.Collapsed)
            {
                nav.SelectedItem = generalTab;
            }
        }
    }
}
