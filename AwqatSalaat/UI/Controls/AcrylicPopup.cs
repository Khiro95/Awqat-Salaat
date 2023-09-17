using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace AwqatSalaat.UI.Controls
{
    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public AccentFlags AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }

    [Flags]
    internal enum AccentFlags
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
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        // ...
        WCA_ACCENT_POLICY = 19
        // ...
    }

    public partial class AcrylicPopup : Popup
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private uint _tintColor = 0x990000; /* BGR color format */
        private uint _tintOpacity;

        public Color TintColor
        {
            get => Color.FromRgb((byte)_tintColor, (byte)(_tintColor >> 8), (byte)(_tintColor >> 16));
            set => _tintColor = value.R | ((uint)value.G << 8) | ((uint)value.B << 16);
        }

        public double TintOpacity
        {
            get => _tintOpacity / 255d;
            set => _tintOpacity = (uint)(value * 255);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            EnableBlur();
            //  0 -> Dark
            //  1 -> Light
            // -1 -> Unknown
            int res = (int)Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "SystemUsesLightTheme", -1);
        }

        internal void EnableBlur()
        {
            //var windowHelper = new WindowInteropHelper(this.);

            var accent = new AccentPolicy();
            accent.AccentFlags = AccentFlags.DrawAllBorders;
            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = (_tintOpacity << 24) | (_tintColor & 0xFFFFFF);

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            //SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            SetWindowCompositionAttribute(((HwndSource)HwndSource.FromVisual(this.Child)).Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }
    }
}
