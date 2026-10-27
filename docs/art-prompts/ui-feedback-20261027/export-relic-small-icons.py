"""Authorized mechanical outer-border export; no generative repainting.

Original/Big PNGs are never written. Only dark pixels in a bounded band next to
external transparency may lose alpha. Closed interior holes and all interior
RGBA values are protected. Existing Pillow/NumPy are offline production tools.
"""
from pathlib import Path
import argparse
import hashlib
import json
import math
import shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

root=Path(__file__).resolve().parents[3]
parser=argparse.ArgumentParser()
parser.add_argument('--pilot',action='store_true')
args=parser.parse_args()
out=root/'artifacts/ui-feedback-20261027/k5'
out.mkdir(parents=True,exist_ok=True)
sources=sorted((root/'assets/LaRata/images/relics').glob('*.png'))+[root/'assets/LaRata/images/a_thousand_cuts.png',root/'assets/LaRata/images/well.png']
pilots={'relic_remover','royal_pack','a_thousand_cuts','sootbound_ticket'}
if args.pilot: sources=[p for p in sources if p.stem in pilots]
target=out/'relic-small-pilot' if args.pilot else root/'assets/LaRata/images/relic_native'
target.mkdir(parents=True,exist_ok=True)
rows=[]
def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
for path in sources:
    source_sha=sha(path)
    backup=out/'before/relic-originals'/(path.stem+'-'+source_sha[:12]+'.png')
    backup.parent.mkdir(parents=True,exist_ok=True)
    if not backup.exists(): shutil.copy2(path,backup)
    with Image.open(path) as im: original=im.convert('RGBA')
    a=np.array(original)
    alpha=a[:,:,3]
    # Flood only transparency connected to the OUTSIDE, not closed inner holes.
    opaque=Image.fromarray(np.where(alpha>=16,255,0).astype('uint8'))
    padded=Image.new('L',(original.width+2,original.height+2))
    padded.paste(opaque,(1,1))
    ImageDraw.floodfill(padded,(0,0),128,thresh=0)
    exterior=padded.crop((1,1,original.width+1,original.height+1)).point(lambda x:255 if x==128 else 0)
    radius=max(1,math.ceil(min(original.size)*.02))
    near_exterior=np.array(exterior.filter(ImageFilter.MaxFilter(2*radius+1)))>0
    band=near_exterior & (alpha>0)
    rgb=a[:,:,:3].astype('int16')
    dark=(rgb.max(axis=2)<=64)&((rgb.max(axis=2)-rgb.min(axis=2))<=20)
    candidate=band & dark
    # A colored surface is a barrier: do not jump across a thin gold ring to
    # erase its inner dark edge, even when it is within the radius budget.
    reachable=(np.array(exterior)>0)|candidate
    connected=Image.new('L',(original.width+2,original.height+2))
    connected.paste(Image.fromarray(np.where(reachable,0,255).astype('uint8')),(1,1))
    ImageDraw.floodfill(connected,(0,0),128,thresh=0)
    connected=np.array(connected.crop((1,1,original.width+1,original.height+1)))==128
    edited=candidate & connected
    b=a.copy()
    b[edited,3]=0
    changed=np.any(a!=b,axis=2)
    interior=~near_exterior
    if np.any(changed & ~band) or not np.array_equal(a[interior],b[interior]) or not np.array_equal(a[:,:,:3],b[:,:,:3]):
        raise RuntimeError('Outer-band / interior preservation contract failed: '+path.stem)
    small=Image.fromarray(b)
    outline=Image.new('RGBA',original.size,(255,255,255,0))
    outline.putalpha(original.getchannel('A'))
    small_path=target/(path.stem+'.png')
    outline_path=target/(path.stem+'_outline.png')
    small.save(small_path)
    outline.save(outline_path)
    mask=Image.fromarray(np.where(edited,255,0).astype('uint8'))
    mask_path=out/'relic-border-masks'/(path.stem+'.png')
    mask_path.parent.mkdir(parents=True,exist_ok=True)
    mask.save(mask_path)
    rows.append({'key':path.stem,'original':str(path.relative_to(root)).replace('\\','/'),'originalSha256':source_sha,'originalArchive':str(backup.relative_to(root)).replace('\\','/'),'small':str(small_path.relative_to(root)).replace('\\','/'),'smallSha256':sha(small_path),'outline':str(outline_path.relative_to(root)).replace('\\','/'),'outlineSha256':sha(outline_path),'dimensions':list(original.size),'mode':'RGBA','maximumOuterBandPixels':radius,'changedAlphaPixels':int(changed.sum()),'changesOutsideBand':int(np.count_nonzero(changed & ~band)),'changedRgbPixels':int(np.count_nonzero(np.any(a[:,:,:3]!=b[:,:,:3],axis=2))),'protectedInteriorPixels':int(interior.sum()),'interiorRgbaExact':bool(np.array_equal(a[interior],b[interior])),'protectedDisconnectedDarkPixels':int(np.count_nonzero(candidate & ~connected)),'changesToDisconnectedDarkPixels':int(np.count_nonzero(changed & candidate & ~connected)),'mask':str(mask_path.relative_to(root)).replace('\\','/'),'originalUnchanged':sha(path)==source_sha})

font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',20)
smallfont=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',14)
sheet=Image.new('RGB',(1280,90+len(rows)*190),(35,40,47));d=ImageDraw.Draw(sheet)
d.text((20,15),'RELIC SMALL ICON - OUTER BORDER ONLY'+(' / PILOT' if args.pilot else ''),font=font,fill='white')
d.text((20,48),'Original + old binding / native-style small + separate 50% outline / edit mask / 48px / 32px',font=smallfont,fill='white')
def icon(path,outline_path,size,bg=(35,40,47)):
    with Image.open(path) as im: image=im.convert('RGBA')
    with Image.open(outline_path) as im: mask=im.convert('RGBA').getchannel('A')
    shadow=Image.new('RGBA',image.size,(0,0,0,0));shadow.putalpha(mask.point(lambda a:round(a*128/255)))
    canvas=Image.new('RGBA',image.size,(*bg,255));canvas.alpha_composite(shadow);canvas.alpha_composite(image)
    canvas.thumbnail((size,size),Image.Resampling.LANCZOS)
    return canvas
for i,row in enumerate(rows):
    y=90+i*190
    d.text((20,y+10),row['key'],font=font,fill='white')
    old=icon(root/row['original'],root/row['original'],140)
    new=icon(root/row['small'],root/row['outline'],140)
    sheet.paste(old,(260,y+6),old);sheet.paste(new,(440,y+6),new)
    with Image.open(root/row['mask']) as mask:
        show=Image.new('RGBA',mask.size,(255,70,115,0));show.putalpha(mask);show.thumbnail((140,140),Image.Resampling.LANCZOS)
        sheet.paste(show,(620,y+6),show)
    for j,size in enumerate([48,32]):
        for k,bg in enumerate([(35,40,47),(215,215,203)]):
            image=icon(root/row['small'],root/row['outline'],size,bg)
            x=810+j*85;y2=y+20+k*72
            sheet.paste(image,(x,y2))
    d.text((1000,y+35),str(row['changedAlphaPixels'])+' alpha edits',font=smallfont,fill='white')
    d.text((1000,y+65),'RGB unchanged',font=smallfont,fill='white')
    d.text((1000,y+95),'Interior RGBA exact',font=smallfont,fill='white')
sheet_path=out/('relic-border-pilot.png' if args.pilot else 'relic-border-all.png')
sheet.save(sheet_path)
record={'date':'2026-10-27','authorization':{'source':'Direct user instruction relayed by K1','verbatim':'Ja, Ränder mechanisch bearbeiten; Motive bewahren (empfohlen)'},'method':'Preserve originals/BigIcons. Small variant changes alpha only for neutral dark pixels (maxRGB<=64, chroma<=20) inside at most2%short-edge band AND connected through dark pixels to external transparency. Colored planes are barriers; disconnected inner dark edges remain unchanged. Separate white outline uses original alpha; native NRelic applies black50%. No resizing/repainting or interior-RGBA changes.','pilot':args.pilot,'assets':rows,'preview':str(sheet_path.relative_to(root)).replace('\\','/'),'limits':['Original PNGs remain unchanged.','Import processing is separate; no raw PNG-to-CTEX identity claim.','Static preview is not native live UI acceptance.']}
record_path=out/'relic-border-pilot.json' if args.pilot else Path(__file__).with_name('relic-outline-export.json')
record_path.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'count':len(rows),'record':str(record_path),'preview':str(sheet_path),'allInteriorExact':all(a['interiorRgbaExact'] for a in rows),'allOriginalsUnchanged':all(a['originalUnchanged'] for a in rows)}))
