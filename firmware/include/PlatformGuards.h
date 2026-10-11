// Maintained derivative of koogar/Phat-Stats. Publication changes: LUSTgabriel, 2026-10-11.
// Original notices retained; see SOURCE_PROVENANCE.md and LICENSE.txt (GPL-2.0-or-later).
#pragma once
#include <Arduino.h>
#include <esp_arduino_version.h>
#include "BoardPins.h"

/*
 * VS Code + PlatformIO platform constraints.
 * These are compile-time safety rails only. They do not touch the serial protocol.
 */

#ifndef ARDUINO_ARCH_ESP32
#error "PhatStats requires the Arduino ESP32 framework."
#endif

#ifndef CONFIG_IDF_TARGET_ESP32
#error "PhatStats requires classic ESP32, not S2/S3/C3/C6/H2."
#endif

#ifndef ARDUINO_ESP32_DEV
#error "PhatStats requires PlatformIO board ID 'esp32dev' (Espressif ESP32 Dev Module)."
#endif

#if (ESP_ARDUINO_VERSION_MAJOR != 2) || (ESP_ARDUINO_VERSION_MINOR != 0) || (ESP_ARDUINO_VERSION_PATCH != 5)
#error "PhatStats is pinned to Arduino-ESP32 2.0.5 via espressif32 5.2.0."
#endif

#if F_CPU != 240000000L
#error "PhatStats esp32dev profile requires F_CPU=240 MHz."
#endif

constexpr bool phatIsReservedExpansionPin(int pin) {
  return pin == PHAT_PIN_EXP25 || pin == PHAT_PIN_EXP26 ||
         pin == PHAT_PIN_EXP32 || pin == PHAT_PIN_EXP33 ||
         pin == PHAT_PIN_EXP34 || pin == PHAT_PIN_EXP35;
}

// Default firmware functions must leave the six expansion pins untouched.
static_assert(!phatIsReservedExpansionPin(PHAT_PIN_TFT_CS), "TFT CS collides with reserved expansion GPIO");
static_assert(!phatIsReservedExpansionPin(PHAT_PIN_TFT_DC), "TFT DC collides with reserved expansion GPIO");
static_assert(!phatIsReservedExpansionPin(PHAT_PIN_TFT_RST), "TFT RST collides with reserved expansion GPIO");
static_assert(!phatIsReservedExpansionPin(PHAT_PIN_TFT_SCLK), "TFT SCLK collides with reserved expansion GPIO");
static_assert(!phatIsReservedExpansionPin(PHAT_PIN_TFT_MOSI), "TFT MOSI collides with reserved expansion GPIO");
static_assert(!phatIsReservedExpansionPin(PHAT_PIN_TFT_MISO), "TFT MISO collides with reserved expansion GPIO");
static_assert(!phatIsReservedExpansionPin(PHAT_PIN_TFT_BACKLIGHT), "TFT backlight collides with reserved expansion GPIO");
static_assert(!phatIsReservedExpansionPin(PHAT_PIN_MODE_BUTTON), "Mode button collides with reserved expansion GPIO");
static_assert(!phatIsReservedExpansionPin(PHAT_PIN_NEOPIXEL), "NeoPixel collides with reserved expansion GPIO");
static_assert(!phatIsReservedExpansionPin(PHAT_PIN_TX_LED), "TX LED collides with reserved expansion GPIO");

static_assert(PHAT_PIN_EXP25 == 25, "GPIO25 expansion reservation changed");
static_assert(PHAT_PIN_EXP26 == 26, "GPIO26 expansion reservation changed");
static_assert(PHAT_PIN_EXP32 == 32, "GPIO32 expansion reservation changed");
static_assert(PHAT_PIN_EXP33 == 33, "GPIO33 expansion reservation changed");
static_assert(PHAT_PIN_EXP34 == 34, "GPIO34 expansion reservation changed");
static_assert(PHAT_PIN_EXP35 == 35, "GPIO35 expansion reservation changed");

#ifdef batteryMonitor
static_assert(PHAT_PIN_BATTERY_ADC == PHAT_PIN_EXP34,
              "Battery monitor must use the explicitly reserved input-only ADC pin");
#endif
