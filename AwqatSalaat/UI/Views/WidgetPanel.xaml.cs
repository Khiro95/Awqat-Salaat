using AwqatSalaat.Media;
using AwqatSalaat.UI.Controls;
using Serilog;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for WidgetPanel.xaml
    /// </summary>
    public partial class WidgetPanel : UserControl
    {
        public WidgetPanel()
        {
            InitializeComponent();
            Loaded += WidgetPanel_Loaded;
            Unloaded += WidgetPanel_Unloaded;
#if DEBUG
            themeBtn.Click += (_, __) => ThemeManager.ToggleTheme();
#else
            var parent = themeBtn.Parent ?? VisualTreeHelper.GetParent(themeBtn);
            if (parent != null)
            {
                Utils.RemoveFromParent(themeBtn, parent, out _);
            }
            else
            {
                themeBtn.Visibility = Visibility.Collapsed;
            }
#endif
        }

        private void WidgetPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Widget panel loaded");
            AudioPlayer.Started += AudioPlayer_Started;
            AudioPlayer.Stopped += AudioPlayer_Stopped;

            stopSoundButton.Visibility = AudioPlayer.CurrentSession is null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void WidgetPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Widget panel unloaded");
            AudioPlayer.Started -= AudioPlayer_Started;
            AudioPlayer.Stopped -= AudioPlayer_Stopped;
        }

        private void AudioPlayer_Started()
        {
            stopSoundButton.Visibility = Visibility.Visible;
        }

        private void AudioPlayer_Stopped()
        {
            stopSoundButton.Visibility = Visibility.Collapsed;
        }

        private void StopSound_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Stop Sound");
            AudioPlayer.CurrentSession?.End();
        }

        private void MoreInfo_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on More Info");
            var hwndSource = HwndSource.FromVisual(this);

            if (hwndSource?.RootVisual is FrameworkElement fe && fe.Parent is AcrylicPopup popup)
            {
                popup.SetCurrentValue(AcrylicPopup.IsOpenProperty, false);
            }

            MoreInfoWindow.Open();
        }
    }
}