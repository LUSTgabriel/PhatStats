# Changelog

## 0.01alpha — 2026-10-11

First public release of the maintainer's currently used PhatStats code, dedicated to 四季宝.

- ESP32 v5 portrait firmware and complete Windows host source published together.
- Windows host rebuilt with public version metadata; English / Chinese / Japanese / Korean UI retained.
- Documented hardware, installation, driver downloads, troubleshooting and removal.
- Added source provenance, license notices, translation audits, CI and reproducible release packaging.
- Build no longer force-stops a running PhatStats instance or recursively wipes prior build directories.
- Guarded the historical v4 artwork generator against accidental overwrite of approved v5 assets.
- Synchronized the stale layout audit baseline with three existing v5 lines: red CPU outline and two disabled inner borders. Rendering coordinates and parser signatures remain unchanged.
- Removed palette-redefinition compiler warnings while retaining the existing orange/pink values.

Alpha scope: documented ESP32 hardware and Windows x64 only. Publication does not demonstrate a new live firmware flash, native GUI acceptance test or exhaustive sensor compatibility test.
