"""Reproducible, engine-free artwork export and contact sheets.

Pack export only scales proportionally and places on a common canvas. It never
warps a pack to fake its requested aspect ratio. Original generated pack pixels
remain in booster-sources-20261025. Contact sheets are local review artifacts,
not game captures. Pillow is existing offline tooling, not a runtime dependency.
"""
from pathlib import Path
import hashlib
import json
import math
import sys
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parents[2]
out = root / 'artifacts/dev-wave-20261025/k5'
out.mkdir(parents=True, exist_ok=True)
cards_only = '--cards-only' in sys.argv
manifest_path = Path(__file__).with_name('manifest-dev-wave-20261025.json')
manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
font_path = Path('C:/Windows/Fonts/arial.ttf')
font = ImageFont.truetype(str(font_path), 19)
small = ImageFont.truetype(str(font_path), 15)
heading = ImageFont.truetype(str(font_path), 28)
background = (28, 33, 38)
ink = (236, 238, 230)

def bounds(image):
    return image.getchannel('A').point(lambda x:255 if x >= 16 else 0).getbbox()

def thumb(sheet, path, x, y, width, height):
    if not path.exists(): return
    with Image.open(path) as original:
        image = original.convert('RGBA')
    image.thumbnail((width, height), Image.Resampling.LANCZOS)
    sheet.paste(image, (x+(width-image.width)//2, y+(height-image.height)//2), image)

# Pure proportional placement, no aspect correction or artistic edits.
for asset in manifest['assets']:
    if asset['key'].endswith('_pack') and not cards_only and not asset.get('human_approved'):
        source = Path(__file__).with_name('booster-sources-20261025') / (asset['key']+'.png')
        with Image.open(source) as original:
            image = original.convert('RGBA')
        box = bounds(image)
        scale = 980 / (box[3]-box[1])
        resized = image.resize((round(image.width*scale), round(image.height*scale)), Image.Resampling.LANCZOS)
        dx = round(627 - (box[0]+box[2])*scale/2)
        dy = round(627 - (box[1]+box[3])*scale/2)
        canvas = Image.new('RGBA', (1254,1254))
        canvas.paste(resized, (dx,dy))
        canvas.save(root / asset['target'])
        asset['projectSource'] = str(source.relative_to(root)).replace('\\','/')
        asset['export'] = 'Proportional placement only; target visible height 980 on 1254-square canvas (rounding may add 1 pixel); no warp.'
        asset['requestedPackRatio'] = 292/414

for asset in manifest['assets']:
    path = root / asset['target']
    with Image.open(path) as image:
        asset['dimensions'] = list(image.size)
        asset['mode'] = image.mode
        if image.mode == 'RGBA':
            box = bounds(image)
            asset['alphaBoundsAt16'] = box
            if asset['key'].endswith('_pack'):
                asset['observedOuterSilhouetteRatio'] = round((box[2]-box[0])/(box[3]-box[1]), 5)
    asset['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
    asset['sourceSha256'] = hashlib.sha256(Path(asset['source']).read_bytes()).hexdigest()
    if asset['key'] == 'map_rat_king':
        asset['derivedOutputs'] = []
        for name in ['map_rat_king.png', 'map_rat_king_outline.png']:
            derived = root / 'assets/LaRata/images' / name
            with Image.open(derived) as image:
                asset['derivedOutputs'].append({
                    'target': str(derived.relative_to(root)).replace('\\', '/'),
                    'dimensions': list(image.size), 'mode': image.mode,
                    'alphaBoundsAt16': bounds(image),
                    'sha256': hashlib.sha256(derived.read_bytes()).hexdigest()})
manifest['limits'] = ['No game, engine, package export or live UI inspection.',
    'Pack silhouettes are upright and generated in card-like proportions. Exact contour identity is not implied by a common canvas; observed ratios are recorded.',
    'Human visual approval applies only to assets explicitly marked human_approved at their recorded hashes. See booster-approval-20261025.json and card-art-approval-20261025.json; approval does not imply live UI or technical acceptance.']
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')

if not cards_only:
    relics = [a for a in manifest['assets'] if a.get('kind') == 'relic']
    sheet = Image.new('RGB', (1360, 80+7*230), background)
    draw = ImageDraw.Draw(sheet)
    draw.text((24,16),'LA RATA — RELIC ART: BEFORE / AFTER',font=heading,fill=ink)
    draw.text((24,50),'Local asset review • native-style simplification • not an in-game capture',font=small,fill=ink)
    for index, asset in enumerate(relics):
        x = 20 + (index%2)*670
        y = 84 + (index//2)*230
        draw.rectangle((x,y,x+650,y+218),fill=(38,44,49))
        before = out / 'before' / Path(asset['target']).name
        thumb(sheet,before,x+2,y+8,180,164)
        thumb(sheet,root/asset['target'],x+220,y+8,180,164)
        thumb(sheet,root/asset['target'],x+460,y+53,48,48)
        thumb(sheet,root/asset['target'],x+540,y+61,32,32)
        draw.text((x+12,y+176),'Before' if before.exists() else 'New relic',font=small,fill=ink)
        draw.text((x+232,y+176),'After',font=small,fill=ink)
        draw.text((x+450,y+108),'48px     32px',font=small,fill=ink)
        draw.text((x+12,y+198),asset['key'],font=small,fill=ink)
    sheet.save(out/'relics-before-after.png')

cards=[a for a in manifest['assets'] if a['target'].startswith('assets/LaRata/images/cards/')]
sheet=Image.new('RGB',(1520,80+math.ceil(len(cards)/4)*310),background)
draw=ImageDraw.Draw(sheet)
draw.text((24,16),f'LA RATA — {len(cards)} CARD ARTWORKS',font=heading,fill=ink)
draw.text((24,50),'Existing illustration style; expanded palette • artwork preview, not a game capture',font=small,fill=ink)
titles=json.loads((root/'assets/LaRata/localization/eng/cards.json').read_text(encoding='utf-8'))
for index,asset in enumerate(cards):
    x=20+(index%4)*375;y=84+(index//4)*310
    thumb(sheet,root/asset['target'],x,y,355,266)
    draw.text((x,y+274),titles.get('LARATA-'+asset['key'].upper()+'.title',asset['key'].replace('_',' ').title()),font=font,fill=ink)
sheet.save(out/'new-cards.png')

# Sewer Pact is revised existing artwork, not an additional gameplay card.
new_cards=[a for a in cards if a.get('newGameplayCard', True)]
new_sheet=Image.new('RGB',(1520,80+math.ceil(len(new_cards)/4)*310),background)
draw=ImageDraw.Draw(new_sheet)
draw.text((24,16),f'LA RATA — {len(new_cards)} NEW CARDS',font=heading,fill=ink)
draw.text((24,50),'Selected card illustrations • artwork preview, not a game capture',font=small,fill=ink)
for index,asset in enumerate(new_cards):
    x=20+(index%4)*375;y=84+(index//4)*310
    thumb(new_sheet,root/asset['target'],x,y,355,266)
    draw.text((x,y+274),titles.get('LARATA-'+asset['key'].upper()+'.title',asset['key'].replace('_',' ').title()),font=font,fill=ink)
new_sheet.save(out/'new-gameplay-cards.png')

if cards_only:
    print(out)
    raise SystemExit(0)

packs=[a for a in relics if a['key'].endswith('_pack')]
sheet=Image.new('RGB',(1440,440),background)
draw=ImageDraw.Draw(sheet)
draw.text((24,15),'BOOSTER ALIGNMENT — generated shape, proportional export',font=heading,fill=ink)
draw.text((24,52),'Target 292:414; contour ratios below are measurements, not a declaration of exact identity',font=small,fill=ink)
for index,asset in enumerate(packs):
    x=index*240
    thumb(sheet,root/asset['target'],x+10,80,220,260)
    draw.text((x+20,350),asset['key'],font=font,fill=ink)
    draw.text((x+20,382),'outer w/h '+str(asset['observedOuterSilhouetteRatio']),font=small,fill=ink)
sheet.save(out/'boosters.png')

sheet=Image.new('RGB',(940,470),(128,155,151))
draw=ImageDraw.Draw(sheet)
draw.text((25,14),'MAP HEAD PORTRAIT — native tint-mask asset preview',font=heading,fill=(25,27,39))
for index,path in enumerate([out/'before/map_rat_king.png',root/'assets/LaRata/images/map_rat_king.png']):
    with Image.open(path) as source:
        a=source.convert('RGBA').getchannel('A')
        image=Image.new('RGBA',source.size,(27,28,43,255));image.putalpha(a)
    image.thumbnail((374,350),Image.Resampling.LANCZOS)
    sheet.paste(image,(45+index*460+(374-image.width)//2,70),image)
    draw.text((165+index*460,430),'Before' if index==0 else 'After',font=font,fill=(25,27,39))
sheet.save(out/'map-portrait.png')
print(out)
