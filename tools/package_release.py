"""Package a verified publish folder, corresponding source and license notices."""
from pathlib import Path
import hashlib, json, subprocess, sys, zipfile, shutil

root = Path(__file__).resolve().parents[1]
subprocess.run([sys.executable,str(root/'tools/audit_release.py')],check=True)
v = json.loads((root/'VERSION.json').read_text())
dist = root/'host/dist'
publish = Path((dist/'publish-path.txt').read_text(encoding='utf-8-sig').strip()).resolve()
assert publish.is_relative_to(dist.resolve()), 'Publish output must stay under host/dist'
assert (publish/'PhatStats.exe').is_file(), 'Build host first'
files = []
excluded = {'.git','bin','obj','.pio','dist','__pycache__','.vs'}
for p in root.rglob('*'):
    if p.name.startswith('.env') or p.name == 'settings.json' or p.suffix.lower() in {'.pfx','.key','.user','.suo','.log'}:
        continue
    if p.is_file() and not excluded.intersection(p.relative_to(root).parts) and ('.vscode' not in p.parts or p.name == 'extensions.json'):
        files.append(p)
sourcezip = dist/f'PhatStats-{v["display"]}-source.zip'
with zipfile.ZipFile(sourcezip,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for p in sorted(files):
        z.write(p,p.relative_to(root).as_posix())
package = dist/f'PhatStats-{v["display"]}-win-x64.zip'
assets = json.loads((root/'host/src/PhatStats/obj/project.assets.json').read_text())
inventory = [{'package':name, 'sha512':info.get('sha512','')} for name,info in assets['libraries'].items() if info['type']=='package']
(dist/'DEPENDENCIES.json').write_text(json.dumps(inventory,indent=2)+'\n')
with zipfile.ZipFile(package,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as z:
    for p in sorted(publish.rglob('*')):
        if p.is_file(): z.write(p,p.relative_to(publish).as_posix())
    for name in ['LICENSE','THIRD_PARTY_NOTICES.md','SOURCE_PROVENANCE.md','CHANGELOG.md','VERSION.json']:
        z.write(root/name,name)
    z.write(root/'host/LICENSE','LICENSES/PhatStats-GPL-3.0.txt')
    for p in (root/'LICENSES').rglob('*'):
        if p.is_file(): z.write(p,'LICENSES/'+p.relative_to(root/'LICENSES').as_posix())
    for p in (root/'host/LICENSES').rglob('*'):
        if p.is_file(): z.write(p,'LICENSES/'+p.name)
    for p in (root/'docs').glob('*.md'): z.write(p,'docs/'+p.name)
    z.write(dist/'BUILD_INFO.txt','BUILD_INFO.txt')
    z.write(dist/'DEPENDENCIES.json','DEPENDENCIES.json')
    z.writestr('START-HERE.md', '# PhatStats '+v['display']+'\n\n[English](docs/install.en.md) · [简体中文](docs/install.zh-CN.md) · [日本語](docs/install.ja-JP.md) · [한국어](docs/install.ko-KR.md)\n\nSeparate driver: [official PawnIO](https://pawnio.eu/). No driver is installed automatically.\n\n[Matching source and release files](https://github.com/LUSTgabriel/PhatStats/releases/tag/'+v['tag']+').\n')
releasefiles = [package, sourcezip] + sorted(dist.glob('ThirdParty-*-source.zip'))
lines=[]
for p in releasefiles:
    with zipfile.ZipFile(p) as z: assert z.testzip() is None, f'Invalid ZIP: {p}'
    lines.append(hashlib.sha256(p.read_bytes()).hexdigest()+'  '+p.name)
(dist/'SHA256SUMS.txt').write_text('\n'.join(lines)+'\n')
print('\n'.join(lines))
