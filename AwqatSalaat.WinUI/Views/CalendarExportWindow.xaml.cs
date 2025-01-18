using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using AwqatSalaat.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Graphics.Printing;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CalendarExportWindow : Window
    {
        private const int DPI = 288;
        private const double Scale = DPI / 96.0;
        private const int ExportWidth = (int)(794 * Scale);
        private const int ExportHeight = (int)(1123 * Scale);

        public CalendarExportViewModel ViewModel { get; }

        public CalendarExportWindow(CalendarExportViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.Activated += CalendarExportWindow_Activated;
            this.Closed += CalendarExportWindow_Closed;
            LocaleManager.Default.CurrentChanged += LocaleManager_CurrentChanged;

            this.AppWindow.SetPresenter(OverlappedPresenter.CreateForDialog());

            UpdateDirection();
            UpdateTitle();

            printButton.IsEnabled = PrintManager.IsSupported();
        }

        private void CalendarExportWindow_Closed(object sender, WindowEventArgs args)
        {
            LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;
            UnregisterForPrinting();
        }

        private void LocaleManager_CurrentChanged(object sender, EventArgs e)
        {
            UpdateDirection();
            UpdateTitle();
        }

        private void UpdateDirection()
        {
            root.FlowDirection = LocaleManager.Default.CurrentCulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        private void UpdateTitle() => Title = $"{LocaleManager.Default.Get("UI.Calendar.Export")} - {LocaleManager.Default.Get("Data.AppName")}";

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            // Render to an image at the current system scale and retrieve pixel contents
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            viewbox.Height = ExportHeight;
            viewbox.Width = ExportWidth;
            await renderTargetBitmap.RenderAsync(doc, ExportWidth, ExportHeight);
            viewbox.ClearValue(FrameworkElement.HeightProperty);
            viewbox.ClearValue(FrameworkElement.WidthProperty);
            var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

            var savePicker = new FileSavePicker();
            savePicker.DefaultFileExtension = ".png";
            savePicker.FileTypeChoices.Add("Image (*.png)", new List<string> { ".png" });
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(this));

            // Prompt the user to select a file
            var saveFile = await savePicker.PickSaveFileAsync();

            // Verify the user selected a file
            if (saveFile is null)
            {
                return;
            }

            // Encode the image to the selected file on disk
            using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, fileStream);

                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Ignore,
                    (uint)renderTargetBitmap.PixelWidth,
                    (uint)renderTargetBitmap.PixelHeight,
                    DPI,
                    DPI,
                    pixelBuffer.ToArray());

                await encoder.FlushAsync();
            }
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            if (PrintManager.IsSupported())
            {
                try
                {
                    var hWnd = WindowNative.GetWindowHandle(this);
                    await PrintManagerInterop.ShowPrintUIForWindowAsync(hWnd);
                }
                catch (Exception ex)
                {
                    string caption = $"{LocaleManager.Default.Get("UI.Calendar.Print")} - {LocaleManager.Default.Get("Data.AppName")}";
                    MessageBox.Error(ex.Message, caption);
                }
            }
        }

        private bool centered, registeredForPrinting;

        private void CalendarExportWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (!centered)
            {
                Center(this);
                centered = true;
            }

            if (!registeredForPrinting)
            {
                RegisterForPrinting();
                registeredForPrinting = true;
            }
        }

        private static void Center(Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

            if (AppWindow.GetFromWindowId(windowId) is AppWindow appWindow &&
                DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest) is DisplayArea displayArea)
            {
                PointInt32 CenteredPosition = appWindow.Position;
                CenteredPosition.X = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
                CenteredPosition.Y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;
                appWindow.Move(CenteredPosition);
            }
        }

        PrintDocument printDocument = null;
        IPrintDocumentSource printDocumentSource = null;

        private void RegisterForPrinting()
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            PrintManager printManager = PrintManagerInterop.GetForWindow(hWnd);
            printManager.PrintTaskRequested += PrintTask_Requested;

            printDocument = new PrintDocument();
            printDocumentSource = printDocument.DocumentSource;
            printDocument.Paginate += PrintDocument_Paginate;
            printDocument.GetPreviewPage += PrintDocument_GetPreviewPage;
            printDocument.AddPages += PrintDocument_AddPages;
        }

        private void UnregisterForPrinting()
        {
            if (printDocument is null)
            {
                return;
            }

            printDocument.Paginate -= PrintDocument_Paginate;
            printDocument.GetPreviewPage -= PrintDocument_GetPreviewPage;
            printDocument.AddPages -= PrintDocument_AddPages;

            var hWnd = WindowNative.GetWindowHandle(this);
            PrintManager printManager = PrintManagerInterop.GetForWindow(hWnd);
            printManager.PrintTaskRequested -= PrintTask_Requested;
        }

        private void PrintTask_Requested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            // Create the PrintTask.
            // Defines the title and delegate for PrintTaskSourceRequested.
            PrintTask printTask = args.Request.CreatePrintTask("Awqat Salaat - Calendar", PrintTaskSourceRequested);

            // Handle PrintTask.Completed to catch failed print jobs.
            printTask.Completed += PrintTask_Completed;
        }

        private void PrintTaskSourceRequested(PrintTaskSourceRequestedArgs args)
        {
            // Set the document source.
            args.SetSource(printDocumentSource);
        }

        private void PrintTask_Completed(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            if (args.Completion == PrintTaskCompletion.Failed)
            {
                string caption = $"{LocaleManager.Default.Get("UI.Calendar.Print")} - {LocaleManager.Default.Get("Data.AppName")}";
                MessageBox.Warning("Print failed!", caption);
            }
        }

        private void PrintDocument_Paginate(object sender, PaginateEventArgs e)
        {
            printDocument.SetPreviewPageCount(1, PreviewPageCountType.Final);
        }

        private void PrintDocument_GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            // Provide a UIElement as the print preview.
            printDocument.SetPreviewPage(e.PageNumber, doc);
        }

        private void PrintDocument_AddPages(object sender, AddPagesEventArgs e)
        {
            printDocument.AddPage(doc);

            // Indicate that all of the print pages have been provided
            printDocument.AddPagesComplete();
        }
    }
}
