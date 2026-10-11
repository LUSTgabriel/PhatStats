# PhatStats ESP32 · 0.01alpha (v5 artwork)

Target: **ESP32-WROOM-32U / classic ESP32**, built in VS Code with the PlatformIO extension.

## Build target

`platformio.ini` is pinned to:

```ini
platform = espressif32@5.2.0
board = esp32dev
framework = arduino
board_build.mcu = esp32
board_build.f_cpu = 240000000L
board_build.f_flash = 40000000L
board_build.flash_mode = dio
board_build.partitions = partitions/huge_app.csv
```

The local `partitions/huge_app.csv` is the Arduino-ESP32 2.0.5 huge-app layout:

```text
APP0   0x300000 = 3 MiB
SPIFFS 0x0F0000 = 960 KiB
```

There is no second application slot. This project uses USB flashing, not OTA.

## VS Code build

Open the folder itself in VS Code, then use:

```text
PlatformIO → Project Tasks → esp32dev → General → Build
```

or the PlatformIO terminal:

```powershell
pio run
```

Do not create a manual `.vscode/c_cpp_properties.json`; PlatformIO owns compiler paths, defines and IntelliSense.

## User configuration

Edit only:

```text
include/PhatStatsConfig.h
include/BoardPins.h
```

Normal UI/telemetry code should not be edited for configuration changes.

## Automatic artwork selection

Enabled by default:

```cpp
static constexpr bool PHAT_ARTWORK_AUTO = true;
```

No new serial command exists. The renderer only reads the **existing** complete-frame fields:

```text
CPU:...
GPU:...
GMT...
```

Current automatic categories:

```text
CPU
  AMD / Intel
  style preference 1, 2, 5 or 7 per vendor

NVIDIA
  GTX
  GTX Ti
  RTX
  RTX Ti
  RTX Super
  Professional / workstation

AMD
  Radeon
  RDNA
  Radeon iGPU
  RDNA iGPU

Intel
  Arc discrete
  UHD iGPU
  Xe / Arc-class new iGPU
```

CPU style preference is configured with:

```cpp
PHAT_DEFAULT_AMD_CPU_STYLE
PHAT_DEFAULT_INTEL_CPU_STYLE
```

To disable automatic selection:

```cpp
static constexpr bool PHAT_ARTWORK_AUTO = false;
```

and choose:

```cpp
PHAT_MANUAL_CPU_ART
PHAT_MANUAL_GPU_ART
```

## Artwork format

All current CPU/GPU artwork is fixed at **82×82 pixels** and stored as packed 4-bit palette indices. The renderer expands one 82-pixel row at a time to RGB565 and calls `drawRGBBitmap()`.

UI geometry is frozen. CPU and GPU boxes remain at their original dimensions and coordinates.

## Frozen serial protocol

The core serial protocol is intentionally unchanged from the V3.1.7.3 baseline:

```text
baudRate = 9600
Serial_eventDelay = 15 ms per '|'
inputString.reserve(220)
```

`serialEvent()`, `serialBTEvent()` and the portrait `indexOf()/substring()` parser signature are compared with golden source during every PlatformIO build through:

```text
pre:tools/pio_protocol_guard.py
```

The automatic artwork classifier does **not** mutate `inputString`, send data, add tags, ACK frames, alter delimiters, or change serial timing.

## Reserved GPIO

The following pins remain expansion pins:

```text
GPIO25
GPIO26
GPIO32
GPIO33
GPIO34  input-only / ADC1
GPIO35  input-only / ADC1
```

Battery monitoring is disabled by default because the legacy battery circuit uses GPIO34.

## Full audit

Run:

```powershell
python tools/audit_project.py
```

or:

```powershell
.\build_and_audit.cmd
```

The audit checks PlatformIO/INI IDs, 4 MB partition layout, exact serial functions, portrait parser signature, UI geometry signature, reserved GPIOs, artwork IDs and artwork array sizes.

Public installation: [guide](../docs/install.en.md). Historical generator is guarded; it must not overwrite approved v5 artwork by default.
