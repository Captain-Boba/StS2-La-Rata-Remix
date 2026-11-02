# Superseded intermediate portrait-only interpretation

This was not the final user-approved scope. The user clarified that the normal rarity banner of the 10 playable boss-prize cards must be replaced. The portrait-only patch was removed; the [banner correction](../banner-correction/README.md) is the active implementation. The following text records the intermediate technical state only.

The user explicitly withdrew the special Emerald/Obsidian Royal-Prize design and requested only the existing reddish artwork border used by the boss cards. This correction supersedes the preceding style revision; its reports, preview and archived shader remain historical evidence, not the active design.

## Current implementation

- RoyalCardStyle.Frame() and the one RemixCard frame-binding line are restored to the ordinary existing La-Rata frame. The optional prize selector is removed.
- The unused royal_prize_frame.gdshader and its .uid are removed from runtime assets after archiving under `artifacts/content-wave-20261102/revision/k5/portrait-border-correction/before`. No active source/scene/material reference remains.
- Only the 10 playable Royal-Prize aliases receive the actual existing boss-reference material on the existing native PortraitBorder node. No artwork pixel, color constant, shader, texture, overlay, whole-card frame or new UI control is authored for this change.
- Title banner and type plaque keep the normal native rarity material. Rarity, pool membership, effects, costs, card IDs and version 0.9.6 are unchanged.

## Native reference and integration

The locally inspected RoyalCard base uses CardRarity.Token and its existing CreateCustomBannerMaterial override. `ModelDb.Card<ScepterStorm>().BannerMaterial` therefore supplies the actual Royal boss-reference material, including BaseLib's cached override. The correction copies that material reference, rather than approximating the user's described reddish tone with a new hue or changing a prize card's rarity to Token.

Native CardModel.PortraitBorder selects the existing Attack/Skill/Power border texture by card type. NCard.Reload assigns Model.BannerMaterial to the portrait border and title banner, while UpdateTypePlaque uses the same getter. BaseLib exposes custom frame/banner materials but no independent portrait-border material override. Consequently one scoped NCard.Reload postfix in `RoyalPrizePortraitBorder.cs` changes only the existing `_portraitBorder.Material`, after the normal native setup, when the model is RemixCard.IsBossPrize. No other card is touched.

Native Reload sets the normal portrait-border material again on every model reload. A pooled view reused for an ordinary card therefore follows the normal native reset path; no separate persistent presentation state or reversion mechanism is introduced. The source check confirms exactly RoyalScepterPrize, RoyalAlchemyPrize, RoyalAegisPrize, RoyalContractPrize, RoyalTempestPrize, RoyalTithePrize, RoyalPlaguePrize, RoyalKneelPrize, RoyalForeclosurePrize and RoyalNightfallPrize match the existing predicate.

## Evidence and limits

`verify.py`/`checks.json` confirm all 296 existing image/import files are byte-identical, the only RemixCards.cs edit is its authorized frame-binding line, the rarity Banner method is unchanged, the old runtime shader/UID are removed and archived, the 10-model scope remains exact and version is still 0.9.6. Historical artwork approvals and the K1 catalog are untouched.

An isolated product build passed with 0 warnings/errors. The existing technical patch-activation fixture using that product DLL also passed (91 patched methods including fixture substitutions), validating activation of the added native hook without a game or engine start. Logs and before archives are under the matching artifact directory. These are technical/source checks, not evidence of rendered color or native visual acceptance.

K6 must incorporate the new DLL and refresh the package/resource removal as needed in the existing local test cycle. Targeted native acceptance: all 10 Prize borders match the actual Royal boss reference; ordinary frames, native gold/blue title/type rarity layers and ordinary-card controls remain normal; hand/enlarged/Compendium/upgrade views and a reused card node refresh correctly. K5 performed no game start, installation, Godot import/export, Git or release action and generated/edited no artwork.
