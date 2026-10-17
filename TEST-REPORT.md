Current UI update: [0.3.1 visual and RPS test report](docs/UI-TEST-REPORT-0.3.1.md). The report below preserves the 0.3.0 gameplay and compatibility evidence, including its original hashes.

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
