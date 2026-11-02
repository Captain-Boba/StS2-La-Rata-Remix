"""Scoped revision identities, archives, alpha, style source contract and previews."""
from pathlib import Path
import hashlib, json, re
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[4]
DOC=Path(__file__).parent
OUT=ROOT/'artifacts/content-wave-20261102/revision/k5'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
data=json.loads((DOC/'manifest.json').read_text(encoding='utf-8'))
baseline=json.loads((OUT/'before/asset-baseline.json').read_text(encoding='utf-8-sig'))
before={row['path']:row['sha256'] for row in baseline}
for row in data['assets']:
    target=ROOT/row['target']
    assert sha(target)==sha(row['source']),row['key']
    assert sha(ROOT/row['before'])==before[row['target']],row['key']+': archive mismatch'
    with Image.open(target) as image:
        row['mode']=image.mode;row['dimensions']=list(image.size)
        alpha=image.convert('RGBA').getchannel('A')
        row['alphaExtrema']=list(alpha.getextrema());row['alphaBounds']=alpha.getbbox()
        assert alpha.getextrema()==((0,255) if row['type']=='relic' else (255,255))
        if row['type']=='card':assert abs(image.width/image.height-4/3)<.02
    row['sourceSha256']=row['runtimeSha256']=sha(target)
    row['beforeSha256']=sha(ROOT/row['before'])
    row['review']='K5 visual author review only; human/native UI approval pending.'

export=json.loads((DOC/'relic-outline-export.json').read_text(encoding='utf-8'))
assert len(export['assets'])==1
relic=export['assets'][0]
for pathkey,hashkey in [('original','originalSha256'),('small','smallSha256'),('outline','outlineSha256')]:
    assert sha(ROOT/relic[pathkey])==relic[hashkey]
assert relic['originalUnchanged'] and relic['interiorRgbaExact']
assert relic['changedRgbPixels']==relic['changesOutsideBand']==relic['changesToDisconnectedDarkPixels']==0
allowed={row['target'] for row in data['assets']}|{relic['small'],relic['outline']}
changed={path for path,oldhash in before.items() if sha(ROOT/path)!=oldhash}
assert changed==allowed,'Unrequested artwork delta: '+repr(changed^allowed)
protected=json.loads((OUT/'before/protected-style-catalog.json').read_text(encoding='utf-8-sig'))
for row in protected:assert sha(ROOT/row['path'])==row['sha256'],row['path']

remix=(ROOT/'src/LaRata/RemixCards.cs').read_text(encoding='utf-8')
models=[(name,int(index)) for name,index in re.findall(r'class\s+(\w+)\(\)\s*:\s*RemixCard\((\d+)\)',remix)]
selected=[name for name,index in models if index>=10]
assert len(models)==20 and len(selected)==10
assert sorted(index for _,index in models if index>=10)==list(range(10,20))
assert 'RoyalCardStyle.Frame(IsBossPrize)' in remix
style=(ROOT/'src/LaRata/RoyalCardPool.cs').read_text(encoding='utf-8')
assert 'Frame(bool isRoyalPrize = false)' in style
assert 'rarity is CardRarity.Uncommon or CardRarity.Rare ? null' in style
shader=(ROOT/'assets/LaRata/shaders/royal_prize_frame.gdshader').read_text(encoding='utf-8')
assert 'vec4(enamel, source.a) * vertex_tint' in shader and 'TIME' not in shader
assert json.loads((ROOT/'assets/LaRata/localization/eng/cards.json').read_text(encoding='utf-8-sig'))['LARATA-CLEANSE.title']=='Royal Cleansing'
assert 'Rarity => RelicRarity.Uncommon' in (ROOT/'src/LaRata/RelicRemover.cs').read_text(encoding='utf-8')

font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',23)
smallfont=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
cards=[a for a in data['assets'] if a['type']=='card']
sheet=Image.new('RGB',(1120,940),'#20242c');draw=ImageDraw.Draw(sheet)
for i,row in enumerate(cards):
    x=16+(i%2)*552;y=16+(i//2)*462
    with Image.open(ROOT/row['target']) as image:
        image.thumbnail((536,402),Image.Resampling.LANCZOS);sheet.paste(image.convert('RGB'),(x,y))
    draw.text((x,y+411),row['title'],font=font,fill='white')
sheet.save(OUT/'cards.png')
sheet=Image.new('RGB',(1050,430),'#252b35');draw=ImageDraw.Draw(sheet)
for x,path,title in [(20,ROOT/'artifacts/content-wave-20261102/revision/k5/before/relic_remover.png','Historical selected original'),(365,ROOT/relic['original'],'New motif - not human approved')]:
    with Image.open(path) as image:
        image=image.convert('RGBA');image.thumbnail((305,305),Image.Resampling.LANCZOS);sheet.paste(image,(x,12),image)
    draw.text((x,330),title,font=smallfont,fill='white')
with Image.open(ROOT/relic['small']) as image:small=image.convert('RGBA')
with Image.open(ROOT/relic['outline']) as image:alpha=image.getchannel('A')
shadow=Image.new('RGBA',small.size,(0,0,0,0));shadow.putalpha(alpha.point(lambda a:round(a*128/255)))
for i,size in enumerate([48,32]):
    for j,bg in enumerate([(37,43,53,255),(225,220,201,255)]):
        sample=Image.new('RGBA',small.size,bg);sample.alpha_composite(shadow);sample.alpha_composite(small)
        sample.thumbnail((size,size),Image.Resampling.LANCZOS);sheet.paste(sample,(770+i*120,80+j*90))
    draw.text((770+i*120,270),str(size)+'px',font=smallfont,fill='white')
sheet.save(OUT/'relic-remover.png')
data['previews']=['artifacts/content-wave-20261102/revision/k5/cards.png','artifacts/content-wave-20261102/revision/k5/relic-remover.png']
data['relicVariants']=export['assets']
(DOC/'manifest.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
checks={'artworks':5,'sourceRuntimeIdentity':True,'archivesMatchPriorRuntime':True,
        'modifiedExistingImageFiles':sorted(changed),'unrelatedExistingImagesUnchanged':len(before)-len(changed),
        'historicalCatalogAndNormalFrameProtected':len(protected),'relicInteriorExact':True,
        'prizeStyleModels':selected,'prizeStyleCount':len(selected),'nativeRarityGetterRetained':True,
        'scope':'Source/offline checks only; shader GPU compile, native visual appearance and human artwork approval pending.'}
(DOC/'checks.json').write_text(json.dumps(checks,indent=2)+'\n',encoding='utf-8')
print(json.dumps(checks))
