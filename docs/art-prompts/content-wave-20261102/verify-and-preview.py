"""Offline identity/alpha/preservation checks and contact sheets; no runtime art edits."""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
DOC = Path(__file__).parent
OUT = ROOT / 'artifacts/content-wave-20261102/k5'
MANIFEST = DOC / 'manifest.json'
sha = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest()
data = json.loads(MANIFEST.read_text(encoding='utf-8'))
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 22)
smallfont = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 16)

for row in data['assets']:
    source, target = Path(row['source']), ROOT / row['target']
    assert sha(source) == sha(target), row['key'] + ': source/runtime identity'
    with Image.open(target) as im:
        row['mode'] = im.mode
        row['dimensions'] = list(im.size)
        rgba = im.convert('RGBA')
    alpha = rgba.getchannel('A')
    row['sha256'] = row['sourceSha256'] = sha(target)
    row['alphaExtrema'] = list(alpha.getextrema())
    row['alphaBounds'] = alpha.getbbox()
    if row['type'] == 'card':
        assert abs(rgba.width / rgba.height - 4/3) < .02, row['key']
        assert alpha.getextrema() == (255, 255), row['key']
    else:
        assert alpha.getextrema() == (0, 255), row['key']
        assert all(alpha.getpixel(p) == 0 for p in [(0,0),(rgba.width-1,0),(0,rgba.height-1),(rgba.width-1,rgba.height-1)])
        padded = Image.new('L', (rgba.width+2, rgba.height+2))
        padded.paste(alpha.point(lambda x: 255 if x else 0), (1,1))
        ImageDraw.floodfill(padded, (0,0), 128, thresh=0)
        outside = np.array(padded.crop((1,1,rgba.width+1,rgba.height+1))) == 128
        holes = int(np.count_nonzero((np.array(alpha) == 0) & ~outside))
        row['closedTransparentPixels'] = holes
        if row['key'] == 'sootbound_ticket':
            assert holes > 4096, 'Ticket must have a genuine punched alpha hole'
            row['previousOriginalArchive'] = 'artifacts/content-wave-20261102/k5/before/sootbound_ticket.png'
            row['previousOriginalSha256'] = sha(ROOT / row['previousOriginalArchive'])
    row['review'] = 'K5 inspected the generated original; human visual approval and native crop acceptance pending.'

export = json.loads((DOC / 'relic-outline-export.json').read_text(encoding='utf-8'))
assert len(export['assets']) == 4
for row in export['assets']:
    for pathkey, hashkey in [('original','originalSha256'),('small','smallSha256'),('outline','outlineSha256')]:
        assert sha(ROOT / row[pathkey]) == row[hashkey], row['key']
    assert row['interiorRgbaExact'] and row['originalUnchanged']
    assert row['changedRgbPixels'] == row['changesOutsideBand'] == row['changesToDisconnectedDarkPixels'] == 0
    art = next(a for a in data['assets'] if a['key'] == row['key'])
    art['nativeSmall'] = row['small']
    art['nativeOutline'] = row['outline']
    art['nativeSmallSha256'] = row['smallSha256']
    art['nativeOutlineSha256'] = row['outlineSha256']

baseline = json.loads((OUT / 'before/asset-baseline.json').read_text(encoding='utf-8-sig'))
allowed = {'assets/LaRata/images/relics/sootbound_ticket.png',
           'assets/LaRata/images/relic_native/sootbound_ticket.png',
           'assets/LaRata/images/relic_native/sootbound_ticket_outline.png'}
unchanged = []
for row in baseline:
    if row['path'] not in allowed:
        assert sha(ROOT / row['path']) == row['sha256'], 'Existing artwork changed: ' + row['path']
        unchanged.append(row['path'])

def no_duplicates(pairs):
    result = {}
    for key, value in pairs:
        assert key not in result, 'Duplicate localization key: ' + key
        result[key] = value
    return result

locale_checks = []
for locale in ['eng','deu','zhs']:
    folder = ROOT / 'assets/LaRata/localization' / locale
    ui = json.loads((folder/'larata_ui.json').read_text(encoding='utf-8-sig'), object_pairs_hook=no_duplicates)
    powers = json.loads((folder/'powers.json').read_text(encoding='utf-8-sig'), object_pairs_hook=no_duplicates)
    assert '\n' in ui['contest.well_awarded'] and '\\n' not in ui['contest.well_awarded']
    assert '{0}' in ui['contest.well_awarded'] and '{1}' in ui['contest.well_awarded']
    desc = powers['LARATA-ROYAL_DAMAGE_RAMP_POWER.description']
    assert '{Current}' in desc and '{Next}' in desc and '100%' in desc
    assert '100%' in powers['LARATA-ROYAL_DAMAGE_RAMP_POWER.capped.description']
    locale_checks.append(locale)

cards = [a for a in data['assets'] if a['type'] == 'card']
sheet = Image.new('RGB', (1456, 850), '#1c202b')
draw = ImageDraw.Draw(sheet)
for i, row in enumerate(cards):
    x, y = 16 + (i%3)*480, 16 + (i//3)*418
    with Image.open(ROOT/row['target']) as im:
        im.thumbnail((464,348), Image.Resampling.LANCZOS)
        sheet.paste(im.convert('RGB'), (x,y))
    draw.text((x,y+356), row.get('displayName',row['key']), font=font, fill='white')
sheet.save(OUT/'cards.png')

relics = [a for a in data['assets'] if a['type'] == 'relic']
sheet = Image.new('RGB', (1280, 480), '#252b35')
draw = ImageDraw.Draw(sheet)
for i, row in enumerate(relics):
    x = i*320
    with Image.open(ROOT/row['target']) as im:
        im = im.convert('RGBA'); im.thumbnail((296,296),Image.Resampling.LANCZOS)
        sheet.paste(im,(x+(320-im.width)//2,8),im)
    draw.text((x+14,310),row['key'],font=font,fill='white')
    for j, size in enumerate([48,32]):
        with Image.open(ROOT/row['nativeSmall']) as im: small = im.convert('RGBA')
        with Image.open(ROOT/row['nativeOutline']) as im: mask = im.getchannel('A')
        shadow = Image.new('RGBA',small.size,(0,0,0,0))
        shadow.putalpha(mask.point(lambda a: round(a*128/255)))
        for k,bg in enumerate([(37,43,53,255),(225,220,201,255)]):
            sample = Image.new('RGBA',small.size,bg);sample.alpha_composite(shadow);sample.alpha_composite(small)
            sample.thumbnail((size,size),Image.Resampling.LANCZOS)
            sheet.paste(sample,(x+28+j*100,352+k*60))
        draw.text((x+28+j*100,456),str(size)+'px',font=smallfont,fill='white')
sheet.save(OUT/'relics.png')

data['previews'] = ['artifacts/content-wave-20261102/k5/cards.png','artifacts/content-wave-20261102/k5/relics.png']
data['limits'] = ['No human approval implied.','No engine import, package, game launch, installation or release performed by K5.','Contact sheets are offline previews, not native UI captures.']
MANIFEST.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
checks = {'cards':len(cards),'relics':len(relics),'nativeVariants':8,
          'sourceRuntimeIdentity':True,'allRelicInteriorsExact':True,'existingArtworkUnchanged':len(unchanged),
          'onlyExistingArtReplacement':sorted(allowed),'ticketOriginalArchived':True,
          'localesChecked':locale_checks,'humanApproved':False,'nativeUiAcceptance':'NOT TESTED'}
(DOC/'checks.json').write_text(json.dumps(checks,indent=2)+'\n',encoding='utf-8')
print(json.dumps(checks))
