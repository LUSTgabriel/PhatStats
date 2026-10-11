namespace PhatStats.Serial;

public enum SerialConnectionState
{
    Unconfigured,
    Ambiguous,
    NoDevice,
    Available,
    Connecting,
    Connected,
    Busy,
    Recovering,
    Switching,
    Paused
}

public sealed record SerialStatus(
    SerialConnectionState State,
    string? PortName,
    string Summary,
    int RetryCount,
    string? LastError);
