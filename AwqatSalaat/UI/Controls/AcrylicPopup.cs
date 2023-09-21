using AwqatSalaat.Interop;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace AwqatSalaat.UI.Controls
{
    public class AcrylicPopup : Popup
    {
        private uint _tintColor = 0x2c2c2c; /* BGR color format */
        private uint _tintOpacity;

        public static DependencyProperty TintColorProperty = DependencyProperty.Register(
            nameof(TintColor),
            typeof(Color),
            typeof(AcrylicPopup),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender, propertyChangedCallback: OnTintColorChanged));

        public static DependencyProperty TintOpacityProperty = DependencyProperty.Register(
            nameof(TintOpacity),
            typeof(double),
            typeof(AcrylicPopup),
            new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender, propertyChangedCallback: OnTintOpacityChanged));

        private static void OnTintColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AcrylicPopup acrylicPopup = (AcrylicPopup)d;
            Color value = (Color)e.NewValue;
            acrylicPopup._tintColor = value.R | ((uint)value.G << 8) | ((uint)value.B << 16);
            acrylicPopup.EnableBlur();
        }

        private static void OnTintOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AcrylicPopup acrylicPopup = (AcrylicPopup)d;
            double value = (double)e.NewValue;
            acrylicPopup._tintOpacity = (uint)((value < 0 ? 0 : value > 1 ? 1 : value) * 255);
            acrylicPopup.EnableBlur();
        }

        public Color TintColor
        {
            get => (Color)GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }

        public double TintOpacity
        {
            get => (double)GetValue(TintOpacityProperty);
            set => SetValue(TintOpacityProperty, value);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            EnableBlur();
        }

        private void EnableBlur()
        {
            if (IsOpen)
            {
                var accent = new AccentPolicy();
                accent.GradientColor = (_tintOpacity << 24) | (_tintColor & 0xFFFFFF);

                IntPtr popupWindowHandle = ((HwndSource)HwndSource.FromVisual(this.Child)).Handle;

                AcrylicBlur.EnableAcrylicBlur(popupWindowHandle, accent);
            }
        }
    }
}
