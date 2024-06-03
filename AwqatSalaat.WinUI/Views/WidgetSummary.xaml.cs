using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Windows.Foundation;

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
        private DisplayMode currentDisplayMode = DisplayMode.Default;

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
            //widgetPanel.Loaded += WidgetPanel_Loaded;
            ViewModel.WidgetSettings.Updated += WidgetSettings_Updated;
            ViewModel.WidgetSettings.Settings.PropertyChanged += Settings_PropertyChanged;
            LocaleManager.Default.CurrentChanged += (_, _) => UpdateDirection();
            UpdateDirection();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(Properties.Settings.ShowCountdown) or nameof(Properties.Settings.UseCompactMode))
            {
                UpdateDisplayMode();
            }
        }

        private void WidgetSettings_Updated(bool apiSettingsUpdated)
        {
            App.SetLaunchOnWindowsStartup(ViewModel.WidgetSettings.Settings.LaunchOnWindowsStartup);
        }

        private void WidgetPanel_Loaded(object sender, RoutedEventArgs e)
        {
            widgetPanel.Loaded -= WidgetPanel_Loaded;
            var presenter = widgetPanel.Parent as FlyoutPresenter;
            var popup = presenter.Parent as Popup;
            //popup.GettingFocus += (s, a) => a.Cancel = true;
        }

        private void Flyout_Opened(object sender, object e)
        {
            widgetPanel.Focus(FocusState.Programmatic);
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

        private void UpdateDirection()
        {
            var dir = Properties.Resources.Culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            btngrid.FlowDirection = dir;
            widgetPanel.FlowDirection = dir;
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
