using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;

namespace CpuTrayMeter;

// winapi
internal static partial class InteropFunctions
{
    public const int GWL_STYLE = -16;
    public const int GWL_EXSTYLE = -20;

    public const uint WS_POPUP = 0x80000000;
    public const uint WS_SYSMENU = 0x00080000;

    public const uint WS_EX_APPWINDOW = 0x00040000;
    public const uint WS_EX_CONTROLPARENT = 0x00010000;
    public const uint WS_EX_TOOLWINDOW = 0x00000080;
    public const uint WS_EX_TRANSPARENT = 0x00000020;

    [StructLayout(LayoutKind.Sequential)]
    public struct LPRECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    public static partial IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    public static partial IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr FindWindowW(string? lpClassName, string? lpWindowName);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(IntPtr hWnd, out LPRECT lpRect);

    [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial IntPtr FindWindowExW(IntPtr hWndParent, IntPtr hWndChildAfter, string? lpszClass, string? lpszWindow);

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    public static IntPtr ModifyWindowFlags(IntPtr hwnd, int style, long addFlags = 0, long removeFlags = 0)
    {
        long flags = GetWindowLongPtr(hwnd, style).ToInt64();
        return SetWindowLongPtr(hwnd, style, new IntPtr((flags & ~removeFlags) | addFlags));
    }

    // returns window rect in DIPs (device-independent pixels)
    public static DipRect GetWindowRectInDips(IntPtr hWnd, DpiScale dpiScale)
    {
        bool res = GetWindowRect(hWnd, out LPRECT lp);
        if (!res)
            throw new InvalidOperationException($"GetWindowRect({hWnd:x}) failed");

        return new DipRect(
            lp.Left / dpiScale.DpiScaleX,
            lp.Top / dpiScale.DpiScaleY,
            lp.Right / dpiScale.DpiScaleX,
            lp.Bottom / dpiScale.DpiScaleY);
    }

    // available to total system RAM in bytes
    public static (ulong avail, ulong total) GetRamUsage()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };

        if (!GlobalMemoryStatusEx(ref status))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return (status.ullAvailPhys, status.ullTotalPhys);
    }
}
