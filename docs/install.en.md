# Installation · 0.01alpha

[简体中文](install.zh-CN.md) · [日本語](install.ja-JP.md) · [한국어](install.ko-KR.md)

## Windows host

Requirements: Windows 10/11 x64, a matching ESP32 USB display, and an administrator account. The self-contained EXE includes the .NET runtime. The application manifest requests administrator privileges so hardware sensors can be read. No driver is silently installed by PhatStats.

1. Download the Windows ZIP from [this project's Releases](https://github.com/LUSTgabriel/PhatStats/releases). Compare its SHA-256 with `SHA256SUMS.txt`, for example `Get-FileHash .\PhatStats-0.01alpha-win-x64.zip -Algorithm SHA256`.
2. Extract the whole package; keep the licenses and language-resource folders. Choose a permanent directory before enabling start with Windows.
3. Some CPU/mainboard sensors need [PawnIO](https://pawnio.eu/), the driver used by LibreHardwareMonitor. Download/install from the official site, follow its terms and restart if its installer requests it. Do not disable Windows security protections to make a sensor appear. If PawnIO is already installed, check the official instructions before changing it.
4. Connect the display. If no COM device appears, identify the USB serial chip in Device Manager: [Silicon Labs CP210x](https://www.silabs.com/developer-tools/usb-to-uart-bridge-vcp-drivers) or [WCH CH340/CH341](https://www.wch-ic.com/downloads/CH341SER_EXE.html). Only install the driver for your actual chip. A charge-only USB cable will not expose a serial port.
5. Run `PhatStats.exe`, approve the expected UAC prompt, open its tray menu and choose COM port. In Settings select CurrentPortrait, 9600 baud and the desired GPU. Close other serial monitors before connecting.
6. Choose Language in the tray: English / 简体中文 / 日本語 / 한국어. Test Pause, Resume, and Diagnostics. Missing sensors are reported; they are not proof that the PC has that measurement available.

Start with Windows is optional. It creates a Task Scheduler task named `PhatStats` for the current user's interactive logon, with highest privileges. Enable it only after selecting a permanent application folder.

## Firmware

This configuration targets classic ESP32-WROOM with 4 MB flash and ILI9341, not ESP32-S3/C3. Check [pins and electrical requirements](hardware.md). If the matching firmware is already on the board, you can use the host without flashing again.

Install [VS Code](https://code.visualstudio.com/) and the official [PlatformIO IDE extension](https://platformio.org/install/ide?install=vscode), open the `firmware` folder, then use Project Tasks → esp32dev → General → Build. The CLI equivalent is `pio run` (PlatformIO Core 6.2.0).

Review `include/BoardPins.h` and `include/PhatStatsConfig.h` before flashing. Save any existing firmware you need to preserve. Upload only to your identified board: `pio run -t upload --upload-port COMx`. This uses a 3 MiB application partition and no OTA slot; it can replace existing board firmware/data. This repository's publication did not flash the maintainer's device.

## Troubleshooting and removal

| Symptom | Check |
| --- | --- |
| No tray icon | Check hidden tray icons and that a second instance is not already running |
| No COM port | Data cable, actual USB serial chip/driver, Device Manager |
| Access denied / port busy | Close serial monitor/another host; choose correct port |
| Wrong or empty screen | Match baud and CurrentPortrait; check TFT wiring and board type |
| Missing CPU/GPU temperatures | Diagnostics, supported hardware, PawnIO installation; see [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) |
| Startup no longer works | Application moved? Disable start with Windows and re-enable from its new permanent folder |

To uninstall: uncheck Start with Windows in the tray, choose Exit, then remove the extracted application folder. Settings live in `%LOCALAPPDATA%\PhatStats\settings.json`; delete only that application folder if you want to reset preferences. PawnIO and USB drivers are separate products: manage them through their own official uninstall procedures. Do not remove shared drivers just because you removed this host.
