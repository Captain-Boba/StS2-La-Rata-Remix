"""Export unchanged draft originals and requested before/after/native-size sheets.

This production script never writes active runtime assets or selection manifests.
Pillow is existing offline artwork tooling; no runtime dependency is added.
"""
from pathlib import Path
import hashlib
import json
import shutil
from PIL import Image, ImageDraw, ImageFont

folder = Path(__file__).resolve().parent
root = folder.parents[2]
manifest_path = folder / 'manifest-drafts.json'
manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
out = root / 'artifacts/relic-redesign-20261025/k5'
out.mkdir(parents=True, exist_ok=True)
baseline = json.loads((out / 'runtime-baseline.json').read_text(encoding='utf-8'))
old = {row['key']: row for row in baseline}

for row in manifest['assets']:
    target = root / row['draft']
    target.parent.mkdir(parents=True, exist_ok=True)
    if row.get('human_approved'):
        # Approved original/source/hash remain frozen, including the draft file.
        if hashlib.sha256(target.read_bytes()).hexdigest() != row['approvedSha256'] or hashlib.sha256(Path(row['source']).read_bytes()).hexdigest() != row['approvedSha256']:
            raise RuntimeError('Approved artwork changed: '+row['key'])
    else:
        shutil.copy2(row['source'], target)
    with Image.open(target) as im:
        row['dimensions'] = list(im.size)
        row['mode'] = im.mode
        if im.mode == 'RGBA':
            alpha = im.getchannel('A')
            row['alphaExtrema'] = list(alpha.getextrema())
            row['alphaBoundsAt16'] = list(alpha.point(lambda p: 255 if p >= 16 else 0).getbbox())
    row['sha256'] = hashlib.sha256(target.read_bytes()).hexdigest()
    row['sourceSha256'] = hashlib.sha256(Path(row['source']).read_bytes()).hexdigest()
    row['before'] = old[row['key']]
    runtime_sha=hashlib.sha256((root / row['runtime']).read_bytes()).hexdigest()
    row.pop('runtimeUnchangedAtExport',None)
    row['runtimeMatchesOriginalBaseline'] = runtime_sha == old[row['key']]['sha256']
    row['runtimeMatchesSelectedArt'] = runtime_sha == row['sha256']

font_file = 'C:/Windows/Fonts/arial.ttf'
title = ImageFont.truetype(font_file, 26)
font = ImageFont.truetype(font_file, 21)
small = ImageFont.truetype(font_file, 15)
bg, panel, ink = (26, 31, 38), (37, 43, 51), (239, 237, 224)
sheet = Image.new('RGB', (1560, 1000), bg)
draw = ImageDraw.Draw(sheet)
approved_count=sum(bool(row.get('human_approved')) for row in manifest['assets'])
draw.text((24, 14), f'LA RATA - {approved_count} APPROVED DESIGNS / {6-approved_count} PROPOSALS', font=title, fill=ink)
draw.text((24, 48), 'Before / proposal - full-canvas 48px and 32px samples - artwork review, not a game capture', font=small, fill=ink)

def place(path, x, y, width, height, canvas=sheet):
    with Image.open(path) as source:
        image = source.convert('RGBA')
    image.thumbnail((width, height), Image.Resampling.LANCZOS)
    canvas.paste(image, (x+(width-image.width)//2, y+(height-image.height)//2), image)

for i, row in enumerate(manifest['assets']):
    x, y = 20 + (i % 3)*515, 90 + (i // 3)*445
    draw.rounded_rectangle((x, y, x+500, y+426), radius=8, fill=panel)
    draw.text((x+16, y+14), row['title'], font=font, fill=ink)
    draw.text((x+16, y+45), 'Before', font=small, fill=ink)
    draw.text((x+222, y+45), 'Approved design' if row.get('human_approved') else 'New proposal', font=small, fill=ink)
    before, after = root / row['before']['before'], root / row['draft']
    place(before, x+8, y+70, 178, 222)
    place(after, x+206, y+65, 276, 240)
    draw.text((x+16, y+307), 'Before: 48 / 32', font=small, fill=ink)
    draw.text((x+220, y+307), 'New: 48 / 32', font=small, fill=ink)
    for path, px in [(before,x+18), (after,x+222)]:
        place(path, px, y+335, 48, 48)
        place(path, px+64, y+343, 32, 32)
        draw.rectangle((px+113,y+333,px+170,y+389),fill=(215,215,203))
        place(path, px+118,y+337,48,48)
    draw.text((x+16,y+400),'Full image scaled proportionally; no optical-size normalization',font=small,fill=(184,191,195))
sheet_path = out / 'six-relics-before-after.png'
sheet.save(sheet_path)

native = Image.new('RGB', (1040, 250), bg)
draw = ImageDraw.Draw(native)
draw.text((20,14),'RELIC DESIGNS - EXACT 48px / 32px PREVIEWS',font=title,fill=ink)
for i,row in enumerate(manifest['assets']):
    x=12+i*171
    place(root/row['draft'],x+10,65,48,48,native)
    place(root/row['draft'],x+82,73,32,32,native)
    draw.rectangle((x+8,128,x+120,189),fill=(215,215,203))
    place(root/row['draft'],x+12,134,48,48,native)
    place(root/row['draft'],x+82,142,32,32,native)
    draw.text((x+10,205),str(i+1)+'. '+row['shortLabel'],font=small,fill=ink)
native_path = out / 'six-relics-native-sizes.png'
native.save(native_path)
preview_paths=[sheet_path,native_path]
revised=[row for row in manifest['assets'] if row.get('priorDrafts')]
if revised:
    revision=Image.new('RGB',(1400,90+len(revised)*350),bg)
    rd=ImageDraw.Draw(revision)
    all_approved=all(row.get('human_approved') for row in revised)
    rd.text((24,14),'RELIC REMOVER / SOOTBOUND TICKET - '+('APPROVED DESIGNS' if all_approved else 'NEW APPROACHES'),font=title,fill=ink)
    rd.text((24,50),'All six visually approved; technical/package acceptance remains separate' if all_approved else 'Two unapproved proposals; the other four selected designs remain frozen',font=small,fill=ink)
    for i,row in enumerate(revised):
        y=90+i*350
        rd.text((24,y+12),row['title'],font=font,fill=ink)
        for label,x in [('Original runtime',24),('Rejected design',280),('Approved design' if row.get('human_approved') else 'New proposal',580),('48 / 32 px',1030)]:
            rd.text((x,y+48),label,font=small,fill=ink)
        place(root/row['before']['before'],24,y+78,200,230,revision)
        place(root/row['priorDrafts'][-1]['archivedDraft'],280,y+78,235,230,revision)
        place(root/row['draft'],580,y+70,350,250,revision)
        place(root/row['draft'],1030,y+110,48,48,revision)
        place(root/row['draft'],1100,y+118,32,32,revision)
        rd.rectangle((1022,y+182,1144,y+248),fill=(215,215,203))
        place(root/row['draft'],1030,y+190,48,48,revision)
        place(root/row['draft'],1100,y+198,32,32,revision)
    revision_path=out/'two-relics-revision.png'
    revision.save(revision_path)
    preview_paths.append(revision_path)
manifest['previews'] = [{'path':str(p.relative_to(root)).replace('\\','/'),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in preview_paths]
manifest_path.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'designs':len(manifest['assets']),'runtimeMatchesSelectedArt':all(r['runtimeMatchesSelectedArt'] for r in manifest['assets']),'previews':manifest['previews']}))
