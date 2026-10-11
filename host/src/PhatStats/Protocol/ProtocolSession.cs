using PhatStats.Models;

namespace PhatStats.Protocol;

/// <summary>
/// Stateless wire-session encoder for the current release.
///
/// IMPORTANT: the stock/current LOLIN V3.1.7.3 portrait renderer directly
/// parses every required tag from the received inputString on each redraw. It
/// does not advertise whether the optional MCU-side telemetry cache patch is
/// installed. Therefore the host must send one complete *current* portrait
/// frame for every transmitted sample.
///
/// This is not history replay: only the newest snapshot is encoded, and any
/// unsent older snapshot has already been overwritten by LatestSnapshotMailbox.
/// </summary>
public sealed class ProtocolSession
{
    private readonly ProtocolEncoder _encoder;

    public ProtocolSession(ProtocolEncoder encoder)
    {
        _encoder = encoder;
    }

    public string Encode(TelemetrySnapshot snapshot, ProtocolProfile profile, DateTimeOffset now, bool forceSeed,
        TimeSpan dynamicRefresh, TimeSpan fullSeedRefresh)
    {
        // Parameters are intentionally retained in the public shape for settings
        // compatibility with builds that experimented with delta transport.
        // The current safe release never sends a partial frame to an
        // unversioned/unacknowledged firmware endpoint.
        _ = now;
        _ = forceSeed;
        _ = dynamicRefresh;
        _ = fullSeedRefresh;
        return _encoder.EncodeSeed(snapshot, profile);
    }

    public void Reset()
    {
        // Stateless by design. There is no queued or historical protocol state.
    }
}
