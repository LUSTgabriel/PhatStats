using PhatStats.Diagnostics;
using PhatStats.Hardware;
using PhatStats.Serial;
using PhatStats.Settings;

namespace PhatStats.Runtime;

public sealed class TelemetryCoordinator : IDisposable
{
    private readonly HardwareTelemetryEngine _hardware;
    private readonly LatestSnapshotMailbox _mailbox;
    private readonly SerialDeviceManager _serial;
    private readonly SettingsManager _settings;
    private readonly DiagnosticState _diagnostics;
    private readonly SevereErrorRing _errors;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;
    private bool _disposed;

    public TelemetryCoordinator(HardwareTelemetryEngine hardware, LatestSnapshotMailbox mailbox,
        SerialDeviceManager serial, SettingsManager settings, DiagnosticState diagnostics, SevereErrorRing errors)
    {
        _hardware = hardware;
        _mailbox = mailbox;
        _serial = serial;
        _settings = settings;
        _diagnostics = diagnostics;
        _errors = errors;
        _worker = Task.Run(LoopAsync);
    }

    private async Task LoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            var started = Environment.TickCount64;
            try
            {
                var settings = _settings.Current;
                var snapshot = _hardware.Sample(settings.SelectedGpuIdentifier);
                _mailbox.Publish(snapshot);
                _diagnostics.UpdateTelemetry(snapshot, _hardware.Gpus, _hardware.SensorMappings);
                _serial.NotifySnapshotAvailable();
            }
            catch (Exception ex)
            {
                _errors.Add("Telemetry", "SAMPLE_FAILED", ex.Message);
            }

            var interval = _settings.Current.SamplingIntervalMs;
            var elapsed = Math.Max(0, Environment.TickCount64 - started);
            var delay = Math.Max(1, interval - (int)Math.Min(int.MaxValue, elapsed));
            try
            {
                await Task.Delay(delay, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public void SystemResumed()
    {
        _hardware.ResumeOrInvalidate();
        _serial.SystemResumed();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _cts.Cancel();
        try { _worker.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
        _cts.Dispose();
    }
}
