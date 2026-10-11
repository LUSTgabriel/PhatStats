param(
    [Parameter(Mandatory=$true)][string]$PortName
)
$ErrorActionPreference = 'Stop'

# Ensure the tray host cannot still own the COM port or keep an older single-instance build alive.
$running = Get-Process -Name 'PhatStats' -ErrorAction SilentlyContinue
if ($running) {
    $running | ForEach-Object {
        Write-Host "Stopping PhatStats PID $($_.Id) before direct serial protocol test."
        Stop-Process -Id $_.Id -Force
    }
    Start-Sleep -Milliseconds 300
}

# Known-good complete CurrentPortrait frame. It deliberately contains every tag
# consumed by Display_Port_Batt() and no line ending.
$frame = 'C42c 17%|G51c 63%|R12.3GB|RA19.7|GMT8192|GMU2048|GFANL35|GRPM1200|GPWR150|CPU:AMD Ryzen Test CPU GPU:NVIDIA GeForce RTX 3070 Ti|GCC1800|GMC9500|GSC1800|CHC4500|'
$port = [System.IO.Ports.SerialPort]::new($PortName, 9600, [System.IO.Ports.Parity]::None, 8, [System.IO.Ports.StopBits]::One)
$port.Handshake = [System.IO.Ports.Handshake]::None
$port.DtrEnable = $false
$port.RtsEnable = $false
$port.WriteTimeout = 1500
try {
    $port.Open()
    Write-Host "Sending a known-good complete portrait frame to $PortName once per second. Ctrl+C to stop."
    Write-Host "Frame bytes (ASCII): $([Text.Encoding]::ASCII.GetByteCount($frame))"
    Write-Host $frame
    while ($true) {
        $port.Write($frame)
        Start-Sleep -Milliseconds 1000
    }
}
finally {
    if ($port.IsOpen) { $port.Close() }
    $port.Dispose()
}
