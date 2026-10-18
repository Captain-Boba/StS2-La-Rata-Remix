# La Rata Remix 0.8.0 — The Royal Collection

2026-10-17. Targets game v0.107.1 (59260271), BaseLib 3.4.7, .NET 9 and Godot 4.5.1. All new mod text is English. The previously implemented 0.7.0 features remain included. This is a local development build, not a new public release.

## Royal deck and artwork

La Rata now draws from 20 cards instead of ten. The seeded draw/discard engine, three-card hand, 3/4 Energy, hidden hand, native card-play presentation and spells-before-attacks order remain. The additional cards introduce court healing, capped Plating, armor/Buffer, Poison/Frail, status cards, self-Vulnerable attack openings and court debuff removal. The README lists all twenty costs and effects. Native intents announce every attack, including its hit count and pending Blood Contract Strength. Off With Their Heads still postpones Vulnerable until all attacks finish.

Every royal card has an individual illustration. All ten new neutral cards also have individual illustrations; the five playable boss prizes share their corresponding boss illustration. Black frames and emerald banners use the established BaseLib material extension. The new card back and eleven relic icons are original generated assets. There are 42 new raster assets in total; exact prompts and SHA256 hashes are recorded in `art-prompts/manifest-0.8.0.json`.

The two large Kingsguard knights now have branching green energy across their breastplates. The shader addresses the installed Flail Knight atlas region, so the lines follow its native animated mesh. A slow approximately 5.7-second pulse varies the light, without frame changes or gameplay RNG. Native game textures/skeletons are referenced, not bundled.

## Ten new neutral cards

All belong to the regular colorless pool, alongside the existing Cleanse, and may appear through native colorless rewards/shops/events. Each has its own upgrade and uses native card commands, power models and selection contexts.

| Card | Cost / rarity | Base effect | Upgrade |
| --- | --- | --- | --- |
| Grave Spark | 0 / Rare | Gain 1 Energy; draw 1; Exhaust | Gain 2 Energy |
| Umbral Stitch | 1 / Uncommon | 8 Block and 4 Vigor | 11 Block and 6 Vigor |
| Sewer Pact | 1 / Uncommon | 5 Poison and 5 Doom to an enemy | 7 each |
| Phantom Parade | 2 / Uncommon | 3 damage four times to all enemies | 5 per hit |
| Borrowed Breath | 1 / Rare | Heal 3 HP; draw 2; Exhaust | Heal 5 |
| Crown Exchange | 0 / Uncommon | Lose 3 HP; draw 2; Exhaust | Draw 3 |
| Midnight Rehearsal | 1 / Uncommon | Draw 3, then choose 1 card to discard | Draw 4 |
| Emerald Ward | 1 / Uncommon | All living players gain 6 Block | 9 Block |
| Second Shadow | 2 / Rare | Gain native Afterimage: 1 Block whenever you play a card | Costs 1 |
| Open the Gates | 2 / Uncommon | 2 Weak and 2 Vulnerable to all enemies; Exhaust | Costs 1 |

## Six boosters

Ironclad, Silent, Defect, Regent, Necrobinder and La Rata boosters are uncommon shared-pool relics, available to every character. Each opens once upon pickup and remains as an opened memento. Five facedown cards fan out after clicking the pack. Hover/focus gives a pulsing rarity rim; each card must be clicked to flip it. Common, uncommon, rare and royal-prize reveals differ in color, particle count and reveal emphasis. Once all five are revealed, **Choose cards** restores the native grid. Select zero to five and confirm; unselected cards are not added to the deck.

Class boosters draw five distinct unlocked Common/Uncommon/Rare cards from that class's regular pool. Selection is uniform across eligible cards, not weighted by ordinary reward rarity odds. The La Rata pool consists of Cleanse plus the ten new neutral cards. A 20% per-pack roll replaces one slot with one of five playable royal prizes:

| Prize | Cost | Base effect | Upgrade |
| --- | --- | --- | --- |
| Scepter Storm | 2 | 7 damage three times to one enemy | 9 per hit |
| Sewer Alchemy | 2 | Double an enemy's Poison, then add 5; Exhaust | Add 8 |
| Stolen Aegis | 2 | 20 Block and 1 Buffer; Exhaust | 2 Buffer |
| Blood Contract | 0 | Lose 6 HP; all living players gain 2 Strength; Exhaust | 3 Strength |
| Emerald Tempest | 3 | 5 damage four times and 3 Poison to all enemies | 7 per hit |

All prizes are rare player card models with valid player targets, costs and upgrade paths. They are registered through BaseLib but have no reward-pool attribute. The boss-only models never enter player decks. The chance is per pack, not per card; a pack contains at most one royal prize.

## Five combo relics

| Relic | Rarity | Effect |
| --- | --- | --- |
| Ashen Spool | Uncommon | Every Exhaust grants 3 Block; the first each turn also draws 1 |
| Crown Capacitor | Rare | First Power each turn grants 1 Energy and 4 Vigor |
| Crossroads Compass | Uncommon | First combination of two different Attack/Skill/Power types in a turn grants 4 Block and draws 1 |
| Bottled Echo | Rare | Each Potion used in combat grants 1 Strength and 2 extra draws next turn |
| Smuggler's Thread | Uncommon | First discard during your turn grants 1 Energy |

All five use the shared relic pool and have no character restrictions. Once-per-turn guards are set before awaited hooks to prevent recursive retriggers, and reset at side-turn boundaries and combat end. Actions use the owning player and native commands; triggers do not run after combat ends or for dead owners.

## Determinism, persistence and integration

Booster cards are generated on every peer with native `CardFactory.CreateForReward`, a native isolated `Rng` salt containing run seed, owner NetId, pack model ID and acquisition floor, and an ordinal-sorted eligible pool. Ordinary local reward RNG/rarity history is not consumed. Pool/model modifications and post-generation reward hooks are excluded from this draw; ordinary deck-acquisition hooks still run when chosen cards are added. Unlock and multiplayer filters remain native. All peers require identical game/mod versions and content.

The owner uses native `CardSelectCmd.FromSimpleGridForRewards`; other peers wait for its existing synchronized choice indexes. The reveal layer only animates native `NCard` nodes and never rerolls or mutates game state. Native buttons/fonts and the existing grid implement the final choice. No new packet type, transport, dependency, profile migration or save format is introduced.

`Opened` and the five `SerializableCard` entries use native `[SavedProperty]` fields. Pending contents are recorded before waiting for the owner and reused if the pack resumes at a room entry. A pack obtained while native combat selection is ending remains unopened for the next room. Opened packs do not reopen. This follows native room/checkpoint saving; arbitrary mid-animation save/resume is not promised. Combo counters also use saved properties. New runs are required for this development update; do not swap versions mid-run.

## Validation scope

Run the checks from the repository root:

```powershell
dotnet run --project tests/LaRata.PolicyChecks -c Release
dotnet run --project tests/LaRata.ContentChecks -c Release
./scripts/Build.ps1
```

The policy suite exercises encounter budgets, 20-card deck conservation/replay/reachability, regalia, goblin schedules and royal interventions. The content suite uses the game's supported `ModelDb.Inject` and `SavedPropertiesTypeCache.InjectTypeIntoCache` test entry points. It constructs/clones all 35 royal/player card models, checks upgrade isolation and serializable identity, and round-trips pending/opened packs and combo counters through the game's JSON serialization utility. It does not boot BaseLib's complete game registration or execute card combat effects.

Standalone Godot previews inspect the actual native knight models with the mod shader, plus the generated portraits and relic icons at 130/48 pixels. They are visual asset checks, not in-game UI acceptance. No original game files or user saves are modified.

Interactive game/UI testing remains paused at the owner's request. Required follow-up: naturally acquire every booster; reveal with mouse/controller at different resolutions and speeds; retain zero/some/all; close/reload checkpoints before and after acquisition; verify all cards/upgrades/relic triggers; check full royal rotations and intent damage; test simultaneous pickups, remote clients, reconnects and four-peer checksums; test the installed mod combination. Natural-run balance, actual multiplayer correctness and final animation/game-feel approval remain open.

Build, resource and package evidence is stored locally under `artifacts/update-0.8.0/`. The generated archive is `artifacts/LaRata-0.8.0.zip`. No commit, push or public release is part of this task; the linked public release remains v0.6.0.

### Recorded local results

- Release build and PCK export succeeded with zero compiler warnings/errors (`build.log`). No runtime dependency was added.
- Policy checks passed 240,000 encounter plans, 40,000 royal turns, 40,000 goblin blocks, 160,000 potion assignments, 1,000 regalia selections and 120,000 cameo schedules (`policy-checks.log`).
- Content checks passed all 35 card constructors/clones, 15 player upgrades, 11 relic constructors and native JSON roundtrips for pending packs, opened packs and combo counters (`content-checks.log`). The harness uses native test injection; full game registration and combat execution are not covered.
- The exported PCK loaded all 42 new textures, both native fonts and the chest-energy shader uniform without resource errors (`resources.log`). Native UI scene references and 300-by-422 card bounds were inspected. This is not a run of the opening UI.
- Standalone GPU previews rendered the actual knight shader and all new illustrations/icons. Local previews: `kingsguard.png`, `art-proof-0.png`, `art-proof-1.png`, `art-proof-2.png` under `artifacts/update-0.8.0/`.
- Installed while the game was closed. `install.log` and `install-verification.json` confirm that LaRata.dll, LaRata.json and LaRata.pck match the package. No game was launched and no original files or profiles were changed.
- Initial test-harness attempts needed native model/cache initialization and the native field-aware JSON utility; the final harness uses those existing interfaces. An initial NuGet audit lookup failed in the restricted environment; authorized restore subsequently succeeded. These are not in-game failures.

The installation and these checks do not establish in-game UI, balance or multiplayer acceptance. Those checks remain pending under the existing UI-test pause.
