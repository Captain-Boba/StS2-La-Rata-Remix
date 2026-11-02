# Final user clarification: boss rarity banner for playable prizes

This supersedes the rejected Emerald/Obsidian special frame and the intermediate portrait-only interpretation. The final user instruction replaces the normal rarity banner of the 10 playable Royal-Prize cards with the existing boss banner. Actual CardRarity stays Rare. The ordinary existing La-Rata frame stays in place; artworks, pools, effects and version 0.9.6 are unchanged.

## Implementation

RemixCard.CreateCustomFrameMaterial again calls RoyalCardStyle.Frame() without a prize variant. The unused prize-frame shader and UID are removed after archiving. The newly introduced RoyalPrizePortraitBorder postfix was also removed; it was an unaccepted intermediate implementation and is retained only in the artifact archive.

The existing BaseLib interface is sufficient for the clarified scope:

```csharp
public override Material? CreateCustomBannerMaterial => IsBossPrize
    ? ModelDb.Card<ScepterStorm>().BannerMaterial
    : RoyalCardStyle.Banner(Rarity);
```

ScepterStorm is an actual RoyalCard boss-reference model. Its existing Token-rarity/custom-banner path supplies the current boss material; this correction reuses that cached material reference without inventing a color, shader or Token rarity for player cards. Native NCard.Reload and UpdateTypePlaque naturally apply BannerMaterial to the title banner, portrait border and type plaque. This shared native behavior is now intentional. No custom UI patch or overlay is retained.

Only RoyalScepterPrize, RoyalAlchemyPrize, RoyalAegisPrize, RoyalContractPrize, RoyalTempestPrize, RoyalTithePrize, RoyalPlaguePrize, RoyalKneelPrize, RoyalForeclosurePrize and RoyalNightfallPrize match the existing IsBossPrize predicate. All ordinary cards retain their previous rarity-banner fallback. The only edits in RemixCards.cs compared with the pre-correction archive are its 2 authorized style-binding expressions; no mechanics, rarity value or pool registration changed.

## Evidence

`verify.py`/`checks.json` verify all 296 image/import files unchanged, exactly 10 targeted aliases, unchanged normal-card fallback, unchanged gameplay source outside the 2 bindings, restored normal frame, removed special shader/UID and removed portrait-only patch. Historical artwork reports, sources and approvals remain intact. The earlier portrait-only handoff is explicitly superseded and not final acceptance.

The isolated product build passed with 0 warnings/errors. Logs and the candidate DLL are under `artifacts/content-wave-20261102/revision/k5/banner-correction/code-check`. This correction adds no Harmony target; the existing BaseLib material integration is reused. Rendered color/layout and human acceptance remain K6's targeted native check in the existing local cycle: compare all 10 player prizes directly with Royal boss-reference cards, verify ordinary-card controls, Rare filtering/upgrades, hand/preview/Compendium views and the unchanged normal frame.

K5 generated/edited no artwork and performed no game start, Godot import/export, installation, Git or release action.
