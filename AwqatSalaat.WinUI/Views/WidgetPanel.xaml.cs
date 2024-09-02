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

        private void LocationPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
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
    }
}
