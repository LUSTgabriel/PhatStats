# インストール · 0.01alpha

[English](install.en.md) · [简体中文](install.zh-CN.md) · [한국어](install.ko-KR.md)

Windows 10/11 x64 と対応する ESP32-WROOM / ILI9341 が必要です。

1. [Releases](https://github.com/LUSTgabriel/PhatStats/releases) から Windows ZIP をダウンロードし、すべて展開します。実行用 .NET は同梱されています。
2. センサーに必要な場合は [PawnIO 公式サイト](https://pawnio.eu/) のドライバーを自分でインストールしてください。同梱されていません。Windows の安全機能を無効にしないでください。
3. USB 接続し、デバイス マネージャーで COM ポートを確認します。ドライバーがない場合だけ、実際のチップに合わせて [CP210x](https://www.silabs.com/developer-tools/usb-to-uart-bridge-vcp-drivers) / [CH340](https://www.wch-ic.com/downloads/CH341SER_EXE.html) を選びます。
4. `PhatStats.exe` を起動し、管理者確認を承認します。トレイで COM ポートを選び、設定を CurrentPortrait / 9600 にします。同じポートを使うシリアルモニターは閉じてください。
5. トレイの Language から日本語を選べます。診断のエラー詳細と画面上の技術略語は英語です。

自動起動は任意です。現在のユーザー向けに管理者権限の `PhatStats` タスクを作成するため、有効にする前にアプリを固定フォルダーに置いてください。

削除する場合は自動起動を解除し、終了してから展開フォルダーを削除します。設定は `%LOCALAPPDATA%\PhatStats\settings.json` にあります。PawnIO / USB ドライバーは別製品なので、それぞれの公式手順を使ってください。

ファームウェアの書き込み前に [配線](hardware.md) と [英語の完全な手順](install.en.md) を確認してください。これは ESP32-S3/C3 用ではありません。`pio run` はビルドのみです。書き込みは既存データを上書きする可能性があります。対応するファームウェアが動いていれば再書き込みは不要です。
