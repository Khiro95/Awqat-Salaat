using System.Windows;
using System.Windows.Controls;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for SettingsPanel.xaml
    /// </summary>
    public partial class SettingsPanel : UserControl
    {
        public SettingsPanel()
        {
            InitializeComponent();
            IsVisibleChanged += SettingsPanel_IsVisibleChanged;
        }

        private void SettingsPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                tabControl.SelectedIndex = 0;
            }
        }
    }
}
