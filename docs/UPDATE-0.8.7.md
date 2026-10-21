# 0.8.7 — The king and his court

Local iteration, 2026-10-20, for game v0.107.1 / BaseLib 3.4.7. No new runtime dependencies. No publication is included in this update.

## Royal finale

- La Rata has **3,000 base HP**. Native final-boss multiplayer scaling gives **7,800 / 11,700 / 15,600 HP** for two / three / four players. His basic damage is not multiplied by player count.
- Two distinct, seeded random Kingsguard knights replace the two rats. All six pairs are possible. The native encounter RNG chooses the pair; the roster and separate royal deck/relic seeds reconstruct when combat restarts on load.
- All Kingsguard court-wide Block, Plating, Strength and healing support now includes La Rata. The Herald heals the whole living court for 10% maximum HP on its second turn. The Armorer can select La Rata as the other ally with the lowest HP percentage and heal 25% maximum HP on its third turn. Existing knight stats and three-turn cycles remain intact.
- La Rata draws **5 cards** each turn from **25 cards**, with **6 Energy**, rising to **8 Energy at half HP or below**. His relics may increase draw/energy. Skills and powers resolve before attacks; a hand without an attack still ends with the existing 12-damage attack. This is a bounded enemy deck, not a fake player or an independent input system.
- Ten distinct combat relics are selected from fourteen. Their native-model icons appear as **two rows of five**, below the actual wrapped power icons. Hover text describes the enemy-specific effect. Formation markers separate both knights from the king; the king is higher to leave room below his HP/powers.
- Guard attack intents include Strength promised by the current royal hand; the pending amount disappears as the corresponding buffs are applied. The king remains first in the native encounter slot/turn order.

## Five new boss cards

| Card | Energy | Boss effect |
| --- | ---: | --- |
| Black Tithe | 2 | Remove all Block from every player; apply 2 Weak and 2 Frail. Dedicated debuff tooltip warns of the Block removal before attacks. |
| Plague of Crowns | 3 | Apply 8 Poison and 12 Doom to every player; put 2 Burns in each discard pile. Dedicated tooltip states the quantities. |
| Kneel Before the Crown | 2 | Every living court member gains 30 base Block and 3 Strength before attacking. |
| Soul Foreclosure | 3 | Deal 26 damage to every player; heal the living court for 8% maximum HP. |
| Nightfall Decree | 3 | Give the living court 2 Ritual, capped at 6 total Ritual. Native enemy Ritual grants Strength after subsequent enemy turns. |

Enemy Block/powers retain their applicable native multiplayer rules. These stronger cards are **not** added to the intervention pool in normal encounters.

## Rare playable booster variants

The La Rata pack still has a 20% chance to replace one of its five primary offers with a boss prize, then adds the existing two mixed neutral offers. It now chooses among ten playable boss prizes. These five new variants are Rare and do not enter ordinary neutral rewards or the two bonus-neutral slots. Each uses its matching boss-card artwork.

| Card | Energy, base / upgraded | Base effect | Upgrade |
| --- | --- | --- | --- |
| Black Tithe | 1 / 1 | Remove all Block from one enemy; deal 10 damage. | 16 damage. |
| Plague of Crowns | 2 / 2 | Apply 6 Poison and 12 Doom to all enemies. Exhaust. | 9 Poison, 18 Doom. |
| Kneel Before the Crown | 2 / 2 | All living players gain 12 Block and 2 Strength. Exhaust. | 18 Block, 3 Strength. |
| Soul Foreclosure | 3 / 3 | Deal 24 damage to all enemies; heal yourself for 6 HP. Exhaust. | 32 damage, 9 HP. |
| Nightfall Decree | 2 / 2 | Power: all living players gain 1 Ritual. | 2 Ritual. |

## Presentation

- Darker painted rat king: charcoal fur, emerald clothing, glowing green accents and fitted dark gloves. Each hand has three distinct rat-like fingers plus one thumb.
- Twelve painted frames across idle, attack and spell sheets replace the old six-frame sheet in the combat scene. Native Godot atlas registration, opacity crossfades and continuous small transforms provide gentle breathing, a brief blink, attack windup/recovery and spectral-card casting. Idle animation takes 18 seconds; no rapid repeated twitch.
- New illustrated portraits for all five cards. [Source assets and exact prompt set](art-prompts/royal-finale-0.8.7.md). The scene can be regenerated with `python docs/art-prompts/build-royal-animation.py`; it does not edit image pixels.
- Invitation, accept and decline text now describe the throne, laughter, shadows and Architect entirely through lore. Existing unanimous co-op choice and protected regular victory remain unchanged.

## Verification and limits

- Release compilation and native model checks pass. All 46 custom cards construct/serialize; all 21 player-card upgrades preserve canonical values and round-trip through native saved-card JSON.
- Native encounter generation: 128 seeds, four simulated peers per seed, identical rosters, distinct guards, all six pairs reachable; native solo/co-op HP values verified. These are deterministic model checks, not network tests.
- Native guard models build with no Creature for Bestiary previews, then bind their commander in the normal room lifecycle. Support targets include the king and attack intent damage tracks pending Strength. Native owner-scoped Tungsten Rod hook and ten-relic selection/display checked.
- 40,000 royal turns verify draw/discard conservation, seven-card first hands with Bag of Preparation, five-card later hands, energy limits, spells-before-attacks, replay equality and reachability of all 25 cards. The broader existing policy suite also passes.
- Headless Godot imports all new art, loads twelve atlas frames and all animation tracks, samples crossfade weights, and verifies the native 216x84 two-row relic grid and spacing below simulated power rows. Scene geometry confirms king-first slot order and 360-pixel spacing to each knight.
- Evidence: `artifacts/royal-0.8.7/`. Initial checks caught an animation text-number parse issue and an ownerless power construction in the test harness; both were corrected before final checks. NuGet vulnerability-feed lookup warning NU1900 remains unavailable; dependencies are unchanged.

No active game was started. Live UI/animation acceptance, a real save/load of this boss, battle balance, other-mod behavior and real four-player synchronization remain untested for 0.8.7. Do not mix this build with earlier versions in a shared run. Existing native saves restart a combat; a boss saved under an older build will therefore reconstruct with the new roster/deck/stats. A fresh run is recommended for comparing balance. Workshop remains 0.8.3 and the GitHub manual release remains 0.8.2.

Local package: `artifacts/LaRata-0.8.7.zip`. The three runtime files are installed under the game's `mods/LaRata` and verified against the package by SHA-256; the switch script reports `LOCAL TEST (0.8.7)`. No save/profile files were changed. Installation is not a gameplay test.
