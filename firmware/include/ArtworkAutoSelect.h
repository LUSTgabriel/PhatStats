// Maintained derivative of koogar/Phat-Stats. Publication changes: LUSTgabriel, 2026-10-11.
// Original notices retained; see SOURCE_PROVENANCE.md and LICENSE.txt (GPL-2.0-or-later).
#pragma once
#include <Arduino.h>
#include "PhatStatsConfig.h"
#include "ArtworkTypes.h"

/*
 * Artwork auto-selection is intentionally read-only with respect to the legacy
 * serial frame. It inspects the existing CPU:/GPU:/GMT fields, never mutates
 * inputString, and never introduces a new tag, command, ACK, or framing rule.
 */

inline PhatCpuArtwork &phatCurrentCpuArtwork() {
  static PhatCpuArtwork value = PHAT_MANUAL_CPU_ART;
  return value;
}

inline PhatGpuArtwork &phatCurrentGpuArtwork() {
  static PhatGpuArtwork value = PHAT_MANUAL_GPU_ART;
  return value;
}

inline bool phatContains(const String &s, const char *needle) {
  return s.indexOf(needle) >= 0;
}

inline String phatExtractCpuNameForArtwork(const String &frame) {
  const int cpuTag = frame.indexOf("CPU:");
  if (cpuTag < 0) return String();
  const int gpuTag = frame.indexOf("GPU:", cpuTag + 4);
  const int pipe = frame.indexOf('|', cpuTag + 4);
  int end = gpuTag >= 0 ? gpuTag : pipe;
  if (end < 0) end = frame.length();
  return frame.substring(cpuTag + 4, end);
}

inline String phatExtractGpuNameForArtwork(const String &frame) {
  const int gpuTag = frame.indexOf("GPU:");
  if (gpuTag < 0) return String();
  int end = frame.indexOf('|', gpuTag + 4);
  if (end < 0) end = frame.length();
  return frame.substring(gpuTag + 4, end);
}

inline int phatExtractGmtMbForArtwork(const String &frame) {
  const int startTag = frame.indexOf("GMT");
  if (startTag < 0) return 0;
  const int start = startTag + 3;
  int end = frame.indexOf('|', start);
  if (end < 0) end = frame.length();
  return frame.substring(start, end).toInt();
}

inline int phatExtractFirstNumberAfter(const String &text, const char *marker) {
  const int markerPos = text.indexOf(marker);
  if (markerPos < 0) return -1;
  int p = markerPos + static_cast<int>(strlen(marker));
  while (p < static_cast<int>(text.length()) && (text[p] < '0' || text[p] > '9')) ++p;
  if (p >= static_cast<int>(text.length())) return -1;
  int value = 0;
  while (p < static_cast<int>(text.length()) && text[p] >= '0' && text[p] <= '9') {
    value = value * 10 + (text[p] - '0');
    ++p;
  }
  return value;
}

inline PhatCpuArtwork phatAmdCpuStyle(uint8_t style) {
  switch (style) {
    case 2: return PhatCpuArtwork::AmdStyle2;
    case 5: return PhatCpuArtwork::AmdStyle5;
    case 7: return PhatCpuArtwork::AmdStyle7;
    default: return PhatCpuArtwork::AmdStyle1;
  }
}

inline PhatCpuArtwork phatIntelCpuStyle(uint8_t style) {
  switch (style) {
    case 2: return PhatCpuArtwork::IntelStyle2;
    case 5: return PhatCpuArtwork::IntelStyle5;
    case 7: return PhatCpuArtwork::IntelStyle7;
    default: return PhatCpuArtwork::IntelStyle1;
  }
}

inline PhatCpuArtwork phatClassifyCpuArtwork(const String &cpuName) {
  if (phatContains(cpuName, "Intel") || phatContains(cpuName, "INTEL")) {
    return phatIntelCpuStyle(PHAT_DEFAULT_INTEL_CPU_STYLE);
  }
  if (phatContains(cpuName, "AMD") || phatContains(cpuName, "Ryzen") || phatContains(cpuName, "RYZEN")) {
    return phatAmdCpuStyle(PHAT_DEFAULT_AMD_CPU_STYLE);
  }
  return PHAT_MANUAL_CPU_ART;
}

inline bool phatCpuImpliesModernAmdIgpu(const String &cpuName) {
  if (!phatContains(cpuName, "Ryzen") && !phatContains(cpuName, "RYZEN")) return false;
  const int model = phatExtractFirstNumberAfter(cpuName, "Ryzen");
  // Host strings are usually "Ryzen 7 7800X3D", where the first number after
  // Ryzen is the tier (7), so look for the next 4-digit token as a fallback.
  for (int i = 0; i + 3 < static_cast<int>(cpuName.length()); ++i) {
    if (cpuName[i] >= '0' && cpuName[i] <= '9' &&
        cpuName[i+1] >= '0' && cpuName[i+1] <= '9' &&
        cpuName[i+2] >= '0' && cpuName[i+2] <= '9' &&
        cpuName[i+3] >= '0' && cpuName[i+3] <= '9') {
      const int n = (cpuName[i]-'0')*1000 + (cpuName[i+1]-'0')*100 +
                    (cpuName[i+2]-'0')*10 + (cpuName[i+3]-'0');
      if (n >= 7000) return true;
    }
  }
  return model >= 7000;
}

inline PhatGpuArtwork phatClassifyGpuArtwork(const String &gpuName,
                                              const String &cpuName,
                                              int gpuMemoryTotalMb) {
  // NVIDIA professional/workstation/datacenter first, before generic RTX.
  if (phatContains(gpuName, "NVIDIA")) {
    if (phatContains(gpuName, "Quadro") || phatContains(gpuName, "Tesla") ||
        phatContains(gpuName, "RTX A") || phatContains(gpuName, "RTX PRO") ||
        phatContains(gpuName, "Ada Generation") || phatContains(gpuName, "NVIDIA A100") ||
        phatContains(gpuName, "NVIDIA H100") || phatContains(gpuName, "NVIDIA L40") ||
        phatContains(gpuName, "NVIDIA T1000")) {
      return PhatGpuArtwork::NvidiaPro;
    }
    if (phatContains(gpuName, "RTX") && (phatContains(gpuName, "SUPER") || phatContains(gpuName, "Super"))) {
      return PhatGpuArtwork::NvidiaRtxSuper;
    }
    if (phatContains(gpuName, "RTX") && (phatContains(gpuName, " Ti") || phatContains(gpuName, "TI"))) {
      return PhatGpuArtwork::NvidiaRtxTi;
    }
    if (phatContains(gpuName, "RTX")) return PhatGpuArtwork::NvidiaRtx;
    if (phatContains(gpuName, "GTX") && (phatContains(gpuName, " Ti") || phatContains(gpuName, "TI"))) {
      return PhatGpuArtwork::NvidiaGtxTi;
    }
    if (phatContains(gpuName, "GTX")) return PhatGpuArtwork::NvidiaGtx;
    return PHAT_MANUAL_GPU_ART;
  }

  if (phatContains(gpuName, "AMD") || phatContains(gpuName, "Radeon") || phatContains(gpuName, "RADEON")) {
    const bool genericGraphics = phatContains(gpuName, "Radeon(TM) Graphics") ||
                                 phatContains(gpuName, "Radeon Graphics") ||
                                 phatContains(gpuName, "RADEON(TM) GRAPHICS");
    const bool namedGraphicsWithoutRx = phatContains(gpuName, "Graphics") &&
                                        !phatContains(gpuName, " RX ") &&
                                        !phatContains(gpuName, " RX");
    const bool likelyIgpu = (genericGraphics || namedGraphicsWithoutRx) &&
                            gpuMemoryTotalMb < PHAT_IGPU_DEDICATED_VRAM_THRESHOLD_MB;
    if (likelyIgpu) {
      return phatCpuImpliesModernAmdIgpu(cpuName)
             ? PhatGpuArtwork::AmdRdnaIgpu
             : PhatGpuArtwork::AmdRadeonIgpu;
    }

    const int rxModel = phatExtractFirstNumberAfter(gpuName, "RX");
    if (rxModel >= 5000) return PhatGpuArtwork::AmdRdna;
    if (phatContains(gpuName, "RDNA")) return PhatGpuArtwork::AmdRdna;
    return PhatGpuArtwork::AmdRadeon;
  }

  if (phatContains(gpuName, "Intel") || phatContains(gpuName, "INTEL")) {
    if (phatContains(gpuName, "UHD")) return PhatGpuArtwork::IntelUhdIgpu;
    if (phatContains(gpuName, "Iris") || phatContains(gpuName, "Xe") || phatContains(gpuName, "XE")) {
      return PhatGpuArtwork::IntelXeIgpu;
    }
    if (phatContains(gpuName, "Arc") || phatContains(gpuName, "ARC")) {
      const bool explicitDiscrete = phatContains(gpuName, "Arc A") || phatContains(gpuName, "Arc B") ||
                                    phatContains(gpuName, "Arc Pro") || phatContains(gpuName, "ARC A") ||
                                    phatContains(gpuName, "ARC B") || phatContains(gpuName, "ARC PRO");
      if (explicitDiscrete || gpuMemoryTotalMb >= PHAT_IGPU_DEDICATED_VRAM_THRESHOLD_MB) {
        return PhatGpuArtwork::IntelArc;
      }
      return PhatGpuArtwork::IntelXeIgpu;
    }
    if (phatContains(gpuName, "Graphics")) return PhatGpuArtwork::IntelXeIgpu;
  }

  return PHAT_MANUAL_GPU_ART;
}

inline void phatRefreshArtworkSelection(const String &frame) {
  if (!PHAT_ARTWORK_AUTO) {
    phatCurrentCpuArtwork() = PHAT_MANUAL_CPU_ART;
    phatCurrentGpuArtwork() = PHAT_MANUAL_GPU_ART;
    return;
  }

  const String cpuName = phatExtractCpuNameForArtwork(frame);
  const String gpuName = phatExtractGpuNameForArtwork(frame);
  const int gpuMemoryTotalMb = phatExtractGmtMbForArtwork(frame);

  if (cpuName.length() > 0) phatCurrentCpuArtwork() = phatClassifyCpuArtwork(cpuName);
  if (gpuName.length() > 0) phatCurrentGpuArtwork() = phatClassifyGpuArtwork(gpuName, cpuName, gpuMemoryTotalMb);
}
