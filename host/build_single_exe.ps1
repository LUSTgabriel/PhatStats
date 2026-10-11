$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

function Invoke-DotNet {
    param([Parameter(Mandatory=$true)][string[]]$Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

Write-Host '== Source security audit: no application network telemetry stack =='
$sourceFiles = Get-ChildItem -LiteralPath (Join-Path $root 'src\PhatStats') -Recurse -Filter '*.cs' -File | Where-Object { $_.FullName -notmatch '[\\/]\b(bin|obj)[\\/]' }
$forbidden = @(
    'System.Net.',
    'NetworkInterface',
    'NetworkChange',
    'IPGlobalProperties',
    'HttpClient',
    'Socket',
    'TcpClient',
    'UdpClient',
    'WebRequest'
)
foreach ($token in $forbidden) {
    $hit = $sourceFiles | Select-String -SimpleMatch $token
    if ($hit) {
        $hit | ForEach-Object { Write-Host $_.Path ':' $_.LineNumber ':' $_.Line }
        throw "Forbidden network API token found: $token"
    }
}
if (Test-Path -LiteralPath (Join-Path $root 'src\PhatStats\Hardware\NetworkSampler.cs')) {
    throw 'NetworkSampler.cs must not exist.'
}


Write-Host '== Frozen protocol source audit =='
$protocolExpected = @{
    'NameSanitizer.cs'  = '981922c3864dce059e9723b7484e33470471409e35fbb5e1a1fd8521557e4b6e'
    'ProtocolSession.cs' = 'bcc319330ab3b6c2350e3b006b6d0c3b4595ffc8b4bec1c5404d5836466e65f8'
    'ProtocolEncoder.cs' = 'f772b891233052fe3dce721e744aef2380b188361d995d0c6f0e9dd409fe664e'
    'ProtocolProfile.cs' = 'fbe3cbcf453913b707cae7fc201ab2537ba31389f63c7c708568275884ab213a'
}
foreach ($entry in $protocolExpected.GetEnumerator()) {
    $protocolPath = Join-Path $root ('src\PhatStats\Protocol\' + $entry.Key)
    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $protocolPath).Hash.ToLowerInvariant()
    if ($actual -ne $entry.Value) {
        throw "Frozen serial protocol source changed: $($entry.Key) expected $($entry.Value) actual $actual"
    }
}
Write-Host 'Frozen protocol source: PASS'

Write-Host '== Memory-stability source audit =='
$hardwareEngine = Get-Content -LiteralPath (Join-Path $root 'src\PhatStats\Hardware\HardwareTelemetryEngine.cs') -Raw
if ($hardwareEngine -match '_recoveryAttempt\s*%\s*3') {
    throw 'Old periodic LibreHardwareMonitor backend rebuild loop is still present.'
}
if ($hardwareEngine -notmatch 'BackendRebuildCooldown') {
    throw 'Backend rebuild cooldown is missing.'
}
if ($hardwareEngine -notmatch 'ValuesTimeWindow\s*=\s*TimeSpan\.Zero') {
    throw 'LibreHardwareMonitor sensor-history suppression is missing.'
}
$projectXml = Get-Content -LiteralPath (Join-Path $root 'src\PhatStats\PhatStats.csproj') -Raw
if ($projectXml -notmatch 'System\.GC\.ConserveMemory') {
    throw 'GC memory-conservation runtime option is missing.'
}
# Compile-fix guard: keep the degraded-field fallback explicitly typed as IReadOnlyList<string>.
$snapshotValidator = Get-Content -LiteralPath (Join-Path $root 'src\PhatStats\Hardware\SnapshotValidator.cs') -Raw
if ($snapshotValidator -notmatch '\(IReadOnlyList<string>\?\)degraded\s*\?\?\s*Array\.Empty<string>\(\)') {
    throw 'SnapshotValidator degraded-field fallback is not explicitly typed; this can trigger CS0019.'
}
Write-Host 'Memory-stability source audit: PASS'

Write-Host '== .NET SDK =='
Invoke-DotNet -Arguments @('--version')

Write-Host '== Restore =='
Invoke-DotNet -Arguments @('restore', '.\PhatStats.sln', '--locked-mode', '-p:NuGetAudit=false')

Write-Host '== Release build =='
Invoke-DotNet -Arguments @('build', '.\PhatStats.sln', '-c', 'Release', '--no-restore')

Write-Host '== Self tests =='
Invoke-DotNet -Arguments @('run', '--project', '.\tests\PhatStats.SelfTest\PhatStats.SelfTest.csproj', '-c', 'Release', '--no-build')

Write-Host '== Restore win-x64 publish graph =='
Invoke-DotNet -Arguments @('restore', '.\src\PhatStats\PhatStats.csproj', '-r', 'win-x64', '--locked-mode', '-p:NuGetAudit=false')

Write-Host '== Publish self-contained single-file win-x64 =='
$publish = Join-Path $root ('dist\publish-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $publish -Force | Out-Null
Invoke-DotNet -Arguments @(
    'publish', '.\src\PhatStats\PhatStats.csproj',
    '-c', 'Release',
    '-r', 'win-x64',
    '--self-contained', 'true',
    '--no-restore',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:EnableCompressionInSingleFile=true',
    '-p:PublishTrimmed=false',
    '-o', $publish
)

$exe = Join-Path $publish 'PhatStats.exe'
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Publish did not produce $exe"
}

$dist = Join-Path $root 'dist'
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $exe).Hash.ToLowerInvariant()
$info = @(
    'PhatStats Windows Host',
    ('Build package: ' + (Get-Content (Join-Path $root '..\VERSION.json') -Raw | ConvertFrom-Json).display),
    'Target: win-x64 self-contained single-file',
    'UI languages: en-US, zh-CN, ja-JP, ko-KR',
    'Diagnostics/error payload language: English',
    'Application network telemetry stack: removed',
    'CurrentPortrait protocol: complete current frame each sample',
    ('Built UTC: ' + [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')),
    ('PhatStats.exe SHA256: ' + $hash)
)
$info | Set-Content -LiteralPath (Join-Path $dist 'BUILD_INFO.txt') -Encoding UTF8

Write-Host ''
Write-Host 'SUCCESS'
Write-Host "Published EXE: $exe"
Write-Host "SHA256: $hash"

$publish | Set-Content -LiteralPath (Join-Path $dist 'publish-path.txt') -Encoding UTF8
