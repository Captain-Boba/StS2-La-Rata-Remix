"""Bounded new-art audit and previews. Never rewrites source/runtime PNGs."""
from pathlib import Path
import hashlib,json,re
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[3]
DOC=Path(__file__).parent
OUT=ROOT/'artifacts/content-expansion-20261102/k5'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
data=json.loads((DOC/'manifest.json').read_text(encoding='utf-8'))
assert len(data['assets'])==8
code={str(p.relative_to(ROOT)).replace('\\','/'):p.read_text(encoding='utf-8-sig') for p in (ROOT/'src/LaRata').glob('*.cs')}
for row in data['assets']:
    target=ROOT/row['target'];source=Path(row['source'])
    assert sha(target)==sha(source),row['key']
    with Image.open(target) as image:
        row['dimensions']=list(image.size);row['mode']=image.mode
        alpha=image.convert('RGBA').getchannel('A')
        row['alphaExtrema']=list(alpha.getextrema());row['alphaBounds']=alpha.getbbox()
        if row['kind']=='card':
            assert abs(image.width/image.height-4/3)<.02 and alpha.getextrema()==(255,255)
        else:
            assert image.width==image.height and alpha.getextrema()==(0,255)
            assert all(alpha.getpixel(p)==0 for p in [(0,0),(image.width-1,0),(0,image.height-1),(image.width-1,image.height-1)])
            bounds=alpha.point(lambda value:255 if value>=8 else 0).getbbox()
            row['visibleAlphaBoundsThreshold8']=bounds
            row['sub8AlphaAtCanvasEdge']=any(alpha.getpixel((x,y))>0 for x,y in [(x,0) for x in range(image.width)]+[(x,image.height-1) for x in range(image.width)]+[(0,y) for y in range(image.height)]+[(image.width-1,y) for y in range(image.height)])
            assert bounds and bounds[0]>0 and bounds[1]>0 and bounds[2]<image.width and bounds[3]<image.height
    row['sourceSha256']=row['runtimeSha256']=sha(target)
    row['bindingSourceFiles']=[name for name,text in code.items() if '"'+row['key']+'"' in text or 'images/'+('cards/' if row['kind']=='card' else 'potions/')+row['key']+'.png' in text]
    row['bindingSourceFound']=bool(row['bindingSourceFiles'])
    row['review']='K5 inspected original and small-size/contact-sheet preview; no human or native UI acceptance claimed.'
    row['humanApproved']=False

before=json.loads((OUT/'before/images.json').read_text(encoding='utf-8-sig'))
for row in before:assert sha(ROOT/row['path'])==row['sha256'],'Existing image/import changed: '+row['path']
protected=json.loads((OUT/'before/style-bindings.json').read_text(encoding='utf-8-sig'))
for row in protected:assert sha(ROOT/row['path'])==row['sha256'],'Accepted style binding changed: '+row['path']

font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
smallfont=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',17)
cards=[a for a in data['assets'] if a['kind']=='card']
potions=[a for a in data['assets'] if a['kind']=='potion']
sheet=Image.new('RGB',(1456,850),'#1c202b');draw=ImageDraw.Draw(sheet)
for i,row in enumerate(cards):
    x=16+(i%3)*480;y=16+(i//3)*418
    with Image.open(ROOT/row['target']) as image:
        image.thumbnail((464,348),Image.Resampling.LANCZOS);sheet.paste(image.convert('RGB'),(x,y))
    draw.text((x,y+356),row['title'],font=font,fill='white')
sheet.save(OUT/'cards.png')
sheet=Image.new('RGB',(1080,500),'#252b35');draw=ImageDraw.Draw(sheet)
for i,row in enumerate(potions):
    x=i*360
    with Image.open(ROOT/row['target']) as image:
        image=image.convert('RGBA');large=image.copy();large.thumbnail((305,305),Image.Resampling.LANCZOS)
        sheet.paste(large,(x+(360-large.width)//2,8),large)
        for j,size in enumerate([48,32]):
            sample=image.copy();sample.thumbnail((size,size),Image.Resampling.LANCZOS)
            for k,bg in enumerate([(37,43,53,255),(224,218,202,255)]):
                tile=Image.new('RGBA',(size,size),bg);tile.alpha_composite(sample,((size-sample.width)//2,(size-sample.height)//2))
                sheet.paste(tile,(x+70+j*125,365+k*65))
            draw.text((x+70+j*125,480),str(size)+'px',font=smallfont,fill='white')
    draw.text((x+20,325),row['title'],font=font,fill='white')
sheet.save(OUT/'potions.png')

data['previews']=['artifacts/content-expansion-20261102/k5/cards.png','artifacts/content-expansion-20261102/k5/potions.png']
(DOC/'manifest.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
checks={'newCardArtworks':len(cards),'newPotionArtworks':len(potions),'sourceRuntimeIdentity':True,
        'existingImagesAndImportsUnchanged':len(before),'acceptedCardStyleFilesUnchanged':len(protected),
        'rustwaterUnchanged':True,'allPotionAlphaAndVisibleMarginsValid':True,
        'potionMarginScope':'Exact alpha extrema/corners plus silhouette bounds at alpha >=8; sub-8 generated edge specks are recorded without modifying originals.',
        'bindingsFound':sum(row['bindingSourceFound'] for row in data['assets']),
        'bindingScope':'Source art-key/path occurrence only; K2/K3 own native model bindings, K6 owns actual resource loading.',
        'humanApproved':False,'engineImportAndNativeUi':'NOT TESTED'}
(DOC/'checks.json').write_text(json.dumps(checks,indent=2)+'\n',encoding='utf-8')
print(json.dumps(checks))
