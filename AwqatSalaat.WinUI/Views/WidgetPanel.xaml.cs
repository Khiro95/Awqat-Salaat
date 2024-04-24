using AwqatSalaat.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

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
#if DEBUG
            themeBtn.Click += themeBtn_Click;
#else
            commandBar.PrimaryCommands.Remove(themeBtn);
#endif
        }

#if DEBUG
        private void themeBtn_Click(object sender, RoutedEventArgs e)
        {
            // cannot change app theme at runtime so we change popup's theme only
            var presenter = this.Parent as FlyoutPresenter;
            var popup = presenter?.Parent as Popup;

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
    }
}
