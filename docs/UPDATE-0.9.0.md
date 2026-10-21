# La Rata Remix 0.9 — The Crown Strikes Back

Public playtest release, 2026-10-21. Package/manifest version: **0.9.0**. Requires Slay the Spire 2 **v0.107.1** and **BaseLib 3.4.7**. All mod-added text is English. No new runtime dependencies.

## The royal finale

- La Rata now has 3,000 base HP, scaling through the native final-boss rules to 7,800 / 11,700 / 15,600 HP for two / three / four players.
- Two distinct random Kingsguard knights replace his rats. Their support actions also protect and heal La Rata: the Herald heals the living court for 10% max HP on turn two; the Armorer heals the most injured other ally for 25% max HP on turn three.
- The king draws five cards from a 25-card deck and has six Energy, increasing to eight at half HP. Skills and powers resolve before attacks.
- Ten random combat relics, selected from fourteen, appear in two rows below his status icons.
- Five new illustrated boss cards: Black Tithe, Plague of Crowns, Kneel Before the Crown, Soul Foreclosure and Nightfall Decree. Each also has an upgradeable, player-compatible rare La Rata booster variant. [Exact costs and effects](UPDATE-0.8.7.md).
- A darker painted king with green accents, gloved rat hands and twelve animation frames; smoother idle, attack and spell transitions.
- The invitation to the bonus path now uses lore-only text. The regular victory remains protected; defeating La Rata leads to the normal ending.

## Boosters and treasure

- Every player receives a booster for their own class at the beginning of a new run, with opening deferred until the initial transition finishes.
- All six booster relics are Common. Each new pack contains seven distinct cards: five primary offers plus two from the normal and La Rata neutral pools. Previously saved five-card packs retain their contents.
- The La Rata pack retains its 20% chance to replace one primary offer with a playable boss card; ten boss prizes are now available.
- Fix card-face sizing, flip artifacts, hover previews and the centered Choose cards button.
- Keep the cursor visible during Rock, Paper, Scissors selection.

## More varied, less excessive chaos

- Reduce the total twist budget by 25% relative to 0.8.3, with linear scaling from one to four players and increasing budgets across acts. This is a budget change, not a measured difficulty percentage.
- Add eight twist families: Reckless, Brittle Ward, Toxic Shell, Last Stand, Fading Might, Leaden, Static Hex and Crossfire. The pool now contains 44 active families plus reinforcements.
- Remove all random Plating, Slippery, Rupture and Enrage twists. Intrinsic monster mechanics and royal cards are separate.
- Add smaller effect variants and reduced-stat Scout reinforcements for small budgets, giving solo encounters more combinations without increasing their budget.
- Preserve normal Inklet helper attack damage, cap its twist Regen at one and prevent a second multiplayer multiplier on twist-granted Regen.
- Include the 0.8.3 reinforcement diversity improvements: four eligible helpers in act one, seven later, avoiding present and recent enemy types when possible.

## Combat history and presentation

- Add per-combat histories for played royal cards and goblin potions, including round and potion recipient. Compact clickable counters appear above Exhaust only after the corresponding first action.
- Use native card viewing and potion effect hover tips. Histories follow the game's combat-entry save checkpoint; they do not preserve abandoned later turns when a fight restarts.
- Repaint and animate the potion goblin to better fit the game's visual style.
- Fix the Bestiary setup crash, intervention tooltip placement and oversized history icons.
- Add a drawn native-style La Rata map icon and restore access to the final campfire.
- Include the 0.8.3 longer royal intervention reveal and compact map title.

## Installing or updating

Use the existing [Steam Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3822192203), or download **LaRata-0.9.0.zip** from the [GitHub release](https://github.com/Captain-Boba/StS2-La-Rata-Remix/releases/tag/v0.9.0). Install BaseLib separately. See [the installation guide](INSTALL.md).

Start a fresh run and update every co-op player together. An active manual `mods/LaRata` copy takes priority over Workshop, so move it outside `mods` when switching to Workshop. No profile migration or reset is performed by this release.

## Validation scope

The release is checked by compilation, native card/model/save-serialization checks, deterministic encounter/deck policies and headless Godot resource/layout checks. Model checks cover all 46 custom cards and 21 player-card upgrades, all six guard pairs and native HP scaling. Policy checks cover 384,000 encounter plans and 40,000 royal turns.

These are not live gameplay or multiplayer tests. Fresh in-game animation/UI acceptance, real boss save/load, four-player synchronization, balance and other-mod compatibility remain open for this revision. Historical solo/four-local-client gameplay results apply to 0.8.2 only. The owner has reported successful solo runs during subsequent iterations; that feedback does not validate every new 0.9 feature. See [TEST-REPORT](../TEST-REPORT.md) and the preserved [0.8.4](UPDATE-0.8.4.md), [0.8.5](UPDATE-0.8.5.md), [0.8.6](UPDATE-0.8.6.md) and [0.8.7](UPDATE-0.8.7.md) implementation reports.
