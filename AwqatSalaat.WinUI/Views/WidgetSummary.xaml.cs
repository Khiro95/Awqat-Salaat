using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    public sealed partial class WidgetSummary : UserControl
    {
#if DEBUG
        public static WidgetSummary Current { get; private set; }
#endif

        private WidgetViewModel ViewModel => DataContext as WidgetViewModel;

        public WidgetSummary()
        {
#if DEBUG
            Current = this;
            Properties.Settings.Default.IsConfigured = false;
#endif
            this.InitializeComponent();
            //widgetPanel.Loaded += WidgetPanel_Loaded;
            ViewModel.WidgetSettings.Updated += WidgetSettings_Updated;
            LocaleManager.Default.CurrentChanged += (_, __) => UpdateDirection();
            UpdateDirection();
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
            toggle.IsChecked = false;

            if (ViewModel.WidgetSettings.IsOpen && ViewModel.WidgetSettings.Settings.IsConfigured)
            {
                ViewModel.WidgetSettings.Cancel.Execute(null);
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
}
