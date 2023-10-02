using AwqatSalaat.Interop;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace AwqatSalaat.UI.Controls
{
    public class AcrylicPopup : AnimatedPopup
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
            if (acrylicPopup.IsOpen)
            {
                acrylicPopup.EnableBlur(true);
            }
        }

        private static void OnTintOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AcrylicPopup acrylicPopup = (AcrylicPopup)d;
            double value = (double)e.NewValue;
            acrylicPopup._tintOpacity = (uint)((value < 0 ? 0 : value > 1 ? 1 : value) * 255);
            if (acrylicPopup.IsOpen)
            {
                acrylicPopup.EnableBlur(true);
            }
        }

        public bool RemoveBorderAtPlacement { get; set; }
        public override bool AnimateSizeOnOpening => false;
        public override bool AnimateSizeOnClosing => false;

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

        protected override void OnOpeningAnimationStarting()
        {
            EnableBlur(false);
        }

        protected override void OnOpeningAnimationCompleted()
        {
            EnableBlur(true);
        }

        protected override void OnClosingAnimationStarting()
        {
            EnableBlur(false);
        }

        private void EnableBlur(bool showBorders)
        {
            if (HwndSource != null)
            {
                var accent = new AccentPolicy();
                accent.GradientColor = (_tintOpacity << 24) | (_tintColor & 0xFFFFFF);

                if (showBorders)
                {
                    accent.AccentFlags = AccentFlags.DrawAllBorders;

                    if (RemoveBorderAtPlacement)
                    {
                        switch (Placement)
                        {
                            case PlacementMode.Left:
                                accent.AccentFlags &= ~AccentFlags.DrawRightBorder;
                                break;
                            case PlacementMode.Top:
                                accent.AccentFlags &= ~AccentFlags.DrawBottomBorder;
                                break;
                            case PlacementMode.Right:
                                accent.AccentFlags &= ~AccentFlags.DrawLeftBorder;
                                break;
                            case PlacementMode.Bottom:
                                accent.AccentFlags &= ~AccentFlags.DrawTopBorder;
                                break;
                        }
                    }
                }

                AcrylicBlur.EnableAcrylicBlur(Handle, accent);
            }
        }
    }
}
