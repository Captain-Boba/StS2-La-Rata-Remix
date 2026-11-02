"""Latest user scope: existing boss banner for ten prizes, no special frame/hook."""
from pathlib import Path
import hashlib,json,re
ROOT=Path(__file__).resolve().parents[5]
DOC=Path(__file__).parent
BEFORE=ROOT/'artifacts/content-wave-20261102/revision/k5/portrait-border-correction/before'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
rows=json.loads((BEFORE/'images.json').read_text(encoding='utf-8-sig'))
for row in rows:assert sha(ROOT/row['path'])==row['sha256'],row['path']
old=(BEFORE/'RemixCards.cs').read_text(encoding='utf-8')
expected=old.replace('RoyalCardStyle.Frame(IsBossPrize)','RoyalCardStyle.Frame()').replace(
    'public override Material? CreateCustomBannerMaterial => RoyalCardStyle.Banner(Rarity);',
    'public override Material? CreateCustomBannerMaterial => IsBossPrize\n        ? ModelDb.Card<ScepterStorm>().BannerMaterial\n        : RoyalCardStyle.Banner(Rarity);')
current=(ROOT/'src/LaRata/RemixCards.cs').read_text(encoding='utf-8')
assert current==expected,'Unexpected change outside the two authorized style bindings'
style=(ROOT/'src/LaRata/RoyalCardPool.cs').read_text(encoding='utf-8')
old_style=(BEFORE/'RoyalCardPool.cs').read_text(encoding='utf-8')
assert style[style.index('    // Let the native rarity getter'):]==old_style[old_style.index('    // Let the native rarity getter'):]
assert 'Frame(bool' not in style and 'isRoyalPrize' not in style
for path in ['assets/LaRata/shaders/royal_prize_frame.gdshader','assets/LaRata/shaders/royal_prize_frame.gdshader.uid','src/LaRata/RoyalPrizePortraitBorder.cs']:
    assert not (ROOT/path).exists(),path
for path in (ROOT/'src/LaRata').glob('*.cs'):
    text=path.read_text(encoding='utf-8-sig')
    assert 'royal_prize_frame' not in text and 'RoyalPrizePortraitBorder' not in text
models=[name for name,index in re.findall(r'class\s+(\w+)\(\)\s*:\s*RemixCard\((\d+)\)',current) if int(index)>=10]
assert len(models)==10
boss=(ROOT/'src/LaRata/RoyalCards.cs').read_text(encoding='utf-8')
assert 'CardRarity.Token' in boss and 'CreateCustomBannerMaterial => RoyalCardStyle.Banner(Rarity)' in boss
assert '<Version>0.9.6</Version>' in (ROOT/'src/LaRata/LaRata.csproj').read_text(encoding='utf-8')
checks={'unchangedImageAndImportFiles':len(rows),'styleBindingChangesOnly':True,'normalFrameRestored':True,
        'prizeModels':models,'prizeCount':10,'bossReference':'ModelDb.Card<ScepterStorm>().BannerMaterial',
        'integration':'Existing BaseLib CreateCustomBannerMaterial; native shared title/portrait/type-plaque contract.',
        'portraitOnlyPatchRemoved':True,'specialShaderAndUidRemoved':True,'actualCardRarityAndGameplayUnchanged':True,
        'normalCardBannerFallbackUnchanged':True,'version':'0.9.6','nativeVisualAcceptance':'PENDING K6'}
(DOC/'checks.json').write_text(json.dumps(checks,indent=2)+'\n',encoding='utf-8')
print(json.dumps(checks))
