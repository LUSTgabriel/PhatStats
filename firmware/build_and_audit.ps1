$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Get-Command pio -ErrorAction SilentlyContinue)) {
    throw 'PlatformIO CLI (pio) not found. Open this folder in VS Code PlatformIO or install PlatformIO Core.'
}

Write-Host '== Clean =='
pio run -t clean
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== Build =='
pio run
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== Protocol/project audit =='
python tools/audit_project.py
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'SUCCESS'
Write-Host 'Firmware: .pio\build\esp32dev\firmware.bin'
