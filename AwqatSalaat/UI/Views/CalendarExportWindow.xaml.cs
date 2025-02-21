using AwqatSalaat.Helpers;
using AwqatSalaat.UI.Controls;
using Microsoft.Win32;
using Serilog;
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
            this.FlowDirection = LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            this.Language = System.Windows.Markup.XmlLanguage.GetLanguage(LocaleManager.Default.CurrentCulture.IetfLanguageTag);
        }

        private void UpdateTitle() => Title = $"{LocaleManager.Default.Get("UI.Calendar.Export")} - {LocaleManager.Default.Get("Data.AppName")}";

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Export as PNG");

            if (saveFileDialog is null)
            {
                saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Image (*.png)|*.png";
            }

            saveFileDialog.FileName = "";

            if (saveFileDialog.ShowDialog(this) == false)
            {
                Log.Information("Export canceled");
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

            Log.Information("Export done");
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Print");
            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(doc, "Awqat Salaat - Calendar");
                Log.Information("Print request queued");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Log.Information("Closed Calendar export window");
            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;
            base.OnClosed(e);
        }
    }
}
