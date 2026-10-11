$ErrorActionPreference = 'Stop'
$minutes = 20
$intervalSeconds = 10
$samples = [Math]::Max(2, [int](($minutes * 60) / $intervalSeconds))
$p = Get-Process PhatStats -ErrorAction Stop
$pid0 = $p.Id
$rows = [System.Collections.Generic.List[object]]::new($samples)
$previousCpu = $p.TotalProcessorTime.TotalSeconds
$previousTime = [DateTimeOffset]::UtcNow

Write-Host "Monitoring PhatStats PID $pid0 for $minutes minutes..."
for ($i = 0; $i -lt $samples; $i++) {
    Start-Sleep -Seconds $intervalSeconds
    $p = Get-Process -Id $pid0 -ErrorAction Stop
    $now = [DateTimeOffset]::UtcNow
    $cpu = $p.TotalProcessorTime.TotalSeconds
    $elapsed = ($now - $previousTime).TotalSeconds
    $cpuPercent = if ($elapsed -gt 0) { (($cpu - $previousCpu) / $elapsed / [Environment]::ProcessorCount) * 100.0 } else { 0 }
    $previousCpu = $cpu
    $previousTime = $now

    $row = [PSCustomObject]@{
        Time      = Get-Date -Format 'HH:mm:ss'
        PrivateMB = [math]::Round($p.PrivateMemorySize64 / 1MB, 1)
        WorkingMB = [math]::Round($p.WorkingSet64 / 1MB, 1)
        Handles   = $p.HandleCount
        Threads   = $p.Threads.Count
        CPU       = [math]::Round($cpuPercent, 3)
    }
    $rows.Add($row)
    Write-Host ("[{0}] Private={1,6} MB  WS={2,6} MB  Handles={3,4}  Threads={4,3}  CPU={5,6}%" -f $row.Time,$row.PrivateMB,$row.WorkingMB,$row.Handles,$row.Threads,$row.CPU)
}

$first = $rows[0]
$last = $rows[$rows.Count - 1]
Write-Host ''
Write-Host '========== MEMORY SOAK SUMMARY =========='
[PSCustomObject]@{
    Private_Start_MB = $first.PrivateMB
    Private_End_MB = $last.PrivateMB
    Private_Delta_MB = [math]::Round($last.PrivateMB - $first.PrivateMB, 1)
    Private_Min_MB = ($rows.PrivateMB | Measure-Object -Minimum).Minimum
    Private_Max_MB = ($rows.PrivateMB | Measure-Object -Maximum).Maximum
    Working_Start_MB = $first.WorkingMB
    Working_End_MB = $last.WorkingMB
    Working_Delta_MB = [math]::Round($last.WorkingMB - $first.WorkingMB, 1)
    Handles_Start = $first.Handles
    Handles_End = $last.Handles
    Handles_Delta = $last.Handles - $first.Handles
    Threads_Start = $first.Threads
    Threads_End = $last.Threads
    Threads_Delta = $last.Threads - $first.Threads
    CPU_Avg_Percent = [math]::Round(($rows.CPU | Measure-Object -Average).Average, 3)
} | Format-List
