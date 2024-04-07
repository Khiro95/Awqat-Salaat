using System;
using System.Runtime.InteropServices;

namespace AwqatSalaat.Interop
{
    public static class User32
    {
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect([In] IntPtr hWnd, [Out] out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool UpdateLayeredWindow([In] IntPtr hWnd, [In] IntPtr hdcDst, [In] IntPtr pptDst, [In] IntPtr psize, [In] IntPtr hdcSrc, [In] IntPtr pptSrc, [In] uint crKey, [In] IntPtr pblend, [In] UpdateLayeredWindowFlags dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage([In] IntPtr hWnd, [In] uint Msg, [In] uint wParam, [In] uint lParam);

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
    }

    public static class Dwmapi
    {
        [DllImport("dwmapi.dll")]
        public static extern IntPtr DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, [In] IntPtr pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, [In] ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        public static extern IntPtr DwmEnableBlurBehindWindow(IntPtr hwnd, [In] ref DWM_BLURBEHIND pBlurBehind);
    }

    public static class Shell32
    {
        [DllImport("shell32.dll")]
        public static extern UIntPtr SHAppBarMessage([In] AppBarMessage msg, [In, Out] ref APPBARDATA data);
    }

    public enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AccentPolicy
    {
        public AccentState AccentState;
        public AccentFlags AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }

    [Flags]
    public enum AccentFlags
    {
        // ...

        DrawLeftBorder = 0x20,
        DrawTopBorder = 0x40,
        DrawRightBorder = 0x80,
        DrawBottomBorder = 0x100,
        DrawAllBorders = DrawLeftBorder | DrawTopBorder | DrawRightBorder | DrawBottomBorder

        // ...
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    public enum WindowCompositionAttribute
    {
        // ...
        WCA_ACCENT_POLICY = 19
        // ...
    }

    [Flags]
    public enum DWM_BLURBEHIND_FLAGS
    {
        DWM_BB_ENABLE = 1,
        DWM_BB_BLURREGION = 2,
        DWM_BB_TRANSITIONONMAXIMIZED = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DWM_BLURBEHIND
    {
        public DWM_BLURBEHIND_FLAGS dwFlags;
        public bool fEnable;
        public IntPtr hRgnBlur;
        public bool fTransitionOnMaximized;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int cxLeftWidth;      // width of left border that retains its size
        public int cxRightWidth;     // width of right border that retains its size
        public int cyTopHeight;      // height of top border that retains its size
        public int cyBottomHeight;   // height of bottom border that retains its size
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BLENDFUNCTION
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;

        public BLENDFUNCTION(BlendOperation op, byte flags, byte alpha, AlphaFormat format)
        {
            BlendOp = (byte)op;
            BlendFlags = flags;
            SourceConstantAlpha = alpha;
            AlphaFormat = (byte)format;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE
    {
        public int cx;
        public int cy;
    }

    public enum AppBarMessage : uint
    {
        ABM_GETTASKBARPOS = 0x00000005
    }

    public enum BlendOperation : byte
    {
        AC_SRC_OVER = 0x00
    }

    public enum AlphaFormat : byte
    {
        AC_SRC_ALPHA = 0x01
    }

    public enum WindowMessage : uint
    {
        WM_SETREDRAW = 0x000B
    }

    [Flags]
    public enum UpdateLayeredWindowFlags : uint
    {
        ULW_ALPHA = 0x00000002
    }

    public enum DWMWINDOWATTRIBUTE
    {
        /// <summary>
        ///     Use with<see cref="Dwmapi.DwmGetWindowAttribute" />. Discovers whether non-client rendering is enabled. The retrieved value is of type BOOL. TRUE
        ///     if non-client rendering is enabled; otherwise, FALSE.
        /// </summary>
        DWMWA_NCRENDERING_ENABLED = 1,

        /// <summary>
        ///     Use with <see cref="Dwmapi.DwmSetWindowAttribute" />\. Sets the non-client rendering policy. The pvAttribute parameter points to a value from the
        ///     <see cref="DWMNCRENDERINGPOLICY" /> enumeration.
        /// </summary>
        DWMWA_NCRENDERING_POLICY,

        /// <summary>
        ///     Use with <see cref="Dwmapi.DwmSetWindowAttribute" />. Enables or forcibly disables DWM transitions. The pvAttribute parameter points to a value
        ///     of TRUE to disable transitions or FALSE to enable transitions.
        /// </summary>
        DWMWA_TRANSITIONS_FORCEDISABLED,

        /// <summary>
        ///     Use with <see cref="Dwmapi.DwmSetWindowAttribute" />. Enables content rendered in the non-client area to be visible on the frame drawn by DWM.
        ///     The pvAttribute parameter points to a value of TRUE to enable content rendered in the non-client area to be visible on the frame; otherwise, it
        ///     points to FALSE.
        /// </summary>
        DWMWA_ALLOW_NCPAINT,

        /// <summary>
        ///     Use with <see cref="Dwmapi.DwmGetWindowAttribute" />. Retrieves the bounds of the caption button area in the window-relative space. The retrieved
        ///     value is of type RECT.
        /// </summary>
        DWMWA_CAPTION_BUTTON_BOUNDS,

        /// <summary>
        ///     Use with <see cref="Dwmapi.DwmSetWindowAttribute" />. Specifies whether non-client content is right-to-left (RTL) mirrored. The pvAttribute
        ///     parameter points to a value of TRUE if the non-client content is right-to-left (RTL) mirrored; otherwise, it points to FALSE.
        /// </summary>
        DWMWA_NONCLIENT_RTL_LAYOUT,

        /// <summary>
        ///     Use with <see cref="Dwmapi.DwmSetWindowAttribute" /> . Forces the window to display an iconic thumbnail or peek representation (a static bitmap),
        ///     even if a live or snapshot representation of the window is available. This value normally is set during a window's creation and not changed
        ///     throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The pvAttribute parameter points to a
        ///     value of TRUE to require a iconic thumbnail or peek representation; otherwise, it points to FALSE.
        /// </summary>
        DWMWA_FORCE_ICONIC_REPRESENTATION,

        /// <summary>
        ///     Use with <see cref="Dwmapi.DwmSetWindowAttribute" />. Sets how Flip3D treats the window. The pvAttribute parameter points to a value from the
        ///     <see cref="DWMFLIP3DWINDOWPOLICY" /> enumeration.
        /// </summary>
        DWMWA_FLIP3D_POLICY,

        /// <summary>
        ///     Use with <see cref="Dwmapi.DwmGetWindowAttribute" />. Retrieves the extended frame bounds rectangle in screen space. The retrieved value is of
        ///     type <see cref="RECT" />.
        /// </summary>
        DWMWA_EXTENDED_FRAME_BOUNDS,

        /// <summary>
        ///     Use with<see cref="Dwmapi.DwmSetWindowAttribute" />. The window will provide a bitmap for use by DWM as an iconic thumbnail or peek
        ///     representation (a static bitmap) for the window. <see cref="DWMWA_HAS_ICONIC_BITMAP" /> can be specified with
        ///     <see cref="DWMWA_FORCE_ICONIC_REPRESENTATION" />. <see cref="DWMWA_HAS_ICONIC_BITMAP" /> normally is set during a window's creation and not
        ///     changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The pvAttribute parameter points
        ///     to a value of TRUE to inform DWM that the window will provide an iconic thumbnail or peek representation; otherwise, it points to FALSE.
        /// </summary>
        DWMWA_HAS_ICONIC_BITMAP,

        /// <summary>
        ///     Use with <see cref="Dwmapi.DwmSetWindowAttribute" />. Do not show peek preview for the window. The peek view shows a full-sized preview of the
        ///     window when the mouse hovers over the window's thumbnail in the taskbar. If this attribute is set, hovering the mouse pointer over the window's
        ///     thumbnail dismisses peek (in case another window in the group has a peek preview showing). The pvAttribute parameter points to a value of TRUE to
        ///     prevent peek functionality or FALSE to allow it.
        /// </summary>
        DWMWA_DISALLOW_PEEK,

        /// <summary>
        ///     Use with <see cref="Dwmapi.DwmSetWindowAttribute" />. Prevents a window from fading to a glass sheet when peek is invoked. The pvAttribute
        ///     parameter points to a value of TRUE to prevent the window from fading during another window's peek or FALSE for normal behavior.
        /// </summary>
        DWMWA_EXCLUDED_FROM_PEEK,

        /// <summary>
        ///     The maximum recognized <see cref="DWMWINDOWATTRIBUTE" /> value, used for validation purposes.
        /// </summary>
        DWMWA_LAST
    };

    /// <summary>
    ///     Flags used by the <see cref="Dwmapi.DwmSetWindowAttribute" /> function to specify the non-client area rendering policy.
    /// </summary>
    public enum DWMNCRENDERINGPOLICY
    {
        /// <summary>The non-client rendering area is rendered based on the window style.</summary>
        DWMNCRP_USEWINDOWSTYLE,

        /// <summary>The non-client area rendering is disabled; the window style is ignored.</summary>
        DWMNCRP_DISABLED,

        /// <summary>The non-client area rendering is enabled; the window style is ignored.</summary>
        DWMNCRP_ENABLED,

        /// <summary>
        ///     The maximum recognized <see cref="DWMNCRENDERINGPOLICY" /> value, used for validation purposes.
        /// </summary>
        DWMNCRP_LAST
    };
}
