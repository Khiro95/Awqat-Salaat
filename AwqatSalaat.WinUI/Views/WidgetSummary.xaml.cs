using AwqatSalaat.Extensions;
using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Helpers;
using AwqatSalaat.WinUI.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Serilog;
using System;
using System.Linq;
using Windows.Foundation;
#if PACKAGED
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
#endif

namespace AwqatSalaat.WinUI.Views
{
    public sealed partial class WidgetSummary : UserControl
    {
        private const string NearNotificationTag = "NearNotification";
        private const string AdhanSoundTag = "Adhan";

#if DEBUG
        public static WidgetSummary Current { get; private set; }
#endif

        public static readonly DependencyProperty ElementsAlignmentProperty = DependencyProperty.Register(
            "ElementsAlignment",
            typeof(HorizontalAlignment),
            typeof(WidgetSummary),
            new PropertyMetadata(HorizontalAlignment.Center));

        public HorizontalAlignment ElementsAlignment
        {
            get => (HorizontalAlignment)GetValue(ElementsAlignmentProperty);
            set => SetValue(ElementsAlignmentProperty, value);
        }

        public static readonly DependencyProperty IsVerticalTaskbarProperty = DependencyProperty.Register(
            "IsVerticalTaskbar",
            typeof(bool),
            typeof(WidgetSummary),
            new PropertyMetadata(false, (d, e) => (d as WidgetSummary)?.UpdateDisplayMode()));

        /// <summary>True when the taskbar is docked to the left/right edge: the widget then shows only the prayer time over the countdown.</summary>
        public bool IsVerticalTaskbar
        {
            get => (bool)GetValue(IsVerticalTaskbarProperty);
            private set => SetValue(IsVerticalTaskbarProperty, value);
        }

        private FlyoutPlacementMode defaultFlyoutPlacement;
        private bool shouldBeCompactHorizontally;
        private DisplayMode currentDisplayMode = DisplayMode.Default;
        private AudioPlayerSession currentAudioSession;
#if PACKAGED
        private DispatcherTimer lockScreenTimer;
#endif

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
            ViewModel.WidgetSettings.Realtime.PropertyChanged += Settings_PropertyChanged;
            ViewModel.NearNotificationStarted += ViewModel_NearNotificationStarted;
            ViewModel.NearNotificationStopped += ViewModel_NearNotificationStopped;
            ViewModel.PrayerTimeEntered += ViewModel_PrayerTimeEntered;
            LocaleManager.Default.CurrentChanged += LocaleManager_CurrentChanged;
            ThemeHelper.ThemeChanged += ThemeHelper_ThemeChanged;
            Notification.NotificationManager.ShowWidgetRequested += NotificationManager_ShowWidgetRequested;
            Notification.NotificationManager.DismissReminderRequested += NotificationManager_DismissReminderRequested;
            Notification.NotificationManager.StopReminderSoundRequested += NotificationManager_StopReminderSoundRequested;
            Notification.NotificationManager.StopAdhanRequested += NotificationManager_StopAdhanRequested;
            DisplayHelper.DisplayChanged += DisplayHelper_DisplayChanged;

            defaultFlyoutPlacement = flyout.Placement;

            UpdateDirection();
            UpdateNotificationSound();
        }

        public void SetTaskbarOrientation(bool vertical, bool onRightEdge)
        {
            // Open the panel towards the inside of the screen when the taskbar is on a side
            flyout.Placement = !vertical
                ? defaultFlyoutPlacement
                : onRightEdge ? FlyoutPlacementMode.Left : FlyoutPlacementMode.Right;
            IsVerticalTaskbar = vertical;
        }

        public static string FormatCountdown(string format, TimeSpan countdown, bool vertical)
        {
            // Vertical taskbar: 52:34 (or 1:52:34 when an hour or more is left) instead of 00:52:34
            if (vertical && format is not null && format.Contains("ss"))
            {
                return countdown.Duration().TotalHours >= 1
                    ? countdown.ToString(@"h\:mm\:ss")
                    : countdown.ToString(@"mm\:ss");
            }

            return string.Format(format, countdown);
        }

        private void DisplayHelper_DisplayChanged(object sender, DisplayChangedEventArgs e)
        {
            if (e.Reason
                is DisplayChangedReason.Connected
                or DisplayChangedReason.Disconnected
                or DisplayChangedReason.PrimaryDisplay
                or DisplayChangedReason.PrimaryDuplicated
                or DisplayChangedReason.PrimaryDeduplicated)
            {
                DispatcherQueue.TryEnqueue(UpdateDisplayMenu);
            }
        }

        private void NotificationManager_StopAdhanRequested()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (currentAudioSession?.Tag == AdhanSoundTag)
                {
                    Log.Information("Stopping adhan sound after interacting with toast notification");
                    currentAudioSession.End();
                }
            });
        }

        private void NotificationManager_StopReminderSoundRequested()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (currentAudioSession?.Tag == NearNotificationTag)
                {
                    Log.Information("Stopping reminder sound after interacting with toast notification");
                    currentAudioSession.End();
                }
            });
        }

        private void NotificationManager_DismissReminderRequested()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Log.Information("Dismissing reminder after interacting with toast notification");
                ViewModel.DisplayedTime.DismissNotification.Execute(null);
            });
        }

        private void NotificationManager_ShowWidgetRequested()
        {
            Log.Information("Showing widget's panel after clicking on toast notification");
            //DispatcherQueue.TryEnqueue(() => ToggleButton_Checked(toggle, null));
            DispatcherQueue.TryEnqueue(() => toggle.IsChecked = true);
        }

        private void ThemeHelper_ThemeChanged()
        {
            DispatcherQueue.TryEnqueue(UpdateTheme);
        }

        private void UpdateTheme()
        {
            this.RequestedTheme = ThemeHelper.ButtonTheme;
            Log.Information($"Updated theme: {this.RequestedTheme}");
        }

        private void ReloadThemes()
        {
            ThemeHelper.ReloadElementTheme(this, this.RequestedTheme);

            var flyoutPresenter = flyout.GetPresenter();

            if (flyoutPresenter?.RequestedTheme is ElementTheme currentTheme)
            {
                ThemeHelper.ReloadElementTheme(flyoutPresenter, currentTheme);
            }
        }
        
        private void WidgetSummary_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Widget summary loaded");
            UpdateTheme();
            UpdateDisplayMode();
            UpdateDisplayMenu();
#if PACKAGED
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, InvalidateLockScreenTimer);
#endif
        }

        private void WidgetSummary_Unloaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Widget summary unloaded");
            ViewModel.WidgetSettings.Realtime.PropertyChanged -= Settings_PropertyChanged;
            ViewModel.WidgetSettings.Updated -= WidgetSettings_Updated;
            ViewModel.NearNotificationStarted -= ViewModel_NearNotificationStarted;
            ViewModel.NearNotificationStopped -= ViewModel_NearNotificationStopped;
            ViewModel.PrayerTimeEntered -= ViewModel_PrayerTimeEntered;
            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;
            ThemeHelper.ThemeChanged -= ThemeHelper_ThemeChanged;
            Notification.NotificationManager.ShowWidgetRequested -= NotificationManager_ShowWidgetRequested;
            Notification.NotificationManager.DismissReminderRequested -= NotificationManager_DismissReminderRequested;
            Notification.NotificationManager.StopReminderSoundRequested -= NotificationManager_StopReminderSoundRequested;
            Notification.NotificationManager.StopAdhanRequested -= NotificationManager_StopAdhanRequested;
            DisplayHelper.DisplayChanged -= DisplayHelper_DisplayChanged;

            currentAudioSession?.End();

#if PACKAGED
            try
            {
                DestroyLockScreenTimer();
            }
            catch (Exception ex)
            {
                Log.Warning($"An error occured while destroying lock screen timer in Unloaded handler: {ex?.Message}");
            }
#endif
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(Properties.Settings.ShowCountdown) or nameof(Properties.Settings.UseCompactMode))
            {
                flyout?.DisableLightDismissTemporarily();
                UpdateDisplayMode();
            }
            else if (e.PropertyName is nameof(Properties.Settings.AutoAlignment) or nameof(Properties.Settings.DisplayLanguage))
            {
                TaskBarManager.InvalidateWidgetElementsAlignment(ViewModel.WidgetSettings.Realtime.AutoAlignment);
            }
            else if (e.PropertyName ==  nameof(Properties.Settings.ThemeAccent))
            {
                ReloadThemes();
            }
            else if (e.PropertyName == nameof(Properties.Settings.ShortTimePattern))
            {
                Bindings.Update();
            }
        }

        private void ViewModel_PrayerTimeEntered(PrayerTimeViewModel prayerTime)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var config = ViewModel.WidgetSettings.Settings.GetPrayerConfig(prayerTime.Key);
                var file = config.EffectiveAdhanFile();
                var adhanRequested = !string.IsNullOrEmpty(file);

                if (adhanRequested)
                {
                    Log.Information("Adhan requested for " + prayerTime.Key);
                    var session = new AudioPlayerSession
                    {
                        File = file,
                        Tag = AdhanSoundTag,
                    };
                    PlaySound(session); 
                }

                if (prayerTime.Key != nameof(Data.PrayerTimes.Shuruq) && ViewModel.WidgetSettings.Settings.EnablePrayerTimeToast)
                {
                    Notification.NotificationManager.SendTimeEnteredToast(prayerTime.Key, adhanRequested);
                }
            });
        }

        private void ViewModel_NearNotificationStarted()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                bool repeat = ViewModel.WidgetSettings.Settings.RepeatNotificationSound;
                var file = ViewModel.WidgetSettings.Settings.NotificationSoundFilePath;
                var session = new AudioPlayerSession
                {
                    File = file,
                    Loop = repeat,
                    Tag = NearNotificationTag,
                };
                PlaySound(session);

                if (ViewModel.WidgetSettings.Settings.EnableReminderToast)
                {
                    var prayer = ViewModel.DisplayedTime.Key;
                    var time = ViewModel.DisplayedTime.Time;
                    var playingSound = currentAudioSession is not null;
                    Notification.NotificationManager.SendReminderToast(prayer, time, playingSound);
                }
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
#if PACKAGED
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, InvalidateLockScreenTimer);
#endif
        }

        private void LocaleManager_CurrentChanged(object sender, EventArgs e)
        {
            UpdateDirection();
        }

        private void Flyout_Opened(object sender, object e)
        {
            Log.Information("Flyout opened");
            //flyoutContent.Focus(FocusState.Programmatic);
        }

        private void Flyout_Closed(object sender, object e)
        {
            Log.Information("Flyout closed");

            toggle.IsChecked = false;

            if (ViewModel.WidgetSettings.IsOpen && ViewModel.WidgetSettings.Settings.IsConfigured)
            {
                ViewModel.WidgetSettings.Cancel.Execute(null);
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

            if (IsVerticalTaskbar)
            {
                displayMode = ViewModel.WidgetSettings.Realtime.ShowCountdown
                    ? DisplayMode.Vertical
                    : DisplayMode.VerticalNoCountdown;
            }
            else if (shouldBeCompactHorizontally)
            {
                displayMode = ViewModel.WidgetSettings.Realtime.ShowCountdown
                    ? DisplayMode.CompactHorizontal
                    : DisplayMode.CompactHorizontalNoCountdown;
            }
            else if (!ViewModel.WidgetSettings.Realtime.ShowCountdown)
            {
                displayMode = DisplayMode.CompactNoCountdown;
            }
            else if (ViewModel.WidgetSettings.Realtime.UseCompactMode)
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
            var dir = LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            btngrid.FlowDirection = dir;
            flyoutContent.FlowDirection = dir;
        }

        private void UpdateDisplayMenu()
        {
            Log.Information("Updating Display context-menu");
            int count = 0;
            var primaryItem = displayFlyoutSubItem.Items[0] as RadioMenuFlyoutItem;

            // Using displayFlyoutSubItem.Items.Clear() then calling displayFlyoutSubItem.Items.Add(primaryItem) causes crash
            // So primaryItem need to stay in the list all the time
            foreach (var item in displayFlyoutSubItem.Items.ToArray())
            {
                if (ReferenceEquals(item, primaryItem))
                    continue;

                displayFlyoutSubItem.Items.Remove(item);
            }

            primaryItem.IsChecked = ViewModel.WidgetSettings.Settings.Display == "PRIMARY";

            foreach (var display in DisplayHelper.AvailableDisplays)
            {
                var item = new RadioMenuFlyoutItem()
                {
                    Text = display.Summary,
                    GroupName = "DisplayGroup",
                    Command = TaskBarManager.SetDisplay,
                    CommandParameter = display.Display.DevicePath,
                    IsChecked = ViewModel.WidgetSettings.Settings.Display == display.Display.DevicePath,
                };
                displayFlyoutSubItem.Items.Add(item);
                count++;
            }

            Log.Information($"Added {count} sub-item's");

            foreach (var item in displayFlyoutSubItem.Items.OfType<RadioMenuFlyoutItem>())
            {
                // avoid interacting with an already selected option
                item.IsHitTestVisible = item.IsTabStop = !item.IsChecked;
            }

            displayFlyoutSubItem.Visibility = count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

#if PACKAGED
        private void InvalidateLockScreenTimer()
        {
            try
            {
                Log.Information("Invalidating lock screen timer");

                if (ViewModel.WidgetSettings.Settings.ShowContentOnLockScreen)
                {
                    if (lockScreenTimer is null)
                    {
                        Log.Information("Creating a timer for lock screen");
                        lockScreenTimer = new DispatcherTimer();
                        lockScreenTimer.Interval = TimeSpan.FromMinutes(1);
                        lockScreenTimer.Tick += LockScreenTimer_Tick;
                        lockScreenTimer.Start();
                    }

                    UpdateLockScreen();
                }
                else
                {
                    DestroyLockScreenTimer();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"An exception was thrown during lock screen timer invalidation: {ex?.Message}");
            }
        }

        private void DestroyLockScreenTimer()
        {
            if (lockScreenTimer is not null)
            {
                Log.Information("Destroying the lock screen timer");
                lockScreenTimer.Stop();
                lockScreenTimer.Tick -= LockScreenTimer_Tick;
                lockScreenTimer = null;
            }

            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
        }

        private void LockScreenTimer_Tick(object sender, object e)
        {
            UpdateLockScreen();
        }

        private void UpdateLockScreen()
        {
            try
            {
                if (!ViewModel.WidgetSettings.Settings.IsConfigured)
                {
                    return;
                }

                if (ViewModel.HasError)
                {
                    TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                    return;
                }

                Log.Information("Updating the content on the lock screen");

                string line3 = (afterTB.Visibility == Visibility.Visible ? afterTB : sinceTB).Text + " " + countdownTB.Text;
                string content = $"<tile><visual version=\"4\"><binding template=\"TileWide\">" +
                    $"<text id=\"1\">{prayerTB.Text}</text>" +
                    $"<text id=\"2\">{timeTB.Text}</text>" +
                    $"<text id=\"3\">{line3}</text>" +
                    $"</binding></visual></tile>";
                var tileXml = new XmlDocument();
                tileXml.LoadXml(content);
                var notification = new TileNotification(tileXml);
                TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"An error occured while updating lock screen: {ex?.Message}");
            }
        }
#endif
    }

    public enum DisplayMode
    {
        Default,
        Compact,
        CompactNoCountdown,
        CompactHorizontal,
        CompactHorizontalNoCountdown,
        Vertical,
        VerticalNoCountdown
    }
}
