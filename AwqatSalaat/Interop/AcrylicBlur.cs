using AwqatSalaat.Helpers;
using System;
using System.Runtime.InteropServices;

namespace AwqatSalaat.Interop
{
    public class AcrylicBlur
    {
        public static void EnableAcrylicBlur(IntPtr hwnd, AccentPolicy accentPolicy)
        {
            if (SystemInfos.IsWindows10_Redstone4_OrLater)
            {
                accentPolicy.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            }
            else
            {
                accentPolicy.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
            }

            var accentStructSize = Marshal.SizeOf(accentPolicy);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accentPolicy, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            User32.SetWindowCompositionAttribute(hwnd, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        // For Windows 7 we have to use DWM, no Acrylic blur available
        public static void EnableBlurBehindWin7(IntPtr hwnd, bool isEnable)
        {
            var data = new DWM_BLURBEHIND()
            {
                dwFlags = DWM_BLURBEHIND_FLAGS.DWM_BB_ENABLE,
                fEnable = isEnable
            };

            Dwmapi.DwmEnableBlurBehindWindow(hwnd, ref data);
        }

        public static void DisableAcrylicBlur(IntPtr hwnd)
        {
            AccentPolicy accentPolicy = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_DISABLED
            };

            var accentStructSize = Marshal.SizeOf(accentPolicy);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accentPolicy, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            User32.SetWindowCompositionAttribute(hwnd, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }
    }
}
