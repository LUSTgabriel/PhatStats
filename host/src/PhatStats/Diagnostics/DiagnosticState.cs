using PhatStats.Models;
using PhatStats.Serial;

namespace PhatStats.Diagnostics;

public sealed record DiagnosticSnapshot(
    TelemetrySnapshot? Telemetry,
    SerialConnectionState SerialState,
    string? SerialPort,
    IReadOnlyList<SerialDeviceInfo> Ports,
    IReadOnlyList<GpuDeviceDescriptor> Gpus,
    IReadOnlyList<SensorResolutionDescriptor> SensorMappings,
    string LastEncodedBatch,
    DateTimeOffset? LastSuccessfulSend,
    int RetryCount,
    string? LastTransportError);

public sealed class DiagnosticState
{
    private readonly object _gate = new();
    private DiagnosticSnapshot _snapshot = new(
        null,
        SerialConnectionState.Unconfigured,
        null,
        Array.Empty<SerialDeviceInfo>(),
        Array.Empty<GpuDeviceDescriptor>(),
        Array.Empty<SensorResolutionDescriptor>(),
        string.Empty,
        null,
        0,
        null);

    public DiagnosticSnapshot Snapshot()
    {
        lock (_gate)
            return _snapshot;
    }

    public void UpdateTelemetry(TelemetrySnapshot snapshot, IReadOnlyList<GpuDeviceDescriptor> gpus,
        IReadOnlyList<SensorResolutionDescriptor> mappings)
    {
        lock (_gate)
            _snapshot = _snapshot with
            {
                Telemetry = snapshot,
                Gpus = gpus,
                SensorMappings = mappings
            };
    }

    public void UpdateSerial(SerialConnectionState state, string? port, IReadOnlyList<SerialDeviceInfo> ports,
        string lastBatch, DateTimeOffset? lastSuccessfulSend, int retryCount, string? lastError)
    {
        lock (_gate)
            _snapshot = _snapshot with
            {
                SerialState = state,
                SerialPort = port,
                Ports = ports,
                LastEncodedBatch = lastBatch,
                LastSuccessfulSend = lastSuccessfulSend,
                RetryCount = retryCount,
                LastTransportError = lastError
            };
    }
}
