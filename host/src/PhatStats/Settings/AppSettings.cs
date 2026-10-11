using PhatStats.Protocol;
using PhatStats.Serial;

namespace PhatStats.Settings;

public sealed record AppSettings
{
    public const int CurrentVersion = 2;

    public int Version { get; init; } = CurrentVersion;
    public int SamplingIntervalMs { get; init; } = 1000;
    public ProtocolProfile ProtocolProfile { get; init; } = ProtocolProfile.CurrentPortrait;

    public bool AutoSerial { get; init; } = true;
    public SerialDeviceIdentity? SerialIdentity { get; init; }
    public bool DtrEnable { get; init; }
    public bool RtsEnable { get; init; }
    public int WriteTimeoutMs { get; init; } = 1200;

    public string? SelectedGpuIdentifier { get; init; }

    // Empty means: resolve once from the current Windows UI culture.
    // Explicit choices are persisted as one of the four supported cultures.
    public string Language { get; init; } = string.Empty;

    public int DynamicRefreshMs { get; init; } = 3000;
    public int FullSeedRefreshMs { get; init; } = 30000;

    public bool StartWithWindows { get; init; }

    public AppSettings Validate()
    {
        var sampling = Math.Clamp(SamplingIntervalMs, 500, 5000);
        var timeout = Math.Clamp(WriteTimeoutMs, 500, 10000);
        var dynamicRefresh = Math.Clamp(DynamicRefreshMs, 1000, 6000);
        var seedRefresh = Math.Clamp(FullSeedRefreshMs, 8000, 300000);
        var profile = Enum.IsDefined<ProtocolProfile>(ProtocolProfile) ? ProtocolProfile : PhatStats.Protocol.ProtocolProfile.CurrentPortrait;
        var language = Language is "en-US" or "zh-CN" or "ja-JP" or "ko-KR" ? Language : string.Empty;

        return this with
        {
            Version = CurrentVersion,
            SamplingIntervalMs = sampling,
            ProtocolProfile = profile,
            WriteTimeoutMs = timeout,
            DynamicRefreshMs = dynamicRefresh,
            FullSeedRefreshMs = seedRefresh,
            Language = language
        };
    }
}
