"""Rebuild La Rata's native AnimationPlayer scene from authored pose metadata.

Reads whole, unmodified pose PNGs. All registration is native Sprite2D uniform
scale and translation; no pixel edits, masks, segmentation or runtime framework.
Run after atlas-layout.json is authored. K6 owns engine/build/live validation.
"""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
layout_path = Path(__file__).with_name('royal-combat-20261025') / 'atlas-layout.json'
layout = json.loads(layout_path.read_text(encoding='utf-8'))
frames = layout['poses']
out = [f'[gd_scene load_steps={1+len(frames)*2+7} format=3]']
for i, frame in enumerate(frames):
    out.append(f'[ext_resource type="Texture2D" path="res://{frame["path"].removeprefix("assets/")}" id="pose_{i}"]')
for i, frame in enumerate(frames):
    width, height = frame['dimensions']
    out.extend([f'[sub_resource type="AtlasTexture" id="frame_{i}"]',
        f'atlas=ExtResource("pose_{i}")', f'region=Rect2(0,0,{width},{height})', 'filter_clip=true'])

def number(value):
    return format(float(value), '.8f').rstrip('0').rstrip('.') if value else '0'

def track(index, path, times, values, interpolation=1):
    out.extend([f'tracks/{index}/type="value"', f'tracks/{index}/path=NodePath("{path}")',
        f'tracks/{index}/interp={interpolation}', f'tracks/{index}/enabled=true',
        f'tracks/{index}/keys={{"times":PackedFloat32Array({",".join(number(t) for t in times)}),'
        f'"transitions":PackedFloat32Array({",".join("1" for _ in times)}),"update":0,"values":[{",".join(values)}]}}'])

by_key = {frame['key']: i for i, frame in enumerate(frames)}
for clip in layout['clips']:
    name, length = clip['name'], clip['length']
    poses = [(time, by_key[key]) for time, key in clip['poses']]
    out.extend([f'[sub_resource type="Animation" id="{name}"]',
        f'resource_name="{name}"', f'length={number(length)}', f'loop_mode={1 if clip.get("loop") else 0}'])
    blend = clip.get('blend', .055)
    for index in range(len(frames)):
        keys = {0: 1.0 if index == poses[0][1] else 0.0}
        last_time, last_pose = poses[0]
        for time, pose in poses[1:]:
            keys[max(last_time, round(time-blend, 5))] = 1.0 if index == last_pose else 0.0
            keys[time] = 1.0 if index == pose else 0.0
            last_time, last_pose = time, pose
        keys[length] = 1.0 if index == last_pose else 0.0
        keys = dict(sorted(keys.items()))
        track(index, f'Frame{index}:modulate:a', list(keys), [number(v) for v in keys.values()])
    offset = len(frames)
    motion = clip.get('motion', [[0,0,0],[length,0,0]])
    track(offset, '.:position', [p[0] for p in motion], [f'Vector2({number(p[1])},{number(p[2])})' for p in motion])
    breathing = clip.get('breathing', [[0,1,1],[length,1,1]])
    track(offset+1, '.:scale', [p[0] for p in breathing], [f'Vector2({number(p[1])},{number(p[2])})' for p in breathing])
    track(offset+2, '.:modulate', [0,length], ['Color(1,1,1,1)']*2)
    if clip.get('next'):
        # Native AnimationPlayer method track, no polling/task or custom state machine.
        out.extend([f'tracks/{offset+3}/type="method"',
            f'tracks/{offset+3}/path=NodePath("AnimationPlayer")', f'tracks/{offset+3}/enabled=true',
            f'tracks/{offset+3}/keys={{"times":PackedFloat32Array({number(length)}),"transitions":PackedFloat32Array(1),'
            '"values":[{"method":&"set_speed_scale","args":[1.0]}]}',
            f'tracks/{offset+4}/type="method"', f'tracks/{offset+4}/path=NodePath("AnimationPlayer")',
            f'tracks/{offset+4}/enabled=true',
            f'tracks/{offset+4}/keys={{"times":PackedFloat32Array({number(length)}),"transitions":PackedFloat32Array(1),'
            f'"values":[{{"method":&"play","args":[&"{clip["next"]}"]}}]}}'])

names = [c['name'] for c in layout['clips']]
out.extend(['[sub_resource type="AnimationLibrary" id="library"]',
    '_data={'+','.join(f'"{name}":SubResource("{name}")' for name in names)+'}',
    '[node name="RatKing" type="Node2D"]', '[node name="Visuals" type="Node2D" parent="."]', 'unique_name_in_owner=true'])
for i, frame in enumerate(frames):
    ax, ay = frame['anchor']
    scale = frame['scale']
    out.extend([f'[node name="Frame{i}" type="Sprite2D" parent="Visuals"]',
        f'texture=SubResource("frame_{i}")', 'centered=false',
        f'position=Vector2({number(-ax*scale)},{number(-ay*scale)})',
        f'scale=Vector2({number(scale)},{number(scale)})', f'modulate=Color(1,1,1,{1 if i==0 else 0})'])
out.extend(['[node name="Bounds" type="Control" parent="."]', 'unique_name_in_owner=true',
    'offset_left=-235.0', 'offset_top=-455.0', 'offset_right=235.0', 'offset_bottom=18.0', 'mouse_filter=2'])
for name, position in [('IntentPos',(0,-515)), ('CenterPos',(0,-225)), ('TalkPos',(-100,-380))]:
    out.extend([f'[node name="{name}" type="Marker2D" parent="."]', 'unique_name_in_owner=true',
        f'position=Vector2({position[0]},{position[1]})'])
out.extend(['[node name="AnimationPlayer" type="AnimationPlayer" parent="Visuals"]',
    'libraries={&"":SubResource("library")}', 'autoplay="Idle"', 'playback_default_blend_time=0.04'])
target = ROOT / 'assets/LaRata/scenes/rat_king.tscn'
target.write_text('\n'.join(out)+'\n', encoding='utf-8')

# Manifest and contact sheets are production evidence, not engine validation.
from PIL import Image, ImageDraw, ImageFont
manifest_path = Path(__file__).with_name('manifest-night-wave-20261025.json')
manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
for asset in manifest['assets']:
    path = ROOT / asset['target']
    with Image.open(path) as image:
        asset['dimensions'] = list(image.size)
        asset['mode'] = image.mode
        if image.mode == 'RGBA':
            asset['alphaBoundsAt16'] = image.getchannel('A').point(lambda p:255 if p>=16 else 0).getbbox()
    asset['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
    asset['sourceSha256'] = hashlib.sha256(Path(asset['source']).read_bytes()).hexdigest()
manifest['scene'] = {'path': str(target.relative_to(ROOT)).replace('\\','/'),
    'sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
    'layout': str(layout_path.relative_to(ROOT)).replace('\\','/'),
    'layoutSha256': hashlib.sha256(layout_path.read_bytes()).hexdigest()}
manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')

preview = ROOT / 'artifacts/night-wave-20261025/k5'
preview.mkdir(parents=True, exist_ok=True)
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 22)
sheet = Image.new('RGB', (3000, 1120), (38,42,50))
draw = ImageDraw.Draw(sheet)
for index, frame in enumerate(frames):
    x, y = (index%5)*600, (index//5)*560
    draw.line((x,y+510,x+599,y+510),fill=(137,157,148),width=2)
    with Image.open(ROOT/frame['path']) as original:
        image=original.convert('RGBA')
    # Exact native proportional sprite transform on a shared preview ground.
    scale=frame['scale']
    image=image.resize((round(image.width*scale),round(image.height*scale)),Image.Resampling.LANCZOS)
    ax,ay=frame['anchor']
    sheet.paste(image,(round(x+300-ax*scale),round(y+510-ay*scale)),image)
    draw.text((x+12,y+528),frame['key'],fill=(238,238,226),font=font)
sheet.save(preview/'boss-registered-poses.png')
icons=[a for a in manifest['assets'] if a['kind']=='relic']
sheet=Image.new('RGB',(1500,400),(38,42,50));draw=ImageDraw.Draw(sheet)
for index,asset in enumerate(icons):
    x=index*300
    with Image.open(ROOT/asset['target']) as original:
        for size,cy in [(210,145),(48,300),(32,300)]:
            image=original.convert('RGBA');image.thumbnail((size,size),Image.Resampling.LANCZOS)
            cx=x+150 if size==210 else x+(110 if size==48 else 190)
            sheet.paste(image,(cx-image.width//2,cy-image.height//2),image)
    draw.text((x+12,345),asset['key'],fill=(238,238,226),font=font)
sheet.save(preview/'new-relics.png')
print(f'Wrote {len(frames)} registered whole-PNG poses, {len(names)} native animations, manifest and contact sheets.')
