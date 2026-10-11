using LibreHardwareMonitor.Hardware;
using PhatStats.Diagnostics;
using PhatStats.Models;

namespace PhatStats.Hardware;

public sealed class HardwareTelemetryEngine : IDisposable
{
    private static readonly int[] ReresolveBackoffSeconds = [5, 15, 60, 300];
    private static readonly TimeSpan BackendRebuildCooldown = TimeSpan.FromSeconds(60);

    private readonly object _gate = new();
    private readonly SevereErrorRing _errors;
    private readonly SnapshotValidator _validator = new();
    private Computer? _computer;
    private SensorBindings? _bindings;
    private string? _boundGpuPreference;
    private GpuDeviceDescriptor[] _gpus = [];
    private SensorResolutionDescriptor[] _sensorMappings = [];
    private DateTimeOffset _nextReresolveAttempt = DateTimeOffset.MinValue;
    private int _reresolveAttempt;
    private DateTimeOffset _lastBackendRebuild = DateTimeOffset.MinValue;

    public HardwareTelemetryEngine(SevereErrorRing errors)
    {
        _errors = errors;
        RebuildBackend(force: true);
    }

    public TelemetrySnapshot Sample(string? selectedGpuIdentifier)
    {
        lock (_gate)
        {
            EnsureBackend();
            if (_bindings is null || !string.Equals(_boundGpuPreference, selectedGpuIdentifier, StringComparison.OrdinalIgnoreCase))
                Resolve(selectedGpuIdentifier);

            try
            {
                _bindings?.Cpu.Update();
                if (_bindings?.Gpu != _bindings?.Cpu)
                    _bindings?.Gpu.Update();
            }
            catch (Exception ex)
            {
                _errors.Add("Hardware", "BACKEND_UPDATE_FAILED", ex.Message);

                // Do not spin-create LibreHardwareMonitor Computer instances on a persistent
                // driver/sensor failure. Repeated open/close cycles are expensive and can grow
                // native working set in vendor/driver code. One rebuild per cooldown is enough.
                if (RebuildBackend(force: false))
                    Resolve(selectedGpuIdentifier);
            }

            var mem = WindowsMemoryReader.Read();
            var b = _bindings;
            var raw = new RawTelemetrySnapshot
            {
                Timestamp = DateTimeOffset.UtcNow,
                CpuName = b?.CpuName ?? "AMD CPU",
                CpuTemperature = Value(b?.CpuTemperature),
                CpuLoad = Value(b?.CpuLoad),
                CpuClock = MaxClock(b?.CpuClocks),
                CpuPower = Value(b?.CpuPower),
                GpuName = b?.GpuName ?? "NVIDIA GeForce GPU",
                GpuIdentifier = b?.GpuIdentifier,
                GpuTemperature = Value(b?.GpuTemperature),
                GpuLoad = Value(b?.GpuLoad),
                GpuCoreClock = Value(b?.GpuCoreClock),
                GpuMemoryClock = Value(b?.GpuMemoryClock),
                GpuShaderClock = Value(b?.GpuShaderClock),
                GpuMemoryTotal = Value(b?.GpuMemoryTotal),
                GpuMemoryUsed = Value(b?.GpuMemoryUsed),
                GpuMemoryLoad = Value(b?.GpuMemoryLoad),
                GpuPower = Value(b?.GpuPower),
                GpuFanSpeedLoad = Value(b?.GpuFanLoad),
                GpuFanSpeedRpm = Value(b?.GpuFanRpm),
                RamLoad = mem.LoadPercent,
                RamUsed = mem.UsedGb,
                RamAvailable = mem.AvailableGb
            };

            var snapshot = _validator.Validate(raw);
            var now = DateTimeOffset.UtcNow;
            if (!_validator.NeedsReresolution)
            {
                _reresolveAttempt = 0;
                _nextReresolveAttempt = DateTimeOffset.MinValue;
            }
            else if (_nextReresolveAttempt == DateTimeOffset.MinValue || now >= _nextReresolveAttempt)
            {
                // Sensor loss only re-runs the resolver against the already-open backend.
                // A missing optional/driver sensor must never cause an endless backend rebuild loop.
                Resolve(selectedGpuIdentifier);
                _reresolveAttempt++;
                var delay = ReresolveBackoffSeconds[Math.Min(_reresolveAttempt - 1, ReresolveBackoffSeconds.Length - 1)];
                _nextReresolveAttempt = now.AddSeconds(delay);
            }

            return snapshot;
        }
    }

    // These arrays are immutable snapshots replaced only while holding _gate.
    // Returning the current reference avoids two ToArray allocations every telemetry sample.
    public IReadOnlyList<GpuDeviceDescriptor> Gpus
    {
        get
        {
            lock (_gate)
                return _gpus;
        }
    }

    public IReadOnlyList<SensorResolutionDescriptor> SensorMappings
    {
        get
        {
            lock (_gate)
                return _sensorMappings;
        }
    }

    public void ResumeOrInvalidate()
    {
        lock (_gate)
        {
            RebuildBackend(force: true);
            _reresolveAttempt = 0;
            _nextReresolveAttempt = DateTimeOffset.MinValue;
        }
    }

    private void Resolve(string? preferred)
    {
        if (_computer is null)
            return;

        DisableSensorHistory(_computer.Hardware);
        var result = SensorResolver.Resolve(_computer, preferred);
        _bindings = result.Bindings;
        _gpus = result.Gpus as GpuDeviceDescriptor[] ?? result.Gpus.ToArray();
        _sensorMappings = result.Bindings?.Resolutions as SensorResolutionDescriptor[]
            ?? result.Bindings?.Resolutions.ToArray()
            ?? [];
        _boundGpuPreference = preferred;
    }

    private void EnsureBackend()
    {
        if (_computer is null)
            RebuildBackend(force: false);
    }

    private bool RebuildBackend(bool force)
    {
        var now = DateTimeOffset.UtcNow;
        if (!force && _lastBackendRebuild != DateTimeOffset.MinValue &&
            now - _lastBackendRebuild < BackendRebuildCooldown)
            return false;

        var old = _computer;
        _computer = null;
        _bindings = null;
        _boundGpuPreference = null;
        _gpus = [];
        _sensorMappings = [];

        if (old is not null)
        {
            try { old.Close(); } catch { }
        }

        Computer? computer = null;
        try
        {
            computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true };
            computer.Open();
            DisableSensorHistory(computer.Hardware);
            _computer = computer;
            _lastBackendRebuild = now;
            return true;
        }
        catch (Exception ex)
        {
            // Open can fail after vendor groups have already allocated native resources.
            // Close the partially opened instance before dropping the reference.
            try { computer?.Close(); } catch { }
            _lastBackendRebuild = now;
            _errors.Add("Hardware", "BACKEND_OPEN_FAILED", ex.Message);
            return false;
        }
    }

    private static void DisableSensorHistory(IEnumerable<IHardware> hardware)
    {
        foreach (var item in hardware)
        {
            foreach (var sensor in item.Sensors)
            {
                try
                {
                    // LibreHardwareMonitor 0.9.6 keeps up to one day of SensorValue history
                    // by default. PhatStats only consumes the current Value, so retaining that
                    // history is pure memory growth for this long-running 1 Hz sampler.
                    sensor.ValuesTimeWindow = TimeSpan.Zero;
                }
                catch
                {
                    // A vendor-specific sensor that rejects history configuration must not
                    // prevent hardware telemetry from starting.
                }
            }

            DisableSensorHistory(item.SubHardware);
        }
    }

    private static double? Value(ISensor? sensor) =>
        sensor?.Value is float value && float.IsFinite(value) ? value : null;

    private static double? MaxClock(IEnumerable<ISensor>? sensors)
    {
        if (sensors is null)
            return null;
        double? max = null;
        foreach (var sensor in sensors)
        {
            if (sensor.Value is float value && float.IsFinite(value) && value > 0)
                max = !max.HasValue || value > max ? value : max;
        }
        return max;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            var computer = _computer;
            _computer = null;
            _bindings = null;
            _gpus = [];
            _sensorMappings = [];
            try { computer?.Close(); } catch { }
        }
    }
}
