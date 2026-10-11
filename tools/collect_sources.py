"""Collect pinned corresponding source, never install/run third-party software."""
from pathlib import Path
import io, json, urllib.request, zipfile, hashlib

root = Path(__file__).resolve().parents[1]
dist = root/'host/dist'
dist.mkdir(parents=True,exist_ok=True)
sources=json.loads((root/'docs/third-party-sources.json').read_text())
target=dist/'ThirdParty-corresponding-source.zip'
with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED,compresslevel=9) as out:
    for item in sources:
        url='https://codeload.github.com/'+item['repo']+'/zip/'+item['ref']
        print('Source download:',item['name'],flush=True)
        req=urllib.request.Request(url,headers={'User-Agent':'PhatStats-source-packager'})
        with urllib.request.urlopen(req,timeout=120) as response: data=response.read()
        item['archive_sha256']=hashlib.sha256(data).hexdigest()
        with zipfile.ZipFile(io.BytesIO(data)) as archive:
            for entry in archive.infolist():
                parts=Path(entry.filename).parts
                if entry.is_dir() or len(parts)<2: continue
                # The unrelated upstream GUI's proprietary driver installer is
                # not part of this host's corresponding library source.
                if parts[-1].lower()=='pawnio_setup.exe': continue
                out.writestr(item['name']+'/'+Path(*parts[1:]).as_posix(),archive.read(entry))
    out.writestr('SOURCE-MANIFEST.json',json.dumps(sources,indent=2)+'\n')
    out.writestr('README.md','# Corresponding dependency source\n\nExact revisions are in SOURCE-MANIFEST.json. Each component retains its upstream license. PawnIO driver installer is excluded; obtain it from https://pawnio.eu/. Host rebuild/relink instructions are in the PhatStats repository docs/maintenance.md.\n')
with zipfile.ZipFile(target) as archive: assert archive.testzip() is None
print('Source archive:',target)
