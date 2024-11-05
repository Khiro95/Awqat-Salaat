using AwqatSalaat.Helpers;
using AwqatSalaat.UI.Controls;
using System;
using System.Windows;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for MoreInfoWindow.xaml
    /// </summary>
    public partial class MoreInfoWindow : AcrylicWindow
    {
        private static MoreInfoWindow current;

        public static void Open()
        {
            current = current ?? new MoreInfoWindow();
            current.Show();

            if (!current.IsActive)
            {
                current.Activate();
            }

            if (current.WindowState == WindowState.Minimized)
            {
                current.WindowState = WindowState.Normal;
            }
        }

        public MoreInfoWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(AcrylicWindow));
            tabControl.SelectionChanged += (s, e) => UpdateTitle();
            LocaleManager.Default.CurrentChanged += LocaleManager_CurrentChanged;

            UpdateDirection();
            UpdateTitle();
        }

        private void LocaleManager_CurrentChanged(object sender, EventArgs e)
        {
            UpdateDirection();
            UpdateTitle();
        }

        private void UpdateDirection()
        {
            this.FlowDirection = Properties.Resources.Culture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(Properties.Resources.Culture.IetfLanguageTag);
        }

        private void UpdateTitle() => Title = (tabControl.SelectedItem as TabItemEx)?.Header + $" - {LocaleManager.Default.Get("Data.AppName")}";

        protected override void OnClosed(EventArgs e)
        {
            if (current == this)
            {
                current = null;
            }

            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;

            base.OnClosed(e);
        }
    }
}
