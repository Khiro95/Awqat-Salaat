using AwqatSalaat.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace AwqatSalaat.UI.Controls
{
    public class AcrylicWindow : Window
    {
        private FrameworkElement titleButtonsArea;
        private uint _tintColor = 0x2c2c2c; /* BGR color format */
        private uint _tintOpacity;

        public static DependencyProperty EnableAcrylicEffectProperty = DependencyProperty.Register(
            nameof(EnableAcrylicEffect),
            typeof(bool),
            typeof(AcrylicWindow),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, propertyChangedCallback: OnEnableAcrylicChanged));

        private static void OnEnableAcrylicChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AcrylicWindow window = (AcrylicWindow)d;

            if (window.IsLoaded)
            {
                if (window.EnableAcrylicEffect)
                {
                    window.EnableBlur(false);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public static DependencyProperty TintColorProperty = DependencyProperty.Register(
            nameof(TintColor),
            typeof(Color),
            typeof(AcrylicWindow),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender, propertyChangedCallback: OnTintColorChanged));

        public static DependencyProperty TintOpacityProperty = DependencyProperty.Register(
            nameof(TintOpacity),
            typeof(double),
            typeof(AcrylicWindow),
            new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender, propertyChangedCallback: OnTintOpacityChanged));

        private static void OnTintColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AcrylicWindow window = (AcrylicWindow)d;
            Color value = (Color)e.NewValue;
            window._tintColor = value.R | ((uint)value.G << 8) | ((uint)value.B << 16);

            if (window.IsLoaded && window.EnableAcrylicEffect)
            {
                window.EnableBlur(false);
            }
        }

        private static void OnTintOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AcrylicWindow window = (AcrylicWindow)d;
            double value = (double)e.NewValue;
            window._tintOpacity = (uint)((value < 0 ? 0 : value > 1 ? 1 : value) * 255);

            if (window.IsLoaded && window.EnableAcrylicEffect)
            {
                window.EnableBlur(false);
            }
        }

        public static DependencyProperty TitleBarBackgroundProperty = DependencyProperty.Register(
            nameof(TitleBarBackground),
            typeof(Brush),
            typeof(AcrylicWindow),
            new FrameworkPropertyMetadata(Brushes.White));

        public static DependencyProperty TitleBarForegroundProperty = DependencyProperty.Register(
            nameof(TitleBarForeground),
            typeof(Brush),
            typeof(AcrylicWindow),
            new FrameworkPropertyMetadata(Brushes.Black));

        public static DependencyProperty FunctionBarContentProperty = DependencyProperty.Register(
            nameof(FunctionBarContent),
            typeof(object),
            typeof(AcrylicWindow));

        public Brush TitleBarBackground
        {
            get => (Brush)GetValue(TitleBarBackgroundProperty);
            set => SetValue(TitleBarBackgroundProperty, value);
        }

        public Brush TitleBarForeground
        {
            get => (Brush)GetValue(TitleBarForegroundProperty);
            set => SetValue(TitleBarForegroundProperty, value);
        }

        public object FunctionBarContent
        {
            get => GetValue(FunctionBarContentProperty);
            set => SetValue(FunctionBarContentProperty, value);
        }

        public bool EnableAcrylicEffect
        {
            get => (bool)GetValue(EnableAcrylicEffectProperty);
            set => SetValue(EnableAcrylicEffectProperty, value);
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

        static AcrylicWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AcrylicWindow), new FrameworkPropertyMetadata(typeof(AcrylicWindow)));
        }

        public AcrylicWindow() : base()
        {
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, Maximize_Executed, Maximize_CanExecute));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, Minimize_Executed, Minimize_CanExecute));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, Restore_Executed, Restore_CanExecute));
            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, Close_Executed, Close_CanExecute));
            CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, IconShowSystemMenu_Executed));

            Loaded += (s, e) =>
            {
                if (EnableAcrylicEffect)
                {
                    EnableBlur(false);
                }
            };
        }

        private void Maximize_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is Window window && window.WindowState == WindowState.Normal;
        }

        private void Maximize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(e.Parameter as Window);
        }

        private void Minimize_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is Window window && window.WindowState != WindowState.Minimized;
        }

        private void Minimize_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(e.Parameter as Window);
        }

        private void Restore_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is Window window && window.WindowState == WindowState.Maximized;
        }

        private void Restore_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(e.Parameter as Window);
        }

        private void Close_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is Window;
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(e.Parameter as Window);
        }

        private void IconShowSystemMenu_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is Window window)
            {
                double leftPos = WindowState == WindowState.Maximized ? 0 : Left + BorderThickness.Left;
                double topPos = WindowState == WindowState.Maximized ? 0 : Top + BorderThickness.Top;
                Point point = new Point(leftPos, topPos + titleButtonsArea.ActualHeight);
                SystemCommands.ShowSystemMenu(window, point);
            }
        }

        private void EnableBlur(bool showBorders)
        {
            var HwndSource = PresentationSource.FromVisual(this) as HwndSource;
            var Handle = HwndSource.Handle;
            if (HwndSource != null)
            {
                if (Helpers.SystemInfos.IsWindows10)
                {
                    var accent = new AccentPolicy();
                    // Actually we ignore the tint
                    accent.GradientColor = /*(_tintOpacity << 24) |*/ (_tintColor & 0xFFFFFF);

                    if (showBorders)
                    {
                        accent.AccentFlags = AccentFlags.DrawAllBorders;
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

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            titleButtonsArea = GetTemplateChild("TitleBarButtonsArea") as FrameworkElement;
        }
    }
}
