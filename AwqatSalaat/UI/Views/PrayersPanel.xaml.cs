using Serilog;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for PrayersPanel.xaml
    /// </summary>
    public partial class PrayersPanel : UserControl
    {
        public static readonly DependencyProperty DesiredHeightProperty = DependencyProperty.Register(
            nameof(DesiredHeight),
            typeof(double),
            typeof(PrayersPanel),
            new PropertyMetadata(250.0));

        public double DesiredHeight { get => (double)GetValue(DesiredHeightProperty); set => SetValue(DesiredHeightProperty, value); }

        public PrayersPanel()
        {
            LayoutUpdated += PrayersPanel_LayoutUpdated;
            InitializeComponent();
        }

        private void PrayersPanel_LayoutUpdated(object sender, object e)
        {
            if (DesiredSize.Height > 0)
            {
                DesiredHeight = DesiredSize.Height;
            }
        }

        private void LocationPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.Information("Location panel's size changed");
            StackPanel stackPanel = (StackPanel)sender;

            if (stackPanel.Children.OfType<UIElement>().Sum(c => c.DesiredSize.Width) > stackPanel.MaxWidth)
            {
                stackPanel.Orientation = Orientation.Vertical;
            }
            else
            {
                stackPanel.Orientation = Orientation.Horizontal;
            }
        }
    }
}
