using AwqatSalaat.Helpers;
using AwqatSalaat.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Serilog;
using System;
using System.ComponentModel;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AwqatSalaat.WinUI.Views
{
    public sealed partial class CalendarDocument : UserControl
    {
        public static readonly DependencyProperty SectionBackgroundProperty = DependencyProperty.Register(
            nameof(SectionBackground),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(HexToBrush("#202024")));

        public Brush SectionBackground { get => (Brush)GetValue(SectionBackgroundProperty); set => SetValue(SectionBackgroundProperty, value); }

        public static readonly DependencyProperty SectionForegroundProperty = DependencyProperty.Register(
            nameof(SectionForeground),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush SectionForeground { get => (Brush)GetValue(SectionForegroundProperty); set => SetValue(SectionForegroundProperty, value); }

        public static readonly DependencyProperty SectionOverlayBackgroundProperty = DependencyProperty.Register(
            nameof(SectionOverlayBackground),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(HexToBrush("#07FFFFFF")));

        public Brush SectionOverlayBackground { get => (Brush)GetValue(SectionOverlayBackgroundProperty); set => SetValue(SectionOverlayBackgroundProperty, value); }

        public static readonly DependencyProperty SectionOverlayForegroundProperty = DependencyProperty.Register(
            nameof(SectionOverlayForeground),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush SectionOverlayForeground { get => (Brush)GetValue(SectionOverlayForegroundProperty); set => SetValue(SectionOverlayForegroundProperty, value); }

        public static readonly DependencyProperty SectionOverlayBorderProperty = DependencyProperty.Register(
            nameof(SectionOverlayBorder),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(HexToBrush("#BAA272")));

        public Brush SectionOverlayBorder { get => (Brush)GetValue(SectionOverlayBorderProperty); set => SetValue(SectionOverlayBorderProperty, value); }

        public static readonly DependencyProperty SectionBorderProperty = DependencyProperty.Register(
            nameof(SectionBorder),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(HexToBrush("#BAA272")));

        public Brush SectionBorder { get => (Brush)GetValue(SectionBorderProperty); set => SetValue(SectionBorderProperty, value); }

        public static readonly DependencyProperty TableForegroundProperty = DependencyProperty.Register(
            nameof(TableForeground),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(HexToBrush("#1f1f2f")));

        public Brush TableForeground { get => (Brush)GetValue(TableForegroundProperty); set => SetValue(TableForegroundProperty, value); }

        public static readonly DependencyProperty TableBorderProperty = DependencyProperty.Register(
            nameof(TableBorder),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(HexToBrush("#D1BA8C")));

        public Brush TableBorder { get => (Brush)GetValue(TableBorderProperty); set => SetValue(TableBorderProperty, value); }

        public static readonly DependencyProperty TableHeaderBackgroundProperty = DependencyProperty.Register(
            nameof(TableHeaderBackground),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(HexToBrush("#BAA272")));

        public Brush TableHeaderBackground { get => (Brush)GetValue(TableHeaderBackgroundProperty); set => SetValue(TableHeaderBackgroundProperty, value); }

        public static readonly DependencyProperty TableHeaderForegroundProperty = DependencyProperty.Register(
            nameof(TableHeaderForeground),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush TableHeaderForeground { get => (Brush)GetValue(TableHeaderForegroundProperty); set => SetValue(TableHeaderForegroundProperty, value); }

        public static readonly DependencyProperty TableRowSeparatorProperty = DependencyProperty.Register(
            nameof(TableRowSeparator),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(new SolidColorBrush(Colors.LightGray)));

        public Brush TableRowSeparator { get => (Brush)GetValue(TableRowSeparatorProperty); set => SetValue(TableRowSeparatorProperty, value); }

        public static readonly DependencyProperty HighlightForegroundProperty = DependencyProperty.Register(
            nameof(HighlightForeground),
            typeof(Brush),
            typeof(CalendarDocument),
            new PropertyMetadata(HexToBrush("#F2CC80")));

        public Brush HighlightForeground { get => (Brush)GetValue(HighlightForegroundProperty); set => SetValue(HighlightForegroundProperty, value); }

        public static readonly DependencyProperty LocationTextProperty = DependencyProperty.Register(
            nameof(LocationText),
            typeof(string),
            typeof(CalendarDocument),
            new PropertyMetadata(""));

        public string LocationText { get => (string)GetValue(LocationTextProperty); set => SetValue(LocationTextProperty, value); }

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

        public CalendarResult Source { get => (CalendarResult)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

        // The standard FlowDirection property, when is set to RightToLeft, makes RenderTargetBitmap render a full black image :(
        public static readonly DependencyProperty FlowDirection2Property = DependencyProperty.Register(
            nameof(FlowDirection2),
            typeof(FlowDirection),
            typeof(CalendarDocument),
            new PropertyMetadata(FlowDirection.LeftToRight));

        public FlowDirection FlowDirection2 { get => (FlowDirection)GetValue(FlowDirection2Property); set => SetValue(FlowDirection2Property, value); }

        public CalendarDocument()
        {
            this.InitializeComponent();
            var country = CountriesProvider.GetCountries().FirstOrDefault(c => c.Code == Properties.Settings.Default.CountryCode);
            LocationText = $"{Properties.Settings.Default.City}, {country?.Name}";
            LocaleManager.Default.CurrentChanged += LocaleManager_CurrentChanged;
            this.Unloaded += (_, _) => LocaleManager.Default.CurrentChanged -= LocaleManager_CurrentChanged;

            UpdateArabicMonthsVisibility();
        }

        private void LocaleManager_CurrentChanged(object sender, EventArgs e)
        {
            UpdateArabicMonthsVisibility();
        }

        private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CalendarResult.HasData))
            {
                UpdateDisplayedDates();
            }
        }

        private void UpdateArabicMonthsVisibility()
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

        private static SolidColorBrush HexToBrush(string hexColor)
        {
            //get the color as System.Drawing.Color
            var color = (System.Drawing.Color)new System.Drawing.ColorConverter().ConvertFromString(hexColor);

            //convert it to Windows.UI.Color
            return new SolidColorBrush(Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
