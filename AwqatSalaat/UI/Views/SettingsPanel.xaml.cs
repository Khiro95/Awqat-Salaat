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
            if ((bool)e.NewValue)
            {
                tabControl.SelectedIndex = 0;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
        }

        private void BrowseSound_Click(object sender, RoutedEventArgs e)
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
                openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(ViewModel.Settings.NotificationSoundFilePath);

                if (openFileDialog.ShowDialog() == true)
                {
                    ViewModel.Settings.NotificationSoundFile = openFileDialog.FileName;
                }
            }
            finally
            {
                ParentPopup.StaysOpen = false;
                ParentPopup.IsTopMost = true;
            }
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
    }
}
