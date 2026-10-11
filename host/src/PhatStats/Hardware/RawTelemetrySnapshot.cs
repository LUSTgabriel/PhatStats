namespace PhatStats.Hardware;

internal sealed record RawTelemetrySnapshot
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string CpuName { get; init; } = "AMD CPU";
    public double? CpuTemperature { get; init; }
    public double? CpuLoad { get; init; }
    public double? CpuClock { get; init; }
    public double? CpuPower { get; init; }
    public double? CpuFanSpeedLoad { get; init; }

    public string GpuName { get; init; } = "NVIDIA GeForce GPU";
    public string? GpuIdentifier { get; init; }
    public double? GpuTemperature { get; init; }
    public double? GpuLoad { get; init; }
    public double? GpuCoreClock { get; init; }
    public double? GpuMemoryClock { get; init; }
    public double? GpuShaderClock { get; init; }
    public double? GpuMemoryTotal { get; init; }
    public double? GpuMemoryUsed { get; init; }
    public double? GpuMemoryLoad { get; init; }
    public double? GpuPower { get; init; }
    public double? GpuFanSpeedLoad { get; init; }
    public double? GpuFanSpeedRpm { get; init; }

    public double? RamLoad { get; init; }
    public double? RamUsed { get; init; }
    public double? RamAvailable { get; init; }
}
