using AwqatSalaat.Helpers;
using AwqatSalaat.UI.Controls;
using AwqatSalaat.ViewModels;
using Serilog;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AwqatSalaat.UI.Views
{
    /// <summary>
    /// Interaction logic for CalendarDocument.xaml
    /// </summary>
    public partial class CalendarDocument : CalendarDocumentBase
    {
        private static readonly FontFamily ArabicSymbols;

        public static readonly DependencyProperty LocationTextProperty = DependencyProperty.Register(
            nameof(LocationText),
            typeof(string),
            typeof(CalendarDocument));

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(CalendarResult),
            typeof(CalendarDocument),
            new PropertyMetadata(null, OnSourceChanged));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var doc = (CalendarDocument)d;
            doc.UpdateDisplayedDates();

            if (e.OldValue is CalendarResult oldResult)
            {
                oldResult.PropertyChanged -= doc.Source_PropertyChanged;
            }

            if (e.NewValue is CalendarResult newResult)
            {
                newResult.PropertyChanged += doc.Source_PropertyChanged;
            }
        }

        static CalendarDocument()
        {
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var baseDir = Path.GetDirectoryName(assemblyPath) + "\\";

            ArabicSymbols = new FontFamily(new Uri(baseDir), "Fonts/#KFGQPC Arabic Symbols 01");
        }

        public string LocationText { get => (string)GetValue(LocationTextProperty); set => SetValue(LocationTextProperty, value); }
        public CalendarResult Source { get => (CalendarResult)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

        public CalendarDocument()
        {
            InitializeComponent();
            hmonthRTL.FontFamily = ArabicSymbols;
            hmonth2RTL.FontFamily = ArabicSymbols;
            var country = CountriesProvider.GetCountries().FirstOrDefault(c => c.Code == Properties.Settings.Default.CountryCode);
            LocationText = $"{Properties.Settings.Default.City}, {country?.Name}";
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == LanguageProperty)
            {
                if (LocaleManager.Default.Current == "ar")
                {
                    hmonthRTLContainer.Visibility = Visibility.Visible;
                    hmonthContainer.Visibility = Visibility.Collapsed;
                }
                else
                {
                    hmonthContainer.Visibility = Visibility.Visible;
                    hmonthRTLContainer.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CalendarResult.HasData))
            {
                UpdateDisplayedDates();
            }
        }

        private void UpdateDisplayedDates()
        {
            Log.Information("[Calendar document] Updating displayed dates");

            if (Source is CalendarResult result && result.HijriCalendar != null)
            {
                Log.Information($"First date={result.FirstDate:u}, Last date={result.LastDate:u}");
                var calendar = result.HijriCalendar;
                var currentDate = TimeStamp.Date;
                var firstHijriMonth = calendar.GetMonth(result.FirstDate ?? currentDate);
                var lastHijriMonth = calendar.GetMonth(result.LastDate ?? currentDate);

                hmonthRTL.Text = char.ConvertFromUtf32(firstHijriMonth + 48);
                hmonth2RTL.Text = char.ConvertFromUtf32(lastHijriMonth + 48);

                if (result.IsHijriMonth)
                {
                    var firstMonth = (result.FirstDate ?? currentDate).Month;
                    var lastMonth = (result.LastDate ?? currentDate).Month;

                    if (firstMonth != lastMonth)
                    {
                        gmonthSeparator.Visibility = Visibility.Visible;
                        gmonth2.Visibility = Visibility.Visible;
                        gmonth.FontSize = 14;
                    }
                    else
                    {
                        gmonthSeparator.Visibility = Visibility.Collapsed;
                        gmonth2.Visibility = Visibility.Collapsed;
                        gmonth.ClearValue(TextBlock.FontSizeProperty);
                    }

                    var firstYear = (result.FirstDate ?? currentDate).Year;
                    var lastYear = (result.LastDate ?? currentDate).Year;

                    if (firstYear != lastYear)
                    {
                        gyearSeparator.Visibility = Visibility.Visible;
                        gyear2.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        gyearSeparator.Visibility = Visibility.Collapsed;
                        gyear2.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    if (firstHijriMonth != lastHijriMonth)
                    {
                        hmonth2.Visibility = Visibility.Visible;
                        hmonth.FontSize = 14;

                        hmonthRTLSeparator.Visibility = Visibility.Visible;
                        hmonth2RTL.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        hmonth2.Visibility = Visibility.Collapsed;
                        hmonth.ClearValue(TextBlock.FontSizeProperty);

                        hmonthRTLSeparator.Visibility = Visibility.Collapsed;
                        hmonth2RTL.Visibility = Visibility.Collapsed;
                    }

                    var firstYear = calendar.GetYear(result.FirstDate ?? currentDate);
                    var lastYear = calendar.GetYear(result.LastDate ?? currentDate);

                    if (firstYear != lastYear)
                    {
                        hyearSeparator.Visibility = Visibility.Visible;
                        hyear2.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        hyearSeparator.Visibility = Visibility.Collapsed;
                        hyear2.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
    }
}
