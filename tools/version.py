"""Synchronize public and toolchain-compatible version metadata."""
from pathlib import Path
import argparse, json, re

root = Path(__file__).resolve().parents[1]
args = argparse.ArgumentParser()
args.add_argument('--check', action='store_true')
check = args.parse_args().check
v = json.loads((root / 'VERSION.json').read_text())
props = f'''<Project>
  <PropertyGroup>
    <Version>{v['package']}</Version>
    <AssemblyVersion>{v['assembly']}</AssemblyVersion>
    <FileVersion>{v['assembly']}</FileVersion>
    <InformationalVersion>{v['display']}</InformationalVersion>
    <IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <RuntimeIdentifiers>win-x64</RuntimeIdentifiers>
    <Deterministic>true</Deterministic>
    <PathMap>$(MSBuildThisFileDirectory)=/_/</PathMap>
    <RuntimeFrameworkVersion>10.0.11</RuntimeFrameworkVersion>
  </PropertyGroup>
</Project>
'''
p = root / 'Directory.Build.props'
source = root / 'firmware/src/main.cpp'
current = source.read_text(encoding='utf-8')
updated = re.sub(r'(#define CODE_VERS\s+)"[^"]+"', lambda m: m[1] + '"' + v['display'] + '"', current)
if check:
    assert p.read_text(encoding='utf-8') == props, 'Run python tools/version.py'
    assert updated == current, 'Firmware version out of sync'
    print('Version metadata: PASS')
else:
    p.write_text(props, encoding='utf-8')
    source.write_text(updated, encoding='utf-8')
    print('Version metadata synchronized')
