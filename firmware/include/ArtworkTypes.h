// Maintained derivative of koogar/Phat-Stats. Publication changes: LUSTgabriel, 2026-10-11.
// Original notices retained; see SOURCE_PROVENANCE.md and LICENSE.txt (GPL-2.0-or-later).
#pragma once
#include <Arduino.h>

// Runtime artwork IDs. These are UI-only and never appear on the serial wire.
enum class PhatCpuArtwork : uint8_t {
  AmdStyle1,
  AmdStyle2,
  AmdStyle5,
  AmdStyle7,
  IntelStyle1,
  IntelStyle2,
  IntelStyle5,
  IntelStyle7
};

enum class PhatGpuArtwork : uint8_t {
  NvidiaGtx,
  NvidiaGtxTi,
  NvidiaRtx,
  NvidiaRtxTi,
  NvidiaRtxSuper,
  NvidiaPro,
  AmdRadeon,
  AmdRdna,
  AmdRadeonIgpu,
  AmdRdnaIgpu,
  IntelArc,
  IntelUhdIgpu,
  IntelXeIgpu
};
