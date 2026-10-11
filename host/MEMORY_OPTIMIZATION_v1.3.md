# PhatStats Windows Host v1.3 — memory stability pass

This pass does not change the Phat-Stats serial protocol. The four files under `src/PhatStats/Protocol/` are SHA-256 frozen against v1.2 and the build script fails if any of them changes.

Changes:

- **Primary fix:** disables LibreHardwareMonitor 0.9.6 per-sensor history by setting `ISensor.ValuesTimeWindow = TimeSpan.Zero` for every enabled CPU/GPU sensor. LHM defaults this history to one day and appends averaged `SensorValue` records while sensors update; PhatStats only needs the current `Value`, so retaining the history causes unnecessary long-run memory growth.
- Removed the old sensor-degradation path that periodically recreated the whole LibreHardwareMonitor `Computer` backend. Persistent missing sensors now only trigger sensor re-resolution on the existing backend with 5/15/60/300 s backoff.
- Backend recreation is reserved for real backend update failures or resume, and repeated update failures are rate-limited by a 60 s rebuild cooldown.
- Cached immutable GPU/sensor-mapping arrays are reused instead of cloning them every telemetry sample.
- Cached immutable serial-port arrays are reused instead of cloning them every serial diagnostics update.
- Serial ASCII writes reuse a byte buffer; the bytes placed on the wire remain unchanged.
- Healthy telemetry no longer allocates a degraded-field `List<string>` every sample.
- Tray icon/status text are only sent to WinForms/Shell when the value actually changes.
- Dynamic tray submenu items are explicitly disposed before rebuilding, releasing WinForms/GDI resources immediately.
- Diagnostics refresh timer runs only while the Diagnostics window is visible.
- Added the .NET runtime hint `System.GC.ConserveMemory=5` for a long-running background tray process. This is not a hard memory limit and does not invoke forced periodic GC.
- Added `tools/Watch-PhatStatsMemory.ps1` for a 20-minute Private Bytes / Working Set / handle / thread soak test.

Expected result: lower allocation churn and, more importantly, no repeated LHM backend reconstruction caused merely by a sensor that stays unavailable.
