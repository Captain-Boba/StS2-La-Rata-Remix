# La Rata 0.2.0 — Test report

Date: 2026-10-15 (Europe/Berlin). This is a technical prototype report, not a full balance approval.

The gameplay evidence below describes **0.2.0**. See the final section for the separate **0.2.1 branding update**; its newly built binary was not used for the historical interactive sessions.

## Environment and delivered build

- Slay the Spire 2 v0.107.1, commit 59260271, Steam build 23811903.
- MegaDot 4.5.1.m.12, Windows x64, .NET 9 target; local .NET SDK 10.0.302.
- Final Release build: **passed, 0 warnings, 0 errors**.
- Final DLL SHA-256: `0531FACF1157610756B02634E18AB5DE4C8BE4126858A80460FC2CA698892E46`.
- Built, packaged and installed DLL hashes match.
- Runtime dependencies: native game assemblies and the game's bundled Harmony. No additional runtime packages.

## Automated policy checks

**120,000 cases passed** using the actual game's seeded `Rng`: 10,000 seeds × 4 party sizes × normal/elite/boss tiers. Each case was replayed and compared. Checks cover budget limits, 1–3 affixes, HP/damage caps, power limits, exclusion of stacked damage/buff amplifiers and reinforcement exclusivity. The forced rat reinforcement also consumes the full budget.

These checks validate policy generation, not every monster's behavior, boss combination or network state.

## Interactive solo checks

Local isolated profile 2000, Steam disabled. Evidence log: `artifacts/logs/20261015-010628-Solo.log`.

- New Ironclad run starts with **5 potion slots**.
- Golden Pearl's native 150-gold grant produces **187**, taking the balance from 99 to 286. The universal native gain hook was also inspected; individual gold sources were not all exercised.
- A normal Seapunk encounter received Echoing and Empowered. Its native BUBBLE_BURP action visibly included the added debuff intent, then applied the additional Weak. Native Strength increased from 1 to 2 and native multiplayer-aware powers remained in use.
- Mecha Knight executed its new strongly announced action after the flamethrower: **3 Weak + 3 Frail**, then continued its native wind-up sequence.
- Native save/quit/load restored the normal Seapunk encounter with the same policy, 46 HP and starting Plating. Potion slots stayed at 5 rather than becoming 7; 286 gold was retained. The additional hex executed again after reloading.
- A debug Queen finale spawned Queen, Amalgam and Waterfall Giant. This was a formation/spawn observation, not a complete Queen-duo battle.
- Some solo debug combat used native god mode. Solo checks are functional observations and do not establish balance.

## Four-player local co-op

Four separate game processes used the game's native ENet host/join path with Steam disabled. All four were controlled through the desktop UI. Debug commands used the native networked console. No custom network simulation or replacement combat loop was built.

### Scenario A: reinforcement, elite action, final encounter entry

Logs:

- `artifacts/logs/20261015-011527-Host.log`
- `artifacts/logs/20261015-011534-Client1.log`
- `artifacts/logs/20261015-011534-Client2.log`
- `artifacts/logs/20261015-011534-Client3.log`

**25 native checksum IDs, values and contexts matched on all four peers.** Required rat and four-target suppression markers were present in every log. No detected state divergence or error in these logs.

Observed:

- Four-rat formation, with the extra rat correctly positioned and its reduced attack reflected in both intent and executed damage (5).
- Three Mecha Knight rounds, including charge, flamethrower and the new suppression action resolving for all four living players.
- All players had five potion slots; the Golden Pearl recipient had 286 gold.
- Native map voting entered the actual selected final boss node and produced Aeonglass with Waterfall Giant. Main HP 2662, helper HP 749, helper factor 0.60.
- A native checkpoint was retained for reload testing.

This scenario predates the final English panel placement, tighter duo camera framing and helper-music fix; the rat and elite combat rules were unchanged afterwards.

### Scenario B: final-build reload and helper death sequence

Logs:

- `artifacts/logs/20261015-013943-LoadHost.log`
- `artifacts/logs/20261015-013949-Client1.log`
- `artifacts/logs/20261015-013949-Client2.log`
- `artifacts/logs/20261015-013949-Client3.log`

**20 native checksum IDs, values and contexts matched on all four peers.** The same boss-duo and native EXPLODE_MOVE markers were present in every log. No detected state divergence, Godot ERROR, script error or logged exception in this scenario.

Observed:

- Native multiplayer loading recreated the **same Aeonglass + Waterfall Giant**, with the same HP and opening checksum as the earlier load.
- Saved gold and five potion slots remained intact.
- Native networked healing prepared low-health debug characters for the test. This was not a natural run victory.
- Both enemies fit the tested 1280×720 game viewport, including their health bars and intents.
- Aeonglass executed its native 26-damage party attack and block; Waterfall Giant executed PRESSURIZE_MOVE and gained Steam Eruption without the former music error.
- Native networked `kill 1` triggered the helper's death-prevention phase: infinite HP display and stun intent. Native `kill 0` removed the main boss; the fight correctly continued with the helper still present.
- ABOUT_TO_BLOW_MOVE led to a visible 15-damage explosion intent. EXPLODE_MOVE dealt the announced damage, removed the helper, ended combat and reached the normal continuation button. This checks native helper cleanup and victory gating, not damage-based lethal-hit interactions with every card or power.
- God mode was not used in the co-op scenarios.

The comparer results are saved as `artifacts/prototype-coop-checksums.txt` and `artifacts/boss-reload-checksums.txt`. They cover only the recorded scenarios.

## UI and fixes found during testing

- English panel text, all four potion capacities, and collapse/re-expand were visually checked on the map at 1280×720. The panel sits below the legend and avoids the four-player HUD. Base-game German text was intentionally left unchanged.
- The initial native fast-host test used profile ID 2100, while the native host always has network ID 1. Native save validation rejected that mismatched test profile on reload. The launch script now uses host profile ID 1, and the unchanged checkpoint loaded successfully. The rejected test logs are retained separately; this was a test setup error, not a successful save test.
- The first helper action exposed an FMOD music-parameter error: Waterfall Giant's own soundtrack parameter does not exist on the final boss track. The final mod suppresses only the guest's encounter-specific music parameter while retaining its sound effects and mechanics. Scenario B retested the action and entire death sequence successfully. The original failing log remains `artifacts/logs/20261015-013209-LoadHost.log`.
- The checksum comparer now rejects plain Godot `ERROR:` and `SCRIPT ERROR:` lines as well as bracketed errors, exceptions, missing scenario markers and missing/duplicate/mismatched checksum coverage.
- Ceremonial Beast was excluded after inspecting its absolute HP phase threshold. Reduced helper HP would incorrectly trigger its phase too early.

## Integrity and cleanup

SHA-256 rechecks confirmed the original game executable, PCK and sts2.dll match the pre-mod baseline in `artifacts/original-hashes.json`. The real Steam profile's normal progress file retained SHA-256 `B6FA7803844560163D8AC99462BC605D940076C9B6840B89C38F161CB91DA5E8`.

Interactive tests used separate local test profiles. No real Steam save was edited during this implementation/test pass. The four local test processes were closed after the final scenario. Only the mod's own DLL and manifest were installed. No commits, staging or pushes were performed.

## Remaining validation

- Four human players over Steam/Internet, including latency, disconnect/reconnect and host loss.
- Natural full-run balance at different ascensions and with varied characters/decks. Budget limits are design controls, not a measured percentage increase in difficulty.
- Interactive 2- and 3-player runs; their policy generation was tested automatically, but their runtime sessions were not played.
- Every allowed boss pairing, all native phase transitions, and double-final-boss progression. Queen received a spawn check; Aeonglass/Waterfall Giant received the recorded four-player phase test. Test Subject and other helpers still need full runtime coverage.
- Broad card/potion/relic interaction coverage, mod interoperability, all gold sources, Ectoplasm runtime behavior, ultrawide/controller UI and accessibility.

Do not treat matching checksums in these scenarios as proof that desynchronization is impossible everywhere. Use the same release files on every peer and start new runs for this prototype.

## Version 0.2.1 branding update

Date: 2026-10-15. The display name is now **La Rata Remix** in the manifest, README and both map-panel states. Assembly/manifest/log version is 0.2.1; technical identity and ruleset version remain unchanged. The design brief and endgame integration study record the accepted four-word route and source-backed integration proposal. No endgame logic, boss mechanics or new runtime dependency was added.

- Release build succeeded with **0 errors and 2 NU1900 warnings** because the NuGet vulnerability service index was unreachable. This was not a successful online vulnerability audit. The project lock file contains no NuGet package dependencies.
- DLL SHA-256: `B99B47D941655D82B70BD0A59ECB15AE0B9BA1ED9BDBF5F561D1068DB805D867`.
- Built, packaged and installed DLL hashes match. The installed manifest reports La Rata Remix 0.2.1 with stable ID `LaRata`. Installation replaced only this mod's DLL and manifest while the game was closed.
- Source hashes against the pre-branding snapshot changed only `MapPanelPatch.cs` and `ModEntry.cs`; reviewed changes in those files are title/version strings. Combat, RNG, save, targeting and formation code is unchanged.
- The checksum comparer accepts an explicit expected version and retains 0.2.0 as its default for historical logs. Rechecking Scenarios A and B still yields 25 and 20 matching native checksum observations respectively; this is a re-analysis of existing logs, not another multiplayer session.
- The original `LaRata-0.2.0.zip` remains byte-identical. `LaRata-0.2.1.zip` contains the new binary, manifest, documentation and user-supplied concept artwork. The installer still copies only the mod DLL and manifest.
- No interactive test was repeated for this text-only update. The longer map-panel title still needs an in-game visual check; historical UI approval applies to the earlier title.
- Endgame feasibility is based on inspected source. Invitation voting, an extra act, its save/load and packet state, the word-node labels and the future La Rata encounter have **not** been implemented or runtime-tested.
