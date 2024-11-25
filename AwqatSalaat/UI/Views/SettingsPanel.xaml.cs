using AwqatSalaat.Helpers;
using AwqatSalaat.UI.Controls;
using AwqatSalaat.ViewModels;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for SettingsPanel.xaml
    /// </summary>
    public partial class SettingsPanel : UserControl
    {
        private static readonly string Version = typeof(SettingsPanel).Assembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?
            .Version;
        private static readonly string Architecture = Environment.Is64BitProcess ? "64-bit" : "32-bit";

        private OpenFileDialog openFileDialog;

        private WidgetSettingsViewModel ViewModel => DataContext as WidgetSettingsViewModel;

        public static DependencyProperty ParentPopupProperty = DependencyProperty.Register(
            nameof(ParentPopup),
            typeof(AcrylicPopup),
            typeof(SettingsPanel),
            new FrameworkPropertyMetadata(null));

        public AcrylicPopup ParentPopup
        {
            get => (AcrylicPopup)GetValue(ParentPopupProperty);
            set => SetValue(ParentPopupProperty, value);
        }

        public SettingsPanel()
        {
            InitializeComponent();
            IsVisibleChanged += SettingsPanel_IsVisibleChanged;
            version.Text = "v" + (Version ?? "{ERROR}");
            architecture.Text = Architecture;
        }

        private void SettingsPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            tabControl.SelectedIndex = (bool)e.NewValue ? 0 : -1;

            if (!(bool)e.NewValue)
            {
                CollapseExpanders(null);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        }

        private async void CheckForUpdatesClick(object sender, RoutedEventArgs e)
        {
            // MessageBox will make the popup disappear so we have to force it to stay open temporarily
            var popup = Utils.GetOpenPopups().First();
            bool alteredPopup = false;

            if (popup != null && !popup.StaysOpen)
            {
                popup.StaysOpen = true;
                alteredPopup = true;
            }

            try
            {
                var current = System.Version.Parse(Version);
#if DEBUG
                current = System.Version.Parse("1.0");
#endif
                var latest = await ViewModel.CheckForNewVersion(current);

                if (latest is null)
                {
                    MessageBoxEx.Info(Properties.Resources.Dialog_WidgetUpToDate);
                }
                else
                {
                    var result = MessageBoxEx.Question(string.Format(Properties.Resources.Dialog_NewUpdateAvailableFormat, latest.Tag));

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(latest.HtmlUrl));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.Error(Properties.Resources.Dialog_CheckingUpdatesFailed + $"\nError: {ex.Message}");
            }
            finally
            {
                if (alteredPopup)
                {
                    popup.StaysOpen = false;
                }
            }
        }

        private void BrowseNotificationSound_Click(object sender, RoutedEventArgs e)
        {
            BrowseSoundFile(ViewModel.Settings.NotificationSoundFilePath, (s, f) => s.NotificationSoundFile = f);
        }

        private void BrowseAdhanSound_Click(object sender, RoutedEventArgs e)
        {
            BrowseSoundFile(ViewModel.Settings.AdhanSoundFilePath, (s, f) => s.AdhanSoundFile = f);
        }

        private void BrowseAdhanFajrSound_Click(object sender, RoutedEventArgs e)
        {
            BrowseSoundFile(ViewModel.Settings.AdhanFajrSoundFilePath, (s, f) => s.AdhanFajrSoundFile = f);
        }

        private void BrowseSoundFile(string initialPath, Action<Properties.Settings, string> fileSetter)
        {
            if (openFileDialog is null)
            {
                openFileDialog = new OpenFileDialog()
                {
                    Filter = "Audio Files(*.wav;*.wma;*.mp3;*.aac)|*.wav;*.wma;*.mp3;*.aac;"
                };
            }

            ParentPopup.StaysOpen = true;
            ParentPopup.IsTopMost = false;

            try
            {
                openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(initialPath);

                if (openFileDialog.ShowDialog() == true)
                {
                    fileSetter(ViewModel.Settings, openFileDialog.FileName);
                }
            }
            finally
            {
                ParentPopup.StaysOpen = false;
                ParentPopup.IsTopMost = true;
            }
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            CollapseExpanders(sender as Expander);
        }

        private void CollapseExpanders(Expander exception)
        {
            foreach (var item in timesPanel.Items)
            {
                var container = timesPanel.ItemContainerGenerator.ContainerFromItem(item);

                if (container != null)
                {
                    var expander = VisualTreeHelper.GetChild(container, 0) as Expander;

                    if (expander != exception)
                    {
                        expander.IsExpanded = false;
                    }
                }
            }
        }
    }
}
