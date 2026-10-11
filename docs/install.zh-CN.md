# 安装指南 · 0.01alpha

[English](install.en.md) · [日本語](install.ja-JP.md) · [한국어](install.ko-KR.md)

## Windows 上位机

要求：Windows 10/11 x64、匹配的 ESP32 显示板、管理员账户。发布 EXE 已带 .NET 运行时。读取硬件传感器需要管理员权限，因此启动时会有 UAC 提示；上位机不会静默安装驱动。

1. 从 [本项目 Releases](https://github.com/LUSTgabriel/PhatStats/releases) 下载 Windows ZIP，用 `Get-FileHash .\PhatStats-0.01alpha-win-x64.zip -Algorithm SHA256` 对照随 Release 提供的 `SHA256SUMS.txt`。
2. 完整解压，保留语言资源目录、许可证和说明。先放在固定目录，再考虑开启开机启动。
3. 部分 CPU/主板传感器需要 [PawnIO 官方驱动](https://pawnio.eu/)。到官网自行下载、阅读条款并安装；仅按官方安装器要求重启。已经安装的请先确认官方要求。不要为了显示传感器而关闭 Windows 安全保护。
4. 插入显示板，在设备管理器识别串口芯片；缺驱动时选 [Silicon Labs CP210x 官方驱动](https://www.silabs.com/developer-tools/usb-to-uart-bridge-vcp-drivers) 或 [WCH CH340/CH341 官方驱动](https://www.wch-ic.com/downloads/CH341SER_EXE.html)。不要两个都盲目安装；仅充电的数据线不会出现串口。
5. 启动 `PhatStats.exe`，确认管理员提示，在托盘选 COM 端口；设置中使用 **CurrentPortrait**、**9600** 波特率并选择 GPU。关闭占用同一端口的串口监视器或其他上位机。
6. 托盘 Language 可切换英语、中文、日语、韩语；切换即时生效。检查暂停、恢复和诊断信息。不存在或不受支持的传感器可能没有读数。

“随 Windows 启动”是可选项，会为当前用户建立名为 `PhatStats`、以最高权限执行的交互登录计划任务。启用后不要随意移动 EXE；移动后应关闭此选项，再在新固定目录重新开启。

## 固件

目标是经典 ESP32-WROOM、4 MB Flash、ILI9341 竖屏，不是 ESP32-S3/C3 通用包。先核对 [引脚与电气说明](hardware.md)。现有板子已运行匹配固件时，无需重新刷机。

安装 [VS Code](https://code.visualstudio.com/) 和官方 [PlatformIO 扩展](https://platformio.org/install/ide?install=vscode)，打开 `firmware` 文件夹，在 Project Tasks → esp32dev → General → Build 编译；命令行等价 `pio run`，使用 PlatformIO Core 6.2.0。

上传前核对 `include/BoardPins.h`、`include/PhatStatsConfig.h`，保存需要保留的旧固件。只向已确认的板子上传：`pio run -t upload --upload-port COMx`。分区为 3 MiB 应用、无 OTA 槽，上传可能覆盖现有固件或数据。本次整理发布没有向维护者设备刷机。

## 常见问题和卸载

| 现象 | 排查 |
| --- | --- |
| 找不到托盘图标 | 查看隐藏托盘区，确认是否已有另一个实例 |
| 没有 COM 端口 | 数据线、设备管理器、对应芯片驱动 |
| 端口拒绝访问/被占用 | 关闭串口监视器和其他上位机，核对端口 |
| 屏幕无数据或错乱 | CurrentPortrait、9600、板型和屏幕接线 |
| 温度缺失 | 诊断、PawnIO、硬件是否被 [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) 支持 |

卸载：先在托盘取消随 Windows 启动，再退出，删除解压目录。设置位于 `%LOCALAPPDATA%\PhatStats\settings.json`；想重置时仅删除这个应用的设置目录。PawnIO 和 USB 驱动是独立软件，按各自官方卸载方法处理；不要因为移除上位机就删除共享驱动。
