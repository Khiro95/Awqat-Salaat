using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    public sealed partial class WidgetPanel : UserControl
    {
        private WidgetViewModel ViewModel => DataContext as WidgetViewModel;

        public WidgetPanel()
        {
            this.InitializeComponent();
            Loaded += WidgetPanel_Loaded;
            Unloaded += WidgetPanel_Unloaded;
#if DEBUG
            themeBtn.Click += themeBtn_Click;
#else
            commandBar.PrimaryCommands.Remove(themeBtn);
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
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () => stopSoundButton.Visibility = Visibility.Collapsed);
        }

#if DEBUG
        private void themeBtn_Click(object sender, RoutedEventArgs e)
        {
            // cannot change app theme at runtime so we change popup's theme only
            var popups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot);

            if (popups.Count != 1)
            {
                return;
            }

            var popup = popups[0];

            if (popup is not null)
            {
                if (popup.RequestedTheme == ElementTheme.Dark)
                    popup.RequestedTheme = ElementTheme.Light;
                else
                    popup.RequestedTheme = ElementTheme.Dark;

                WidgetSummary.Current.RequestedTheme = popup.RequestedTheme;
            }
        }
#endif
        private void StopSound_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Stop Sound");
            AudioPlayer.CurrentSession?.End();
        }

        private void MoreInfo_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on More Info");
            MoreInfoWindow.Open();
        }

        private void LocationPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Log.Information("Location panel's size changed");
            // if Height changed then the size has changed because of orientation change
            if (e.PreviousSize.Height != e.NewSize.Height && e.PreviousSize.Height > 0)
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

        private void ErrorBounds_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            errorMessage.MaxHeight = e.NewSize.Height;
        }
    }
}
