using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
}
