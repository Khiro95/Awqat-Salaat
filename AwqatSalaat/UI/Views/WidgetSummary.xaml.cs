using AwqatSalaat.UI.ViewModels;
using System.Windows.Controls;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for WidgetSummary.xaml
    /// </summary>
    public partial class WidgetSummary : UserControl
    {
        private WidgetViewModel ViewModel => DataContext as WidgetViewModel;

        public WidgetSummary()
        {
            InitializeComponent();
            popup.Closed += (_, __) =>
            {
                if (ViewModel.WidgetSettings.IsOpen && ViewModel.WidgetSettings.Settings.IsConfigured)
                {
                    ViewModel.WidgetSettings.Cancel.Execute(null);
                }
            };
        }
    }
}
