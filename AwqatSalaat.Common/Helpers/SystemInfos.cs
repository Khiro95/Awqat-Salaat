using AwqatSalaat.Interop;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace AwqatSalaat.Helpers
{
    public static class SystemInfos
    {
        private static readonly int osBuildNumber;

        // https://en.wikipedia.org/wiki/List_of_Microsoft_Windows_versions
        private const int Windows7BuildNumber = 7601;
        private const int Windows8BuildNumber = 9200;
        private const int Windows81BuildNumber = 9600;
        private const int Windows10_Min_BuildNumber = 10240;
        private const int Windows10_RS4_BuildNumber = 17134;
        private const int Windows10_19H1_BuildNumber = 18362;
        private const int Windows11_Min_BuildNumber = 22000;

        public static bool IsWindows7 => osBuildNumber == Windows7BuildNumber;
        public static bool IsWindows8 => osBuildNumber == Windows8BuildNumber;
        public static bool IsWindows81 => osBuildNumber == Windows81BuildNumber;
        public static bool IsWindows81_OrEarlier => osBuildNumber <= Windows81BuildNumber && osBuildNumber >= Windows7BuildNumber;
        public static bool IsWindows10 => osBuildNumber >= Windows10_Min_BuildNumber && osBuildNumber < Windows11_Min_BuildNumber;
        public static bool IsWindows10_Redstone4_OrLater => osBuildNumber >= Windows10_RS4_BuildNumber;
        public static bool IsWindows10_19H1_OrLater => osBuildNumber >= Windows10_19H1_BuildNumber;
        public static bool IsWindows11_OrLater => osBuildNumber >= Windows11_Min_BuildNumber;

        static SystemInfos()
        {
            osBuildNumber = GetOSBuildNumber();
        }

        public static bool IsTaskBarHorizontal()
        {
            RECT bounds = GetTaskBarBounds();

            return (bounds.right - bounds.left) > (bounds.bottom - bounds.top);
        }

        public static RECT GetTaskBarBounds()
        {
            APPBARDATA data = new APPBARDATA();
            data.cbSize = Marshal.SizeOf(data);
            Shell32.SHAppBarMessage(AppBarMessage.ABM_GETTASKBARPOS, ref data);

            return data.rc;
        }

        public static bool? IsLightThemeUsed()
        {
            if (IsWindows10_19H1_OrLater)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        int value = Convert.ToInt32(key.GetValue("SystemUsesLightTheme", 0));
                        return value != 0;
                    }
                }
            }

            return null;
        }

        public static bool? IsAccentColorOnTaskBar()
        {
            if (IsWindows10_19H1_OrLater)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        int value = Convert.ToInt32(key.GetValue("ColorPrevalence", 0));
                        return value != 0;
                    }
                }
            }

            return null;
        }

        public static bool IsTaskBarCentered()
        {
            if (IsWindows11_OrLater)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (key != null)
                    {
                        int value = Convert.ToInt32(key.GetValue("TaskbarAl", 1));
                        return value == 1;
                    }

                    // Taskbar is centered by default in Windows 11
                    return true;
                }
            }

            return false;
        }

        public static bool IsTaskBarWidgetsEnabled()
        {
            if (IsWindows11_OrLater)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (key != null)
                    {
                        int value = Convert.ToInt32(key.GetValue("TaskbarDa", 1));
                        return value == 1;
                    }
                }

                // Widgets button is enabled by default in Windows 11
                return true;
            }

            return false;
        }

        private static int GetOSBuildNumber()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                var osBuildNumberValue = key.GetValue("CurrentBuildNumber");
                return Convert.ToInt32(osBuildNumberValue);
            }
        }

        // https://stackoverflow.com/a/50848113/4644774
        public static (byte r, byte g, byte b, byte a) GetAccentColor()
        {
            if (IsWindows10_19H1_OrLater)
            {
                using (RegistryKey dwmKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM"))
                {
                    if (dwmKey != null)
                    {
                        var accentColor = Convert.ToInt32(dwmKey.GetValue("AccentColor", 0xff000000));
                        return ParseDWordColor(accentColor);
                    }
                }
            }

            return (0, 0, 0, 0);
        }

        private static (byte r, byte g, byte b, byte a) ParseDWordColor(int color)
        {
            byte
                a = (byte)((color >> 24) & 0xFF),
                b = (byte)((color >> 16) & 0xFF),
                g = (byte)((color >> 8) & 0xFF),
                r = (byte)((color >> 0) & 0xFF);

            return (r, g, b, a);
        }
    }
}
