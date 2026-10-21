"""Prepare the generated ink drawing for native boss tinting. Requires Pillow.

Run from the repository root: python docs/art-prompts/prepare-map-icon.py
The owner explicitly approved deterministic mask/outline processing.
"""
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps

root = Path(__file__).resolve().parents[2]
source = Image.open(Path(__file__).with_name('map-rat-king-source.png')).convert('RGBA')
alpha = source.getchannel('A')
ink = Image.new('RGBA', source.size, 'white')
ink.putalpha(alpha)
target = root / 'assets/LaRata/images'
ink.save(target / 'map_rat_king.png')

# Fill enclosed cutouts while preserving the outer silhouette. Padding makes
# the exterior flood-fill safe even if a future source touches a canvas edge.
solid = ImageOps.expand(alpha.point(lambda p: 255 if p >= 16 else 0), 2, 0)
ImageDraw.floodfill(solid, (0, 0), 128, thresh=0)
filled = solid.point(lambda p: 0 if p == 128 else 255).crop((2, 2, source.width + 2, source.height + 2))
outline_alpha = ImageChops.lighter(alpha, filled).filter(ImageFilter.MaxFilter(3))
outline = Image.new('RGBA', source.size, 'white')
outline.putalpha(outline_alpha)
outline.save(target / 'map_rat_king_outline.png')
assert ink.getchannel('A').tobytes() == alpha.tobytes(), 'Ink drawing alpha changed'
assert ImageChops.subtract(alpha, outline_alpha).getbbox() is None, 'Outline does not cover the icon'
print('Prepared native white masks; source alpha preserved exactly; outline covers every ink pixel.')
