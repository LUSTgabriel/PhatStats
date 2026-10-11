# Hardware / 硬件

Default: classic ESP32-WROOM-32U class, 4 MB flash, 240 MHz CPU, SPI ILI9341 portrait TFT. Verify your own display schematic: labels and backlight wiring vary. ESP32 GPIO is 3.3 V logic; never feed 5 V logic into it. Power voltage must follow your module's specification.

| Signal | ESP32 GPIO | Notes |
| --- | --- | --- |
| TFT CS | 17 | Chip select |
| TFT DC | 16 | Data/command |
| TFT RESET | 15 | Boot-strapping pin; check external pull resistors |
| SPI SCLK | 18 | Clock |
| SPI MOSI | 23 | Display input |
| SPI MISO | 19 | If required by display |
| Backlight reference | 4 | Check `BoardPins.h` and module wiring before use |
| Button | 0 | Boot-strapping pin; holding it on reset can enter bootloader |
| NeoPixel | 2 | Optional; also a boot-strapping pin |
| TX LED | 5 | Board-dependent optional output |
| GND | GND | Shared ground |

Expansion GPIO 25/26/32/33/34/35 is optional. GPIO34/35 are input-only. Battery measurement is disabled by default. Review `firmware/include/BoardPins.h` and configuration before attaching peripherals. [ESP32 datasheet](https://www.espressif.com/sites/default/files/documentation/esp32_datasheet_en.pdf).

本配置不是 ESP32-S3/C3 通用接线。先核对板卡和屏幕原理图、电压及启动引脚外部电阻，再接线或上传；GPIO34/35 只能输入。
