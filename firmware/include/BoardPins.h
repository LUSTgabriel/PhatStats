// Maintained derivative of koogar/Phat-Stats. Publication changes: LUSTgabriel, 2026-10-11.
// Original notices retained; see SOURCE_PROVENANCE.md and LICENSE.txt (GPL-2.0-or-later).
#pragma once

/*
 * PhatStats classic ESP32 DevKit/WROOM application pin map.
 * All application pin assignments live here. Main logic should not contain raw GPIO numbers.
 */

// ILI9341 SPI display
#define PHAT_PIN_TFT_CS          17
#define PHAT_PIN_TFT_DC          16
#define PHAT_PIN_TFT_RST         15
#define PHAT_PIN_TFT_SCLK        18   // hardware SPI, documentation only
#define PHAT_PIN_TFT_MOSI        23   // hardware SPI, documentation only
#define PHAT_PIN_TFT_MISO        19   // reserved/touch/reference
#define PHAT_PIN_TFT_BACKLIGHT    4

// Local controls / indicators
#define PHAT_PIN_MODE_BUTTON      0
#define PHAT_PIN_NEOPIXEL         2
#define PHAT_PIN_TX_LED           5

// Reserved expansion header brought out by this board/project.
// GPIO34 and GPIO35 are INPUT-ONLY on classic ESP32 and must never be configured as outputs.
#define PHAT_PIN_EXP25           25
#define PHAT_PIN_EXP26           26
#define PHAT_PIN_EXP32           32
#define PHAT_PIN_EXP33           33
#define PHAT_PIN_EXP34           34   // ADC1, input-only
#define PHAT_PIN_EXP35           35   // ADC1, input-only

#define PHAT_PIN_BATTERY_ADC PHAT_PIN_EXP34

// Compatibility aliases used by the preserved upstream code.
#define TFT_CS              PHAT_PIN_TFT_CS
#define TFT_DC              PHAT_PIN_TFT_DC
#define TFT_RST             PHAT_PIN_TFT_RST
#define NEOPIN              PHAT_PIN_NEOPIXEL
#define TX_LEDPin           PHAT_PIN_TX_LED
