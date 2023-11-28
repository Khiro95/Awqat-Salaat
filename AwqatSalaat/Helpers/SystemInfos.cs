using AwqatSalaat.Interop;
using System.Runtime.InteropServices;

namespace AwqatSalaat.Helpers
{
    public static class SystemInfos
    {
        public static bool IsTaskBarHorizontal()
        {
            APPBARDATA data = new APPBARDATA();
            data.cbSize = Marshal.SizeOf(data);
            Shell32.SHAppBarMessage(AppBarMessage.ABM_GETTASKBARPOS, ref data);

            return (data.rc.right - data.rc.left) > (data.rc.bottom - data.rc.top);
        }
    }
}
