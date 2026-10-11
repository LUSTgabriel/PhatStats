using System.Runtime.InteropServices;

namespace PhatStats.App;

internal sealed class SystemMessageWindow : NativeWindow, IDisposable
{
    private const int WmDeviceChange = 0x0219;
    private const int WmPowerBroadcast = 0x0218;
    private const int DbtDevtypDeviceinterface = 0x00000005;
    private const int DeviceNotifyWindowHandle = 0x00000000;
    private const int PbtApmResumeAutomatic = 0x0012;
    private const int PbtApmResumeSuspend = 0x0007;
    private readonly IntPtr _notification;

    public SystemMessageWindow()
    {
        CreateHandle(new CreateParams { Caption = "PhatStats.MessageWindow" });
        var filter = new DEV_BROADCAST_DEVICEINTERFACE
        {
            dbcc_size = Marshal.SizeOf<DEV_BROADCAST_DEVICEINTERFACE>(),
            dbcc_devicetype = DbtDevtypDeviceinterface,
            dbcc_classguid = new Guid("86E0D1E0-8089-11D0-9CE4-08003E301F73")
        };
        var ptr = Marshal.AllocHGlobal(filter.dbcc_size);
        try { Marshal.StructureToPtr(filter, ptr, false); _notification = RegisterDeviceNotification(Handle, ptr, DeviceNotifyWindowHandle); }
        finally { Marshal.FreeHGlobal(ptr); }
    }

    public event EventHandler? SerialDevicesChanged;
    public event EventHandler? SystemResumed;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmDeviceChange) SerialDevicesChanged?.Invoke(this, EventArgs.Empty);
        else if (m.Msg == WmPowerBroadcast && (m.WParam.ToInt32() == PbtApmResumeAutomatic || m.WParam.ToInt32() == PbtApmResumeSuspend))
            SystemResumed?.Invoke(this, EventArgs.Empty);
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_notification != IntPtr.Zero) UnregisterDeviceNotification(_notification);
        DestroyHandle();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DEV_BROADCAST_DEVICEINTERFACE
    {
        public int dbcc_size;
        public int dbcc_devicetype;
        public int dbcc_reserved;
        public Guid dbcc_classguid;
        public short dbcc_name;
    }
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnregisterDeviceNotification(IntPtr handle);
}
