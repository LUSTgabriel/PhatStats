@echo off
setlocal
cd /d "%~dp0"
where pio >nul 2>nul
if errorlevel 1 (
  echo [ERROR] PlatformIO CLI ^(pio^) not found.
  echo Open this folder in VS Code PlatformIO, or install PlatformIO Core.
  exit /b 1
)

echo == Clean ==
pio run -t clean || exit /b 1

echo == Build ==
pio run || exit /b 1

echo == Protocol/project audit ==
python tools\audit_project.py || exit /b 1

echo.
echo SUCCESS
if exist .pio\build\esp32dev\firmware.bin echo Firmware: .pio\build\esp32dev\firmware.bin
endlocal
