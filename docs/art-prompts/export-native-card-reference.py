"""Extract native local-only WebP reference pixels; no engine and no new decoder.

Godot 4.5 compressed_texture.cpp load_image_from_file stores a byte count and an
ordinary WebP payload for DATA_FORMAT_WEBP (2). Only this observed single-mip
GST2 v1 variant is accepted. Pillow performs the existing WebP decoding.
Never copy the reference PNGs into mod assets or distributed documentation.
"""
import hashlib
import io
import json
from pathlib import Path
import struct
from PIL import Image

root = Path(__file__).resolve().parents[2]
folder = root / '.research/native-tester-reference-0.107.1-20261025'
output = folder / 'reference-png'
output.mkdir(exist_ok=True)
records = []
for path in sorted((folder / '.godot/imported').glob('*.ctex')):
    data = path.read_bytes()
    if len(data) < 56 or data[:4] != b'GST2':
        raise ValueError(f'Unsupported signature: {path}')
    version, display_w, display_h = struct.unpack_from('<3I', data, 4)
    encoding = struct.unpack_from('<I', data, 36)[0]
    width, height = struct.unpack_from('<2H', data, 40)
    mipmaps, image_format, size = struct.unpack_from('<3I', data, 44)
    if (version, encoding, mipmaps) != (1, 2, 0) or image_format not in (4, 5):
        raise ValueError(f'Unsupported variant: {path}')
    if (width, height) != (display_w, display_h) or len(data) != 56 + size:
        raise ValueError(f'Unexpected dimensions or payload: {path}')
    payload = data[56:]
    if payload[:4] != b'RIFF' or payload[8:12] != b'WEBP':
        raise ValueError(f'Not an embedded standard WebP: {path}')
    target = output / (path.name.split('.png-')[0] + '.png')
    with Image.open(io.BytesIO(payload)) as image:
        image.load()
        if image.size != (width, height):
            raise ValueError(f'WebP dimensions differ: {path}')
        image.save(target)
    records.append({'source': str(path.relative_to(root)).replace('\\', '/'),
        'sourceSha256': hashlib.sha256(data).hexdigest(),
        'target': str(target.relative_to(root)).replace('\\', '/'),
        'sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
        'dimensions': [width, height], 'encoding': 'WebP, Pillow decoder'})
(output / 'sources.json').write_text(json.dumps(records, indent=2) + '\n', encoding='utf-8')
print(output)
