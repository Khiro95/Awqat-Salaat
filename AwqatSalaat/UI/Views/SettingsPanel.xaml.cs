using AwqatSalaat.UI.Controls;
using AwqatSalaat.ViewModels;
using Microsoft.Win32;
using System;
using System.Diagnostics;
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
    }
}
