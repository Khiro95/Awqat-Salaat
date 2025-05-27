using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AwqatSalaat.WinUI.Controls
{
    public class PrayersPanelContainer : Control
    {
        public static readonly DependencyProperty PanelProperty = DependencyProperty.Register(
            nameof(Panel),
            typeof(PrayersPanel),
            typeof(PrayersPanelContainer),
            new PropertyMetadata(null));

        public PrayersPanel Panel { get => (PrayersPanel)GetValue(PanelProperty); set => SetValue(PanelProperty, value); }

        public static readonly DependencyProperty PanelVisibilityProperty = DependencyProperty.Register(
            nameof(PanelVisibility),
            typeof(Visibility),
            typeof(PrayersPanelContainer),
            new PropertyMetadata(Visibility.Visible));

        public Visibility PanelVisibility { get => (Visibility)GetValue(PanelVisibilityProperty); set => SetValue(PanelVisibilityProperty, value); }

        public PrayersPanelContainer()
        {
            DefaultStyleKey = typeof(PrayersPanelContainer);
        }
    }
}
