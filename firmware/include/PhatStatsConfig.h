// Maintained derivative of koogar/Phat-Stats. Publication changes: LUSTgabriel, 2026-10-11.
// Original notices retained; see SOURCE_PROVENANCE.md and LICENSE.txt (GPL-2.0-or-later).
#pragma once
#include <Arduino.h>
#include "BoardPins.h"

/*
 * PhatStats compile-time configuration.
 * Edit this file instead of editing src/.
 *
 * SERIAL PROTOCOL WARNING:
 * The USB serial parser and portrait tag parser are intentionally frozen to the
 * upstream V3.1.7.3 behavior. Do not add protocol commands here.
 */

// ---------------- Platform ----------------
// The PlatformIO environment is fixed to board=esp32dev (classic ESP32 / ESP32-WROOM class).
// Board selection belongs in platformio.ini; application feature switches stay here.
#define PHAT_TARGET_ESP32_DEVKIT 1

// ---------------- Transport ----------------
// USB serial is the default and release path.
// #define enable_BT
// #define enable_DualSerialEvent
// #define enableTX_LED
static const int TX_LED_Delay = 0;

// Frozen protocol defaults. Host and firmware must match.
static const int baudRate = 9600;
static const int Serial_eventDelay = 15;

// ---------------- Display / artwork ----------------
#define PHAT_DISPLAY_ROTATION 0  // portrait coordinate system used by this UI

// Pixel-art renderer: exact 82x82 packed 4-bit indexed RGB565.
#define PHAT_ART_FORMAT_INDEXED4 1
#define PHAT_USE_PIXEL_CPU_ART 1
#define PHAT_USE_PIXEL_GPU_ART 1

// Runtime auto-selection reads ONLY the existing CPU:/GPU:/GMT fields.
// It does not add or modify any serial protocol tag.
static constexpr bool PHAT_ARTWORK_AUTO = true;

// CPU vendor is automatic. Style remains a user preference. Allowed: 1, 2, 5, 7.
static constexpr uint8_t PHAT_DEFAULT_AMD_CPU_STYLE = 7;
static constexpr uint8_t PHAT_DEFAULT_INTEL_CPU_STYLE = 1;

// iGPU/discrete helper threshold used only when a GPU name is ambiguous.
static constexpr int PHAT_IGPU_DEDICATED_VRAM_THRESHOLD_MB = 1024;

// Manual/fallback artwork when auto-selection is disabled or the name is unknown.
#include "ArtworkTypes.h"
static constexpr PhatCpuArtwork PHAT_MANUAL_CPU_ART = PhatCpuArtwork::AmdStyle1;
static constexpr PhatGpuArtwork PHAT_MANUAL_GPU_ART = PhatGpuArtwork::NvidiaRtxTi;

// Legacy compile-time artwork switches are kept only for the old drawBitmap()
// fallback path. The indexed renderer ignores them when PHAT_ARTWORK_AUTO=true.
#define AMD_CPU
// #define INTEL_CPU
#define NVIDIA_RTX_Ti_GRAPHICS

#if !PHAT_ART_FORMAT_INDEXED4
#error This project revision requires the exact indexed-4bpp artwork renderer.
#endif

// Legacy name-display offsets. These are part of the display behavior, not the wire format.
#define cpuNameStartLength 19
#define gpuNameStartLength 19
#define gpuTotalMemPos     13

// #define Manual_cpuName
static String set_CPUname = "xxxxxxxx";
// #define Manual_gpuName
static String set_GPUname = "xxxxxxxx";
// #define Manual_gpuRam
static String set_GPUram = "xx";

#define noDegree
#define smallPercent

// ---------------- Telemetry display features ----------------
// #define CPU_OverClocked
#define enable_cpuPowerStats
#define enable_gpuCore
#define enable_gpuPowerStats
#define enable_gpuFanStatsPerc
#define enable_gpuFanStatsRPM
#define enable_BigGPUw
// #define enable_gpuVram
// #define enable_gpuShader

// Frequency gain numerical readout
// #define enable_ShowFrequencyGain
// #define ShowFrequencyGainMHz
// #define ShowFrequencyGainPerc

// ---------------- Thresholds / status indicators ----------------
#define CPU_TJMAX 105
#define GPU_TJMAX 85
#define CPU_BOOST 4500
#define GPU_BOOST 1500
#define enable_ThrottleIndicator
#define enable_BoostIndicator

// Independent, non-blocking UI stabilization. UI only; does not alter serial parsing.
static const uint32_t PHAT_STATUS_MIN_HOLD_MS = 1500;
static const int PHAT_CPU_BOOST_HYSTERESIS_MHZ = 150;
static const int PHAT_GPU_BOOST_HYSTERESIS_MHZ = 100;

// ---------------- NeoPixel ----------------
#define NUM_PIXELS 16
static int NeoBrightness = 20;
// #define enable_NeopixelGauges
// #define enable_Thresholdtriggers_PCB

// ---------------- Backlight ----------------
#define fixedBacklight
static volatile int brightness_count = 250;

// ---------------- Activity watchdog ----------------
#define enableActivityChecker
#define lastActiveDelay 8000

// ---------------- Optional battery input ----------------
// Disabled so GPIO34 remains available on the expansion header by default.
// #define batteryMonitor

// ---------------- UI ----------------
// #define RETRO_MONO
// #define RETRO_AMBER
// #define RETRO_GREEN
// #define splashScreenLS
static int debounceButton = 1000;

// ---------------- Debug ----------------
// #define Debug

// Network telemetry is intentionally not part of this build.
// enable_LibreNet is deliberately undefined.

#define device_BT "TMLabs_BT_ESP32Dev"

#if defined(AMD_CPU) && defined(INTEL_CPU)
#error Select only one CPU artwork vendor.
#endif

#if defined(batteryMonitor) && (PHAT_PIN_BATTERY_ADC != PHAT_PIN_EXP34)
#error Battery ADC mapping must be reviewed before changing reserved expansion pins.
#endif

