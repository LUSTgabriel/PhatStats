# Source provenance / 代码来源

## Firmware

Original Phat-Stats / Gnat-Stats and serial-monitor work: **Rupert Hirst & Colin Conway**, copyright notices retained. Upstream: [koogar/Phat-Stats](https://github.com/koogar/Phat-Stats), [license](https://github.com/koogar/Phat-Stats/blob/main/LICENSE.txt).

Imported from the maintainer's current local `PhatStats_ESP32_PlatformIO_VSCode_v5` snapshot. It contains the older 3.1.7.3 baseline under `firmware/legacy/`; the current renderer/configuration includes portrait layout, v5 82×82 artwork, automatic CPU/GPU artwork classification and PlatformIO guards. No claim is made that this supplied snapshot is byte-identical to the upstream default branch. [Import SHA-256 manifest](docs/import-manifest.json) records the supplied file bytes before publication edits.

Publication edits (2026-10-11): display version 0.01alpha, source/license attribution, install/maintenance docs, safer build process, translation checks, release packaging and CI. Existing frozen protocol files and parser behavior are preserved.

## Windows host

Imported from the maintainer's current `PhatStats-Windows-Host_SingleEXE_i18n_NoNetwork_v1_3_1` snapshot, rather than the old upstream HardwareSerialMonitor executable. The local folder name and old embedded assembly versions differed; public display version is now **0.01alpha** (MSBuild package version `0.0.1-alpha`). Host source is distributed under GPL-3.0-or-later. The sensor library is [LibreHardwareMonitor 0.9.6](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor/tree/3d331e3370efb858411f19511373eff65a218701), under its own MPL-2.0 and embedded-component licenses. Dependencies are not claimed as the maintainer's original code.

## Artwork and external software

The supplied v5 pixel assets and editable semantic layers are retained with the source. AMD / Intel / NVIDIA branding identifies hardware; names and trademarks remain with their respective owners and no affiliation is implied. The external reference-board screenshot had no independently established redistribution provenance and is excluded from public packages. [Brand information](https://www.amd.com/en/legal/trademarks.html) · [Intel trademarks](https://www.intel.com/content/www/us/en/legal/trademarks.html) · [NVIDIA legal](https://www.nvidia.com/en-us/about-nvidia/legal-info/).

PawnIO installer and USB drivers are **link-only**, not copied into releases. Their licenses must be handled at their official source. See the installation guide.
