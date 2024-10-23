using AwqatSalaat.Media;
using System.Windows;
using System.Windows.Controls;
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
            AudioPlayer.Started += AudioPlayer_Started;
            AudioPlayer.Stopped += AudioPlayer_Stopped;

            stopSoundButton.Visibility = AudioPlayer.CurrentSession is null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void WidgetPanel_Unloaded(object sender, RoutedEventArgs e)
        {
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

        private void LocationPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // if Height changed then the size has changed because of orientation change
            if (e.HeightChanged && e.PreviousSize.Height > 0)
            {
                return;
            }

            StackPanel stackPanel = (StackPanel)sender;

            if (stackPanel.Orientation == Orientation.Horizontal && e.NewSize.Width > 200)
            {
                stackPanel.Orientation = Orientation.Vertical;
            }
            else
            {
                stackPanel.Orientation = Orientation.Horizontal;
            }
        }

        private void StopSound_Click(object sender, RoutedEventArgs e)
        {
            AudioPlayer.CurrentSession?.End();
        }
    }
}