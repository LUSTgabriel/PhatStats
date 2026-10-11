// Maintained derivative of koogar/Phat-Stats. Publication changes: LUSTgabriel, 2026-10-11.
// Original notices retained; see SOURCE_PROVENANCE.md and LICENSE.txt (GPL-2.0-or-later).
#pragma once
#include <Arduino.h>
#include <Adafruit_ILI9341.h>
#include "PhatStatsConfig.h"
#include "PixelArtIndexed4.h"
#include "ArtworkAutoSelect.h"

extern Adafruit_ILI9341 tft;

/*
 * Exact indexed-art renderer.
 *
 * Complete 82x82 images are stored as packed 4-bit palette indices and expanded
 * to RGB565 one row at a time. UI geometry remains fixed at the legacy 82x82
 * CPU/GPU image boxes. Runtime selection is UI-only and reads existing frame
 * text; it does not alter serial framing or parser behavior.
 */

inline void drawIndexed4Art(int16_t x, int16_t y, const uint8_t *pixels) {
  static uint16_t row[PHAT_ART_W];

  for (uint8_t py = 0; py < PHAT_ART_H; ++py) {
    const size_t rowBase = static_cast<size_t>(py) * PHAT_ART_ROW_BYTES;
    for (uint8_t px = 0; px < PHAT_ART_W; ++px) {
      const uint8_t packed = pgm_read_byte(pixels + rowBase + (px >> 1));
      const uint8_t paletteIndex = (px & 1) ? (packed & 0x0F) : (packed >> 4);
      row[px] = pgm_read_word(&PHAT_ART_PALETTE[paletteIndex]);
    }
    tft.drawRGBBitmap(x, y + py, row, PHAT_ART_W, 1);
  }
}

inline const uint8_t *phatCpuIndexedArt() {
  switch (phatCurrentCpuArtwork()) {
    case PhatCpuArtwork::AmdStyle2:   return CPU_AMD_STYLE2_I4;
    case PhatCpuArtwork::AmdStyle5:   return CPU_AMD_STYLE5_I4;
    case PhatCpuArtwork::AmdStyle7:   return CPU_AMD_STYLE7_I4;
    case PhatCpuArtwork::IntelStyle1: return CPU_INTEL_STYLE1_I4;
    case PhatCpuArtwork::IntelStyle2: return CPU_INTEL_STYLE2_I4;
    case PhatCpuArtwork::IntelStyle5: return CPU_INTEL_STYLE5_I4;
    case PhatCpuArtwork::IntelStyle7: return CPU_INTEL_STYLE7_I4;
    case PhatCpuArtwork::AmdStyle1:
    default:                          return CPU_AMD_STYLE1_I4;
  }
}

inline const uint8_t *phatGpuIndexedArt() {
  switch (phatCurrentGpuArtwork()) {
    case PhatGpuArtwork::NvidiaGtx:       return GPU_NVIDIA_GTX_I4;
    case PhatGpuArtwork::NvidiaGtxTi:     return GPU_NVIDIA_GTX_TI_I4;
    case PhatGpuArtwork::NvidiaRtx:       return GPU_NVIDIA_RTX_I4;
    case PhatGpuArtwork::NvidiaRtxSuper:  return GPU_NVIDIA_RTX_SUPER_I4;
    case PhatGpuArtwork::NvidiaPro:       return GPU_NVIDIA_PRO_I4;
    case PhatGpuArtwork::AmdRadeon:       return GPU_AMD_RADEON_I4;
    case PhatGpuArtwork::AmdRdna:         return GPU_AMD_RDNA_I4;
    case PhatGpuArtwork::AmdRadeonIgpu:   return GPU_AMD_RADEON_IGPU_I4;
    case PhatGpuArtwork::AmdRdnaIgpu:     return GPU_AMD_RDNA_IGPU_I4;
    case PhatGpuArtwork::IntelArc:        return GPU_INTEL_ARC_I4;
    case PhatGpuArtwork::IntelUhdIgpu:    return GPU_INTEL_UHD_IGPU_I4;
    case PhatGpuArtwork::IntelXeIgpu:     return GPU_INTEL_XE_IGPU_I4;
    case PhatGpuArtwork::NvidiaRtxTi:
    default:                              return GPU_NVIDIA_RTX_TI_I4;
  }
}

inline void drawCpuPixelArt(int x, int y) {
  drawIndexed4Art(x, y, phatCpuIndexedArt());
}

inline void drawGpuPixelArt(int x, int y) {
  drawIndexed4Art(x, y, phatGpuIndexedArt());
}
