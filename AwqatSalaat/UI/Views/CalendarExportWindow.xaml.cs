using AwqatSalaat.Helpers;
using AwqatSalaat.UI.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for CalendarExportWindow.xaml
    /// </summary>
    public partial class CalendarExportWindow : AcrylicWindow
    {
        private const int DPI = 288;
        private const double Scale = DPI / 96.0;
        private const int ExportWidth = (int)(794 * Scale);
        private const int ExportHeight = (int)(1123 * Scale);

        private SaveFileDialog saveFileDialog;

        public CalendarExportWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(AcrylicWindow));
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

        private void UpdateTitle() => Title = $"{LocaleManager.Default.Get("UI.Calendar.Export")} - {LocaleManager.Default.Get("Data.AppName")}";

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (saveFileDialog is null)
            {
                saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Image (*.png)|*.png";
            }

            saveFileDialog.FileName = "";

            if (saveFileDialog.ShowDialog(this) == false)
            {
                return;
            }

            var target = new RenderTargetBitmap(ExportWidth, ExportHeight, DPI, DPI, PixelFormats.Default);

            target.Render(doc);

            using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(target));
                encoder.Save(fileStream);
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(doc, "Awqat Salaat - Calendar");
            }
        }
    }
}
