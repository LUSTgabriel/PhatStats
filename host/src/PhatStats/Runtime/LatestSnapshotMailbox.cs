using PhatStats.Models;

namespace PhatStats.Runtime;

public sealed class LatestSnapshotMailbox
{
    private TelemetrySnapshot? _latest;
    private long _version;

    public long Publish(TelemetrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Interlocked.Exchange(ref _latest, snapshot);
        return Interlocked.Increment(ref _version);
    }

    public TelemetrySnapshot? Latest => Volatile.Read(ref _latest);
    public long Version => Volatile.Read(ref _version);
}
