using System.Globalization;
using System.Text;
using PhatStats.Models;

namespace PhatStats.Protocol;

public sealed class ProtocolEncoder
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public string EncodeSeed(TelemetrySnapshot snapshot, ProtocolProfile profile)
    {
        return profile switch
        {
            ProtocolProfile.CurrentPortrait => EncodeCurrentSeed(snapshot),
            ProtocolProfile.LegacyWeeStatServer052 => EncodeLegacy(snapshot, includeCpuPower: false),
            ProtocolProfile.ExtendedCompatibility => EncodeLegacy(snapshot, includeCpuPower: true),
            _ => EncodeCurrentSeed(snapshot)
        };
    }

    public string EncodeCurrentSeed(TelemetrySnapshot s)
    {
        var sb = new StringBuilder(220);
        AppendCpu(sb, s);
        AppendGpu(sb, s);
        AppendRamUsed(sb, s);
        AppendRamAvailable(sb, s);
        AppendGpuMemoryTotal(sb, s);
        AppendGpuMemoryUsed(sb, s);
        AppendGpuFanLoad(sb, s);
        AppendGpuFanRpm(sb, s);
        AppendGpuPower(sb, s);
        AppendNames(sb, s);
        AppendGpuCoreClock(sb, s);
        AppendGpuMemoryClock(sb, s);
        AppendGpuShaderClock(sb, s);
        AppendCpuClock(sb, s);
        return sb.ToString();
    }

    public string EncodeLegacy(TelemetrySnapshot s, bool includeCpuPower)
    {
        var sb = new StringBuilder(260);
        AppendCpu(sb, s);
        AppendGpu(sb, s);
        AppendRamUsed(sb, s);
        AppendRamAvailable(sb, s);
        AppendRamLoad(sb, s);
        AppendGpuMemoryTotal(sb, s);
        AppendGpuMemoryUsed(sb, s);
        AppendGpuMemoryLoad(sb, s);
        AppendGpuFanLoad(sb, s);
        AppendGpuFanRpm(sb, s);
        AppendGpuPower(sb, s);
        if (includeCpuPower)
            AppendCpuPower(sb, s);
        AppendNames(sb, s);
        AppendGpuCoreClock(sb, s);
        AppendGpuMemoryClock(sb, s);
        AppendGpuShaderClock(sb, s);
        AppendCpuClock(sb, s);

        // Network telemetry is intentionally removed from the application.
        // Keep zero-valued legacy tags so old parsers retain their expected frame shape
        // without invoking any Windows network enumeration or counter APIs.
        sb.Append("ETD0B/s|ETU0B/s|");
        return sb.ToString();
    }

    public IReadOnlyList<EncodedField> GetCurrentFields(TelemetrySnapshot s)
    {
        return
        [
            new("C", Build(AppendCpu, s), true),
            new("G", Build(AppendGpu, s), true),
            new("R", Build(AppendRamUsed, s), true),
            new("RA", Build(AppendRamAvailable, s), true),
            new("GMT", Build(AppendGpuMemoryTotal, s), false),
            new("GMU", Build(AppendGpuMemoryUsed, s), true),
            new("GFANL", Build(AppendGpuFanLoad, s), true),
            new("GRPM", Build(AppendGpuFanRpm, s), true),
            new("GPWR", Build(AppendGpuPower, s), true),
            new("NAMES", Build(AppendNames, s), false),
            new("GCC", Build(AppendGpuCoreClock, s), true),
            new("GMC", Build(AppendGpuMemoryClock, s), true),
            new("GSC", Build(AppendGpuShaderClock, s), true),
            new("CHC", Build(AppendCpuClock, s), true)
        ];
    }

    private static string Build(Action<StringBuilder, TelemetrySnapshot> append, TelemetrySnapshot s)
    {
        var sb = new StringBuilder(96);
        append(sb, s);
        return sb.ToString();
    }

    private static string I(double value) => Math.Round(Safe(value), MidpointRounding.AwayFromZero).ToString(Invariant);
    private static string One(double value) => Safe(value).ToString("0.0", Invariant);
    private static double Safe(double value) => double.IsFinite(value) ? value : 0d;

    private static void AppendCpu(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append('C').Append(I(s.CpuTemperature)).Append('c').Append(' ').Append(I(s.CpuLoad)).Append("%|");

    private static void AppendGpu(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append('G').Append(I(s.GpuTemperature)).Append('c').Append(' ').Append(I(s.GpuLoad)).Append("%|");

    private static void AppendRamUsed(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append('R').Append(One(s.RamUsed)).Append("GB|");

    private static void AppendRamAvailable(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("RA").Append(One(s.RamAvailable)).Append('|');

    private static void AppendRamLoad(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("RL").Append(I(s.RamLoad)).Append('|');

    private static void AppendGpuMemoryTotal(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("GMT").Append(I(s.GpuMemoryTotal)).Append('|');

    private static void AppendGpuMemoryUsed(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("GMU").Append(I(s.GpuMemoryUsed)).Append('|');

    private static void AppendGpuMemoryLoad(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("GML").Append(I(s.GpuMemoryLoad)).Append('|');

    private static void AppendGpuFanLoad(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("GFANL").Append(I(s.GpuFanSpeedLoad)).Append('|');

    private static void AppendGpuFanRpm(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("GRPM").Append(I(s.GpuFanSpeedRpm)).Append('|');

    private static void AppendGpuPower(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("GPWR").Append(I(s.GpuPower)).Append('|');

    private static void AppendCpuPower(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("CPWR").Append(I(s.CpuPower)).Append('|');

    private static void AppendNames(StringBuilder sb, TelemetrySnapshot s)
    {
        var cpu = NameSanitizer.Cpu(s.CpuName);
        var gpu = NameSanitizer.Gpu(s.GpuName);
        sb.Append("CPU:").Append(cpu).Append("GPU:").Append(gpu).Append('|');
    }

    private static void AppendGpuCoreClock(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("GCC").Append(I(s.GpuCoreClock)).Append('|');

    private static void AppendGpuMemoryClock(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("GMC").Append(I(s.GpuMemoryClock)).Append('|');

    private static void AppendGpuShaderClock(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("GSC").Append(I(s.GpuShaderClock)).Append('|');

    private static void AppendCpuClock(StringBuilder sb, TelemetrySnapshot s) =>
        sb.Append("CHC").Append(I(s.CpuClock)).Append('|');
}

public sealed record EncodedField(string Key, string Wire, bool Dynamic);
