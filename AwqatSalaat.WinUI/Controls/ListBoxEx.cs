using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace AwqatSalaat.WinUI.Controls
{
    internal partial class ListBoxEx : ListBox
    {
        public static readonly DependencyProperty AlternativeBackgroundProperty = DependencyProperty.Register(
            nameof(AlternativeBackground),
            typeof(Brush),
            typeof(ListBoxEx),
            new PropertyMetadata(null));

        public Brush AlternativeBackground
        {
            get => (Brush)GetValue(AlternativeBackgroundProperty);
            set => SetValue(AlternativeBackgroundProperty, value);
        }

        public event EventHandler<ScrollViewerViewChangedEventArgs> ViewChanged;

        private ScrollViewer scrollViewer;

        public void ResetScroll() => scrollViewer?.ScrollToVerticalOffset(0);

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            var listBoxItem = element as ListBoxItem;
            if (listBoxItem != null)
            {
                var index = IndexFromContainer(element);

                if (index % 2 == 1)
                {
                    listBoxItem.Background = AlternativeBackground;
                }
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            scrollViewer = GetTemplateChild("ScrollViewer") as ScrollViewer;
            scrollViewer.ViewChanged += (s, e) => ViewChanged?.Invoke(s, e);
        }
    }
}
