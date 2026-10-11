using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;

namespace PhatStats.Serial;

public sealed class SetupApiPortEnumerator
{
    private static readonly Guid PortsClassGuid = new("4D36E978-E325-11CE-BFC1-08002BE10318");
    private const uint DigcfPresent = 0x00000002;
    private const uint SpdrpHardwareId = 0x00000001;
    private const uint SpdrpFriendlyName = 0x0000000C;
    private const uint SpdrpLocationInformation = 0x0000000D;
    private const uint SpdrpLocationPaths = 0x00000023;
    private const uint DicsFlagGlobal = 0x00000001;
    private const uint DiregDev = 0x00000001;
    private const int KeyRead = 0x20019;
    private static readonly IntPtr InvalidHandleValue = new(-1);

    public SerialDeviceInfo[] Enumerate()
    {
        var existing = new HashSet<string>(SerialPort.GetPortNames(), StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, SerialDeviceInfo>(StringComparer.OrdinalIgnoreCase);
        var portsClassGuid = PortsClassGuid;
        IntPtr set = SetupDiGetClassDevs(ref portsClassGuid, null, IntPtr.Zero, DigcfPresent);
        if (set != InvalidHandleValue)
        {
            try
            {
                for (uint index = 0; ; index++)
                {
                    var data = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                    if (!SetupDiEnumDeviceInfo(set, index, ref data))
                    {
                        if (Marshal.GetLastWin32Error() == 259) break; // ERROR_NO_MORE_ITEMS
                        continue;
                    }
                    var port = ReadPortName(set, data);
                    if (string.IsNullOrWhiteSpace(port) || !existing.Contains(port)) continue;
                    var friendly = ReadProperty(set, data, SpdrpFriendlyName) ?? port;
                    var hardwareId = ReadProperty(set, data, SpdrpHardwareId);
                    var instance = ReadInstanceId(set, data);
                    var locationPaths = ReadProperty(set, data, SpdrpLocationPaths);
                    var location = locationPaths?.Split('\0', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                        ?? ReadProperty(set, data, SpdrpLocationInformation);
                    var source = hardwareId + " " + instance;
                    var vid = ExtractToken(source, "VID_", 4);
                    var pid = ExtractToken(source, "PID_", 4);
                    var bluetooth = Contains(source, "BTHENUM") || Contains(friendly, "Bluetooth");
                    var usb = !bluetooth && (Contains(source, "USB\\") || vid is not null || Contains(friendly, "USB"));
                    var serial = ExtractUsbSerial(instance);
                    result[port] = new SerialDeviceInfo(port, friendly, vid, pid, serial, instance, location, usb, bluetooth);
                }
            }
            finally { SetupDiDestroyDeviceInfoList(set); }
        }

        foreach (var port in existing)
            result.TryAdd(port, new SerialDeviceInfo(port, port, null, null, null, null, null, false, false));

        return result.Values.Where(d => !d.IsBluetooth).OrderBy(PortSortKey).ThenBy(x => x.PortName, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static int PortSortKey(SerialDeviceInfo d)
    {
        var p = d.PortName;
        if (p.StartsWith("COM", StringComparison.OrdinalIgnoreCase) && int.TryParse(p.AsSpan(3), out var n)) return n;
        return int.MaxValue;
    }

    private static string? ReadPortName(IntPtr set, SP_DEVINFO_DATA data)
    {
        IntPtr hKey = SetupDiOpenDevRegKey(set, ref data, DicsFlagGlobal, 0, DiregDev, KeyRead);
        if (hKey == InvalidHandleValue) return null;
        try
        {
            uint size = 512;
            var buffer = new byte[size];
            var rc = RegQueryValueEx(hKey, "PortName", IntPtr.Zero, out _, buffer, ref size);
            if (rc != 0 || size < 2) return null;
            return Encoding.Unicode.GetString(buffer, 0, (int)Math.Min(size, (uint)buffer.Length)).TrimEnd('\0');
        }
        finally { RegCloseKey(hKey); }
    }

    private static string? ReadProperty(IntPtr set, SP_DEVINFO_DATA data, uint property)
    {
        var buffer = new byte[2048];
        if (!SetupDiGetDeviceRegistryProperty(set, ref data, property, out _, buffer, (uint)buffer.Length, out _)) return null;
        return Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }

    private static string? ReadInstanceId(IntPtr set, SP_DEVINFO_DATA data)
    {
        var sb = new StringBuilder(1024);
        return SetupDiGetDeviceInstanceId(set, ref data, sb, sb.Capacity, out _) ? sb.ToString() : null;
    }

    private static string? ExtractToken(string? source, string marker, int length)
    {
        if (string.IsNullOrEmpty(source)) return null;
        var i = source.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (i < 0 || i + marker.Length + length > source.Length) return null;
        var v = source.Substring(i + marker.Length, length);
        return v.All(Uri.IsHexDigit) ? v.ToUpperInvariant() : null;
    }

    private static string? ExtractUsbSerial(string? instance)
    {
        if (string.IsNullOrWhiteSpace(instance) || !instance.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase)) return null;
        var i = instance.LastIndexOf('\\');
        if (i < 0 || i == instance.Length - 1) return null;
        var tail = instance[(i + 1)..];
        return tail.Contains('&') ? null : tail;
    }

    private static bool Contains(string? source, string value) => source?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVINFO_DATA { public uint cbSize; public Guid ClassGuid; public uint DevInst; public IntPtr Reserved; }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, string? enumerator, IntPtr hwndParent, uint flags);
    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, uint memberIndex, ref SP_DEVINFO_DATA deviceInfoData);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceRegistryProperty(IntPtr set, ref SP_DEVINFO_DATA data, uint property, out uint propertyRegDataType, byte[] buffer, uint bufferSize, out uint requiredSize);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInstanceId(IntPtr set, ref SP_DEVINFO_DATA data, StringBuilder instanceId, int instanceIdSize, out int requiredSize);
    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiOpenDevRegKey(IntPtr set, ref SP_DEVINFO_DATA data, uint scope, uint hwProfile, uint keyType, int samDesired);
    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegQueryValueEx(IntPtr hKey, string valueName, IntPtr reserved, out uint type, byte[] data, ref uint dataSize);
    [DllImport("advapi32.dll")]
    private static extern int RegCloseKey(IntPtr hKey);
}
