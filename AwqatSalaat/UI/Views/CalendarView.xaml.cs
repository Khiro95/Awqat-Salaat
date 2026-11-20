using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for CalendarView.xaml
    /// </summary>
    public partial class CalendarView : UserControl
    {
        public static readonly DependencyProperty InViewDateProperty = DependencyProperty.Register(
            nameof(InViewDate),
            typeof(DateTime),
            typeof(CalendarView),
            new PropertyMetadata(new DateTime(2000, 1, 1)));

        public DateTime InViewDate { get => (DateTime)GetValue(InViewDateProperty); set => SetValue(InViewDateProperty, value); }

        private CalendarViewModel ViewModel => DataContext as CalendarViewModel;

        public CalendarView()
        {
            InitializeComponent();
            Loaded += CalendarView_Loaded;
            Unloaded += CalendarView_Unloaded;
            ViewModel.Result.PropertyChanged += Result_PropertyChanged;
        }

        private void CalendarView_Unloaded(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.PropertyChanged -= Settings_PropertyChanged;
        }

        private void CalendarView_Loaded(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.PropertyChanged -= Settings_PropertyChanged;
            Properties.Settings.Default.PropertyChanged += Settings_PropertyChanged;

            InvalidateService();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Properties.Settings.Service) || e.PropertyName == nameof(Properties.Settings.CSV_Range))
            {
                InvalidateService();
            }
        }

        object oldContent;

        private void InvalidateService()
        {
            if (Properties.Settings.Default.Service == Data.PrayerTimesService.QCH
                || Properties.Settings.Default.Service == Data.PrayerTimesService.Diyanet)
            {
                Log.Information("Hiding content in Calendar view");

                if (oldContent is null)
                {
                    oldContent = Content;
                }

                Content = Resources["ServiceNotSupportedNotice"];
            }
            else if (oldContent != null)
            {
                Log.Information("Showing content in Calendar view");
                Content = oldContent;
                oldContent = null;
            }

            bool isCSV = Properties.Settings.Default.Service == Data.PrayerTimesService.CSV;
            bool isMonthly = Properties.Settings.Default.CSV_Range == Configurations.CsvImportRange.Month;

            if (isCSV)
            {
                Log.Information($"Adapting Calendar view content to CSV service. (Monthly range: {isMonthly})");
            }

            hijriRadioButton.IsEnabled = !isCSV;
            gregorianYear.IsEnabled = !isCSV;
            gregorianMonth.IsEnabled = !isCSV || !isMonthly;
        }

        private void Result_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CalendarResult.HasData))
            {
                UpdateInViewDate(listBox);

                if (listBox.HasItems)
                {
                    listBox.ScrollIntoView(listBox.Items[0]);
                }
            }
        }

        private void ListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Log.Debug("[Calendar] Scroll changed");
            var listBox = (ListBox)sender;

            UpdateInViewDate(listBox);
        }

        private void UpdateInViewDate(ListBox listBox)
        {
            Log.Debug("[Calendar] Updating displayed date");

            if (listBox.HasItems)
            {
                Log.Debug($"listbox has {listBox.Items.Count} item(s)");
                DateTime first = ViewModel.Result.PrayerTimes[0].Date;

                foreach (var time in ViewModel.Result.PrayerTimes)
                {
                    var container = listBox.ItemContainerGenerator.ContainerFromItem(time) as ListBoxItem;

                    if (container != null && IsUserVisible(container, listBox))
                    {
                        first = time.Date;
                        break;
                    }
                }

                InViewDate = first;
                Log.Debug($"Determined date: {first:u}");
            }
        }

        private bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (!element.IsVisible)
                return false;

            Rect bounds = element.TransformToAncestor(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
        }

        private void ExportClick(object sender, RoutedEventArgs e)
        {
            Log.Information("Clicked on Calendar Export");
            var window = new CalendarExportWindow()
            {
                DataContext = new CalendarExportViewModel { CalendarResult = ViewModel.Result },
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }
    }

    internal class RecordContainerStyleSelector : StyleSelector
    {
        public Style Standard { get; set; }
        public Style Today { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is CalendarRecord record)
            {
                return record.Date.Date == TimeStamp.Date ? Today : Standard;
            }

            return base.SelectStyle(item, container);
        }
    }
}
