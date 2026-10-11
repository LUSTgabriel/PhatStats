using System.Runtime.InteropServices;

namespace PhatStats.Hardware;

internal static class WindowsMemoryReader
{
    public static (double UsedGb, double AvailableGb, double LoadPercent) Read()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status))
            return (0, 0, 0);

        const double gib = 1024d * 1024d * 1024d;
        var total = status.ullTotalPhys / gib;
        var available = status.ullAvailPhys / gib;
        var used = Math.Max(0, total - available);
        return (used, available, status.dwMemoryLoad);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
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

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
