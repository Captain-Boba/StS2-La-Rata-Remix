# Content wave technical QA — 2026-11-02

Owner: K6. Scope: K1-authorized combined technical acceptance of the K2/K3/K5 November wave. **PASS for the targeted technical checks below. Live gameplay, UI, real save/load and network acceptance remain NOT TESTED for this candidate.** No installation or release approval is implied.

## Candidate and reference identity

Candidate: `artifacts/content-wave-20261102/final-candidate/LaRata/`. Manifest version remains **0.9.5** as instructed; this directory is an unpublished technical candidate, not the public 0.9.5 release. Logs and machine-readable evidence are under `artifacts/content-wave-20261102/qa/`.

| File | Bytes | SHA-256 |
|---|---:|---|
| LaRata.dll | 553984 | `ff48fa4eef5061fac1cbef817d573f264f2c19a9a60fc715bc5f9ede0cda6a70` |
| LaRata.pck | 156075084 | `fc973f6d5604bd8816bd6f48936ab8f9e17c3fc57a6093c0202223692308e8e3` |
| LaRata.json | 675 | `19a17fb91a143584b83e6f8812f1b31379d30bac1e6a01f1502b0613e71e0040` |
| Final LaRata.ContentChecks.dll | 340480 | `c3b9bec435f122a15f6c94681551d83ac31abe583809250faca63eb8ea0a76de` |

The candidate DLL equals the product DLL referenced by the test output directory. `source-before.json` and `source-final.json` identify the shared source and asset snapshot. The five reference files in `references.json` remained byte-identical during QA:

| Reference | SHA-256 |
|---|---|
| Installed sts2.dll, public v0.107.1 contract | `a1f9e653f1e28e4076558fee1e60d218619cb7e057b887c6417f62c62c6d7a52` |
| Installed GodotSharp.dll | `0e4897ecdfb31456a97c7d8028dfb8d7dbdc632e2f73fc9b438d7b266a139289` |
| Installed 0Harmony.dll | `ef1898322c9f5c86dc1b0758b272a9c440823b4a41ca9a0b82a3aa6b3d206387` |
| BaseLib.dll, 3.4.7 | `55863ca6adc30a61a7544874e157a519ffe148ccf2b18025947609c4bff813ce` |
| Official Godot 4.5.1 console | `f0336c8b2ae039d39767a64c416f01b1b49d95d8dde87724012874dd4be1c1a8` |

## Results and evidence boundaries

Release build: **PASS**, zero errors, one existing CS8600 nullable warning in `DisconnectRegressionChecks.cs:219`. Evidence: `build-final.log`. The targeted runner commands use `dotnet tests/LaRata.ContentChecks/bin/Release/net9.0/LaRata.ContentChecks.dll <flag>`. Successful suites were not needlessly repeated after changes confined to unrelated fixtures.

| Check | Result and measurable evidence | Boundary |
|---|---|---|
| `--november-cards` | **PASS**, 328 assertions: six new cards and Midnight Rehearsal, base/upgrades, native effects and card JSON. Royal Shuffle preserves its stable RoyalReplication identity and checks snapshot copying/native shuffle behavior. | Supplied combat/OnPlay boundary, predetermined selections and viewport-timing substitution; not full card-play UI. |
| `--content-wave-relics` | **PASS**, 35 assertions plus existing Reset/await checks. Counterfeit first creation, multiple-owner Plague Testament at actual death, Usurper actual Strength gain, uncapped Brass payout. Ticket price zero at zero Gold; preview/cancel do not consume; successful native purchase/receipt, owner attribution, merchant subrooms, Courier restock and concurrent gating. Reset actual debit/credit, EndedTurn and disabled statistics. | Native commands, merchant/packet payloads and supplied desktop/combat boundaries; no sockets or full disk save. |
| `--night-relics` | **PASS**, Ticket/Key/Bell/Brass/Stopper contracts, owner rejection, caps, native effect commands, cost-change signals and saved properties. Brass pays all supplied 20 blocked damage once; metrics remain zero. | Damage results and direct hook entry supplied where stated in the log. |
| `--followup-cards` | **PASS**, Thorn Buster, Graveyard Shift, Court, Shadow, Alchemy, Reset, identity roundtrip, Reset await boundaries; 64 royal forecasts with modeled 1–4 owners and final heal caps. | No full boss-hand lifecycle, actual rendered intent or full disk save. |
| `--patches-only` | **PASS**, 90 patched methods including fixture substitutions; 42 statistics patch classes inactive; 17 ENet lifecycle checks; both booster focus getters patched. | Patch activation/native host callback fixtures, not live transport or controller behavior. |
| `--metrics-disabled` | **PASS**, 94 checks, native poison/36 previews, inert old/new statistics, relic JSON and remover choice identity/cancel/reopen contracts. | Predetermined selection UI and no-op singleplayer transport. |
| `--royal-potions-ramp` | **PASS**, 120 checks, native potion commands/JSON and existing damage-ramp hooks; directed intent forecasts. | No rendered ramp indicator or real encounter session. |
| `--booster-policy` | **PASS**, 48 owner/unlock/model cases, 136 production routing boundaries, 50 prize/slot boundaries, 96 seed cases with exact native RNG replay. Class weights 60/30/10, neutral slots 75/25, one five-way royal gate per pack; saved legacy/current contents and insufficient-pool failure checked. | Controlled boundary inputs are not a statistical frequency estimate or live pack opening. |
| `--rarity-pools` | **PASS**, 66 catalog IDs, 29 regular neutrals at 0 Common/19 Uncommon/10 Rare, 10 separate Rare prizes and two class cards; modeled 1–4 player filtering. New relic native small-icon bindings included. | Catalog/pool models; no Compendium rendering. |
| `--wave-cards` | **PASS**, 14 exact base/upgrade contracts, booster cases and five native intent-hook forecasts. | Metadata and supplied announcements, with overlap with the focused suites above. |
| `--disconnect-regression` | **PASS**, 314 assertions, zero failures. Native reward identity/sequence, checksum and serialization. | No real disconnect/rejoin session. |
| `--shop-booster-regression` | **PASS**, 224 assertions, zero failures. Native shop purchase, packets, choice waiters and checksums. | No UI or network session. |
| `--wave-localization assets/LaRata/localization` | **PASS**, 2082 actual native formatter calls, duplicate/UTF-8/JSON and EN/DE/ZHS key/token parity, empty model providers. | Fonts, line wrapping, final rendered text and engine/BaseLib language discovery remain untested. |
| `--asset-paths <repository>` | **PASS**, 134 actual model portrait/relic/map bindings resolve within mod assets. | Filesystem binding check; separate packed loading below. |

Raw logs use the flag name without `--`, except `localization.log`; accompanying `.exit` files preserve actual process exit codes. Counts overlap and must not be summed into a purported independent test total. This is not a full legacy-suite PASS: active-statistics suites such as `--wave-payment` and `--wave-deferred` were not run or redefined to imply that retired statistics remain active. `--atc` and the full policy runner were not newly executed in this wave.

## Fixture repairs and failed attempts retained

K6 changed **only three test source files**, with no product-code or artwork edits:

- `NightRelicChecks.cs`: replace the retired Ticket exhaust-recovery expectation with the new Rare/free-first-card-per-merchant contract; assert disabled metrics remain zero while checking actual Bell/Brass/Stopper effects. Remove the obsolete 12-Gold expectation, retain once-per-combat. Merchant renewal uses a real additional shop subroom in current map-point history.
- `RarityPoolChecks.cs`: use the established `relic_native` small-icon path and include the three new relics. K2's existing 29-neutral/66-ID catalog changes were preserved, not authored by K6.
- `RoyalFollowupChecks.cs`: initial-round Grave Levy damage respects the existing 0.9.5 40% ramp: floor(18 × 0.4) = 7, leaving 93 HP. Healing/cap assertions remain unchanged; ramp behavior also passes its independent focused suite.

Initial `night-relics-before` **FAIL** exposed obsolete Ticket, statistics and Brass-cap assumptions. `night-relics-first` **FAIL** was a K6 fixture mistake: appending another map point with the same location did not create a new merchant key; corrected to the native additional-shop-subroom representation. `rarity-pools-first` **FAIL** was the stale small-icon path; `followup-cards-first` **FAIL** was the stale full-damage expectation. Their original logs/exit codes are retained. The final versions of all three suites pass. No runtime behavior was changed to satisfy an obsolete assertion.

## Packed resources and provenance

Official export: **PASS**, `godot-export.log`, exit 0 and no ERROR/WARNING lines. The first sandboxed export returned 0 but contained editor/config/temp-file errors and was therefore **FAIL**, not accepted. `godot-export-first.log` retains the diagnostics. The authorized repeat used the official executable with normal editor/temp access and wrote only the separate candidate package, not canonical release output.

`pck-validation.json`: **PASS**, 182 mod resources: 148 textures, three compiled scenes, one audio asset and the remaining plain files, including all **27 localization tables (nine per language)**. Imported assets match the current Godot cache and source MD5; PNG dimensions match; compiled scenes match current exported resources; plain files match source bytes. All 134 model bindings are included. Packed project settings are nonempty. No docs/tests/research trees or matches to the 59 inspected native-reference asset hashes are packed. This comparison is scoped to those reference hashes, not a universal ownership proof.

`resource-load.json`: **PASS**, official standalone Godot mounts the PCK and loads all 148 textures, checks dimensions, alpha for the four relics' 12 Big/Small/Outline resources, and reads all 27 language files byte-identically. This is a minimal resource-check SceneTree, not a game start or scene/UI acceptance. The first attempt crashed before checking resources because sandbox access to the default `user://logs` directory failed; `resource-load-first.log` and its nonzero exit remain **FAIL** evidence. The successful repeat uses Godot's documented `--log-file` option to write within QA. No log filtering was used.

`integrity-final.json`: **PASS**, all ten card/relic originals match K5's recorded hashes and generated source files, with six 1448×1086 opaque card images and four transparent relic originals. Eight Small/Outline variants match the handoff hashes; small RGB is unchanged, pixels outside each declared border mask are unchanged, and each white outline retains original alpha. Ticket retains 111477 enclosed transparent pixels. The 130 preexisting non-Ticket image assets match K5's baseline. **Human artwork approval, native crop quality and readability remain NOT TESTED.**

Resource QA reuses the existing official Godot export and installed GodotPckTool/Python/Pillow/NumPy workflow; no dependency was installed. Reproduction helpers are `qa/check_pck.py`, `qa/check_integrity.py` and `qa/resource-check/check.gd`. They write QA evidence only.

## Source review and remaining acceptance

Source inspection confirms the Well winner is still selected by the existing seeded rule before presentation; K5's animation/cancellation is presentation code. The ramp indicator is a detached native power view, with owner/event binding and cleanup, not an applied `Creature.Powers` entry or a new persistent gameplay counter. Existing ramp rules were exercised separately above. The UI binding/animation paths themselves have not run in the game's scene tree.

Prioritized follow-up, only after K1 assigns the installation/Desktop window and authorizes live work:

1. **P0 — Solo candidate smoke and real save/load:** install an explicitly identified candidate in an exclusive window; exercise actual card plays/selections, Ticket cancel then purchase then reload/new merchant, Reset payment/end-turn, new relic triggers and persisted fields. Capture before/after state and real native checkpoint reload. Expected: exact owner/resource changes once, unchanged IDs and no duplicated rewards or free purchases after reload.
2. **P0 — Separate 2-, 3- and 4-player sessions:** record per-peer versions/hashes, seed, IDs and full logs. Exercise simultaneous Ticket/remover/booster selections, multiple Plague Testament owners, Reset payments across owners, save/quit/load-host and same-ID re-entry through the supported native path. Compare action/choice IDs and synchronized states at each boundary. No missing/duplicate result, unresolved waiter or state divergence is acceptable. Model 1–4-owner tests do not replace any of these three session sizes. Real Steam/network behavior needs its own evidence; prior ENet teardown acceptance does not prove this content wave or resolve the separate Act-4 incident.
3. **P1 — Well and ramp Live-UI:** Well with one/multiple/all holders at each multiplayer size, selection overlap, cancellation, room exit and resize; winner/result must agree on every peer and award once. Ramp icons for all three final-encounter participants across rounds 1–8, matching current/next percentages and 100% cap; none in standalone guard encounters; no gameplay/save/checksum mutation from the view. Include controller focus and cleanup.
4. **P1 — Visual/localization/content acceptance:** actual Compendium inclusion, boss labels, six card crops/base/upgrades, new relic Small/Outline/Big views and EN/DE/ZHS layout. Human approval of the new art remains separate from byte/provenance checks.

Statistical booster frequency sampling was **NOT TESTED** in this wave; the production boundary and RNG replay checks above establish the selector contract only. A few runs without a Royal card do not prove a probability error. Full natural-run balance, all combinations, external-mod compatibility, installation and release smoke checks are also **NOT TESTED**.

## Preservation and handoff

All 164 files recorded in `protected-before.json` remain byte-identical, including the canonical package, existing root release ZIP, repository Workshop files, K1's content catalog and `Ideen - La Rata.md`. The three installed Steam Workshop files also remain byte-identical. The local game `mods` directory remains empty; no candidate installation was performed. The older asset-binding report was restored byte-for-byte after the runner's fixed output path was copied into this QA directory. No test game processes remain from this task.

The source baseline covers 290 files; its only changed source files are the three test files listed above. Existing K2/K3/K5 changes and the preexisting dirty Ideas file were preserved. This report and ignored QA/candidate artifacts are the remaining K6 outputs. No staging, commit, push, Git history, public version, release or Workshop publication changes were made.
