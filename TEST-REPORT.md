Release 0.9.5, 2026-10-27: [Fixes & Potions](docs/UPDATE-0.9.5.md). The focused two-client local ENet playtest matched 20 native checksum contexts, exercised Silent Poison in the royal fight and completed an orderly disconnect without network exceptions. Both clients still logged engine resource diagnostics on shutdown; full-log cleanliness and broad Steam multiplayer acceptance are not claimed. The versioned release is rebuilt and checked separately. Reports below describe historical candidates and installations.

Current installed 0.9.4, 2026-10-25: [rarity, explicit booster policy and six relic-image QA](docs/RARITY-ART-QA-20261025.md). Fresh targeted native model/policy/serialization/import checks pass; package/install hashes agree and all 838 user-data files are byte-unchanged across installation. No game started. The earlier strict ENet disconnect FAIL and live acceptance gaps remain open. Older reports below are historical.

Installed local 0.9.4 hotfix, 2026-10-25: [ATC constant first price and concise Discard/Exhaust text](docs/ATC-FIRST-PRICE-20261025.md#installed-follow-up-atc-plus-concise-card-text). User-authorized installation completed with fresh zero-process checks, backup and identical installed/package hashes. Tested ATC DLL retained; new PCK carries four card descriptions per language without optional quantity wording. Native locale/formatter checks pass. No game started; live UI, real checkpoint/network acceptance and the prior strict ENet disconnect FAIL remain open. Preparation and older reports below are historical.

Current ATC correction (2026-10-25): [constant first-price contract and focused evidence](docs/ATC-FIRST-PRICE-20261025.md). Technically checked local 0.9.4 package; installation/live acceptance pending the active user game ending safely. Earlier increasing-price ATC PASS results below tested the WRONG requirement and do not validate the intended constant first price. Historical candidate 06 disconnect FAIL remains open.

Local candidate 0.9.4, 2026-10-25: [development and final QA evidence](docs/DEV-WAVE-20261025.md#final-local-qa-handoff-candidate-06-2026-10-25). Targeted native tests and packed resources pass. Two actual local ENet clients exercised changed relics/cards, Reset refunds, a real completed-combat save/load and boss actions. Candidate 05 matched 36 native/private-ledger points; candidate 06 matched 33. The candidate 06 **strict full-log gate remains FAIL** because the host logged ENet errors after the client window was closed. No observed hang/crash/save-write failure, but root cause and native exit codes remain unverified. User profiles/settings were restored and hash-checked; final candidate 06 remains locally installed. This is not release approval, full visual/balance acceptance or current three-/four-client coverage. Historical reports follow unchanged.

Release 0.9.3: [Double Boss Spoils](docs/UPDATE-0.9.3.md). Release build, native content checks and seeded policy checks cover boss loot, Uncommon Barricade, Thorns weighting and repeated-removal prices. Native reward fixtures cover 1–4 players, actual card/potion population, native Statue hooks, ownership and RNG/odds snapshot replay. Desktop UI bindings are stubbed; this is not live gameplay, full save/load or network acceptance. See the release notes for exact boundaries.

Release 0.9.2: [Royal Rebalance](docs/UPDATE-0.9.2.md). Build, native content checks and seeded policy checks cover the reduced boss HP, 23-card active deck, six-card intervention pool and revised reinforcement exclusions. The wider hidden intervention encoding preserves legacy decoding. Potion-history tooltip layering is implemented but has no fresh visual acceptance. No live gameplay or network test was performed for these changes. Historical results follow.

Release 0.9.0 packages the implementation through 0.8.7. See [the consolidated release notes](docs/UPDATE-0.9.0.md). Compilation, native model/serialization/policy and headless resource/layout checks are the release gates. Fresh live gameplay, real save/load, UI/animation acceptance and real four-player synchronization remain open. No active game is launched for publication. Historical results follow.

Local test update: [0.8.7 The king and his court](docs/UPDATE-0.8.7.md). Native model, card-upgrade/save, encounter-RNG, deck-policy and headless Godot animation/layout checks pass. No active gameplay, real save/load, UI acceptance or live multiplayer test was performed. The much stronger boss balance remains unvalidated; previous in-game results below do not validate 0.8.7.

Local test update: [0.8.6 royal map and history layout fixes](docs/UPDATE-0.8.6.md). The owner reported a blocked final campfire and oversized royal-history icon in 0.8.5. Headless Godot reproduced both causes; corrected geometry, fixed icon sizing and imported masks pass focused checks. Release build and native content checks pass. No active game, save-load or multiplayer test was performed; in-game visual/input acceptance remains pending. Earlier results below are historical.

Local test update: [0.8.5 solo variety and presentation](docs/UPDATE-0.8.5.md), following [0.8.4 first-playtest feedback and starting class packs](docs/UPDATE-0.8.4.md). The owner's solo log confirms starting-pack acquisition and active encounter plans; their screenshot identifies an off-center native button. The new build has code/model/policy and asset checks only. Assistant-driven playtests and Workshop publication remain paused. This update has no in-game visual or real multiplayer acceptance. The results below are historical and do not validate the new build.

Current update: [0.8.2 Gifts from the throne](docs/UPDATE-0.8.2.md), tested 2026-10-18 on v0.107.1 with BaseLib 3.4.7. Fresh-run solo checks covered all three starting relics, five potion slots, Cleanse, Emerald Ward and same-turn Bottled Echo. Four local ENet peers exercised unanimous bonus entry, full-screen throne artwork, Ancient healing/rewards, Kingsguard stats/heals, both Emerald Ward versions, a sequential four-recipient goblin visit and Cleanse after the splash. All 31 native checksum values and normalized contexts matched in the recorded gameplay window. The saved completed bonus Ancient reloaded on all four peers with rewards retained; this is not a mid-combat reload test.

The full first co-op host log contains native ENet disconnect errors after clients were manually closed, followed by engine resource-cleanup diagnostics. Original logs are retained; the strict full-log error gate is not claimed to pass. Gameplay prefix snapshots ending at checksum 30 pass separately. Tests used isolated Steam-off profiles, with no real Steam saves changed. Other Workshop mods, all 58 live potion effects/choice screens, full boss balance and four-machine Internet play remain outside this test. The corrected Energy-icon DLL is installed. Detailed logs, observations and limitations are linked above.

## Historical update summaries

Previous update: [0.8.1 Deeper chaos](docs/UPDATE-0.8.1.md). 384,000 encounter policies across four acts and all 16 player-card upgrades passed. No in-game testing or installation at that stage; the active co-op session was left alone. Historical evidence follows.

Current update: [0.8.0 Royal cards and boosters](docs/UPDATE-0.8.0.md). The policy suite now covers the 20-card boss deck. The new content suite uses native models, upgrades and JSON saved-property roundtrips. Original evidence below remains historical. Game/UI/audio acceptance and four-peer combat checks remain pending while interactive tests are paused.

Previous update: [0.6.1 starting Bowler Hat](docs/UPDATE-0.6.1.md). Native new-run/save paths reviewed and installation hashes matched; gameplay verification remains pending while UI tests are paused.

Current update: [0.6.0 party-sized chaos](docs/UPDATE-0.6.0.md). 240,000 encounter policy cases passed; native-power gameplay, new formations and four-peer synchronization remain untested while UI tests are paused.

Current update: [0.5.2 goblin animation and 0.5.1 Cleanse](docs/UPDATE-0.5.1.md), following [0.5.0 technical checks](docs/UPDATE-0.5.0.md). Assistant-driven interactive tests remain paused. Owner observations identify a third-party compatibility failure and a too-brief goblin appearance; neither is full gameplay or multiplayer acceptance. Previous evidence: [0.4.0 partial solo test report](docs/UPDATE-0.4.0.md) and [0.3.1 visual and RPS report](docs/UI-TEST-REPORT-0.3.1.md). The report below preserves the 0.3.0 evidence and original hashes; it does not validate later releases.

# La Rata Remix 0.3.0 — technical test report

Date: 2026-10-16, Europe/Berlin. This report distinguishes implementation, local runtime evidence and remaining playtesting. Historical 0.2.0/0.2.1 evidence is retained in docs/TEST-REPORT-0.2.1-HISTORICAL.md.

## Environment and scope

Game v0.107.1, commit 59260271, Steam build 23811903; Windows x64; .NET 9 target. BaseLib 3.4.7 is now required for custom content, localization, animations and native save/network fields. Official Godot 4.5.1 exports mod-owned resources. Original game assemblies/assets are referenced, not redistributed or edited.

The release contains interactive RPS, rare co-op Well, a unanimous optional bonus invitation, YOU → ARE → NOT → PREPARED → La Rata, a four-action custom boss, preservation of the regular victory on bonus defeat, and the native Architect ending after bonus victory. Previous director, gold, potion-slot, map-panel and final-duo behavior remains included.

Tests used isolated Steam-off profiles (Solo 2000; host 1; clients 2101–2103), native ENet and the native console. Real Steam profiles, Workshop originals and Steam launch options were not changed. Console travel/win/die/godmode accelerated specific functional checks. These are not natural full-run balance tests.

## Four-player RPS and Well

Logs: artifacts/logs/20261016-012118-Host.log and 20261016-012130-Client1.log / Client2.log / Client3.log.

- Four contenders privately selected hands for Orichalcum. All-Rock and Rock/Paper/Scissors rounds tied. Subsequent rounds eliminated losing hands; eliminated players watched rather than voted. The final player 2101 received the relic through the native award flow.
- Every peer recorded the same five rounds: 0,0,0,0; 0,1,2,0; 0,0,0,2; 0,1,1; 0,2. Native consolation behavior was retained.
- One Well holder automatically won Juzu Bracelet against three nonholders (winner 1).
- Two Well holders contested Game Piece; every peer recorded the same seeded winner (2101). The native random selection gives equal chances, but this one observation is not a statistical validation of 50/50.
- Seven native combat checksum IDs/values/contexts matched across the associated debug boss encounter. RPS agreement is separately evidenced by identical contest logs and observed awards; combat checksums alone do not prove RPS.
- The native fight animation replays the recorded rounds after the interactive reveal. Functional, but redundant and slower than ideal.

Comparison result: artifacts/night-20261016/checksums-rps-well.txt. It explicitly classifies 12 engine resource-leak diagnostics per process after the FMOD shutdown marker; original logs retain them. No gameplay exceptions were found in this scenario.

## Invitation and save/network consistency

An old 0.2.x run initially exposed a BaseLib serialization mismatch: an absent custom integer loaded as -1 locally but zero over a native multiplayer save packet. La Rata now uses zero as the same legacy sentinel on both paths. Failed logs (013849/013859) are retained.

Retest logs: 20261016-014144-LoadHost.log and 014154-Client1/2/3.log. All four loaded the same Aeonglass + Waterfall Giant finale, HP 2662/749, and initial checksum 1406466945. A three-yes/one-no invitation resolved to decline and all clients reached the Architect. This scenario has one combat checksum, not broad combat coverage.

Reloading the pending invitation and choosing yes on all four peers recorded votes 0,0,0,0 and entered bonus act index 3. YOU resolved Hungry Mushrooms; all received Big Mushroom. ARE resolved a normal unknown-room Shop. NOT resolved Devoted Sculptor. PREPARED was a guaranteed Shop. The custom boss was entered from the native map.

A real bug occurred at the first four-player bonus combat: the game's multiplayer scaling table only accepts act indices 0–2. A scoped adapter now maps RatDomain's bonus index to the native final-act table. It does not replace native HP/block rules. Failed logs 014643/014707 and the NOT checkpoint are retained.

Retest logs: 20261016-015955-LoadHost.log and 020009-Client1/2/3.log. The NOT checkpoint loaded Devoted Sculptor with 863 HP, native Artifact and Plating; the same director policy appeared on all peers. Native test win, PREPARED shop and boss entry completed. Two native checksums matched. Result: artifacts/night-20261016/checksums-bonus-entry.txt, with shutdown-resource diagnostics noted separately.

## Four-player boss and normal ending

Logs: 20261016-020859-LoadHost.log, 020920-Client1.log, 020921-Client2.log and 020921-Client3.log.

- Native save reload restored bonus act, boss coordinate, five potion slots and all four players. All initial checksums were 3083077479.
- La Rata had 2236 HP (430 base with native four-player final-boss scaling); rats had 109 and 98 HP. Native positions and intent display were observed at 1282 × 752.
- Royal Decree applied 2 Weak and 2 Frail to all living players. Tax Collection used 20 damage, Scepter Strikes 7 × 3. Passive clients died during the functional sequence; the host used godmode from round two.
- Exposed Crown's tooltip showed 130 Block and self 2 Vulnerable. Execution produced exactly 130 Block and 2 Vulnerable. The tooltip uses native block modifiers, not a fixed solo number. The cycle returned to Royal Decree.
- Native win ended the encounter. All four pressed Continue, reached the Architect, completed the native dialogue and saw the ordinary victory endscreen.
- Saved history has win=true and was_abandoned=false (artifacts/night-20261016/coop-bonus-win.run).
- All 22 recorded native checksum IDs/values/contexts matched. No state-divergence message or gameplay exception preceded manual client closure.

IMPORTANT: the full-log error gate failed after clients were closed from the completed endscreen: the host emitted native ENet 'Peer not connected' and null-peer cleanup diagnostics. All processes also emitted engine resource-leak diagnostics on shutdown. The checksum-only result is recorded explicitly in checksums-boss-ending.txt; this is not a claim of a wholly error-free session or validated disconnect/rejoin behavior. The error gate was not weakened to hide ENet errors.

## Solo bonus behavior

Earlier solo tests traversed the bonus events/shop and reloaded native checkpoints. The actual boss checkpoint restores 430 HP and rats 20/18. The first full moveset/Architect test (011324-Solo.log) exposed unsupported act-four character-unlock epochs and animation tracks after native visual reparenting. Both were fixed: bonus-only epoch bypass, and AnimationPlayer beneath the reparented Visuals node.

Latest defeat retest: 20261016-022506-Solo.log. Restored the boss checkpoint, executed native die, observed 'Regular victory secured' and the English explanation that the bonus loss preserves progression. History remains win=true, was_abandoned=false (solo-bonus-loss-final.run). No gameplay ERROR/exception occurred. Screenshot: screenshots/solo-bonus-loss-final.jpg. Older win/loss evidence is retained with its original diagnostics rather than relabeled as final-build evidence.

## Automated checks and remaining gaps

The existing 120,000 native-Rng policy cases passed during this work: replay, budgets, modifier counts/caps, offense exclusions and reinforcement exclusivity. They do not cover UI or all enemy mechanics.

Still requires human playtesting: natural difficulty and pacing, all characters, two/three-player runtime sessions, genuine four-machine Steam/Internet play, disconnect/rejoin during choices, controller/accessibility and other resolutions. Well's natural shop/reward appearance and three/four simultaneous Well holders were not observed at runtime. Native pool filtering and seeded selection are implemented; they are not substitutes for those acceptance tests. No claim is made that every finale/helper combination or every installed mod interaction is tested.

Build, compatibility and final file-hash results are recorded below.

## Installed-mod compatibility checks

Temporarily staged exact copies of installed Workshop content under mods_STEAMTEST/LaRataCompat20261016, with Steam disabled and only test account 2000. BaseLib 3.4.7 and La Rata were combined with Unified Save Path Forked 1.0.4, Limitless 1.1.7, Who Carried 1.3.0, More Ironclad Animations 1.9.58-layer.1, More Defect Animations 0.15.9 and More Necrobinder Animations 0.1.23. No extra runtime dependency was added to La Rata for these tests.

- Loading a La Rata checkpoint created without Limitless FAILED in Limitless's ContextManager.UncappedEnchantmentsEnabled (NullReferenceException). Log: 20261016-022720-Solo.log. This is evidence against adding that mod mid-run, not proof that all fresh runs are incompatible. No foreign code was patched to conceal the error.
- A fresh run with all eight mods initialized correctly (022903-Solo.log), received five starting potion slots, and retained the La Rata map panel. Neow's native Potion Belt then increased capacity to six.
- Using native act/travel/win to target the ending, Aeonglass + Knowledge Demon entered successfully. Limitless added its own boss reward screen. The La Rata invitation, accepted bonus act and custom boss entered successfully. The three bonus unknown rooms were skipped in this focused compatibility scenario; their baseline checks are described above.
- La Rata's exit reached the Architect even with Limitless's endless-act feature enabled. La Rata takes precedence at the regular finale; Limitless's Mysterious Door is therefore not offered on this route. This is deliberate fulfillment of the requested normal ending, not compatibility with endless chapter continuation.
- Unified Save Path Forked reported 3/5 applicable patches and loaded/wrote only default/2000/profile1. Real Steam saves were not used. The two missing overloads are reported as unavailable by that mod.
- The native recursive loader reported two malformed-manifest entries in existing Workshop content: obsolete UnifiedSavePath.json and Ironclad offering/sigil.json. All eight actual mods initialized. These existing package diagnostics are retained in the logs; original Workshop files were not altered.

This is a focused solo integration check, not a four-player validation of the whole external mod set. Cosmetic Defect/Necrobinder actions were not exercised with their characters. For the first multiplayer acceptance run, use La Rata + BaseLib only, then add optional mods between new runs.

Reference inspected: [Limitless author description](https://steamcommunity.com/workshop/filedetails/?id=3789146295), for its configurable endless acts and required BaseLib. Runtime conclusions above come from the installed binaries and local logs.

## Final build and integrity

Release build and official Godot export passed with 0 warnings and 0 errors (artifacts/night-20261016/final-build.log).

- DLL SHA-256: 3C35DDC5D9B4E6263BBEFB30C657EE37F033FB8507B01C6D2AD64AD205802EE7
- PCK SHA-256: D46C4D88A7AFB1AC9A4F0723684B65489D54DC58EE9AD9DF7B2ABB7AECF7C646
- Original game EXE, PCK and sts2.dll all still match the original recorded SHA-256 values (original-files-verification.json).
- The final code changes after the multiplayer sessions hide the offset duplicate placeholder outline on the bonus boss map portrait and register its encounter title through BaseLib localization. Combat/network behavior was not changed by these presentation corrections.

Exact final-build retest: 20261016-024442-Solo.log, with all eight mods. Loaded the fresh compatibility run's native boss checkpoint successfully. The map showed one boss portrait and the benefits panel (screenshots/final-map-portrait.jpg). Native win, Limitless rewards, the Architect dialogue, Who Carried recap and the ordinary victory endscreen completed. Who Carried now correctly displays 'Beat La Rata' (screenshots/final-who-carried.jpg). Saved history again has win=true and was_abandoned=false (compat-final-win.run).

This log retains the two existing Workshop manifest errors, one 'Unable to open clipboard' during test-console input (refocused and entered successfully), a Limitless pending-tween warning, and 12 engine resource diagnostics after shutdown. No gameplay exception or blocked transition was observed. It is not an error-free full-log result.

After testing, all game test clients were closed, the owned LaRataCompat20261016 staging folder was removed, and the original Steam-off account 2000 unmodded test profile was restored from its backup with matching file hashes (test-profile-restoration.json). The compatibility profile is archived locally for evidence. La Rata 0.3.0 remains installed in mods/LaRata. Steam launch options, real Steam profiles and original Workshop files were not changed. Temporary foreign mod copies are excluded from the release archive.
