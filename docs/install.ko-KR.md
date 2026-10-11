# 설치 · 0.01alpha

[English](install.en.md) · [简体中文](install.zh-CN.md) · [日本語](install.ja-JP.md)

Windows 10/11 x64 및 호환 ESP32-WROOM / ILI9341 디스플레이가 필요합니다.

1. [Releases](https://github.com/LUSTgabriel/PhatStats/releases)에서 Windows ZIP을 받아 모두 압축 해제합니다. 실행용 .NET 런타임은 포함되어 있습니다.
2. 센서에 필요한 경우 [PawnIO 공식 사이트](https://pawnio.eu/)에서 드라이버를 직접 설치하세요. 이 패키지에는 포함되지 않습니다. Windows 보안 기능을 끄지 마세요.
3. USB로 연결하고 장치 관리자에서 COM 포트를 확인합니다. 드라이버가 없을 때만 실제 칩에 맞는 [CP210x](https://www.silabs.com/developer-tools/usb-to-uart-bridge-vcp-drivers) / [CH340](https://www.wch-ic.com/downloads/CH341SER_EXE.html) 공식 드라이버를 설치합니다.
4. `PhatStats.exe`를 실행하고 관리자 확인을 승인합니다. 트레이에서 COM 포트를 선택하고 CurrentPortrait / 9600으로 설정하세요. 같은 포트를 사용하는 다른 시리얼 프로그램을 종료합니다.
5. 트레이 Language에서 한국어를 선택할 수 있습니다. 진단 오류 상세와 디스플레이의 기술 약어는 영어입니다.

자동 시작은 선택 사항입니다. 현재 사용자를 위한 관리자 권한의 `PhatStats` 작업을 생성하므로, 먼저 프로그램을 고정 폴더에 보관하세요.

제거하려면 자동 시작을 해제하고 종료한 다음 압축 해제 폴더를 삭제합니다. 설정 파일은 `%LOCALAPPDATA%\PhatStats\settings.json`입니다. PawnIO / USB 드라이버는 별도 제품이므로 각 공식 제거 절차를 따르세요.

펌웨어 업로드 전에 [배선](hardware.md)과 [전체 영어 안내](install.en.md)를 확인하세요. ESP32-S3/C3용이 아닙니다. `pio run`은 빌드만 합니다. 업로드는 기존 데이터를 덮어쓸 수 있습니다. 호환 펌웨어가 이미 실행 중이면 다시 업로드할 필요가 없습니다.
