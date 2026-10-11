# Validation · 0.01alpha

Publication checks are performed on the staged copy; the maintainer's source directories are preserved.

- Windows host: .NET SDK 10.0.401, Release build, zero compiler warnings/errors; existing behavioral self-tests pass (serial frame contracts, runtime/diagnostic state, settings and localization).
- Four-language resource audit: 45 matching keys, no empty translations, matching format placeholders and valid literal resource references.
- Firmware: frozen serial functions and 65 parser-signature lines are checked; layout/pin/artwork checks run before compilation. The old golden layout file was stale relative to the supplied v5: three existing color/comment differences were reconciled; no display coordinate or parser behavior was changed.
- Firmware compiles on espressif32 5.2.0 / Arduino-ESP32 2.0.5 / classic ESP32: 1,087,121 bytes Flash (34.6% of 3 MiB) and 31,956 bytes RAM (9.8%). This is compile-only evidence.
- Release: self-contained win-x64 EXE, matching project source, corresponding MPL/LGPL dependency source, upstream license notices, ZIP integrity checks and SHA-256 manifest.

Not performed for this publication: firmware upload, electrical/display acceptance, native tray GUI review across four locales, long-duration memory soak test or exhaustive hardware sensor support. The alpha is unsigned; a compiled/self-tested binary is not a guarantee of hardware compatibility.

See GitHub Actions for clean runner build results. Firmware compilation and host compilation are distinct jobs; CI does not connect to hardware.
