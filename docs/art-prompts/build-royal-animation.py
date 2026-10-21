"""Rebuild native Godot atlas/animation resources. Never modifies image pixels."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
frames = []
# Atlas cells and foot anchors measured on the selected generated sheets.
# Register every pose to a common ground line; keep 445 px crown-to-ground height.
for sheet, width, height, anchors, scales in [
    ('idle', 1312, 1199, [(367,571),(333,571),(367,568),(333,568)], [445/551,445/551,445/545,445/545]),
    ('attack', 1536,1024, [(462,498),(365,497),(445,485),(366,493)], [445/481,445/477,445/477,445/480]),
    ('cast',1536,1024, [(426,510),(391,510),(426,510),(391,510)], [445/499]*4),
]:
    for i, ((ax,ay),scale) in enumerate(zip(anchors,scales)):
        x,y=i%2,i//2
        left,top=x*width//2,y*height//2
        frames.append(dict(sheet=sheet, region=(left,top,(x+1)*width//2-left,(y+1)*height//2-top), anchor=(ax,ay), scale=scale))

out=['[gd_scene load_steps=23 format=3]']
for sheet in ['idle','attack','cast']:
    out.append(f'[ext_resource type="Texture2D" path="res://LaRata/images/rat_king_{sheet}.png" id="{sheet}"]')
for i,f in enumerate(frames):
    out += [f'[sub_resource type="AtlasTexture" id="frame_{i}"]',f'atlas=ExtResource("{f["sheet"]}")', 'region=Rect2(%s)' % ','.join(map(str,f['region'])), 'filter_clip=true']

def track(n,path,times,values,interp=1):
    out.extend([f'tracks/{n}/type="value"',f'tracks/{n}/path=NodePath("{path}")',f'tracks/{n}/interp={interp}',f'tracks/{n}/enabled=true',
        f'tracks/{n}/keys={{"times":PackedFloat32Array({",".join(map(str,times))}),"transitions":PackedFloat32Array({",".join("1" for _ in times)}),"update":0,"values":[{",".join(values)}]}}'])

def animation(name,length,poses,blend=.07,loop=False,motion=None,tint=None):
    out.extend([f'[sub_resource type="Animation" id="{name}"]',f'resource_name="{name}"',f'length={length}',f'loop_mode={1 if loop else 0}'])
    # Native opacity tracks crossfade registered painted frames. No timer or RNG.
    for frame in range(len(frames)):
        keys={0:1.0 if frame==poses[0][1] else 0.0}
        last=poses[0][1]
        last_time=0
        for when,pose in poses[1:]:
            keys[max(last_time,round(when-blend,4))]=1.0 if frame==last else 0.0
            keys[when]=1.0 if frame==pose else 0.0
            last=pose
            last_time=when
        keys[length]=1.0 if frame==last else 0.0
        keys=dict(sorted(keys.items()))
        track(frame,f'Frame{frame}:modulate:a',list(keys),[str(v) for v in keys.values()])
    times,values=motion or ([0,length],['Vector2(0,0)']*2)
    track(12,'.:position',times,values)
    if loop:
        track(13,'.:scale',[0,3,6,9,12,15,18],['Vector2(1,1)','Vector2(1.004,1.009)','Vector2(1,1)','Vector2(1.003,1.006)','Vector2(1,1)','Vector2(1.004,1.009)','Vector2(1,1)'],2)
    else:
        track(13,'.:scale',[0,length],['Vector2(1,1)']*2)
    times,values=tint or ([0,length],['Color(1,1,1,1)']*2)
    track(14,'.:modulate',times,values)

animation('Idle',18,[(0,0),(3,1),(6,0),(9.05,2),(9.17,0),(13,3),(16,0)],blend=.16,loop=True)
animation('Attack',.75,[(0,0),(.08,4),(.18,5),(.32,6),(.53,7),(.75,0)],motion=([0,.18,.32,.53,.75],['Vector2(0,0)','Vector2(6,0)','Vector2(-30,-4)','Vector2(-8,0)','Vector2(0,0)']))
animation('Cast',1.1,[(0,0),(.08,8),(.22,9),(.48,10),(.78,11),(1.1,0)],tint=([0,.48,1.1],['Color(1,1,1,1)','Color(.9,1.12,.96,1)','Color(1,1,1,1)']))
animation('Hit',.32,[(0,0),(.09,5),(.32,0)],motion=([0,.09,.32],['Vector2(0,0)','Vector2(9,0)','Vector2(0,0)']),tint=([0,.09,.32],['Color(1,1,1,1)','Color(1.6,.65,.65,1)','Color(1,1,1,1)']))
animation('Dead',.9,[(0,0)],motion=([0,.9],['Vector2(0,0)','Vector2(0,35)']),tint=([0,.4,.9],['Color(1,1,1,1)','Color(.3,.7,.4,.6)','Color(.1,.3,.2,0)']))
animation('RESET',.01,[(0,0)])
out.extend(['[sub_resource type="AnimationLibrary" id="library"]','_data={'+','.join(f'"{n}":SubResource("{n}")' for n in ['Idle','Attack','Cast','Hit','Dead','RESET'])+'}',
    '[node name="RatKing" type="Node2D"]','[node name="Visuals" type="Node2D" parent="."]','unique_name_in_owner=true'])
for i,f in enumerate(frames):
    ax,ay=f['anchor'];s=f['scale']
    out.extend([f'[node name="Frame{i}" type="Sprite2D" parent="Visuals"]',f'texture=SubResource("frame_{i}")','centered=false',f'position=Vector2({-ax*s:.6f},{-ay*s:.6f})',f'scale=Vector2({s:.8f},{s:.8f})',f'modulate=Color(1,1,1,{1 if i==0 else 0})'])
out.extend(['[node name="Bounds" type="Control" parent="."]','unique_name_in_owner=true','offset_left=-190.0','offset_top=-445.0','offset_right=190.0','offset_bottom=15.0','mouse_filter=2'])
for name,pos in [('IntentPos',(0,-465)),('CenterPos',(0,-230)),('TalkPos',(-100,-380))]:
    out.extend([f'[node name="{name}" type="Marker2D" parent="."]','unique_name_in_owner=true',f'position=Vector2({pos[0]},{pos[1]})'])
out.extend(['[node name="AnimationPlayer" type="AnimationPlayer" parent="Visuals"]','libraries={&"":SubResource("library")}','autoplay="Idle"','playback_default_blend_time=0.12'])
text = re.sub(r'(?<=[(,])\.(\d)', r'0.\1', '\n'.join(out)+'\n')
(ROOT/'assets/LaRata/scenes/rat_king.tscn').write_text(text,encoding='utf-8')
print('Wrote 12 registered frames, native crossfades and six animations; PNGs unchanged.')
