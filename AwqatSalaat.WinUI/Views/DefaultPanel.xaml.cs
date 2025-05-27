using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Controls;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    public sealed partial class DefaultPanel : PrayersPanel
    {
        private WidgetViewModel ViewModel => (DataContext ?? cachedDataContext) as WidgetViewModel;

        public DefaultPanel()
        {
            InitializeComponent();
            Unloaded += DefaultPanel_Unloaded;
        }

        private void DefaultPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Bindings.StopTracking();
        }
    }
}
