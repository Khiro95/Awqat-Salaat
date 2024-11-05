using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CalendarPage : Page
    {
        public static readonly DependencyProperty InViewDateProperty = DependencyProperty.Register(
            nameof(InViewDate),
            typeof(DateTime),
            typeof(CalendarView),
            new PropertyMetadata(new DateTime(2000, 1, 1)));

        public DateTime InViewDate { get => (DateTime)GetValue(InViewDateProperty); set => SetValue(InViewDateProperty, value); }

        private CalendarViewModel ViewModel => DataContext as CalendarViewModel;

        public CalendarPage()
        {
            this.InitializeComponent();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            listBox.Loaded += ListBox_Loaded;

            // Workaround for a bug https://github.com/microsoft/microsoft-ui-xaml/issues/4035
            gregorianCombobox.RegisterPropertyChangedCallback(ComboBox.ItemsSourceProperty, OnItemsSourceChanged);
            hijriCombobox.RegisterPropertyChangedCallback(ComboBox.ItemsSourceProperty, OnItemsSourceChanged);
        }

        private void ListBox_Loaded(object sender, RoutedEventArgs e)
        {
            listBox.Loaded -= ListBox_Loaded;
            listBox.ViewChanged += ScrollViewer_ViewChanged;
        }

        private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            UpdateInViewDate(listBox);
        }

        // Workaround for a bug https://github.com/microsoft/microsoft-ui-xaml/issues/4035
        private static void OnItemsSourceChanged(DependencyObject sender, DependencyProperty dp)
        {
            ComboBox comboBox = sender as ComboBox;

            if (comboBox.ItemsSource is not null)
            {
                comboBox.SelectedValuePath = null;
                comboBox.SelectedValuePath = "Id";
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CalendarViewModel.HasData))
            {
                UpdateInViewDate(listBox);

                if (listBox.Items.Count > 0)
                {
                    listBox.ResetScroll();
                }
            }
        }

        private void UpdateInViewDate(ListBox listBox)
        {
            if (listBox.Items.Count > 0)
            {
                DateTime first = ViewModel.PrayerTimes.First().Date;

                foreach (var time in ViewModel.PrayerTimes)
                {
                    var container = listBox.ItemContainerGenerator.ContainerFromItem(time) as ListBoxItem;

                    if (container != null && IsUserVisible(container, listBox))
                    {
                        first = time.Date;
                        break;
                    }
                }

                InViewDate = first;
            }
        }

        private bool IsUserVisible(FrameworkElement element, FrameworkElement container)
        {
            if (element.Visibility == Visibility.Collapsed)
                return false;

            Rect bounds = element.TransformToVisual(container).TransformBounds(new Rect(0.0, 0.0, element.ActualWidth, element.ActualHeight));
            Rect rect = new Rect(0.0, 0.0, container.ActualWidth, container.ActualHeight);
            return rect.Contains(new Point(bounds.Left, bounds.Top)) || rect.Contains(new Point(bounds.Right, bounds.Bottom));
        }
    }

    internal class RecordContainerStyleSelector : StyleSelector
    {
        public Style Standard { get; set; }
        public Style Today { get; set; }

        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            if (item is CalendarRecord record)
            {
                return record.Date.Date == TimeStamp.Date ? Today : Standard;
            }

            return base.SelectStyleCore(item, container);
        }
    }
}
