using AwqatSalaat.Interop;
using System;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

namespace AwqatSalaat.UI.Controls
{
    public partial class AcrylicPopup : Popup
    {
        private uint _tintColor = 0x2c2c2c; /* BGR color format */
        private uint _tintOpacity;

        public Color TintColor
        {
            get => Color.FromRgb((byte)_tintColor, (byte)(_tintColor >> 8), (byte)(_tintColor >> 16));
            set => _tintColor = value.R | ((uint)value.G << 8) | ((uint)value.B << 16);
        }

        public double TintOpacity
        {
            get => _tintOpacity / 255d;
            set => _tintOpacity = (uint)((value < 0 ? 0 : value > 1 ? 1 : value) * 255);
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

        private void EnableBlur()
        {
            var accent = new AccentPolicy();
            accent.GradientColor = (_tintOpacity << 24) | (_tintColor & 0xFFFFFF);

            IntPtr popupWindowHandle = ((HwndSource)HwndSource.FromVisual(this.Child)).Handle;

            AcrylicBlur.EnableAcrylicBlur(popupWindowHandle, accent);
        }
    }
}
