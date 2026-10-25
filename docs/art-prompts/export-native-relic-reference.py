"""Offline, local-only Godot 4.5 BC7 relic references; no game or engine process.

Container layout: godotengine/godot 4.5 scene/resources/compressed_texture.cpp
(_load_data and load_image_from_file); Image::FORMAT_BPTC_RGBA in core/io/image.h.
Pixel decoder: installed Pillow DdsImagePlugin (DXGI_FORMAT_BC7_UNORM).
Only the observed GST2 v1, raw BC7, single-mip format is accepted. Never distribute
the resulting native art: outputs stay under the ignored .research directory.
"""
import hashlib
import io
import json
from pathlib import Path
import struct
from PIL import Image

root = Path(__file__).resolve().parents[2]
source = root / '.research/native-reference-0.107.1-20261025/.godot/imported'
target = root / '.research/native-reference-0.107.1-20261025/relic-reference-png'
target.mkdir(parents=True, exist_ok=True)
records = []
for name in ('anchor', 'vajra', 'membership_card'):
    files = list(source.glob(name + '.png-*.bptc.ctex'))
    if len(files) != 1:
        raise ValueError(f'Expected one source texture for {name}, found {len(files)}')
    path = files[0]
    data = path.read_bytes()
    if len(data) < 52 or data[:4] != b'GST2':
        raise ValueError(f'Unsupported CTEX signature: {path}')
    version, display_w, display_h = struct.unpack_from('<3I', data, 4)
    encoding = struct.unpack_from('<I', data, 36)[0]
    width, height = struct.unpack_from('<2H', data, 40)
    mipmaps, image_format = struct.unpack_from('<2I', data, 44)
    if version != 1 or encoding != 0 or image_format != 22 or mipmaps != 0:
        raise ValueError(f'Unsupported CTEX variant: {version=}, {encoding=}, {image_format=}, {mipmaps=}')
    if not 0 < width <= 4096 or not 0 < height <= 4096 or (width, height) != (display_w, display_h):
        raise ValueError(f'Unexpected dimensions: {width}x{height}, display {display_w}x{display_h}')
    payload = data[52:]
    expected = ((width + 3) // 4) * ((height + 3) // 4) * 16
    if len(payload) != expected:
        raise ValueError(f'Unexpected payload size {len(payload)} != {expected}')
    header = b'DDS ' + struct.pack('<7I', 124, 0x81007, height, width, expected, 0, 1)
    header += bytes(44)
    header += struct.pack('<II4sIIIII', 32, 4, b'DX10', 0, 0, 0, 0, 0)
    header += struct.pack('<5I', 0x1000, 0, 0, 0, 0)
    header += struct.pack('<5I', 98, 3, 0, 1, 0)  # BC7_UNORM, TEXTURE2D, one image
    with Image.open(io.BytesIO(header + payload)) as image:
        image.load()  # Entire BC7 interpretation is Pillow's established decoder.
        image.save(target / (name + '.png'))
    records.append(dict(name=name, source=str(path.relative_to(root)), sha256=hashlib.sha256(data).hexdigest(),
                        width=width, height=height, format='BPTC_RGBA / BC7_UNORM', payload_bytes=expected))
(target / 'sources.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
print(target)
