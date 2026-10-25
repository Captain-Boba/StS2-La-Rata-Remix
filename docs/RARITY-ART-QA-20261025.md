# Rarity, booster policy and six relic images: local 0.9.4 QA

Installed on **2026-10-25 at 15:45:21 Europe/Berlin**, after the user's explicit policy/art approval and fresh checks showing no game process. The previous ATC/card-text runtime and ZIP are backed up. **838 files** under the current SlayTheSpire2 user-data directory are byte-identical before and after installation, with no added/removed files. No game start, profile restoration, commit, push or publication occurred.

## Approved behavior

- 23 regular La Rata neutral cards: **0 Common / 16 Uncommon / 7 Rare**. Umbral Stitch, Crown Exchange, Midnight Rehearsal, Gutter Feint, Ragpicker's Guard, Cinder Receipt, Venomous Encore and Graveyard Shift changed from Common to Uncommon. Costs, effects, upgrades and IDs are unchanged.
- Five Royal/La-Rata main slots: explicit **75% Uncommon / 25% Rare**.
- Two mixed neutral bonus slots of every pack: explicit **75% Uncommon / 25% Rare**, with eligible native plus La Rata cards and no origin quota.
- Five class main slots: **60% Common / 30% Uncommon / 10% Rare**.
- Royal-only **20% single gate per pack**, at most one uniformly selected prize from ten, replacing one uniformly selected main slot. Seven total distinct cards, no prize in bonus slots, no guaranteed Rare.
- Empty supported categories renormalize; policy-ineligible Common cards cannot satisfy neutral minimum pool size. Native unlock/SP/MP eligibility, saved contents, owner identity and deterministic selection remain in use.
- Six visually approved relic images are included. The ATC first-price fix and concise EN/DE/ZHS card texts remain present. K2's previously source-only unused text cleanup is now included in the rebuilt DLL.

## Focused evidence

| Check | Result and actual scope |
| --- | --- |
| Source delta | Exactly eight rarity changes in two files; no cost/effect/upgrade edits. `BoosterRelics.cs` adds explicit policy routing/filtering; saved `Open` method is byte-equivalent to its pre-change source. |
| Rarity/catalog | PASS: 60 unique catalog IDs, 23 neutrals at 0/16/7, eight base+upgrade rarity mappings, native U/R banner materials, ten separate Rare prizes, two separate class cards, existing base/upgrade assertions. |
| Selector boundaries | PASS: exhaustive 100 integer outcomes for 60/30/10 and 75/25; neutral Common injection rejected, insufficient/zero/duplicate pools and single surviving U/R groups checked. |
| Actual pack routing | PASS: 136 directed main/bonus boundary cases across six pack types; all five boss-gate outcomes and all 50 prize/replacement-slot combinations. These exercise production routing, not merely weight constants. |
| Replay and eligibility | PASS: 96 seeded six-pack cases plus 48 cases spanning modeled 1/2/3/4 players and none/all unlocks. Seven distinct eligible IDs, ownership and native RNG counters/order repeat exactly. These are models, not network clients. |
| Saved contents | PASS: six pack types with legacy five/new seven stored cards, reversed order and an upgraded card roundtrip through native SavedProperties JSON and native card loading. Actual `Open` restores all saved cards without calling the generator, then defers at a simulated combat-ending boundary. Not a real checkpoint/session reload or interactive pack reopening. |
| ATC regression | PASS again on the rebuilt DLL: 128 public native ATC purchases, 32 native non-owner controls, prior counters/discounts/Inflation, cancellation/gold boundaries, loss/reacquisition; plus three no-UI-refresh purchases. UI selection/desktop services substituted. |
| Locales and patch activation | PASS: EN/DE/ZHS validation, 861 native formatter calls and complete Harmony activation. All packed locale files are byte-identical to the previously installed corrected text. |
| Assets | PASS: six approved original=draft=runtime PNG hashes, RGBA 1254x1254, alpha bounds and previous-asset archives. Existing model paths resolve to those six files. |
| Native import/PCK | PASS: headless Godot 4.5.1 mounts the prepared PCK and loads all six textures. Every pixel equals the result of native `Image.fix_alpha_edges` applied to the approved source. PCK entry sets remain identical (220 entries); exactly six CTEX files differ from the ATC/card-text PCK. Locale, scene, audio and other packed resources are unchanged. |

The first raw PNG-to-CTEX equality check exposed normal alpha-edge processing, not source modification. Alpha is identical; RGB changes only below alpha 20. Complete raw RGBA equality is **not** claimed. This matches [Godot 4.5.1's alpha-edge implementation](https://github.com/godotengine/godot/blob/4.5.1-stable/core/io/image.cpp#L3678-L3740). See also K5's `art-prompts/relic-redesign-20261025/IMPORT-PROCESSING.md`. No import flags were changed to conceal the difference. The native expected-output check establishes exact import correctness; the static 48/32-pixel contact sheet is separate from in-game visual acceptance.

Build: zero errors, two NU1900 warnings from unavailable NuGet vulnerability metadata. This is not a fresh dependency-security audit. No new dependencies were introduced.

## Reproduction and hashes

```powershell
dotnet build tests/LaRata.ContentChecks -c Release --no-restore
dotnet tests/LaRata.ContentChecks/bin/Release/net9.0/LaRata.ContentChecks.dll --rarity-pools
dotnet tests/LaRata.ContentChecks/bin/Release/net9.0/LaRata.ContentChecks.dll --booster-policy
dotnet tests/LaRata.ContentChecks/bin/Release/net9.0/LaRata.ContentChecks.dll --atc
dotnet tests/LaRata.ContentChecks/bin/Release/net9.0/LaRata.ContentChecks.dll --wave-localization assets/LaRata/localization
dotnet tests/LaRata.ContentChecks/bin/Release/net9.0/LaRata.ContentChecks.dll --patches-only
```

Evidence directory: `artifacts/rarity-art-pending-20261025/`. Its name reflects the initial preparation before policy approval; current status is installed. Important files: `rarity-only.diff`, `source-rarity-audit.json` (initial pre-policy evidence), `preinstall-audit.json` (final approved policy), `*-final.log`, `approved-source-assets.json`, `packed-assets-audit.json`, `check_native_import.gd`, `native-import-audit.json`, `installation-audit.json`, `profiles-before.json`, `profiles-after.json`, `final-package-audit.json`, `source-snapshot.json`.

| Installed/package file | SHA256 |
| --- | --- |
| LaRata.dll | `D354E92F9197DDC123B950BC24CB75E383BF02C4601FCA89917D7AAFD4B8D0C3` |
| LaRata.pck | `C6B307CA6C35038828EDF9A45BFCC7206B09F21F3F9023DEAB791138291AA187` |
| LaRata.json | `1A2834DB1A10732A1B64E880E7D7077F4BE83BA81B1667AE49983996DF13455D` |

Canonical `artifacts/package/LaRata` and `artifacts/LaRata-0.9.4.zip` are refreshed from the checked preparation. ZIP integrity and all entry hashes are recorded separately. Product source hashes and approval manifests are frozen in the source snapshot.

## Acceptance limits

The previous 8/8/7 catalog and all-slots-60/30/10 booster PASS results are historical. Their fixtures have been adapted; this report supplies fresh targeted evidence for the newly approved contract. The native RNG range changes may change **newly generated** offers for old seeds; same-build replay is checked, while stored offers remain authoritative.

No statistical sampling claim is made: exhaustive bounded integer outcomes establish the exact configured probabilities. A few runs without a boss card are not a failure. Actual appearance frequencies after five draws, prize replacement, eligibility and category depletion must not be confused with a per-slot independent guarantee.

No new live UI, balance/gameplay, real save/load or network acceptance occurred. In particular, the existing candidate 06 strict ENet disconnect FAIL remains unresolved. Human design approval, native importer correctness, technical model tests and live acceptance are separate evidence levels.
