using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    public sealed partial class PanelAlt : PrayersPanel
    {
        private WidgetViewModel ViewModel => (DataContext ?? cachedDataContext) as WidgetViewModel;

        public PanelAlt()
        {
            InitializeComponent();
            Unloaded += PanelAlt_Unloaded;
        }

        private void PanelAlt_Unloaded(object sender, RoutedEventArgs e)
        {
            Bindings.StopTracking();
        }

        private void LocationPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.Information("Location panel's size changed");
            StackPanel stackPanel = (StackPanel)sender;

            if (stackPanel.Orientation == Orientation.Horizontal)
            {
                comma.Margin = new Thickness(0, 0, 4, 0);
            }
            else
            {
                comma.Margin = new Thickness();
            }
        }
    }
}
