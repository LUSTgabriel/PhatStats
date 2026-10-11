using PhatStats.Diagnostics;
using PhatStats.Models;
using PhatStats.Localization;
using PhatStats.Protocol;
using PhatStats.Runtime;
using PhatStats.Serial;
using PhatStats.Settings;

var failures = new List<string>();
void Check(bool condition, string name) { if (!condition) failures.Add(name); }

var sample = new TelemetrySnapshot
{
    CpuName = "AMD Ryzen 9 9950X3D",
    CpuTemperature = 61.2, CpuLoad = 17.6, CpuClock = 5225,
    GpuName = "NVIDIA GeForce RTX 3070 Ti", GpuTemperature = 58.4, GpuLoad = 72.2,
    GpuCoreClock = 1905, GpuMemoryClock = 9501, GpuShaderClock = 1905,
    GpuMemoryTotal = 8192, GpuMemoryUsed = 3210, GpuMemoryLoad = 39.2, GpuPower = 221.4,
    GpuFanSpeedLoad = 48, GpuFanSpeedRpm = 1675,
    RamUsed = 12.3, RamAvailable = 19.7, RamLoad = 38.4
};

var encoder = new ProtocolEncoder();
var seed = encoder.EncodeSeed(sample, ProtocolProfile.CurrentPortrait);
Check(seed.StartsWith("C61c 18%|G58c 72%|R12.3GB|RA19.7|", StringComparison.Ordinal), "current profile leading order");
Check(seed.Contains("CPU:AMD Ryzen 9 9950X3DGPU:NVIDIA GeForce RTX 3070 Ti|", StringComparison.Ordinal), "vendor prefixes/names");
Check(!seed.Contains("RL", StringComparison.Ordinal) && !seed.Contains("GML", StringComparison.Ordinal) && !seed.Contains("ETD", StringComparison.Ordinal) && !seed.Contains("CPWR", StringComparison.Ordinal), "current profile excludes inactive legacy fields");
Check(!seed.EndsWith("\r", StringComparison.Ordinal) && !seed.EndsWith("\n", StringComparison.Ordinal), "no line ending");

var legacy = encoder.EncodeSeed(sample, ProtocolProfile.LegacyWeeStatServer052);
Check(legacy.Contains("RL38|", StringComparison.Ordinal) && legacy.Contains("GML39|", StringComparison.Ordinal), "legacy compatibility fields");
Check(legacy.EndsWith("ETD0B/s|ETU0B/s|", StringComparison.Ordinal), "legacy network tags retained as zero constants without network telemetry");
Check(!legacy.Contains("CPWR", StringComparison.Ordinal), "legacy 0.5.2 excludes CPWR");

var session = new ProtocolSession(encoder);
var t0 = DateTimeOffset.UtcNow;
var first = session.Encode(sample, ProtocolProfile.CurrentPortrait, t0, false, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30));
var unchanged = session.Encode(sample, ProtocolProfile.CurrentPortrait, t0.AddSeconds(1), false, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30));
var changed = session.Encode(sample with { CpuTemperature = 62 }, ProtocolProfile.CurrentPortrait, t0.AddSeconds(2), false, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30));
Check(first == seed && unchanged == seed, "current portrait always emits a complete realtime frame");
Check(changed.StartsWith("C62c 18%|G58c 72%|R12.3GB|RA19.7|", StringComparison.Ordinal), "changed sample remains a complete frame");
foreach (var requiredTag in new[] { "GMT", "GMU", "GFANL", "GRPM", "GPWR", "CPU:", "GPU:", "GCC", "GMC", "GSC", "CHC" })
    Check(changed.Contains(requiredTag, StringComparison.Ordinal), "complete frame contains " + requiredTag);

var mailbox = new LatestSnapshotMailbox();
mailbox.Publish(sample);
mailbox.Publish(sample with { CpuLoad = 99 });
Check(mailbox.Latest?.CpuLoad == 99 && mailbox.Version == 2, "latest-only mailbox overwrites stale snapshot");

var ring = new SevereErrorRing();
for (var i = 0; i < 7; i++) ring.Add("Test", "E" + i, "error " + i);
Check(ring.Snapshot().Count == 5 && ring.Snapshot()[0].Code == "E2", "severe error ring capacity 5");
ring.Add("Test", "E6", "error 6");
Check(ring.Snapshot()[^1].OccurrenceCount == 2, "severe error coalescing");

var identity = new SerialDeviceIdentity { Vid = "10C4", Pid = "EA60", UsbSerialNumber = "ABC", LastKnownPort = "COM8" };
var moved = new SerialDeviceInfo("COM12", "Silicon Labs CP210x", "10C4", "EA60", "ABC", "USB\\VID_10C4&PID_EA60\\ABC", "Port_#0001", true, false);
Check(identity.IsStrongMatch(moved), "USB identity follows COM reassignment");

var validated = new AppSettings { SamplingIntervalMs = 100, WriteTimeoutMs = 20, Language = "xx-YY" }.Validate();
Check(validated.SamplingIntervalMs == 500 && validated.WriteTimeoutMs == 500, "settings safe lower bounds");
Check(validated.Language == string.Empty, "unsupported language falls back to system-language selection");

LocalizationService.SetCulture("zh-CN");
Check(LocalizationService.T("Menu.Settings") == "设置", "zh-CN resource lookup");
LocalizationService.SetCulture("ja-JP");
Check(LocalizationService.T("Menu.Settings") == "設定", "ja-JP resource lookup");
LocalizationService.SetCulture("ko-KR");
Check(LocalizationService.T("Menu.Settings") == "설정", "ko-KR resource lookup");
LocalizationService.SetCulture("en-US");
Check(LocalizationService.T("Menu.Settings") == "Settings", "en-US resource lookup");

if (failures.Count == 0)
{
    Console.WriteLine("All PhatStats self-tests passed.");
    return 0;
}

Console.Error.WriteLine("FAILED:");
foreach (var failure in failures) Console.Error.WriteLine(" - " + failure);
return 1;
