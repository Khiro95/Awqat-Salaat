using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AwqatSalaat.UI.Controls
{
    //https://stackoverflow.com/a/42867454/4644774
    public class CalendarDocumentBase : UserControl
    {
        public static readonly DependencyProperty SectionBackgroundProperty = DependencyProperty.Register(
            nameof(SectionBackground),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata((SolidColorBrush)new BrushConverter().ConvertFrom("#202024")));

        public Brush SectionBackground { get => (Brush)GetValue(SectionBackgroundProperty); set => SetValue(SectionBackgroundProperty, value); }
        
        public static readonly DependencyProperty SectionForegroundProperty = DependencyProperty.Register(
            nameof(SectionForeground),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata(Brushes.White));

        public Brush SectionForeground { get => (Brush)GetValue(SectionForegroundProperty); set => SetValue(SectionForegroundProperty, value); }

        public static readonly DependencyProperty SectionOverlayBackgroundProperty = DependencyProperty.Register(
            nameof(SectionOverlayBackground),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata((SolidColorBrush)new BrushConverter().ConvertFrom("#07FFFFFF")));

        public Brush SectionOverlayBackground { get => (Brush)GetValue(SectionOverlayBackgroundProperty); set => SetValue(SectionOverlayBackgroundProperty, value); }
        
        public static readonly DependencyProperty SectionOverlayForegroundProperty = DependencyProperty.Register(
            nameof(SectionOverlayForeground),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata(Brushes.White));

        public Brush SectionOverlayForeground { get => (Brush)GetValue(SectionOverlayForegroundProperty); set => SetValue(SectionOverlayForegroundProperty, value); }

        public static readonly DependencyProperty SectionOverlayBorderProperty = DependencyProperty.Register(
            nameof(SectionOverlayBorder),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata((SolidColorBrush)new BrushConverter().ConvertFrom("#BAA272")));

        public Brush SectionOverlayBorder { get => (Brush)GetValue(SectionOverlayBorderProperty); set => SetValue(SectionOverlayBorderProperty, value); }

        public static readonly DependencyProperty SectionBorderProperty = DependencyProperty.Register(
            nameof(SectionBorder),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata((SolidColorBrush)new BrushConverter().ConvertFrom("#BAA272")));

        public Brush SectionBorder { get => (Brush)GetValue(SectionBorderProperty); set => SetValue(SectionBorderProperty, value); }

        public static readonly DependencyProperty TableForegroundProperty = DependencyProperty.Register(
            nameof(TableForeground),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata((SolidColorBrush)new BrushConverter().ConvertFrom("#1f1f2f")));

        public Brush TableForeground { get => (Brush)GetValue(TableForegroundProperty); set => SetValue(TableForegroundProperty, value); }

        public static readonly DependencyProperty TableBorderProperty = DependencyProperty.Register(
            nameof(TableBorder),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata((SolidColorBrush)new BrushConverter().ConvertFrom("#D1BA8C")));

        public Brush TableBorder { get => (Brush)GetValue(TableBorderProperty); set => SetValue(TableBorderProperty, value); }

        public static readonly DependencyProperty TableHeaderBackgroundProperty = DependencyProperty.Register(
            nameof(TableHeaderBackground),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata((SolidColorBrush)new BrushConverter().ConvertFrom("#BAA272")));

        public Brush TableHeaderBackground { get => (Brush)GetValue(TableHeaderBackgroundProperty); set => SetValue(TableHeaderBackgroundProperty, value); }

        public static readonly DependencyProperty TableHeaderForegroundProperty = DependencyProperty.Register(
            nameof(TableHeaderForeground),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata(Brushes.White));

        public Brush TableHeaderForeground { get => (Brush)GetValue(TableHeaderForegroundProperty); set => SetValue(TableHeaderForegroundProperty, value); }

        public static readonly DependencyProperty TableRowSeparatorProperty = DependencyProperty.Register(
            nameof(TableRowSeparator),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata(Brushes.LightGray));

        public Brush TableRowSeparator { get => (Brush)GetValue(TableRowSeparatorProperty); set => SetValue(TableRowSeparatorProperty, value); }

        public static readonly DependencyProperty HighlightForegroundProperty = DependencyProperty.Register(
            nameof(HighlightForeground),
            typeof(Brush),
            typeof(CalendarDocumentBase),
            new PropertyMetadata((SolidColorBrush)new BrushConverter().ConvertFrom("#F2CC80")));

        public Brush HighlightForeground { get => (Brush)GetValue(HighlightForegroundProperty); set => SetValue(HighlightForegroundProperty, value); }
    }
}
