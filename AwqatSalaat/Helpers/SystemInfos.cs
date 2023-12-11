using AwqatSalaat.Interop;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace AwqatSalaat.Helpers
{
    public static class SystemInfos
    {
        private static int osBuildNumber;

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

        static SystemInfos()
        {
            GetOSBuildNumber();
        }

        public static bool IsTaskBarHorizontal()
        {
            APPBARDATA data = new APPBARDATA();
            data.cbSize = Marshal.SizeOf(data);
            Shell32.SHAppBarMessage(AppBarMessage.ABM_GETTASKBARPOS, ref data);

            return (data.rc.right - data.rc.left) > (data.rc.bottom - data.rc.top);
        }

        public static bool? IsLightThemeUsed()
        {
            if (IsWindows10_19H1_OrLater)
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        int value = (int)key.GetValue("SystemUsesLightTheme", 0);
                        return value != 0;
                    }
                }
            }
            return null;
        }

        private static void GetOSBuildNumber()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                var osBuildNumberValue = key.GetValue("CurrentBuildNumber");
                osBuildNumber = Convert.ToInt32(osBuildNumberValue);
            }
        }
    }
}
