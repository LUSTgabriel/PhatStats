# PhatStats · 0.01alpha

简体中文 · [English](README.md) · [日本語](docs/install.ja-JP.md) · [한국어](docs/install.ko-KR.md)

把电脑状态放到一块 USB 小屏上：ESP32 + ILI9341 固件，以及 Windows 托盘上位机。由 LUSTgabriel 维护，第一公开版 **0.01alpha** 致敬四季宝。

项目基于 [Rupert Hirst / koogar 的 Phat-Stats](https://github.com/koogar/Phat-Stats)，是个人维护的衍生版本。[代码来源](SOURCE_PROVENANCE.md) · [许可证](LICENSE) · [第三方声明](THIRD_PARTY_NOTICES.md)。

## 下载和使用

1. 在 [Releases](https://github.com/LUSTgabriel/PhatStats/releases) 下载 `PhatStats-0.01alpha-win-x64.zip`，完整解压到一个固定目录。
2. 如果传感器需要，安装 [PawnIO 官方驱动](https://pawnio.eu/)。驱动不随包分发，请自行阅读并完成官方安装。
3. 用 USB 连接与本固件匹配的显示板。设备管理器提示缺少串口驱动时，再去芯片厂商官网安装对应驱动。
4. 启动 `PhatStats.exe`，确认管理员提示，在托盘中选对 COM 端口、**CurrentPortrait** 协议，波特率 **9600**。

完整步骤、卸载方法和常见问题见 [中文安装指南](docs/install.zh-CN.md)。运行发布包不需要额外安装 .NET；从源码构建才需要 .NET 10 SDK。

## 内容和边界

- `firmware/`：当前 ESP32 v5 固件、配置、像素素材、历史基线和 PlatformIO 构建脚本。
- `host/`：完整 Windows 上位机源码、自动测试和构建脚本。
- 上位机支持英语、简体中文、日语、韩语，托盘中切换即时生效。诊断错误详情保留英语，固件屏幕仍为技术英文缩写。
- 编译版本放 Release，附带说明、许可证、校验值及对应源码；PawnIO 用官方链接引导安装。

这是经典 ESP32-WROOM / 4 MB / ILI9341 配置的 alpha，不是所有 ESP32 型号通用的刷机包。使用前核对 [硬件接线](docs/hardware.md)。

## 开发与维护

Windows 安装 .NET 10 SDK，在 `host` 中运行 `./build_single_exe.ps1`。脚本会检查协议、运行测试并发布，不会强制结束正在运行的上位机。固件在 `firmware` 中运行 `pio run`，只编译；上传前自己选择端口。

[贡献说明](CONTRIBUTING.md) · [维护和发布](docs/maintenance.md) · [变更日志](CHANGELOG.md) · [验证记录](docs/validation.md)。
