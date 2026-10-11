namespace PhatStats.Serial;

public sealed record SerialDeviceIdentity
{
    public string? Vid { get; init; }
    public string? Pid { get; init; }
    public string? UsbSerialNumber { get; init; }
    public string? DeviceInstanceId { get; init; }
    public string? Location { get; init; }
    public string? FriendlyName { get; init; }
    public string? LastKnownPort { get; init; }

    public bool HasStableIdentity => !string.IsNullOrWhiteSpace(DeviceInstanceId) ||
        (!string.IsNullOrWhiteSpace(UsbSerialNumber) && !string.IsNullOrWhiteSpace(Vid) && !string.IsNullOrWhiteSpace(Pid));

    public int Score(SerialDeviceInfo device)
    {
        var score = 0;
        if (!string.IsNullOrWhiteSpace(DeviceInstanceId) && Eq(DeviceInstanceId, device.InstanceId)) score += 1000;
        if (!string.IsNullOrWhiteSpace(UsbSerialNumber) && Eq(UsbSerialNumber, device.UsbSerialNumber)) score += 700;
        if (!string.IsNullOrWhiteSpace(Vid) && Eq(Vid, device.Vid)) score += 120;
        if (!string.IsNullOrWhiteSpace(Pid) && Eq(Pid, device.Pid)) score += 120;
        if (!string.IsNullOrWhiteSpace(Location) && Eq(Location, device.Location)) score += 80;
        if (!string.IsNullOrWhiteSpace(FriendlyName) && Eq(FriendlyName, device.FriendlyName)) score += 20;
        if (!string.IsNullOrWhiteSpace(LastKnownPort) && Eq(LastKnownPort, device.PortName)) score += 5;
        return score;
    }

    public bool IsStrongMatch(SerialDeviceInfo device)
    {
        if (!string.IsNullOrWhiteSpace(DeviceInstanceId) && Eq(DeviceInstanceId, device.InstanceId)) return true;
        if (!string.IsNullOrWhiteSpace(UsbSerialNumber) && Eq(UsbSerialNumber, device.UsbSerialNumber) &&
            !string.IsNullOrWhiteSpace(Vid) && Eq(Vid, device.Vid) && !string.IsNullOrWhiteSpace(Pid) && Eq(Pid, device.Pid))
            return true;
        return string.IsNullOrWhiteSpace(UsbSerialNumber) && !string.IsNullOrWhiteSpace(Location) && Eq(Location, device.Location) &&
               !string.IsNullOrWhiteSpace(Vid) && Eq(Vid, device.Vid) && !string.IsNullOrWhiteSpace(Pid) && Eq(Pid, device.Pid);
    }

    private static bool Eq(string? a, string? b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

public sealed record SerialDeviceInfo(
    string PortName,
    string FriendlyName,
    string? Vid,
    string? Pid,
    string? UsbSerialNumber,
    string? InstanceId,
    string? Location,
    bool IsPhysicalUsb,
    bool IsBluetooth)
{
    public SerialDeviceIdentity ToIdentity() => new()
    {
        Vid = Vid, Pid = Pid, UsbSerialNumber = UsbSerialNumber, DeviceInstanceId = InstanceId,
        Location = Location, FriendlyName = FriendlyName, LastKnownPort = PortName
    };

    public string DisplayName => $"{PortName} — {FriendlyName}";
}
