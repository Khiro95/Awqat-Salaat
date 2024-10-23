using AwqatSalaat.Helpers;
using AwqatSalaat.Media;
using AwqatSalaat.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for WidgetSummary.xaml
    /// </summary>
    public partial class WidgetSummary : UserControl
    {
        private const string NearNotificationTag = "NearNotification";
        private const string AdhanSoundTag = "Adhan";

        private bool shouldBeCompactHorizontally;
        private DisplayMode currentDisplayMode = DisplayMode.Default;
        private AudioPlayerSession currentAudioSession;

        private WidgetViewModel ViewModel => DataContext as WidgetViewModel;

        public static DependencyProperty PanelPlacementProperty = DependencyProperty.Register(
            nameof(PanelPlacement),
            typeof(PlacementMode),
            typeof(WidgetSummary),
            new FrameworkPropertyMetadata(PlacementMode.Bottom));

        public static DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(WidgetSummary),
            new FrameworkPropertyMetadata(SystemInfos.IsTaskBarHorizontal() ? Orientation.Horizontal : Orientation.Vertical, propertyChangedCallback: OnOrientationChanged));

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var widget = (WidgetSummary)d;
            widget.UpdateDisplayMode();
        }

        public PlacementMode PanelPlacement
        {
            get => (PlacementMode)GetValue(PanelPlacementProperty);
            set => SetValue(PanelPlacementProperty, value);
        }

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public bool RemovePopupBorderAtPlacement
        {
            get => popup?.RemoveBorderAtPlacement ?? false;
            set
            {
                if (popup != null)
                {
                    popup.RemoveBorderAtPlacement = value;
                }
            }
        }

        public event Action<DisplayMode> DisplayModeChanged;

        public WidgetSummary()
        {
            InitializeComponent();

            popup.Opened += (_, __) =>
            {
                var src = HwndSource.FromVisual(popup.Child) as HwndSource;
                (src.RootVisual as UIElement)?.Focus();
            };
            popup.Closed += (_, __) =>
            {
                if (ViewModel.WidgetSettings.IsOpen && ViewModel.WidgetSettings.Settings.IsConfigured)
                {
                    ViewModel.WidgetSettings.Cancel.Execute(null);
                }
            };
            popup.KeyDown += (_, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    toggle.IsChecked = false;
                    e.Handled = true;
                }
            };
            this.Loaded += (_, __) => UpdateDisplayMode();
            this.Unloaded += WidgetSummary_Unloaded;
            ViewModel.WidgetSettings.Settings.PropertyChanged += Settings_PropertyChanged;
            ViewModel.WidgetSettings.Updated += WidgetSettings_Updated;
            ViewModel.NearNotificationStarted += ViewModel_NearNotificationStarted;
            ViewModel.NearNotificationStopped += ViewModel_NearNotificationStopped;
            ViewModel.AdhanRequested += ViewModel_AdhanRequested;
            LocaleManager.Default.CurrentChanged += LocaleManager_CurrentChanged;

            UpdateDirection();
            UpdateCountdownState();
            UpdateNotificationSound();
        }

        private void WidgetSummary_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.WidgetSettings.Settings.PropertyChanged -= Settings_PropertyChanged;
            ViewModel.WidgetSettings.Updated -= WidgetSettings_Updated;
            ViewModel.NearNotificationStarted -= ViewModel_NearNotificationStarted;
            ViewModel.NearNotificationStopped -= ViewModel_NearNotificationStopped;
            ViewModel.AdhanRequested -= ViewModel_AdhanRequested;
            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;

            currentAudioSession?.End();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Properties.Settings.ShowCountdown) || e.PropertyName == nameof(Properties.Settings.UseCompactMode))
            {
                UpdateDisplayMode();
            }
            else if (e.PropertyName == nameof(Properties.Settings.ShowSeconds))
            {
                UpdateCountdownState(); 
            }
        }

        private void ViewModel_AdhanRequested(bool isFajrTime)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var file = isFajrTime
                        ? ViewModel.WidgetSettings.Settings.AdhanFajrSoundFilePath
                        : ViewModel.WidgetSettings.Settings.AdhanSoundFilePath;
                var session = new AudioPlayerSession(file, tag: AdhanSoundTag);
                PlaySound(session);
            }));
        }

        private void ViewModel_NearNotificationStarted()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var file = ViewModel.WidgetSettings.Settings.NotificationSoundFilePath;
                var session = new AudioPlayerSession(file, tag: NearNotificationTag, loop: true);
                PlaySound(session);
            }));
        }

        private void ViewModel_NearNotificationStopped()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                currentAudioSession?.End();
            }));
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

        private void WidgetSettings_Updated(bool obj)
        {
            UpdateNotificationSound();
        }

        private void LocaleManager_CurrentChanged(object sender, EventArgs e)
        {
            UpdateDirection();
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            bool needCompactH = arrangeBounds.Height < grid.MaxHeight && Orientation == Orientation.Horizontal;

            if (shouldBeCompactHorizontally != needCompactH)
            {
                shouldBeCompactHorizontally = needCompactH;
                Dispatcher.BeginInvoke(new Action(UpdateDisplayMode), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }

            return base.ArrangeOverride(arrangeBounds);
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
            else if (Orientation == Orientation.Vertical || ViewModel.WidgetSettings.Settings.UseCompactMode)
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

        private void UpdateCountdownState()
        {
            if (ViewModel.WidgetSettings.Settings.ShowSeconds)
            {
                VisualStateManager.GoToState(this, "WithSeconds", false);
            }
            else
            {
                VisualStateManager.GoToState(this, "WithoutSeconds", false);
            }
        }

        private void UpdateDirection()
        {
            this.FlowDirection = Properties.Resources.Culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(Properties.Resources.Culture.IetfLanguageTag);
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
