using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for WidgetSummary.xaml
    /// </summary>
    public partial class WidgetSummary : UserControl
    {
        private bool shouldBeCompactHorizontally;
        private bool isPlayingSound;
        private DisplayMode currentDisplayMode = DisplayMode.Default;
        private readonly MediaPlayer mediaPlayer = new MediaPlayer();

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

            mediaPlayer.MediaEnded += (_, __) => mediaPlayer.Position = TimeSpan.Zero;
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
            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;

            mediaPlayer.Close();
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

        private void ViewModel_NearNotificationStarted()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (mediaPlayer.Source != null)
                {
                    mediaPlayer.Position = TimeSpan.Zero;
                    mediaPlayer.Play();
                    isPlayingSound = true;
                }
            }));
        }

        private void ViewModel_NearNotificationStopped()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                mediaPlayer.Stop();
                isPlayingSound = false;
            }));
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

            if (!string.Equals(mediaPlayer.Source?.AbsolutePath, filePath, StringComparison.InvariantCultureIgnoreCase))
            {
                mediaPlayer.Close();

                if (!string.IsNullOrEmpty(filePath))
                {
                    mediaPlayer.Open(new Uri(filePath));
                }
                else
                {
                    isPlayingSound = false;
                }

                if (isPlayingSound)
                {
                    mediaPlayer.Play();
                }
                else if (ViewModel.DisplayedTime?.State == PrayerTimeState.Near)
                {
                    ViewModel_NearNotificationStarted();
                }
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
