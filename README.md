# PhatStats · 0.01alpha

[简体中文](README.zh-CN.md) · English · [日本語](docs/install.ja-JP.md) · [한국어](docs/install.ko-KR.md)

A small USB-connected PC stats display: ESP32 + ILI9341 firmware and a Windows tray host, maintained by LUSTgabriel. The first public version **0.01alpha** is a nod to 四季宝 (Skippy).

This is a maintained derivative of [Rupert Hirst / koogar's Phat-Stats](https://github.com/koogar/Phat-Stats), not the upstream author's official release. [Source history and attribution](SOURCE_PROVENANCE.md) · [Licenses](LICENSE) · [Third-party notices](THIRD_PARTY_NOTICES.md).

## Get started

1. Download `PhatStats-0.01alpha-win-x64.zip` from [Releases](https://github.com/LUSTgabriel/PhatStats/releases). Extract the entire archive into a permanent folder.
2. Install the separate [official PawnIO driver](https://pawnio.eu/) if your hardware sensors require it. It is not bundled. Follow its installer and license terms yourself.
3. Connect the matching ESP32 display via USB. Install a USB serial driver only if Device Manager reports that it is missing.
4. Run `PhatStats.exe`, accept the expected administrator prompt, choose the correct COM port and **CurrentPortrait** protocol from the tray settings. Default firmware baud is **9600**.

Read the [installation and troubleshooting guide](docs/install.en.md) before flashing a board. The release EXE is self-contained; you do not need to install .NET just to run it.

## What is included

| Component | Location | Details |
| --- | --- | --- |
| Firmware | `firmware/` | Classic ESP32-WROOM class, 4 MB flash, ILI9341 portrait display, pinned PlatformIO dependencies, v5 artwork |
| Windows host | `host/` | Windows 10/11 x64, .NET 10, LibreHardwareMonitor 0.9.6, complete serial frames, bounded diagnostic history |
| Languages | host `.resx` | English, Simplified Chinese, Japanese, Korean; switch live in the tray |
| Release | GitHub Releases | Rebuilt Windows binary, installation docs, notices, checksums and matching source archive |

The firmware uses technical ASCII labels; it does not render full CJK translations. Diagnostic error payloads remain English. Legacy network-rate tags are zero constants; the host does not sample network traffic.

## Build and maintain

Windows, .NET 10 SDK: `cd host` then `./build_single_exe.ps1`. The script runs source guards, builds, tests and publishes without stopping your running application. Outputs go to `host/dist/`.

PlatformIO Core 6.2.0: `cd firmware` then `pio run`. This compiles only. Select your own board/COM port before any upload. [Hardware and wiring](docs/hardware.md) · [Contributor guide](CONTRIBUTING.md) · [Release checklist](docs/maintenance.md) · [Changelog](CHANGELOG.md).

This is an alpha intended for the documented hardware configuration. Build/self-test results do not prove every sensor or display works on another PC or board. See [validation](docs/validation.md).
