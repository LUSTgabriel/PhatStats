namespace PhatStats.Models;

public sealed record GpuDeviceDescriptor(
    string Identifier,
    string Name,
    string HardwareType,
    double DedicatedMemoryMb,
    bool IsSelected);

public sealed record SensorResolutionDescriptor(
    string Field,
    string? Hardware,
    string? SensorIdentifier,
    string? SensorName,
    string? SensorType,
    string Reason,
    int FallbackLevel,
    bool Healthy);
