"""Check translation contracts and repository publication boundaries."""
from pathlib import Path
import re, subprocess, sys, xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
subprocess.run([sys.executable, str(root/'tools/version.py'), '--check'], check=True)
resources = root/'host/src/PhatStats/Resources'
def entries(path):
    data = ET.parse(path).getroot().findall('data')
    assert len({e.attrib['name'] for e in data}) == len(data), f'Duplicate resource in {path}'
    return {e.attrib['name']: e.findtext('value') or '' for e in data}
base = entries(resources/'Strings.resx')
placeholder = re.compile(r'(?<!\{)\{(\d+)(?:[^{}]*)\}(?!\})')
for name in ['Strings.zh-CN.resx','Strings.ja-JP.resx','Strings.ko-KR.resx']:
    translated = entries(resources/name)
    assert translated.keys() == base.keys(), f'Missing/extra keys: {name}'
    for key, value in translated.items():
        assert value.strip(), f'Empty {name}:{key}'
        assert sorted(placeholder.findall(value)) == sorted(placeholder.findall(base[key])), f'Format mismatch {name}:{key}'
print(f'i18n key/placeholder parity: PASS ({len(base)} keys, 4 languages)')
for cs in (root/'host/src/PhatStats').rglob('*.cs'):
    if 'obj' in cs.parts or 'bin' in cs.parts:
        continue
    for key in re.findall(r'LocalizationService\.[TF]\("([^"]+)"\s*[,)]', cs.read_text(encoding='utf-8-sig')):
        assert key in base, f'Unknown resource key {key} in {cs}'
for p in [root/'README.md', root/'README.zh-CN.md', *list((root/'docs').glob('*.md'))]:
    for target in re.findall(r'\]\(([^)]+)\)', p.read_text(encoding='utf-8')):
        if re.match(r'[a-z]+:', target) or target.startswith('#'):
            continue
        assert (p.parent/target.split('#')[0]).exists(), f'Broken local link {p.name}: {target}'
print('Markdown links and referenced resource keys: PASS')
