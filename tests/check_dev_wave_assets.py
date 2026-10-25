"""Read-only PNG/manifest checks; emits evidence only, never rewrites artwork."""
from pathlib import Path
import hashlib
import json
import argparse
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('--expected-new-cards', type=int, default=11)
parser.add_argument('--report-name', default='asset-check-01.json')
args = parser.parse_args()

root = Path(__file__).resolve().parents[1]
out = root / 'artifacts/dev-wave-20261025'
manifest_path = root / 'docs/art-prompts/manifest-dev-wave-20261025.json'
approval = json.loads((root / 'docs/art-prompts/booster-approval-20261025.json').read_text(encoding='utf-8'))
manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
bindings = json.loads((out / 'asset-model-bindings.json').read_text(encoding='utf-8'))

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def require(value, message):
    if not value:
        raise AssertionError(message)

def inspect(entry):
    path = (root / entry['target']).resolve()
    require(path.is_relative_to(root), 'Target escapes project: ' + str(path))
    require(digest(path) == entry['sha256'], 'Hash mismatch: ' + str(path))
    with Image.open(path) as check:
        require(check.format == 'PNG', 'Not PNG: ' + str(path))
        check.verify()
    with Image.open(path) as image:
        image.load()
        require(list(image.size) == entry['dimensions'] and image.mode == entry['mode'], 'Image metadata mismatch: ' + str(path))
        result = dict(target=entry['target'], sha256=digest(path), dimensions=list(image.size), mode=image.mode)
        if image.mode == 'RGBA':
            alpha = image.getchannel('A')
            bounds = alpha.point(lambda x: 255 if x >= 16 else 0).getbbox()
            require(list(bounds) == entry['alphaBoundsAt16'], 'Alpha bounds mismatch: ' + str(path))
            require(alpha.getextrema() == (0, 255), 'No fully transparent/opaque alpha: ' + str(path))
            require(bounds[0] > 0 and bounds[1] > 0 and bounds[2] < image.width and bounds[3] < image.height, 'Visible contour meets canvas: ' + str(path))
            result['alphaBoundsAt16'] = bounds
        return result

require(len(manifest['assets']) == args.expected_new_cards + 15, 'Manifest inventory changed')
require(len({a['key'] for a in manifest['assets']}) == len(manifest['assets']), 'Duplicate keys')
require(len({a['target'] for a in manifest['assets']}) == len(manifest['assets']), 'Duplicate targets')
rows = []
for asset in manifest['assets']:
    rows.append(inspect(asset))
    require(digest(Path(asset['source'])) == asset['sourceSha256'], 'Generated source hash mismatch: ' + asset['key'])
    if 'projectSource' in asset:
        require(digest(root / asset['projectSource']) == asset['sourceSha256'], 'Durable source differs: ' + asset['key'])
    rows.extend(inspect(derived) for derived in asset.get('derivedOutputs', []))
    if asset['target'].startswith('assets/'):
        require(asset['target'] in {b['path'] for b in bindings}, 'Manifest asset is not used by a checked model: ' + asset['target'])
cards = [a for a in manifest['assets'] if '/cards/' in a['target']]
relics = [a for a in manifest['assets'] if a.get('kind') == 'relic']
require(len(cards) == args.expected_new_cards and all(a['mode'] == 'RGB' for a in cards), 'Card count/opacity mismatch')
for card in cards:
    # Big O and the fresh Thorn original are deliberately unresampled. K5 confirmed
    # this exact generator rounding deviation; do not broaden other art tolerances.
    allowed_original = card['key'] in ('wrath_of_the_big_o', 'ser_kevs_thorn_buster') and card['dimensions'] == [1447, 1087]
    require(allowed_original or abs(card['dimensions'][0] - card['dimensions'][1]*4/3) <= 1,
            'Card ratio differs from recorded original: ' + card['key'])
card_ratios = [dict(key=a['key'], ratio=a['dimensions'][0]/a['dimensions'][1],
                   deviationPercent=(a['dimensions'][0]/a['dimensions'][1]/(4/3)-1)*100) for a in cards]
require(len(relics) == 14, 'Not all 14 relics covered')
require({a['target'] for a in relics} == {b['path'] for b in bindings if b['kind'] == 'relic'}, 'Relic manifest/model inventory differs')
require(digest(root / approval['manifest']['path']) == approval['manifest']['sha256'], 'Approved manifest snapshot hash differs')
require(digest(root / approval['contactSheet']['path']) == approval['contactSheet']['sha256'], 'Approved contact sheet differs')
require(len(approval['assets']) == 6, 'Approval must cover exactly six packs')
pack_rows = []
for pack in approval['assets']:
    asset = next(a for a in manifest['assets'] if a['key'] == pack['key'])
    require(asset['human_approved'] and asset['sha256'] == pack['sha256'] and digest(root / pack['target']) == pack['sha256'], 'Approved runtime pack differs')
    require(digest(root / pack['projectSource']) == pack['sourceSha256'], 'Approved pack source differs')
    box = asset['alphaBoundsAt16']
    ratio = (box[2]-box[0])/(box[3]-box[1])
    require(round(ratio, 5) == pack['observedOuterSilhouetteRatio'] and box[3]-box[1] in (980, 981), 'Pack measured geometry differs')
    pack_rows.append(dict(key=pack['key'], ratio=ratio, deviationPercent=(ratio/(292/414)-1)*100, sha256=asset['sha256']))
require(all(not a['human_approved'] for a in manifest['assets'] if not a['key'].endswith('_pack')), 'Approval broadened beyond packs')

# Source-level exclusion only: no export is performed or claimed here.
reference_root = root / '.research/native-reference-0.107.1-20261025'
reference_roots = [reference_root, root / '.research/native-tester-reference-0.107.1-20261025']
reference_hashes = {digest(p) for folder in reference_roots for p in folder.rglob('*') if p.is_file() and p.suffix in ('.png', '.ctex', '.tres')}
for folder in ('assets', 'docs'):
    for path in (root / folder).rglob('*'):
        if path.is_file() and '.godot' not in path.parts:
            require(digest(path) not in reference_hashes, 'Native reference byte copy found in packaging input: ' + str(path))
evidence = dict(status='PASS', manifestSha256=digest(manifest_path), approval=approval,
                images=rows, boosterMeasurements=pack_rows, cardRatios=card_ratios, modelBindings=len(bindings),
                nativeReferenceHashesCompared=len(reference_hashes),
                limits=['Filesystem and decoded-PNG checks only; no engine import/PCK/live UI.',
                        'Native exclusion checks exact reference file copies in assets/docs plus source package boundary; no claim about an exported package.',
                        'Human approval covers only the six recorded booster images.'])
(out / args.report_name).write_text(json.dumps(evidence, indent=2), encoding='utf-8')
print(f'PASS: {len(rows)} PNGs ({len(manifest["assets"])} sources/targets plus 2 map derivatives), manifest/source/approval hashes, alpha/geometry and {len(bindings)} actual model bindings; 6 booster approvals scoped. No export/live UI.')
