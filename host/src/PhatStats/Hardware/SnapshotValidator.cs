using PhatStats.Models;

namespace PhatStats.Hardware;

internal sealed class SnapshotValidator
{
    private const int GraceSamples = 3;

    private readonly Field _cpuTemp = new("CpuTemperature", -20, 150);
    private readonly Field _cpuLoad = new("CpuLoad", 0, 100);
    private readonly Field _cpuClock = new("CpuClock", 0, 15000);
    private readonly Field _cpuPower = new("CpuPower", 0, 2000, required: false);
    private readonly Field _cpuFanLoad = new("CpuFanSpeedLoad", 0, 100, required: false);

    private readonly Field _gpuTemp = new("GpuTemperature", -20, 150);
    private readonly Field _gpuLoad = new("GpuLoad", 0, 100);
    private readonly Field _gpuCoreClock = new("GpuCoreClock", 0, 10000);
    private readonly Field _gpuMemoryClock = new("GpuMemoryClock", 0, 50000);
    private readonly Field _gpuShaderClock = new("GpuShaderClock", 0, 20000, required: false);
    private readonly Field _gpuMemoryTotal = new("GpuMemoryTotal", 0, 262144);
    private readonly Field _gpuMemoryUsed = new("GpuMemoryUsed", 0, 262144);
    private readonly Field _gpuMemoryLoad = new("GpuMemoryLoad", 0, 100, required: false);
    private readonly Field _gpuPower = new("GpuPower", 0, 2000);
    private readonly Field _gpuFanLoad = new("GpuFanSpeedLoad", 0, 100);
    private readonly Field _gpuFanRpm = new("GpuFanSpeedRpm", 0, 100000);

    private readonly Field _ramLoad = new("RamLoad", 0, 100);
    private readonly Field _ramUsed = new("RamUsed", 0, 4096);
    private readonly Field _ramAvailable = new("RamAvailable", 0, 4096);

    private readonly Field[] _all;

    public SnapshotValidator()
    {
        _all =
        [
            _cpuTemp, _cpuLoad, _cpuClock, _cpuPower, _cpuFanLoad,
            _gpuTemp, _gpuLoad, _gpuCoreClock, _gpuMemoryClock, _gpuShaderClock,
            _gpuMemoryTotal, _gpuMemoryUsed, _gpuMemoryLoad, _gpuPower, _gpuFanLoad, _gpuFanRpm,
            _ramLoad, _ramUsed, _ramAvailable
        ];
    }

    public bool NeedsReresolution
    {
        get
        {
            foreach (var telemetryField in _all)
            {
                if (telemetryField.Required && telemetryField.InvalidCount > GraceSamples)
                    return true;
            }
            return false;
        }
    }

    public TelemetrySnapshot Validate(RawTelemetrySnapshot raw)
    {
        List<string>? degraded = null;

        double Read(Field telemetryField, double? value)
        {
            var result = telemetryField.Read(value);
            if (result.Degraded && telemetryField.Required)
                (degraded ??= new List<string>(4)).Add(telemetryField.Name);
            return result.Value;
        }

        return new TelemetrySnapshot
        {
            Timestamp = raw.Timestamp,
            CpuName = raw.CpuName,
            CpuTemperature = Read(_cpuTemp, raw.CpuTemperature),
            CpuLoad = Read(_cpuLoad, raw.CpuLoad),
            CpuClock = Read(_cpuClock, raw.CpuClock),
            CpuPower = Read(_cpuPower, raw.CpuPower),
            CpuFanSpeedLoad = Read(_cpuFanLoad, raw.CpuFanSpeedLoad),

            GpuName = raw.GpuName,
            GpuIdentifier = raw.GpuIdentifier,
            GpuTemperature = Read(_gpuTemp, raw.GpuTemperature),
            GpuLoad = Read(_gpuLoad, raw.GpuLoad),
            GpuCoreClock = Read(_gpuCoreClock, raw.GpuCoreClock),
            GpuMemoryClock = Read(_gpuMemoryClock, raw.GpuMemoryClock),
            GpuShaderClock = Read(_gpuShaderClock, raw.GpuShaderClock),
            GpuMemoryTotal = Read(_gpuMemoryTotal, raw.GpuMemoryTotal),
            GpuMemoryUsed = Read(_gpuMemoryUsed, raw.GpuMemoryUsed),
            GpuMemoryLoad = Read(_gpuMemoryLoad, raw.GpuMemoryLoad),
            GpuPower = Read(_gpuPower, raw.GpuPower),
            GpuFanSpeedLoad = Read(_gpuFanLoad, raw.GpuFanSpeedLoad),
            GpuFanSpeedRpm = Read(_gpuFanRpm, raw.GpuFanSpeedRpm),

            RamLoad = Read(_ramLoad, raw.RamLoad),
            RamUsed = Read(_ramUsed, raw.RamUsed),
            RamAvailable = Read(_ramAvailable, raw.RamAvailable),

            Health = degraded is null ? TelemetryHealth.Healthy : TelemetryHealth.Degraded,
            DegradedFields = (IReadOnlyList<string>?)degraded ?? Array.Empty<string>()
        };
    }

    private sealed class Field
    {
        private readonly double _min;
        private readonly double _max;
        private double _lastGood;
        private bool _hasLastGood;

        public Field(string name, double min, double max, bool required = true)
        {
            Name = name;
            _min = min;
            _max = max;
            Required = required;
        }

        public string Name { get; }
        public bool Required { get; }
        public int InvalidCount { get; private set; }

        public (double Value, bool Degraded) Read(double? value)
        {
            if (value.HasValue && double.IsFinite(value.Value) && value.Value >= _min && value.Value <= _max)
            {
                InvalidCount = 0;
                _lastGood = value.Value;
                _hasLastGood = true;
                return (value.Value, false);
            }

            InvalidCount++;
            if (_hasLastGood && InvalidCount <= GraceSamples)
                return (_lastGood, true);

            return (0d, true);
        }
    }
}
