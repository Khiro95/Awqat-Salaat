using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    public sealed partial class WidgetSummary : UserControl
    {
#if DEBUG
        public static WidgetSummary Current { get; private set; }
#endif

        private bool shouldBeCompactHorizontally;
        private bool isPlayingSound;
        private DisplayMode currentDisplayMode = DisplayMode.Default;
        private MediaSource mediaSource;
        private readonly MediaPlayer mediaPlayer = new MediaPlayer() { IsLoopingEnabled = true, AutoPlay = true };

        private WidgetViewModel ViewModel => DataContext as WidgetViewModel;

        public event Action<DisplayMode> DisplayModeChanged;

        public WidgetSummary()
        {
            this.InitializeComponent();
#if DEBUG
            Current = this;
            Properties.Settings.Default.IsConfigured = false;
#endif
            this.Loaded += (_, _) => UpdateDisplayMode();
            this.Unloaded += WidgetSummary_Unloaded;
            ViewModel.WidgetSettings.Updated += WidgetSettings_Updated;
            ViewModel.WidgetSettings.Settings.PropertyChanged += Settings_PropertyChanged;
            ViewModel.NearNotificationStarted += ViewModel_NearNotificationStarted;
            ViewModel.NearNotificationStopped += ViewModel_NearNotificationStopped;
            LocaleManager.Default.CurrentChanged += LocaleManager_CurrentChanged;

            UpdateDirection();
            UpdateNotificationSound();
        }

        private void WidgetSummary_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.WidgetSettings.Settings.PropertyChanged -= Settings_PropertyChanged;
            ViewModel.WidgetSettings.Updated -= WidgetSettings_Updated;
            ViewModel.NearNotificationStarted -= ViewModel_NearNotificationStarted;
            ViewModel.NearNotificationStopped -= ViewModel_NearNotificationStopped;
            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;

            mediaPlayer.Dispose();
            mediaSource?.Dispose();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(Properties.Settings.ShowCountdown) or nameof(Properties.Settings.UseCompactMode))
            {
                UpdateDisplayMode();
            }
        }

        private void ViewModel_NearNotificationStarted()
        {
            if (mediaSource?.Uri is not null)
            {
                mediaPlayer.Position = TimeSpan.Zero;
                mediaPlayer.Source = mediaSource;
                isPlayingSound = true;
            }
        }

        private void ViewModel_NearNotificationStopped()
        {
            mediaPlayer.Source = null;
            isPlayingSound = false;
        }

        private void WidgetSettings_Updated(bool apiSettingsUpdated)
        {
            UpdateNotificationSound();
        }

        private void LocaleManager_CurrentChanged(object sender, EventArgs e)
        {
            UpdateDirection();
        }

        private void Flyout_Opened(object sender, object e)
        {
            //flyoutContent.Focus(FocusState.Programmatic);
        }

        private void Flyout_Closed(object sender, object e)
        {
            var customFlyout = (CustomizedFlyout)sender;

            if (!customFlyout.ClosedBecauseOfResize)
            {
                toggle.IsChecked = false;

                if (ViewModel.WidgetSettings.IsOpen && ViewModel.WidgetSettings.Settings.IsConfigured)
                {
                    ViewModel.WidgetSettings.Cancel.Execute(null);
                }
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var needCompactH = finalSize.Height < grid.MaxHeight;

            if (shouldBeCompactHorizontally != needCompactH)
            {
                shouldBeCompactHorizontally = needCompactH;
                DispatcherQueue.TryEnqueue(UpdateDisplayMode);
            }

            return base.ArrangeOverride(finalSize);
        }
        
        private void UpdateDisplayMode()
        {
            if (!this.IsLoaded)
            {
                return;
            }

            DisplayMode displayMode = DisplayMode.Default;

            if (shouldBeCompactHorizontally)
            {
                displayMode = ViewModel.WidgetSettings.Settings.ShowCountdown
                    ? DisplayMode.CompactHorizontal
                    : DisplayMode.CompactHorizontalNoCountdown;
            }
            else if (!ViewModel.WidgetSettings.Settings.ShowCountdown)
            {
                displayMode = DisplayMode.CompactNoCountdown;
            }
            else if (ViewModel.WidgetSettings.Settings.UseCompactMode)
            {
                displayMode = DisplayMode.Compact;
            }

            if (displayMode == currentDisplayMode)
            {
                return;
            }
            
            bool success = VisualStateManager.GoToState(this, displayMode.ToString(), false);

            if (success)
            {
                currentDisplayMode = displayMode;
                DisplayModeChanged?.Invoke(displayMode);
            }
        }

        private void UpdateNotificationSound()
        {
            var filePath = ViewModel.WidgetSettings.Settings.NotificationSoundFilePath;

            if (!string.Equals(mediaSource?.Uri?.AbsolutePath, filePath, StringComparison.InvariantCultureIgnoreCase))
            {
                mediaSource?.Dispose();

                if (!string.IsNullOrEmpty(filePath))
                {
                    mediaSource = MediaSource.CreateFromUri(new Uri(filePath));
                }
                else
                {
                    isPlayingSound = false;
                    mediaSource = null;
                }

                if (isPlayingSound)
                {
                    mediaPlayer.Source = mediaSource;
                }
                else if (ViewModel.DisplayedTime?.State == PrayerTimeState.Near)
                {
                    ViewModel_NearNotificationStarted();
                }
            }
        }

        private void UpdateDirection()
        {
            var dir = Properties.Resources.Culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            btngrid.FlowDirection = dir;
            flyoutContent.FlowDirection = dir;
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }
    }

    public enum DisplayMode
    {
        Default,
        Compact,
        CompactNoCountdown,
        CompactHorizontal,
        CompactHorizontalNoCountdown
    }
}
