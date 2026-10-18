# La Rata Remix 0.8.1 — Deeper chaos

2026-10-18. Local development update for game v0.107.1 and BaseLib 3.4.7. No game input, launches, installation, commits or publication during this update; the user's co-op session remains untouched.

## Cards and presentation

Borrowed Breath+ heals **6 HP** and draws **3 cards**, retaining its 1 Energy cost and Exhaust. The base card still heals 3 and draws 2. All 16 playable custom cards have effective upgrades: Cleanse, ten neutrals and five playable royal prizes. The 20 enemy-only royal cards deliberately have no upgrades.

All custom card frames now use a lighter charcoal/jade material with emerald and pale-green highlights following the native sculpted frame texture. Banners are brighter emerald. BaseLib's existing frame/banner material extension applies the style to Cleanse, player cards and boss cards; portraits, text layout and original game textures are unchanged. No additional dependency.

Well gameplay already awards the contested relic automatically to the sole Well holder, or uses a deterministic equal-probability choice among competing Well holders. It currently has an outcome panel and native relic award presentation, **not a dedicated well animation**. Ordinary RPS still animates the chosen native hands.

## Progression and reinforcements

Budgets remain linear in party size, with act multipliers **1.0 / 1.4 / 1.8 / 2.2** for acts 1 / 2 / 3 / 4+. The baseline solo budgets are 5 normal, 3.75 elite and 2.5 ordinary-boss points. Maximum selected changes are `3 * players + zero-based act index`, clamped to act 4. Actual selections can be fewer because of cost and compatibility limits.

Flat Plating, Regen and defensive Block grants grow by **1 / 1.5 / 2 / 2.5**, rounded upward, before applicable native party scaling. Armored therefore grants 2–4 / 3–6 / 4–8 / 5–10 base Plating. Sturdy gains 4 percentage points per act (10–18% initially); Keen gains 2 (8–16% initially). Immunity durations, Artifact, direct Strength, retaliation and debuff caps do not automatically multiply. This applies to director twists; the dedicated Kingsguard/final-duo/Rat King encounters retain their own balance.

Eligible normal encounters roll reinforcements at **70 / 80 / 90 / 100%** for 1–4 players, adding 5 percentage points per later act, capped at 100%. Each extra costs **4 points**. Solo can receive a second from act 3; 2+ players can receive two from act 1; 3+ players can receive three from act 2. Every extra must fit both budget and formation. The second rolls at 60% + 5% per later act, and the third at 35% + 5%, conditional on the preceding extra. Starting formations remain at most four enemies. Scene/slot restrictions and the reserved rat summon slot remain enforced; this is not a spawn guarantee for ineligible encounters.

## Ten additional native-power twists

There are now **40 twist kinds, or 41 families counting reinforcement**. Values below are base amounts before the defensive act multiplier where stated.

| Twist | Mechanic | Points |
| --- | --- | --- |
| Spiteful | Native Rupture: gain 1 Strength when damaged on its own turn | 2 |
| Venom Coated | Native Envenom: unblocked powered attacks apply 1 Poison per hit | 3 |
| Ember Guard | Native Flame Barrier: retaliate for 3 during the first player turn | 2 |
| Glass Crown | Native Diamond Diadem: halve attack damage until the first enemy turn ends | 3 |
| Mirror Guard | 6 scaled Block and native Reflect for the first player turn | 4 |
| Blood Feud | Native Crab Rage tuned to 2 Strength and 12 scaled Block on the first ally death | 4 |
| Doomweaver | Announced action adds 2 Doom, capped at 25% max HP; native Shroud grants 4 scaled Block per Doom application | 4 |
| Hotheaded | Native Flex Potion power grants 3 Strength until its first turn ends | 2 |
| Afterglow | 10 scaled Block, retained through the first enemy turn start by native Blur | 2 |
| Skill Hunter | Native Enrage grants 1 Strength per played Skill; normal fights from act 2 only | 5 |

Blood Feud requires an ally. Added offense, hex, shield and block-engine exclusions remain; added retaliation families also cannot stack on the same enemy. Native powers already present on a monster are not replaced. Venom Coated is an on-hit passive and does not use the six-Poison cap of the separate Venomous added action. Its strength is kept at one and it consumes the encounter's added offensive-amplifier allowance.

Audited native implementations rather than reimplementing their hooks. Rejected unsuitable powers: Juggernaut dereferences a player owner, Noxious Fumes targets the player-relative enemy list, Infested requires a specific encounter's summon slots, and Curl Up/Skittish require special animation states. No new library or network protocol.

## Determinism and validation

Director ruleset salt is v5. Act comes from native saved run state and is captured in each twist; no wall-clock or presentation RNG. Same-version peers reconstruct the same plan. Use the same build on every peer and start a new run for these revised rules; an old run resumed with new rules may reconstruct a different battle opening.

384,000 native-Rng encounter plans across four acts and all party sizes passed deterministic replay, exact cost accounting, growing budgets, formation caps, exclusions and reachability for all 40 twist types. All 16 playable-card upgrades were checked for an actual stat/cost change, canonical isolation and native JSON reload; Borrowed Breath+ explicitly checks 6 healing/3 draw. Existing royal-deck, goblin and cameo policy checks also pass.

Build/resource results are recorded in `artifacts/update-0.8.1/`. The C# build succeeded; NuGet vulnerability metadata was unreachable (NU1900), so that audit could not complete. The first sandboxed resource export reported editor/temp write failures; a clean export with access to Godot's own directories succeeded. The exported pack loads all 42 Royal Collection textures, native fonts and the new frame shader without resource errors. These are technical checks. New-power combat interactions, visual acceptance, native full battle reload and real co-op synchronization remain untested; interactive game testing is paused.
