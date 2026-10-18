# La Rata Remix 0.7.0 — The Kingsguard and The King's Gambit

2026-10-17. Game v0.107.1 (59260271), BaseLib 3.4.7, .NET 9, Godot 4.5.1. All added text is English. The earlier 0.6.1 Bowler Hat change remains included.

## The Kingsguard

The bonus act's elite pool now contains only `KingsguardEncounter`; NOT therefore selects it when the act's rooms are generated. Its four persistent native formation slots contain Crown Bulwark, Crown Headsman, Crown Armorer and Crown Herald. The first two use the original Flail Knight scene/animator; the others use Magi Knight and Spectral Knight. No native knight scene, AI or source asset is modified or bundled. BaseLib custom monster/encounter models provide separate identities and native saves/history. A native Spine normal-material shader supplies charcoal steel and emerald accents. The original idle, attack, cast, hit and death animations remain in use.

Base HP is 220/210/165/155; starting Plating is 10/8/6/6. Each knight has a deterministic three-move cycle with native attack, defend, buff and heal intents. Bulwark shields its allies, Headsman reinforces their armor, Armorer repairs the most injured other guard, and Herald heals/protects the group. Full values/roles are in the README and `Kingsguard.cs`. Plating uses the game's 1/3/5/7 party scaling and party-sized decay, with new grants capped at 14 base-equivalent stacks. HP/Block use the existing native final-act scaling adapter. Injured-ally ties use stable CombatId ordering; dead allies are never revived. Generic director twists and royal interventions are excluded.

## Hidden royal interventions

One ordinary/elite room per seeded block of three schedules a single intervention on player turn 2 or 3. Bosses, the Kingsguard, debug-only encounters and unrewarded encounters do not trigger it. Short fights can finish before it happens. A selected living enemy receives a native custom power encoding the round and hidden card in its amount. The displayed amount is zero; description/marker reveal only that La Rata will play an unknown card. This concealment is UI-level, not protection from clients inspecting memory or state packets.

The marker reuses the native card-debuff intent symbol through the game's AtlasResourceLoader, with a mod-owned black/emerald shader and gentle opacity pulse. Its tooltip explains the counterplay. Killing the marked enemy removes the power and cancels the intervention, including enemies whose death is followed by revival. There is no retarget. The per-combat announcement guard prevents a second attempt on extra player turns.

Execution occurs in the awaited `Hook.AfterSideTurnStart` postfix on the enemy side, after native start-turn hooks and block clearing, before monster moves. It consumes the pending power, reveals the card using the native play-pile animation and plays the generated laugh through BaseLib `ModAudio`, respecting native sound-volume settings. No La Rata character is spawned. The 1.35-second reveal remains readable in Instant mode and is tied to the card's lifetime.

The supported deck cards are Sewer Alchemy, Royal Flush, Stolen Aegis and Exposed Crown. They reuse the same `RoyalCard.Resolve` implementation as the boss, adapted to the marked native monster as the caster; no fake Player or fake boss is constructed. Party effects run in stable NetId order. Direct attacks, Doom and Strength immediately before already-previewed attacks are excluded. In particular Exposed Crown trades enemy protection for a Vulnerable opening. If another effect corrupts the encoded card to an unsupported value, the pending power is consumed with a warning instead of interrupting the combat task.

## Determinism, saves and compatibility

Schedules use native saved room history and isolated native-Rng salts. Royal selection uses its own stream, stable CombatId ordering and a separate namespace from the unchanged goblin stream. Pending state is represented by a native power amount, included in the native combat checksum. Native checkpoint loading restarts/reconstructs the encounter; this does not introduce arbitrary mid-action saves. Native monster move states carry the Kingsguard cycles. No network messages, transport, serializer or save-path changes are introduced. All peers must install the same version and start a new run. Already-generated older bonus-act room lists are not migrated.

The goblin's existing visits are unchanged. The royal schedule compares against its actual selected arrival round and shifts a collision to round 2. Sounds, shaders and tweens make no gameplay random choices. No extra runtime dependency is required. Third-party mods can still alter combat hooks, power amounts or UI; compatibility with all installed mods is not established by these checks.

## Validation and remaining tests

The policy suite passes 240,000 encounter plans, 40,000 royal-deck turns, 40,000 goblin blocks/160,000 distinct potion assignments, 1,000 regalia selections, and 120,000 new cameo schedules. The new checks cover exactly one scheduled royal visit per eligible three-room block, replay, disjoint goblin/royal rounds, unchanged prior goblin timing, reversible power encoding and the non-attack intervention pool. Evidence: `artifacts/policy-0.7.0.log`. These are deterministic policy checks, not actual multiplayer tests.

Final Release compilation and official Godot PCK export passed with zero compiler warnings/errors (`artifacts/update-0.7.0/build.log`). The headless resource check mounted the installed game's PCK without starting game scenes/scripts, confirmed all three original knight scenes and the virtual card-debuff atlas entry, loaded both shaders and the four-slot formation, and decoded the MP3 as 2.28 seconds (`resources.log`). An earlier sandboxed editor attempt failed before the script; the successful check/export ran outside that sandbox. The initial icon-file check was corrected to inspect the game's virtual atlas entry, rather than assume that the `.sprites` path is a physical file. No game files were changed by these checks.

Version 0.7.0 was installed while the game was closed. `install.log` and `install-verification.json` confirm that all three installed runtime files match the package. Local ZIP: `artifacts/LaRata-0.7.0.zip`. Installation does not establish gameplay correctness. Source changes remain uncommitted.

Assistant-driven game/UI testing remains paused. Required in-game checks: all four knight models/animations/health bars without overlap; every rotation and support target; final ally deaths; warning hidden until reveal; kill/removal cancellation; card play and correct effects; reveal/laugh timing and audio level; ordinary checkpoint reload; four local peers and real Steam co-op with checksum comparison; installed-mod compatibility and natural-run balance. No current claim of visual, listening, gameplay or multiplayer acceptance is made.

No commit, push or public release is authorized by this development task. The public GitHub release remains v0.6.0.
