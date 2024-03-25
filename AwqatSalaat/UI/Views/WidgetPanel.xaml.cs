using System.Windows.Controls;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for WidgetPanel.xaml
    /// </summary>
    public partial class WidgetPanel : UserControl
    {
        public WidgetPanel()
        {
            InitializeComponent();
#if DEBUG
            themeBtn.Click += (_, __) => ThemeManager.ToggleTheme();
#else
            var parent = themeBtn.Parent ?? VisualTreeHelper.GetParent(themeBtn);
            if (parent != null)
            {
                Utils.RemoveFromParent(themeBtn, parent, out _);
            }
            else
            {
                themeBtn.Visibility = Visibility.Collapsed;
            }
#endif
        }
    }
}