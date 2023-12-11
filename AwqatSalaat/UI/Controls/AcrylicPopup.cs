using AwqatSalaat.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
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

        public AcrylicPopup() : base()
        {
            // Need this to make sure the background render properly when showing for the first time
            if (Helpers.SystemInfos.IsWindows81_OrEarlier)
            {
                this.Loaded += (_, __) =>
                {
                    // Win 7 doesn't support tinting the blur behind for specific window so we use the child background instead.
                    // Win 8/8.1 doesn't support blur behind at all.
                    SolidColorBrush brush = new SolidColorBrush() { Color = TintColor, Opacity = 0.95 /*TintOpacity*/ }; ;
                    if (this.Child is Control ctrl)
                    {
                        ctrl.Background = brush;
                    }
                    else if (this.Child is Border border)
                    {
                        border.Background = brush;
                    }
                };
            }
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
                if (Helpers.SystemInfos.IsWindows10)
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
                else if (Helpers.SystemInfos.IsWindows81_OrEarlier)
                {
                    // Win 8/8.1 doesn't support blur behind
                    if (Helpers.SystemInfos.IsWindows7)
                    {
                        AcrylicBlur.EnableBlurBehindWin7(Handle, true);
                    }

                    int attr;
                    if (showBorders)
                    {
                        const int defaultMargin = 1;
                        MARGINS margins = new MARGINS
                        {
                            cxLeftWidth = defaultMargin,
                            cxRightWidth = defaultMargin,
                            cyBottomHeight = defaultMargin,
                            cyTopHeight = defaultMargin
                        };
                        if (RemoveBorderAtPlacement)
                        {
                            switch (Placement)
                            {
                                case PlacementMode.Left:
                                    margins.cxRightWidth = 0;
                                    break;
                                case PlacementMode.Top:
                                    margins.cyBottomHeight = 0;
                                    break;
                                case PlacementMode.Right:
                                    margins.cxLeftWidth = 0;
                                    break;
                                case PlacementMode.Bottom:
                                    margins.cyTopHeight = 0;
                                    break;
                            }
                        }

                        Dwmapi.DwmExtendFrameIntoClientArea(Handle, ref margins);

                        attr = (int)DWMNCRENDERINGPOLICY.DWMNCRP_ENABLED;
                    }
                    else
                    {
                        attr = (int)DWMNCRENDERINGPOLICY.DWMNCRP_DISABLED;
                    }

                    int attrSize = Marshal.SizeOf(attr);
                    var ptr = Marshal.AllocHGlobal(attrSize);
                    Marshal.StructureToPtr(attr, ptr, false);
                    Dwmapi.DwmSetWindowAttribute(Handle, DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY, ptr, attrSize);
                    Marshal.FreeHGlobal(ptr);
                }
            }
        }
    }
}
