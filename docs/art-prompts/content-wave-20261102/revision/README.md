# Artwork corrections and Royal-Prize style revision

This is the separately authorized 2026-11-02 revision to the earlier content wave. It supersedes only the 5 selected runtime motifs below. Previous prompts, selected sources, before images and historical approvals remain traceable; no new artwork is described as human approved. K1's published content catalog and `docs/content-catalog-art` were not edited by K5.

| Artwork | Runtime target | New motif |
| --- | --- | --- |
| Royal Cleansing | `assets/LaRata/images/cleanse.png` | Crowned rat pours ceremonial cleansing light from a basin around allies, breaking curse chains |
| Scrap Coronation | `assets/LaRata/images/cards/scrap_coronation.png` | Rat artificer forges an improvised scrap crown |
| Smuggler's Dividend | `assets/LaRata/images/cards/smugglers_dividend.png` | Rat discards a card into a contraband chest and receives Vigor-like energy |
| Pawn Promotion | `assets/LaRata/images/cards/pawn_promotion.png` | Humble farmhand receives a royal mantle/crown; no chess imagery |
| The Relic Remover | `assets/LaRata/images/relics/relic_remover.png` | Brass rat-jawed extraction pliers grip a violet relic stone |

All 5 originals were generated separately using built-in ImageGen and copied byte-for-byte to their runtime targets. The 4 card illustrations contain no text, names, whole-card border or UI. The relic has genuine alpha and broad simplified painted planes. K2 owns Royal Cleansing's visible name/localization while its technical ID/artkey remains Cleanse/cleanse; K3 changed the Relic Remover to Uncommon. Those owner changes were read back from current source/localization, not duplicated by K5.

`prompts.json` and `manifest.json` preserve exact prompts, generated source paths, current source/runtime hashes, dimensions, alpha and prior runtime archives. All before images, including the unreleased Pawn Promotion, are under `artifacts/content-wave-20261102/revision/k5/before`. The former Relic Remover Big/Small/Outline are retained separately. The old wave's manifests/checks describe that earlier selection; this revision manifest records the superseding selection without rewriting historical approvals.

The existing mechanical exporter ran only for `--keys relic_remover`. Native Small/Outline remain at `assets/LaRata/images/relic_native/relic_remover.png` and `relic_remover_outline.png`. Original Big is untouched by the export; only externally connected neutral-dark border alpha can change, all RGB/protected interior RGBA stay exact, and the outline retains original alpha. `relic-outline-export.json` records the complete identity/mask assertions. No other relic was exported.

## Royal-Prize card design

The existing BaseLib `CreateCustomFrameMaterial` integration is retained. `RoyalCardStyle.Frame(bool isRoyalPrize = false)` selects the unchanged original shader for every default call and the new `royal_prize_frame.gdshader` only when explicitly passed true. K2 made the agreed minimal one-line binding in RemixCard: `RoyalCardStyle.Frame(IsBossPrize)`. No K5 edit of RemixCards.cs was performed.

The 20 concrete RemixCard aliases cover indices 0–19; only indices 10–19 are marked IsBossPrize. They are RoyalScepterPrize, RoyalAlchemyPrize, RoyalAegisPrize, RoyalContractPrize, RoyalTempestPrize, RoyalTithePrize, RoyalPlaguePrize, RoyalKneelPrize, RoyalForeclosurePrize and RoyalNightfallPrize. The source scope check confirms all and only these 10 models select the variant. Normal La-Rata cards and nonplayable boss-reference models keep their former frame shader.

The prize variant uses obsidian panels, stronger emerald bevels and static green inlay accents on the native frame texture. Native silhouette, relief, source alpha and inherited tint are retained. No overlay/control, second card renderer or frame artwork is added. Pixel-space repeats avoid assuming an atlas sprite fills the whole UV domain. The existing Banner method is unchanged: Uncommon/Rare return null so the native exact blue/gold banner, portrait border and type plaque remain responsible for rarity. Pool/reward eligibility, card IDs, costs, effects, RNG and rarity values were not changed by this style work.

The native card scene and BaseLib material getters were inspected locally. Shader built-ins follow the [Godot 4.5 CanvasItem shader contract](https://docs.godotengine.org/en/4.5/tutorials/shaders/shader_reference/canvas_item_shader.html). `preview-prize-frame.py` uses the existing Pillow BC7 decoder to approximate the palette on native Attack/Skill/Power frame reference pixels, preserving alpha. Its result is local-only `.research/royal-prize-style-20261102/preview.png`, never a runtime/distributed native asset. It is a CPU palette preview, not GPU shader compilation or a game capture.

## Checks and acceptance boundary

`verify.py` and `checks.json` confirm exact generated-source/runtime identity, opaque 4:3 card canvases, true relic alpha, before archives matching prior runtime, exact protected relic interior, precisely 7 changed existing PNGs and 141 other existing image assets unchanged. The original normal-card shader and 4 historical published catalog snapshots are hash-protected. K1's catalog files remain outside K5 ownership.

The isolated C# product build passed with 0 warnings/errors; evidence is under `artifacts/content-wave-20261102/revision/k5/code-check`. The frame predicate and native-rarity fallback are source checks, not a native UI test. `cards.png` and `relic-remover.png` under the revision artifact directory are offline previews.

Still required after this freeze: final K1 engine export, shader GPU compile/load, native rendering at hand/preview/Compendium sizes, sufficient Royal-Prize distinction beside ordinary La-Rata cards, readable native blue/gold rarity layers, all 10 prizes and ordinary controls, imported relic Big/Small/Outline appearance and human artwork acceptance. K2 separately owns the investigation of missing prize cards in the booster path. K5 performed no game start, installation, Godot export/import or Git/release action.
