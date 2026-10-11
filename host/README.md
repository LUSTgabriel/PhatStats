# Windows host

See [project overview](../README.md), [install](../docs/install.en.md) and [maintenance](../docs/maintenance.md).

Windows 10/11 x64; .NET 10 SDK for development. Run `./build_single_exe.ps1` from this directory. It builds, runs existing self-tests and publishes to a new directory under `dist/`. It does not stop a running host or wipe previous outputs. Full package generation: `python ../tools/package_release.py`.

The inherited BUILD_FIX and MEMORY_OPTIMIZATION notes describe the supplied local development history. They are not evidence of new hardware acceptance for this public release.
