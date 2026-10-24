# Version 0.9.3 — Double Boss Spoils

## Patch notes

- Restore combat rewards after the regular final boss, before the optional La Rata invitation.
- Award a complete native boss reward batch for each boss in the reinforced finale: two gold rewards, two independent three-card boss offers and two normal potion rolls per player. With the starting White Beast Statue, both rolls give a potion. A finale without an eligible reinforcement gives one batch.
- Preserve native reward hooks, player ownership and synchronization. Room-specific extra rewards are included once. La Rata's own victory still leads to the Architect through the existing flow.
- Change vanilla Barricade from Rare to Uncommon in its existing Ironclad pool. Its effects, costs, upgrade and identity are unchanged; it follows normal Uncommon reward and shop rules. Bowler Hat keeps its native effect.
- Reduce Bristling and Toxic Shell to one quarter of the normal twist selection weight. Their Thorns strength, encounter budgets and other twist weights are unchanged. This is a 75% weight reduction, not a guaranteed 75% reduction in final encounter frequency.
- Fix A Thousand Cuts using a stale card-removal price. Each completed removal advances the native saved counter, and the next displayed and charged price uses that counter with normal discounts and restrictions.
- Recommend starting without Ascension levels in the Workshop description.

## Compatibility

Requires **Slay the Spire 2 v0.107.1** and **BaseLib 3.4.7**. Use matching versions across the entire co-op party and finish the shared session before updating. Start a new run for consistent encounter generation after the twist weighting change.

Boss loot uses the game's existing reward generation, rarity odds, potion odds and saved per-player RNG. No custom network messages or replacement save formats are introduced. The Barricade rarity change also applies to existing copies while this mod is enabled; after disabling it, the native rarity returns.

## Validation

- Release build and native content checks pass. For fixtures with one through four players, the real reward generator populates gold, two independent three-card rare offers and potions. Checks cover both single-boss and reinforced finales, with and without White Beast Statue, native relic hooks, correct owners, extras included once, repeated-population safety and identical card/potion IDs after restoring native RNG/odds snapshots.
- Barricade's canonical and mutable models report Uncommon; its native upgrade still reduces energy from 3 to 2. Other rare cards remain unchanged. Native boss offers exclude the now-Uncommon Barricade.
- Shop regression checks exercise the native synchronized removal completion and saved counter at both inflation tiers. UI selection, gold presentation and deck-removal commands are simulated; repeated purchases, prior removals, cancellation, affordability, restrictions and counter serialization are covered.
- Seeded policy checks cover 384,000 encounters, 40,000 royal turns and 120,000 intervention schedules. Weighted-selector checks confirm one selection slot for each Thorns family versus four for other families.
- These are automated native-model tests. Desktop logging/mod discovery and controller-button generation are stubbed in the reward runner. No fresh live reward-screen, full save/load, shop UI or Internet co-op test was performed. Those remain acceptance gaps; no multiplayer desynchronization claim is made.
- The dependency vulnerability feed was unavailable in the sandbox (NU1900); compilation succeeded. The earlier investigation is retained in [LOCAL-THORNS-CUTS-20261024.md](LOCAL-THORNS-CUTS-20261024.md).

Balancing remains work in progress. We recommend starting without Ascension levels. German and Chinese localizations are planned; mod-added text is currently English only.
