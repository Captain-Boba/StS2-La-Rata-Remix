# Content artwork and native UI, 2026-11-02

K5 owns the new artwork, RelicContest presentation, and the separately authorized damage-ramp display. K2 owns cards and their localization; K3 owns relic effects and Reset. No card/relic effect, boss damage rule, deck, reward pool, Cleanse rule or synchronized result is changed by this delivery.

## Artwork

All 10 originals were generated individually with built-in ImageGen, then copied unchanged into the workspace. There are no sheet cutouts, placeholder motifs, names or whole-card frames. `prompts.json` and `manifest.json` record exact prompts, generated source paths, runtime artkeys, hashes, dimensions, alpha and review limits. Author inspection is not human visual approval.

| Visible subject | Runtime PNG |
| --- | --- |
| Royal Shuffle: dark royal rat shuffling/multiplying decks | `assets/LaRata/images/cards/royal_replication.png` |
| Dead Letter: courier retrieving a letter from a haunted archive | `assets/LaRata/images/cards/dead_letter.png` |
| Plague Parade: rat procession carrying venom into battle | `assets/LaRata/images/cards/plague_parade.png` |
| Ashen Overture: rat conductor, spent cards becoming defensive ash | `assets/LaRata/images/cards/ashen_overture.png` |
| Pawn Promotion: farmer rat crowned in a court ceremony | `assets/LaRata/images/cards/pawn_promotion.png` |
| Last Laugh: jester dissolving curses into protective confetti | `assets/LaRata/images/cards/last_laugh.png` |
| Sootbound Ticket: new red burned ticket, punched rat-crown hole | `assets/LaRata/images/relics/sootbound_ticket.png` |
| Counterfeit Seal: crooked wax seal with cheeky rat face | `assets/LaRata/images/relics/counterfeit_seal.png` |
| Plague Testament: ivory scroll clasped by a green rat incisor | `assets/LaRata/images/relics/plague_testament.png` |
| Usurper's Tooth: oversized incisor wearing an absurdly small crown | `assets/LaRata/images/relics/usurpers_tooth.png` |

The user subsequently requested the darker classic black/green Royal Shuffle redesign and its new visible title. That fresh dark original is now selected, SHA-256 `54d59a985a376bbb72774411ce45b0933f64a37f21034b8b875ec3e3d3dd030c`. The stable `royal_replication` artkey and `LARATA-ROYAL_REPLICATION` ID are retained. K2 updated the visible titles; current JSON was verified as Royal Shuffle (eng), Königliches Mischen (deu) and 王室洗牌 (zhs). The earlier bright blue/gold illustration is archived as a rejected proposal rather than destroyed. Other selected card illustrations retain their individual color palettes.

The original Sootbound Ticket and both former native variants are archived under `artifacts/content-wave-20261102/k5/before`. The replacement's crown-shaped perforation contains actual transparent pixels. The other existing motifs are checked against the pre-change asset baseline. Historical approval manifests remain historical records; the current wave manifest explicitly supersedes only Sootbound Ticket at its existing artkey.

## Native relic variants

The existing bounded mechanical exporter now accepts `--keys`, `--out`, `--record`, `--date` and `--authorization`. Its pixel algorithm and default behavior remain unchanged. Only the 4 named relics were exported, giving 8 PNGs under `assets/LaRata/images/relic_native/{key}.png` and `{key}_outline.png`; no unrelated original or native variant was rewritten.

Original generated PNGs remain the BigIcon source. Small changes alpha only in the existing bounded, externally connected neutral-dark seam; interior RGBA and all RGB remain exact. Separate white outlines retain original alpha and use the native icon node's black modulation. `relic-outline-export.json` includes every source/small/outline hash, mask and preservation assertion. Normal Godot alpha-border import processing remains a separate step; no raw PNG-to-CTEX equality is claimed.

`verify-and-preview.py` verifies source/runtime identity, opaque 4:3 cards, genuine relic alpha and punched-ticket transparency, all export hashes, protected pixels, localization placeholders/duplicate keys, and preservation of the other existing artwork. `cards.png`, `relics.png` and `relic-border-all.png` under the wave's artifact directory are offline comparisons with 48/32px samples, not native game captures.

## Well contest animation

The existing Well contender filtering, seeded RNG namespace and winner draw remain in `RelicContest.Resolve`. The winner is fixed before `AnimateWell`; no animation value or additional random choice participates in the result. The presentation replaces the former text-only delay with a short native Godot sequence: the unchanged Well icon appears, opposing native character-hand symbols shrink and sink behind it, all Well holders receive labeled tokens, and the preselected winner's token/name are emphasized. Hand textures are explicitly cosmetic symbols; no player RPS choice is fabricated or sent over the network.

Multiple Well holders keep the existing equal-chance winner rule, and their labels use global run player indices. The stage scales from a centered 1920 x 1080 canvas on resize. Native treasure backdrop, existing fonts, existing Well/character textures and Godot tweens are used. New UI keys are limited to `contest.well_swallow` and `contest.well_awarded` in eng/deu/zhs.

The existing cancellation token is linked to treasure-scene exit. Exit cancels the active contest session; disposal disconnects the scene callback, kills owned tweens, removes the overlay and restores native hands/backdrop/label. Winner metrics and native awarding remain on the existing path after successful presentation. Cancellation does not invent a substitute winner or award.

## Royal damage-ramp display

`RoyalDamageRampPresentation.cs` adds a native NPower view for RatKing and KingsguardKnight creatures only inside RatKingEncounter. It uses an owner-bound but detached mutable `RoyalDamageRampPower` view model, added through the existing native NPowerContainer.Add path. The source-inspected private owner field binds the native view model; ApplyInternal/Creature.Powers are deliberately never used. Thus this marker is absent from gameplay power lists, combat serialization, buff removal and Cleanse selection. No Cleanse behavior has been decided or changed.

Current and next values come directly from `RoyalDamageRamp.Factor` and the native CombatState.RoundNumber: 40/50/60/70/80/90/100%, capped thereafter. The existing native combat-state event invalidates the display; there is no frame poller, second damage formula or persistent round counter. NPower's own amount label shows the percentage and its normal hover UI displays the localized explanation; at 100% the description explicitly says the increase has ended. The existing native Ritual icon is reused. Container exit unsubscribes the event and drops the view binding. Only 3 new powers.json keys per language were inserted; existing K2/K3 entries were preserved.

## Technical evidence and remaining acceptance

- Offline asset audit: 6 opaque cards, 4 genuine-alpha relics, 8 native variants; source/runtime hashes identical; all 4 protected interiors exact; 130 other existing image assets unchanged. `checks.json` records the result.
- Isolated product build: PASS, 0 warnings/errors, under `artifacts/content-wave-20261102/k5/code-check`.
- Existing isolated patch-activation fixture: PASS, complete mod patch set activated (90 methods, including fixture substitutions). Fixture build has 1 existing nullable warning in DisconnectRegressionChecks.cs:219. This is target activation, not execution of the new native UI.
- K5 did no Godot import/export, game start, installation, commit, push or release. K1 owns the final export.
- Still required: human art review and native card crops; imported Small/Outline/Big appearance; Well animation in 2/3/4-player contests with single/multiple/all Well holders, resize, interruption and exit; ramp status on all 3 boss-fight participants through rounds 1–8 and absent from standalone Kingsguard encounters. Save/network equality and actual UI cleanup are not established by offline previews or patch activation.
