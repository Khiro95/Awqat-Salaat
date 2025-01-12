using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Controls;
using AwqatSalaat.WinUI.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Windows.Foundation;
using Windows.UI.ViewManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    public sealed partial class WidgetSummary : UserControl
    {
        private const string NearNotificationTag = "NearNotification";
        private const string AdhanSoundTag = "Adhan";

        private static readonly UISettings uiSettings = new UISettings();

#if DEBUG
        public static WidgetSummary Current { get; private set; }
#endif

        private bool shouldBeCompactHorizontally;
        private DisplayMode currentDisplayMode = DisplayMode.Default;
        private AudioPlayerSession currentAudioSession;

        private WidgetViewModel ViewModel => DataContext as WidgetViewModel;

        public event Action<DisplayMode> DisplayModeChanged;

        public WidgetSummary()
        {
            this.InitializeComponent();
#if DEBUG
            Current = this;
            Properties.Settings.Default.IsConfigured = false;
#endif
            this.Loaded += WidgetSummary_Loaded;
            this.Unloaded += WidgetSummary_Unloaded;
            ViewModel.WidgetSettings.Updated += WidgetSettings_Updated;
            ViewModel.WidgetSettings.Settings.PropertyChanged += Settings_PropertyChanged;
            ViewModel.NearNotificationStarted += ViewModel_NearNotificationStarted;
            ViewModel.NearNotificationStopped += ViewModel_NearNotificationStopped;
            ViewModel.AdhanRequested += ViewModel_AdhanRequested;
            LocaleManager.Default.CurrentChanged += LocaleManager_CurrentChanged;
            uiSettings.ColorValuesChanged += UISettings_ColorValuesChanged;

            UpdateDirection();
            UpdateNotificationSound();
        }

        private void UISettings_ColorValuesChanged(UISettings sender, object args)
        {
            DispatcherQueue.TryEnqueue(UpdateThemes);
        }

        private void UpdateThemes()
        {
            if (SystemInfos.IsAccentColorOnTaskBar() == true)
            {
                // When accent color is used, we have to figure out the theme based on the color
                var accent = uiSettings.GetColorValue(UIColorType.Accent);
                bool colorIsDark = (5 * accent.G + 2 * accent.R + accent.B) <= 8 * 200;
                this.RequestedTheme = colorIsDark ? ElementTheme.Dark : ElementTheme.Light;
            }
            else
            {
                // We use "system theme" instead of "apps theme" because the taskbar uses the former
                this.RequestedTheme = SystemInfos.IsLightThemeUsed() == true ? ElementTheme.Light : ElementTheme.Dark;
            }

            if (Parent is FrameworkElement parent)
            {
                // The flyouts are independent of the taskbar so they should respect "apps theme" (we get it from the parent)
                var theme = parent.ActualTheme == this.ActualTheme ? ElementTheme.Default : parent.ActualTheme;
                flyout.SetPresenterTheme(theme);
                (btngrid.ContextFlyout as CustomizedMenuFlyout)?.SetPresenterTheme(theme);
            }
        }

        private void WidgetSummary_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateThemes();
            UpdateDisplayMode();
        }

        private void WidgetSummary_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.WidgetSettings.Settings.PropertyChanged -= Settings_PropertyChanged;
            ViewModel.WidgetSettings.Updated -= WidgetSettings_Updated;
            ViewModel.NearNotificationStarted -= ViewModel_NearNotificationStarted;
            ViewModel.NearNotificationStopped -= ViewModel_NearNotificationStopped;
            ViewModel.AdhanRequested -= ViewModel_AdhanRequested;
            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;
            uiSettings.ColorValuesChanged -= UISettings_ColorValuesChanged;

            currentAudioSession?.End();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(Properties.Settings.ShowCountdown) or nameof(Properties.Settings.UseCompactMode))
            {
                UpdateDisplayMode();
            }
        }

        private void ViewModel_AdhanRequested(bool isFajrTime)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var file = isFajrTime
                        ? ViewModel.WidgetSettings.Settings.AdhanFajrSoundFilePath
                        : ViewModel.WidgetSettings.Settings.AdhanSoundFilePath;
                var session = new AudioPlayerSession
                {
                    File = file,
                    Tag = AdhanSoundTag,
                };
                PlaySound(session);
            });
        }

        private void ViewModel_NearNotificationStarted()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var file = ViewModel.WidgetSettings.Settings.NotificationSoundFilePath;
                var session = new AudioPlayerSession
                {
                    File = file,
                    Loop = true,
                    Tag = NearNotificationTag,
                };
                PlaySound(session);
            });
        }

        private void ViewModel_NearNotificationStopped()
        {
            DispatcherQueue.TryEnqueue(() => currentAudioSession?.End());
        }

        private void PlaySound(AudioPlayerSession session)
        {
            bool started = AudioPlayer.Play(session);

            if (started)
            {
                currentAudioSession = session;
                session.Ended += AudioSession_Ended;
            }
        }

        private void AudioSession_Ended()
        {
            currentAudioSession.Ended -= AudioSession_Ended;
            currentAudioSession = null;
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

            // Do we have a running notification sound?
            if (currentAudioSession?.Tag == NearNotificationTag)
            {
                // Yes, we have

                // Did the file path change?
                if (!string.Equals(currentAudioSession.File, filePath, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Yes,

                    // Stop current sound
                    currentAudioSession.End();
                }
                else
                {
                    return;
                }
            }

            // Start playing if we have notification
            if (!string.IsNullOrEmpty(filePath) && ViewModel.DisplayedTime?.State == PrayerTimeState.Near)
            {
                ViewModel_NearNotificationStarted();
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
