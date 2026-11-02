"""Targeted source/style and unchanged-art checks; no engine/UI execution."""
from pathlib import Path
import hashlib,json,re
ROOT=Path(__file__).resolve().parents[5]
DOC=Path(__file__).parent
OUT=ROOT/'artifacts/content-wave-20261102/revision/k5/portrait-border-correction'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
rows=json.loads((OUT/'before/images.json').read_text(encoding='utf-8-sig'))
for row in rows:assert sha(ROOT/row['path'])==row['sha256'],row['path']
remix=(ROOT/'src/LaRata/RemixCards.cs').read_text(encoding='utf-8')
old=(OUT/'before/RemixCards.cs').read_text(encoding='utf-8')
assert old.replace('RoyalCardStyle.Frame(IsBossPrize)','RoyalCardStyle.Frame()')==remix,'Unexpected RemixCards change'
style=(ROOT/'src/LaRata/RoyalCardPool.cs').read_text(encoding='utf-8')
old_style=(OUT/'before/RoyalCardPool.cs').read_text(encoding='utf-8')
assert style[style.index('    // Let the native rarity getter'):]==old_style[old_style.index('    // Let the native rarity getter'):]
assert 'Frame(bool' not in style and 'isRoyalPrize' not in style
assert not (ROOT/'assets/LaRata/shaders/royal_prize_frame.gdshader').exists()
assert not (ROOT/'assets/LaRata/shaders/royal_prize_frame.gdshader.uid').exists()
assert (OUT/'before/royal_prize_frame.gdshader').exists() and (OUT/'before/royal_prize_frame.gdshader.uid').exists()
for path in [* (ROOT/'src/LaRata').glob('*.cs'), * (ROOT/'assets/LaRata').rglob('*.tres'), * (ROOT/'assets/LaRata').rglob('*.tscn')]:
    assert 'royal_prize_frame' not in path.read_text(encoding='utf-8-sig'),str(path)
hook=(ROOT/'src/LaRata/RoyalPrizePortraitBorder.cs').read_text(encoding='utf-8')
assert 'ModelDb.Card<ScepterStorm>().BannerMaterial' in hook
assert '____portraitBorder.Material =' in hook and 'IsBossPrize: true' in hook
assert not any(text in hook for text in ['new Color','GenerateHsv','Rarity =','new Shader','AddChild'])
models=[name for name,index in re.findall(r'class\s+(\w+)\(\)\s*:\s*RemixCard\((\d+)\)',remix) if int(index)>=10]
assert len(models)==10
assert '<Version>0.9.6</Version>' in (ROOT/'src/LaRata/LaRata.csproj').read_text(encoding='utf-8')
checks={'unchangedImageAndImportFiles':len(rows),'remixEdit':'Only Frame(IsBossPrize) -> Frame() binding line.',
        'normalFrameRestored':True,'rarityBannerMethodUnchanged':True,'prizeModels':models,
        'reference':'Actual ScepterStorm RoyalCard.BannerMaterial; no new color/material.',
        'portraitOnly':True,'specialShaderAndUidRemovedAndArchived':True,'version':'0.9.6',
        'humanOrLiveUiAcceptance':False}
(DOC/'checks.json').write_text(json.dumps(checks,indent=2)+'\n',encoding='utf-8')
print(json.dumps(checks))
