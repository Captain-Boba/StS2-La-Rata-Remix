"""Local-only native frame palette approximation, not an engine/GPU capture."""
from pathlib import Path
import io, json, re, struct
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[4]
SRC=ROOT/'.research/native-ui-20261027'
OUT=ROOT/'.research/royal-prize-style-20261102'
OUT.mkdir(parents=True,exist_ok=True)
path=next((SRC/'.godot/imported').glob('ui_atlas_0.png-*.bptc.ctex'))
data=path.read_bytes()
assert data[:4]==b'GST2'
encoding=struct.unpack_from('<I',data,36)[0]
w,h=struct.unpack_from('<2H',data,40)
mips,fmt=struct.unpack_from('<2I',data,44)
assert encoding==0 and fmt==22
size=((w+3)//4)*((h+3)//4)*16
header=b'DDS '+struct.pack('<7I',124,0x81007,h,w,size,0,1)+bytes(44)
header+=struct.pack('<II4sIIIII',32,4,b'DX10',0,0,0,0,0)+struct.pack('<5I',0x1000,0,0,0,0)+struct.pack('<5I',98,3,0,1,0)
atlas=Image.open(io.BytesIO(header+data[52:52+size])).convert('RGBA')
def smooth(a,b,x):
    t=np.clip((x-a)/(b-a),0,1);return t*t*(3-2*t)
def mix(a,b,t): return np.array(a)*(1-t)+np.array(b)*t
sheet=Image.new('RGB',(1080,1510),'#20242c');draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
draw.text((16,10),'LOCAL CPU PALETTE APPROXIMATION - NOT NATIVE UI / GPU',font=font,fill='white')
for row,kind in enumerate(['attack','skill','power']):
    resource=(SRC/f'images/atlases/ui_atlas.sprites/card/card_frame_{kind}_s.tres').read_text(encoding='utf-8')
    x,y,cw,ch=map(int,re.search(r'region = Rect2\((\d+), (\d+), (\d+), (\d+)\)',resource).groups())
    image=atlas.crop((x,y,x+cw,y+ch));a=np.array(image)
    relief=(a[:,:,:3]/255)@np.array([.299,.587,.114]);r=relief[:,:,None]
    charcoal=mix([.09,.12,.115],[.24,.31,.28],r)
    jade=mix(charcoal,[.20,.53,.38],smooth(.48,.78,r))
    normal=mix(jade,[.60,.82,.62],smooth(.80,1,r))
    obsidian=mix([.025,.045,.038],[.12,.23,.16],r)
    emerald=mix(obsidian,[.055,.38,.16],smooth(.31,.48,r))
    edge=mix(emerald,[.56,.96,.65],smooth(.49,.75,r))
    py,px=np.mgrid[y:y+ch,x:x+cw]
    lozenge=(np.abs(np.mod((px+py)/56,1)-.5)+np.abs(np.mod((px-py)/56,1)-.5))[:,:,None]
    inlay=(1-smooth(.13,.18,lozenge))*smooth(.24,.55,r)
    prize=mix(edge,[.26,.94,.43],inlay*.78)
    for column,(caption,rgb) in enumerate([('native source',a[:,:,:3]/255),('normal La Rata',normal),('Royal Prize',prize)]):
        b=a.copy();b[:,:,:3]=np.round(np.clip(rgb,0,1)*255).astype('uint8')
        assert np.array_equal(b[:,:,3],a[:,:,3])
        preview=Image.fromarray(b);preview.thumbnail((300,422),Image.Resampling.LANCZOS)
        xx=20+column*355;yy=70+row*475
        sheet.paste(preview,(xx,yy),preview)
        draw.text((xx,yy+425),kind+' / '+caption,font=font,fill='white')
sheet.save(OUT/'preview.png')
print(OUT/'preview.png')
