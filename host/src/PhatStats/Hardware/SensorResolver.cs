using LibreHardwareMonitor.Hardware;
using PhatStats.Models;

namespace PhatStats.Hardware;

internal sealed class SensorBindings
{
    public required IHardware Cpu { get; init; }
    public required IHardware Gpu { get; init; }
    public required string CpuName { get; init; }
    public required string GpuName { get; init; }
    public required string GpuIdentifier { get; init; }
    public ISensor? CpuTemperature { get; init; }
    public ISensor? CpuLoad { get; init; }
    public ISensor[] CpuClocks { get; init; } = Array.Empty<ISensor>();
    public ISensor? CpuPower { get; init; }
    public ISensor? GpuTemperature { get; init; }
    public ISensor? GpuLoad { get; init; }
    public ISensor? GpuCoreClock { get; init; }
    public ISensor? GpuMemoryClock { get; init; }
    public ISensor? GpuShaderClock { get; init; }
    public ISensor? GpuMemoryTotal { get; init; }
    public ISensor? GpuMemoryUsed { get; init; }
    public ISensor? GpuMemoryLoad { get; init; }
    public ISensor? GpuPower { get; init; }
    public ISensor? GpuFanLoad { get; init; }
    public ISensor? GpuFanRpm { get; init; }
    public required IReadOnlyList<SensorResolutionDescriptor> Resolutions { get; init; }
}

internal static class SensorResolver
{
    public static (SensorBindings? Bindings, IReadOnlyList<GpuDeviceDescriptor> Gpus) Resolve(Computer computer, string? preferredGpuIdentifier)
    {
        var all = Flatten(computer.Hardware).ToArray();
        var cpu = all.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        var gpuHardware = all.Where(IsGpu).ToArray();
        // One discovery-time refresh is acceptable; steady-state updates only the selected CPU/GPU.
        foreach (var gpu in gpuHardware) { try { gpu.Update(); } catch { } }
        var descriptors = BuildGpuDescriptors(gpuHardware, preferredGpuIdentifier);
        if (cpu is null || gpuHardware.Length == 0)
            return (null, descriptors);

        var selected = SelectGpu(gpuHardware, preferredGpuIdentifier);
        if (selected is null)
            return (null, descriptors);

        var resolutions = new List<SensorResolutionDescriptor>(16);
        ISensor? Pick(string field, IHardware hw, SensorType type, params (string Text, int Score)[] aliases)
        {
            var candidates = hw.Sensors.Where(s => s.SensorType == type).ToArray();
            ISensor? best = null;
            var bestScore = int.MinValue;
            string reason = "No compatible sensor";
            foreach (var sensor in candidates)
            {
                var score = ScoreName(sensor.Name, aliases);
                if (score > bestScore)
                {
                    best = sensor;
                    bestScore = score;
                    reason = score > 0 ? $"alias score {score}" : "type-compatible deterministic fallback";
                }
            }

            if (bestScore < 0)
                best = null; // Explicitly excluded semantic (hotspot/junction/CCD etc.).

            resolutions.Add(ToDescriptor(field, hw, best, reason, best is null ? 3 : bestScore >= 90 ? 0 : 1));
            return best;
        }

        var cpuClocks = cpu.Sensors
            .Where(s => s.SensorType == SensorType.Clock &&
                        (Contains(s.Name, "Core") || Contains(s.Name, "CPU")) &&
                        !Contains(s.Name, "Bus"))
            .OrderBy(s => s.Identifier.ToString(), StringComparer.Ordinal)
            .ToArray();
        resolutions.Add(new SensorResolutionDescriptor("CpuClock", cpu.Name,
            cpuClocks.Length > 0 ? string.Join(",", cpuClocks.Select(s => s.Identifier.ToString())) : null,
            cpuClocks.Length > 0 ? $"{cpuClocks.Length} active/core clock candidates" : null,
            SensorType.Clock.ToString(), cpuClocks.Length > 0 ? "maximum valid active/core clock" : "No compatible clock sensor", cpuClocks.Length > 0 ? 0 : 3, cpuClocks.Length > 0));

        var binding = new SensorBindings
        {
            Cpu = cpu,
            Gpu = selected,
            CpuName = cpu.Name,
            GpuName = selected.Name,
            GpuIdentifier = selected.Identifier.ToString(),
            CpuTemperature = Pick("CpuTemperature", cpu, SensorType.Temperature,
                ("CPU Package", 100), ("Tctl/Tdie", 99), ("CPU (Tctl/Tdie)", 100), ("Package", 92), ("CPU Core", 70)),
            CpuLoad = Pick("CpuLoad", cpu, SensorType.Load,
                ("CPU Total", 100), ("Total", 96), ("CPU", 70)),
            CpuClocks = cpuClocks,
            CpuPower = Pick("CpuPower", cpu, SensorType.Power,
                ("CPU Package", 100), ("Package", 96), ("CPU PPT", 92), ("CPU", 70)),
            GpuTemperature = Pick("GpuTemperature", selected, SensorType.Temperature,
                ("GPU Core", 100), ("GPU", 80)),
            GpuLoad = Pick("GpuLoad", selected, SensorType.Load,
                ("GPU Core", 100), ("D3D 3D", 90), ("GPU", 75)),
            GpuCoreClock = Pick("GpuCoreClock", selected, SensorType.Clock, ("GPU Core", 100), ("Core", 90)),
            GpuMemoryClock = Pick("GpuMemoryClock", selected, SensorType.Clock, ("GPU Memory", 100), ("Memory", 90)),
            GpuShaderClock = Pick("GpuShaderClock", selected, SensorType.Clock, ("GPU Shader", 100), ("Shader", 90)),
            GpuMemoryTotal = Pick("GpuMemoryTotal", selected, SensorType.SmallData, ("GPU Memory Total", 100), ("Memory Total", 95)),
            GpuMemoryUsed = Pick("GpuMemoryUsed", selected, SensorType.SmallData, ("GPU Memory Used", 100), ("Dedicated Memory Used", 90), ("Memory Used", 85)),
            GpuMemoryLoad = Pick("GpuMemoryLoad", selected, SensorType.Load, ("GPU Memory", 100), ("Memory", 80)),
            GpuPower = Pick("GpuPower", selected, SensorType.Power, ("GPU Package", 100), ("GPU Board Power", 96), ("GPU Power", 92), ("Board Power", 88)),
            GpuFanLoad = Pick("GpuFanSpeedLoad", selected, SensorType.Control, ("GPU Fan", 100), ("Fan", 85)),
            GpuFanRpm = Pick("GpuFanSpeedRpm", selected, SensorType.Fan, ("GPU Fan", 100), ("GPU", 90), ("Fan", 85)),
            Resolutions = resolutions.ToArray()
        };
        return (binding, BuildGpuDescriptors(gpuHardware, binding.GpuIdentifier));
    }

    private static int ScoreName(string name, (string Text, int Score)[] aliases)
    {
        if (Contains(name, "Hot Spot") || Contains(name, "Junction") || Contains(name, "CCD"))
            return -1000;

        var best = 1; // type-compatible unique/deterministic fallback, lower than every known alias
        foreach (var (text, score) in aliases)
        {
            if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, score);
            else if (name.Contains(text, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, score - 10);
        }

        return best;
    }

    private static SensorResolutionDescriptor ToDescriptor(string field, IHardware hw, ISensor? s, string reason, int fallback) =>
        new(field, hw.Name, s?.Identifier.ToString(), s?.Name, s?.SensorType.ToString(), reason, fallback, s is not null);

    private static IReadOnlyList<GpuDeviceDescriptor> BuildGpuDescriptors(IEnumerable<IHardware> gpus, string? selected)
    {
        return gpus.Select(g => new GpuDeviceDescriptor(
                g.Identifier.ToString(), g.Name, g.HardwareType.ToString(), GetDedicatedMemory(g),
                string.Equals(g.Identifier.ToString(), selected, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(g => g.IsSelected).ThenByDescending(g => g.DedicatedMemoryMb).ThenBy(g => g.Identifier, StringComparer.Ordinal)
            .ToArray();
    }

    private static IHardware? SelectGpu(IReadOnlyList<IHardware> gpus, string? preferred)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            var exact = gpus.FirstOrDefault(g => string.Equals(g.Identifier.ToString(), preferred, StringComparison.OrdinalIgnoreCase));
            if (exact is not null) return exact;
        }

        return gpus.OrderByDescending(g => DiscreteScore(g))
            .ThenByDescending(GetDedicatedMemory)
            .ThenBy(g => g.Identifier.ToString(), StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static int DiscreteScore(IHardware h) => h.HardwareType switch
    {
        HardwareType.GpuNvidia => 30,
        HardwareType.GpuAmd => GetDedicatedMemory(h) >= 1024 ? 25 : 15,
        HardwareType.GpuIntel => GetDedicatedMemory(h) >= 1024 ? 20 : 10,
        _ => 0
    };

    private static double GetDedicatedMemory(IHardware h) =>
        h.Sensors.FirstOrDefault(s => s.SensorType == SensorType.SmallData && string.Equals(s.Name, "GPU Memory Total", StringComparison.OrdinalIgnoreCase))?.Value ?? 0;

    private static bool IsGpu(IHardware h) => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel;
    private static bool Contains(string source, string value) => source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<IHardware> Flatten(IEnumerable<IHardware> roots)
    {
        foreach (var h in roots)
        {
            yield return h;
            foreach (var sub in Flatten(h.SubHardware)) yield return sub;
        }
    }
}
