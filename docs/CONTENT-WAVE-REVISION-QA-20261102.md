# Content-wave revision — independent technical QA, 2026-11-02

Owner: K6. Contract: K1's final `CONTENT-WAVE-REVISION-20261102.md`, including the additional exclusion of Royal Cleansing/Cleanse boss ID 25. K2/K3/K4 implementation and K5's final source/art handoff were integrated without product changes by K6.

**PASS: targeted technical integration, official export, packed resource loading and isolated GPU shader compilation/rendering. NOT TESTED: the installed game, native card/Compendium UI, real run save/load, live networking or human artwork acceptance.** This report applies only to the candidate hashes below. The preceding candidate and `CONTENT-WAVE-QA-20261102.md` remain historical and unchanged.

## Candidate for K1

Directory: `artifacts/content-wave-20261102/revision/final-candidate/LaRata/`.
Manifest version is deliberately still **0.9.5**, unpublished and not installed. The candidate DLL is ready for K1's native catalog export.

| File | Bytes | SHA-256 |
|---|---:|---|
| LaRata.dll | 553984 | `b571c4b3816cc10489488340723cf1d8509bf196b2a6e13d3fe78f8e10ecfd09` |
| LaRata.pck | 156504576 | `f71ba1f08029faef938d51558a2b3cfcd374b39644cb9fcba352c59aab7d1fc7` |
| LaRata.json | 675 | `19a17fb91a143584b83e6f8812f1b31379d30bac1e6a01f1502b0613e71e0040` |
| ContentChecks runner | 361984 | `35b31049a6c1302463d1057d5a9410a319294371c11bb8b7c29059ee9fbd89e5` |
| PolicyChecks runner | 71680 | `10cda3f880e432025983cc6f527cd4913ed5fb66df1bc6018b799c877f91e924` |

All evidence below is in `artifacts/content-wave-20261102/revision/qa/`. `source-before.json` records 118 C#/project files before the shared build; they remained unchanged. After K1 confirmed K5's final freeze, `freeze-before.json` recorded 303 source/test/asset files excluding build and Godot import sidecars; they also remained unchanged. `references.json` records the five unchanged native/game/BaseLib/Godot reference hashes. The candidate DLL equals the compiled product DLL used by the ContentChecks output directory.

Builds: **PASS**. ContentChecks Release: zero errors, one existing CS8600 at `DisconnectRegressionChecks.cs:219`. PolicyChecks Release: zero warnings/errors. No obsolete expectations failed in this revision; **K6 made no test-source or product-source changes**. Earlier K6 fixture changes and owner changes remain intact.

## Targeted results

The ContentChecks commands use `dotnet tests/LaRata.ContentChecks/bin/Release/net9.0/LaRata.ContentChecks.dll <flag>`. The policy command uses its corresponding Release DLL and `--royal-only`. Each log has an accompanying `.exit` file preserving the actual exit code. Every final check below returned zero.

| Check / evidence log | Result | What this establishes |
|---|---|---|
| `--november-cards` / `november-cards.log` | **PASS, 455 assertions** | Updated base/upgrades, native effects/commands, supplied play/hook boundaries, card identity/JSON and text. Includes the latest Cleanse exclusion while retaining its player/neutral-booster admission. |
| `--royal-booster-path` / `royal-booster-path.log` | **PASS, 937 assertions** | All ten prizes through the actual fresh merchant purchase → obtain/open → native choice waiter → deck path; packet serialization/replay, owner isolation, checksums, no repeat reward, and pending serialized offer restoration. 60 prize cases across modeled 1/2/4 players and none/all unlocks. |
| `--booster-policy` / `booster-policy.log` | **PASS** | 48 owner/unlock cases, 136 routing boundaries, 50 prize/slot boundaries, 96 seed/replay cases; class 60/30/10, neutral 75/25, unchanged one-per-pack five-way Royal gate, pool filtering, duplicate prevention and saved contents. |
| `--content-wave-relics` / `content-wave-relics.log` | **PASS, 69 assertions plus Reset/await** | Includes 33 Stairwell revision assertions, actual native cost/debit/signals, preview nonconsumption, owner isolation, first-play consumption, legacy saved fields, Remove/Melt; existing Ticket/new relic/Brass/Reset and disabled-statistics integration retained. |
| `--night-relics` / `night-relics.log` | **PASS** | Revised Key, previous Ticket/Bell/Brass/Stopper effects, ownership, limits, saved properties and native hand-cost notification boundaries. |
| `--atc` / `atc.log` | **PASS, 128 owner purchases + 32 non-owner controls** | Constant-first-price ATC behavior with prior counters 0/4, repeated purchases, inflation tiers, Membership/Courier combinations, native sent/debited price, cancellation/affordability, relic loss/reacquisition and serialized counters. The shorter text did not change the tested economy rule. |
| `--followup-cards` / `followup-cards.log` | **PASS** | Revised Thorn Buster and earlier card/Reset integration, 64 royal forecasts, native heal caps and Exposed Crown execution for modeled 1–4 owners. |
| `--royal-only` / `royal-policy.log` | **PASS, 24 Reset boundaries + 192 deck turns** | Exactly 42 active instances, preserved 48 stable identities, exclusion set `[3,20,21,25,26,28]`, draw/reshuffle/conservation, payment states and exact hidden-state/native-RNG replay. Policy simulation only. |
| `--rarity-pools` / `rarity-pools.log` | **PASS** | 66 catalog IDs, 29 neutral player cards at 0/19/10, ten separate Rare Royal prizes, two class cards and 1–4-player modeled filtering; current native small-icon bindings. |
| `--patches-only` / `patches-only.log` | **PASS** | 90 patched methods including desktop fixture substitutions, 42 disabled statistics patch classes, both booster focus hooks and 17 native ENet lifecycle fixture checks. |
| `--metrics-disabled` / `metrics-disabled.log` | **PASS, 94 checks** | Statistics stay inert; native remover choices/cancel/reopen and identity rejection, including pending choices for two owners. |
| `--wave-localization assets/LaRata/localization` / `localization.log` | **PASS, 2082 native formatter calls** | Strict JSON/UTF-8/duplicate/key/token checks and current EN/DE/ZHS tables/providers. No fonts or native rendered layout claim. |
| `--asset-paths <repository>` / `asset-paths.log` | **PASS, 134 actual model bindings** | Card portraits, relic Small/Outline/Big and map resources resolve within mod assets. Packed resource validation follows below. |

Counts overlap and are not an independent grand total. Longer unchanged disconnect/reward legacy suites and the full policy corpus were not repeated. The broad legacy runner, full `RoyalFinaleChecks` entry path and retired active-statistics suites were not run; no repository-wide PASS is claimed.

## Revised behavior checked

- Cards: Cinder Receipt+ 15 Vigor; Royal Cleansing visible name with stable Cleanse identity; Scrap Coronation Draw 1 in both versions; Thorn Buster Draw 1/2 and conditional Vulnerable 2/3 while retaining cost 1/Exhaust; Sewer Pact+ Poison/Doom 8; Smuggler's Dividend on each actual own discard; Venomous Encore Doom 5/10; Death Letter cost 2/1 without Block, preserving recovery/next-turn Energy; Ashen Overture Draw 3; Last Laugh+ 5 Block/Draw 2 with mixed base/upgraded power-instance behavior. These use native models/commands with explicitly supplied combat/play and selection boundaries, not actual game UI card plays.
- Stairwell Key: first-card discount refreshes each side turn, floor zero, owner only; zero-cost, auto-play and X/Stars-X first plays consume the first-play state, while native X payment itself is not rewritten. Consumption occurs before effect resolution, so nested plays cannot reuse it. The retired descending-cost Energy/Draw effect is absent. Native JSON reads old `LastEnergySpent`/`DescendingCount` fields without reserializing them and preserves `TriggeredThisTurn`; full historical run loading is a separate gap.
- Relic Remover is Uncommon; ATC English wording is the requested short text. No K6 changes to these contracts were required.
- Exposed Crown: native execution produces **25/65/97/130 Block** for modeled 1/2/3/4 owners, plus 2 Vulnerable. Solo Dexterity +3 produces 28; ordinary intervention remains 16 Block. The existing native DefendIntent remains nonnumeric; the checked boss preview/fallback and localized description carry the value 25. No general boss scaling change was introduced for this revision.

## Royal booster finding and limits

No generation, native selection, packet, retained-deck or pending-offer defect was reproduced against this source. `BoosterRelics.cs` remains unchanged from the earlier candidate. The natural 512-seed reachability corpus found all ten prizes for each tested party/unlock configuration: 104/512 hits for one player, 108/512 for two, 98/512 for four; none/all unlock variants have identical counts. These correlated fixed corpora are **reachability evidence, not an independent statistical estimate**. The new end-to-end fixture does not cover three players; the existing policy/model pool checks do.

The production contract remains one 20% gate per fresh **Royal** pack, one replacement among its first five cards, with ten eligible prizes selected uniformly (2% for a particular prize before selection); class packs and the two bonus slots do not roll a Royal prize. Saved contents remain authoritative; reopening a saved offer does not reroll it. Under independent fresh-pack trials, zero prizes in five packs has probability `0.8^5 = 32.768%`; absence in a few runs alone is not a defect. The user's actual installed-session cause remains unproven. No chance or booster runtime code was changed by this QA.

## Final PCK, art and shader evidence

K5's final `revision/handoff.json` and both owned style-file hashes match the checked source. Export began only after K1 confirmed the final source/art freeze.

- **PASS — official export:** Godot 4.5.1 `--headless --export-pack LaRata` to the new candidate only; exit zero, no ERROR/WARNING diagnostics. Normal editor/temp access was used to avoid the previous task's sandbox-only export failure. `godot-export.log`.
- **PASS — packed completeness:** 183 mod resources, including 148 textures, three compiled scenes, one audio file, the new shader and all 27 language tables (nine per language). Imported resources match current cache and source MD5, PNG dimensions match, plain files are byte-identical and project settings are nonempty. All 134 model bindings are covered. No docs/tests/research paths or matches to the 59 inspected native-reference asset hashes occur in the package. `pck-validation.json`.
- **PASS — actual Godot resource loading:** all 148 textures load from the PCK; 15 Big/Small/Outline alpha checks cover the five wave relics including the revised Remover; all 27 language files read back byte-identically. Standalone resource project only. `resource-load.json`.
- **PASS — new art/provenance:** all five generated originals match current runtime bytes and handoff hashes; archived predecessors match the K5 pre-revision baseline. Exactly seven expected PNGs changed (four card originals plus Remover Big/Small/Outline); 141 other image files remain unchanged. Remover Small preserves RGB and pixels outside the declared border mask; its white Outline preserves original alpha. The unchanged normal shader and four historical catalog-art snapshots are also verified. `integrity-final.json`.
- **PASS — shader source scope:** the 20 concrete RemixCard aliases select the new style only for the ten prize indices 10–19; other calls retain the default shader. Native Uncommon/Rare banner/border/type-plaque fallback remains unchanged. This is source binding evidence, not actual BaseLib UI rendering.
- **PASS — GPU compilation/rendering:** official Godot 4.5.1, OpenGL 3.3 compatibility, NVIDIA RTX 3080 / driver 617.42, in an isolated initially minimized/hidden QA project. Both packed frame shaders render the same synthetic texture and produce output distinct from the unshaded control and from one another. Transparent borders and 0.75 vertex alpha are preserved. Exit zero, no shader/engine ERROR/WARNING diagnostics. `shader-load.log`, `shader-engine.log`, `shader-check/shader-result.json`, and the three small test-output PNGs.

The GPU check deliberately uses a real rendering backend: headless dummy rendering cannot establish GPU compilation. The harness uses official [Godot 4.5 window modes](https://docs.godotengine.org/en/4.5/classes/class_window.html#enum-window-mode) and [RenderingServer.force_draw](https://docs.godotengine.org/en/4.5/classes/class_renderingserver.html#class-renderingserver-method-force-draw), with a hidden standalone SubViewport. No game executable, native card scene, user profile, controller flow or full card atlas was exercised. Other GPU backends, all native hand/preview/Compendium sizes, rarity readability and human visual approval remain **NOT TESTED**. The generated test images are synthetic fixtures, not altered production artwork.

## Preservation and remaining gates

`integrity-final.json` verifies **579 protected files unchanged**: previous candidate and QA evidence/report, canonical package, existing release ZIP, repository/installed Workshop artifacts and Ideas. K1's changing catalog and revision-scope Markdown were intentionally excluded from the immutable baseline and never edited by K6. The old asset-binding report was restored byte-for-byte after copying the runner's fixed-path output into this revision's QA directory. Installed reference files are unchanged; local `mods` remains empty. No installation, game launch, staging, commit, push or publication occurred.

K6 outputs are this report and the isolated revision QA/candidate artifacts. No new dependencies were installed. Existing owner source and artwork were preserved; no product bug requiring an owner fix was found in the targeted checks.

Remaining acceptance, requiring K1's later exclusive installation/Desktop allocation:

1. **P0:** real Solo save/quit/reload with Stairwell first-play state, Ticket purchase receipt, revised cards and Reset; then separate **2-, 3- and 4-player** sessions with owner-specific results, simultaneous choices, saved offers, supported load-host/re-entry and complete per-peer logs/checksums. Fixture transport replay and native JSON do not establish these outcomes.
2. **P1:** native hand/enlarged/Compendium rendering of all ten Royal prizes beside ordinary controls and upgraded variants, native rarity colors, revised Remover icons and EN/DE/ZHS layout. Human acceptance of the five new motifs is still pending, including Pawn Promotion's intended subject.
3. **P1:** previous wave's Well animation/ramp UI and real boss-hand/balance acceptance remain open; the present technical checks do not resolve those historical gaps or the separate Act-4 multiplayer incident.

The candidate is technically ready for K1's catalog export and subsequent explicitly authorized acceptance steps; it is not a blanket release, network or visual approval.
