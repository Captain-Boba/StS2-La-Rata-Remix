"""Synchronize selected originals and create offline evidence; never modifies runtime images."""
from pathlib import Path
import hashlib,json,runpy
from PIL import Image,ImageDraw,ImageFont
DOC=Path(__file__).resolve().parent
WAVE=DOC.parent
ROOT=DOC.parents[3]
OUT=ROOT/'artifacts/content-expansion-20261102/k5/redesign'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
read=lambda p:json.loads(Path(p).read_text(encoding='utf-8-sig'))
write=lambda p,d:Path(p).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
revision=read(DOC/'manifest.json')
updates={row['key']:row for row in revision['assets']}
assert len(updates)==4
before=read(OUT/'before/images.json')
targets={r['target'] for r in updates.values()}
changed=[]
for old in before:
    current=sha(ROOT/old['path'])
    if old['path'] in targets:
        assert current!=old['sha256']
        row=next(r for r in updates.values() if r['target']==old['path'])
        assert sha(ROOT/row['priorRuntimeArchive'])==old['sha256']
        assert current==sha(row['source'])
        row['previousRuntimeSha256']=old['sha256']
        changed.append(old['path'])
    else:
        assert current==old['sha256'],'Other image/import changed: '+old['path']
assert set(changed)==targets
active=read(WAVE/'manifest.json')
active['assets']=[updates.get(row['key'],row) for row in active['assets']]
active['latestRevision']='docs/art-prompts/content-expansion-20261102/redesign/manifest.json'
write(WAVE/'manifest.json',active)
prompts=read(WAVE/'prompts.json')
prompts['assets']=[{**row,'prompt':updates[row['key']]['prompt']} if row['key'] in updates else row for row in prompts['assets']]
prompts['latestRevision']='docs/art-prompts/content-expansion-20261102/redesign/prompts.json'
write(WAVE/'prompts.json',prompts)
runpy.run_path(str(WAVE/'verify-and-preview.py'),run_name='__main__')
active=read(WAVE/'manifest.json')
revision['assets']=[row for row in active['assets'] if row['key'] in updates]
for row in revision['assets']:
    row['previousRuntimeSha256']=updates[row['key']]['previousRuntimeSha256']
write(DOC/'manifest.json',revision)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
sheet=Image.new('RGB',(1016,868),'#202632');draw=ImageDraw.Draw(sheet)
for i,row in enumerate(revision['assets']):
    x=16+(i%2)*500;y=16+(i//2)*426
    im=Image.open(ROOT/row['target']).convert('RGBA')
    if row['kind']=='card':
        im.thumbnail((484,363),Image.Resampling.LANCZOS)
        sheet.paste(im,(x,y),im)
    else:
        large=im.copy();large.thumbnail((300,300),Image.Resampling.LANCZOS)
        sheet.paste(large,(x+20,y+12),large)
        for j,size in enumerate([48,32]):
            icon=im.copy();icon.thumbnail((size,size),Image.Resampling.LANCZOS)
            for k,bg in enumerate(['#202632','#e0dacb']):
                tile=Image.new('RGBA',(size,size),bg);tile.alpha_composite(icon)
                sheet.paste(tile,(x+350+j*70,y+130+k*70))
            draw.text((x+350+j*70,y+262),str(size)+'px',font=small,fill='white')
    draw.text((x,y+375),row['title'],font=font,fill='white')
sheet.save(OUT/'preview.png')
checks={'changedRuntimePngs':changed,'changedRuntimePngCount':len(changed),'otherImagesAndImportsUnchanged':len(before)-len(changed),'sourceRuntimeIdentity':True,'archivedPreviousRuntimeIdentity':True,'allEightActiveSelectionChecks':'PASS','humanApproved':False,'nativeImportAndRendering':'NOT TESTED','preview':'artifacts/content-expansion-20261102/k5/redesign/preview.png'}
write(DOC/'checks.json',checks)
handoff=read(WAVE/'handoff.json')
handoff['status']='Revised art freeze: 4 new originals supersede initial selection; all 8 active originals verified. K6 owns renewed import/export.'
handoff['latestRevision']={'manifest':'docs/art-prompts/content-expansion-20261102/redesign/manifest.json','checks':'docs/art-prompts/content-expansion-20261102/redesign/checks.json','preview':checks['preview'],'sourceRuntimeHashes':{row['key']:row['runtimeSha256'] for row in revision['assets']}}
handoff['alphaNotes']='See current manifest for exact alpha bounds/edge flags. Brass Polish retains its initial generated sub-8 edge specks; Amber Hourglass has a new source and must be re-imported.'
write(WAVE/'handoff.json',handoff)
write(DOC/'handoff.json',{'date':'2026-11-02','owner':'K5','status':'Art-frozen; awaiting K6 import/export and human review','manifest':'docs/art-prompts/content-expansion-20261102/redesign/manifest.json','checks':checks,'sourceRuntimeHashes':handoff['latestRevision']['sourceRuntimeHashes'],'ownership':'Only 4 runtime PNGs and provenance/previews changed. No code/localization/style/import/build/install/game/Git action.'})
print(json.dumps(checks))
