// Maintained derivative of koogar/Phat-Stats. Publication changes: LUSTgabriel, 2026-10-11.
// Original notices retained; see SOURCE_PROVENANCE.md and LICENSE.txt (GPL-2.0-or-later).
#pragma once
#include <Arduino.h>
#include <Adafruit_ILI9341.h>
#include "PhatStatsConfig.h"

extern Adafruit_ILI9341 tft;

enum PhatStatusState : uint8_t {
  PHAT_STATUS_UNINITIALIZED = 0,
  PHAT_STATUS_NORMAL,
  PHAT_STATUS_BOOST,
  PHAT_STATUS_TJMAX
};

struct PhatStatusTracker {
  PhatStatusState state;
  uint32_t changedAt;
};

static PhatStatusTracker phatCpuStatus = { PHAT_STATUS_UNINITIALIZED, 0 };
static PhatStatusTracker phatGpuStatus = { PHAT_STATUS_UNINITIALIZED, 0 };

inline PhatStatusState phatResolveState(
    const PhatStatusTracker& tracker,
    int clockMhz, int tempC, int boostThreshold, int hysteresisMhz, int tjMaxC) {
  const uint32_t now = millis();
  if (tempC >= tjMaxC) {
    return PHAT_STATUS_TJMAX;
  }
  if (tracker.state == PHAT_STATUS_TJMAX && (now - tracker.changedAt) < PHAT_STATUS_MIN_HOLD_MS) {
    return PHAT_STATUS_TJMAX;
  }
  if (tracker.state == PHAT_STATUS_BOOST) {
    if ((now - tracker.changedAt) < PHAT_STATUS_MIN_HOLD_MS) return PHAT_STATUS_BOOST;
    if (clockMhz > (boostThreshold - hysteresisMhz)) return PHAT_STATUS_BOOST;
    return PHAT_STATUS_NORMAL;
  }
  return clockMhz > boostThreshold ? PHAT_STATUS_BOOST : PHAT_STATUS_NORMAL;
}

inline void phatDrawStatusBox(bool cpu, PhatStatusState state) {
  const int x = cpu ? 106 : 13;
  const int y = cpu ? 90 : 233;
  const int fillX = x + 1;
  const int fillY = y + 1;
  const int textX = cpu ? 121 : 29;
  const int textY = cpu ? 94 : 237;

  tft.fillRoundRect(fillX, fillY, 86, cpu ? 21 : 20, 4, ILI9341_BLACK);
  tft.drawRoundRect(x, y, 88, 22, 4, 0xA510);

  if (state == PHAT_STATUS_TJMAX) {
    tft.fillRoundRect(fillX, fillY, 86, cpu ? 21 : 20, 4, ILI9341_RED);
    tft.setTextColor(ILI9341_WHITE, ILI9341_RED);
    tft.setTextSize(2);
    tft.setCursor(cpu ? 121 : 29, textY);
    tft.print("TJMax");
  } else if (state == PHAT_STATUS_BOOST) {
    tft.fillRoundRect(fillX, fillY, 86, cpu ? 21 : 20, 4, ILI9341_GREEN);
    tft.setTextColor(ILI9341_BLACK, ILI9341_GREEN);
    tft.setTextSize(2);
    tft.setCursor(textX, textY);
    tft.print(cpu ? "TURBO" : "BOOST");
  }
}

inline void phatUpdateStatus(PhatStatusTracker& tracker, bool cpu, int clockMhz, int tempC,
                             int boostThreshold, int hysteresisMhz, int tjMaxC) {
  PhatStatusState next = phatResolveState(tracker, clockMhz, tempC, boostThreshold, hysteresisMhz, tjMaxC);
  if (next != tracker.state) {
    tracker.state = next;
    tracker.changedAt = millis();
    phatDrawStatusBox(cpu, next);
  }
}

inline void updateCpuStatusIndicator(int cpuClockMhz, int cpuTempC) {
  phatUpdateStatus(phatCpuStatus, true, cpuClockMhz, cpuTempC,
                   CPU_BOOST, PHAT_CPU_BOOST_HYSTERESIS_MHZ, CPU_TJMAX);
}

inline void updateGpuStatusIndicator(int gpuClockMhz, int gpuTempC) {
  phatUpdateStatus(phatGpuStatus, false, gpuClockMhz, gpuTempC,
                   GPU_BOOST, PHAT_GPU_BOOST_HYSTERESIS_MHZ, GPU_TJMAX);
}

inline void resetStatusIndicators() {
  phatCpuStatus.state = PHAT_STATUS_UNINITIALIZED;
  phatCpuStatus.changedAt = 0;
  phatGpuStatus.state = PHAT_STATUS_UNINITIALIZED;
  phatGpuStatus.changedAt = 0;
}
