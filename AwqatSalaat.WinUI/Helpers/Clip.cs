using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace AwqatSalaat.WinUI.Helpers
{
    //https://stackoverflow.com/a/13668515/4644774

    public class Clip
    {
        public static bool GetToBounds(DependencyObject depObj)
        {
            return (bool)depObj.GetValue(ToBoundsProperty);
        }

        public static void SetToBounds(DependencyObject depObj, bool clipToBounds)
        {
            depObj.SetValue(ToBoundsProperty, clipToBounds);
        }

        /// <summary>
        /// Identifies the ToBounds Dependency Property.
        /// <summary>
        public static readonly DependencyProperty ToBoundsProperty = DependencyProperty.RegisterAttached(
            "ToBounds",
            typeof(bool),
            typeof(Clip),
            new PropertyMetadata(false, OnToBoundsPropertyChanged));

        private static void OnToBoundsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement fe = d as FrameworkElement;

            if (fe is not null)
            {
                ClipToBounds(fe);

                // whenever the element which this property is attached to is loaded
                // or re-sizes, we need to update its clipping geometry
                fe.Loaded += FrameworkElement_Loaded;
                fe.SizeChanged += FrameworkElement_SizeChanged;
            }
        }

        /// <summary>
        /// Creates a rectangular clipping geometry which matches the geometry of the
        /// passed element
        /// </summary>
        private static void ClipToBounds(FrameworkElement fe)
        {
            if (GetToBounds(fe))
            {
                fe.Clip = new RectangleGeometry()
                {
                    Rect = new Rect(0, 0, fe.ActualWidth, fe.ActualHeight)
                };
            }
            else
            {
                fe.Clip = null;
            }
        }

        static void FrameworkElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipToBounds(sender as FrameworkElement);
        }

        static void FrameworkElement_Loaded(object sender, RoutedEventArgs e)
        {
            ClipToBounds(sender as FrameworkElement);
        }
    }
}
