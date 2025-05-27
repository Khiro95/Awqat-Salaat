using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AwqatSalaat.WinUI.Controls
{
    public class PrayersPanel : UserControl
    {
        public static readonly DependencyProperty DesiredHeightProperty = DependencyProperty.Register(
            nameof(DesiredHeight),
            typeof(double),
            typeof(PrayersPanel),
            new PropertyMetadata(250.0));

        public double DesiredHeight { get => (double)GetValue(DesiredHeightProperty); set => SetValue(DesiredHeightProperty, value); }

        protected object cachedDataContext;

        public PrayersPanel()
        {
            LayoutUpdated += PrayersPanel_LayoutUpdated;
            DataContextChanged += PrayersPanel_DataContextChanged;
            Unloaded += PrayersPanel_Unloaded;
        }

        private void PrayersPanel_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            cachedDataContext ??= args.NewValue;
        }

        private void PrayersPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            cachedDataContext = null;
        }

        private void PrayersPanel_LayoutUpdated(object sender, object e)
        {
            if (DesiredSize.Height > 0)
            {
                DesiredHeight = DesiredSize.Height;
            }
        }
    }
}
