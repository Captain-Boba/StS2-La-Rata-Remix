# La Rata Remix design

Updated 2026-10-17 for the authorized 0.5.0 prototype. All mod-added content is English. Historical validation remains in the versioned test reports.

## Core difficulty

Increase the existing 1–4 player seeded encounter budgets by 25%, with a wider pool of native powers/status effects, bounded HP/damage changes and more eligible multi-enemy encounters. Preserve Mecha Knight suppression, +25% newly gained gold, +2 starting potion slots, map panel and regular finale duo. No universal large HP/damage multiplier. Exact numbers and exclusions are in README.md.

## Treasure contests

All players claiming the same chest relic privately select Rock, Paper or Scissors before revealing. Ties repeat. Eliminated players watch. Native consolation rewards and relic ownership remain authoritative. Well is a rare co-op-only relic in the shared pool, including eligible reward and shop generation. It automatically beats non-holders; equal seeded probability among multiple holders. No custom network transport or new save format.

Solo nonempty chests offer three relics and award one selected relic. A Thousand Cuts is a rare solo/co-op relic allowing its owner unlimited paid removals per shop. Keep the native selection, cancellation, gold payment and saved price escalation.

## Bonus path

After the complete regular finale, including the second boss on double-boss runs, offer a shared invitation. Every player must vote Enter; any decline selects the regular ending after all votes arrive. Missing votes are never consent. The native multiplayer disconnect/rejoin lifecycle remains in charge.

The accepted topology is an unseen Ancient with native entry healing → YOU (Shop) → ARE (?) → NOT (Elite) → PREPARED (Rest Site) → La Rata (Boss). Pick the Ancient from all native acts, excluding Neow and previously visited Ancients. The Unknown room uses native room resolution with Glory encounter/event pools. The regular victory remains recorded even if players lose in the bonus. Defeating La Rata leads to the Architect and the ordinary ending. No duplicate victory closure or automatic bonus rewards.

## First boss design

A standing royal gray rat with black robes, gold crown and green scepter, derived from the supplied poster. Six transparent sprite poses and native Godot AnimationPlayer tracks provide idle, attack, cast, hit and defeat presentation in the mod-owned formation with the game's Queen background. It does not redistribute extracted game art. This is sprite animation, not a layered Spine character rig.

1,080 base HP plus native party scaling, with two native Two-Tailed Rats at double HP. The court rats keep their native skeletons, tinted dark emerald with green spirit particles. A seeded ten-card deck draws three each turn and spends three Energy, increasing to four at half HP. Effects include multihits, Poison, Doom, status cards, Buffer, Block, court-wide Strength and self-Vulnerable. Exact cards are documented in README.md. The hand is hidden; black/emerald native card faces appear only during play. Spells and powers resolve first, then normal monster attacks. Intents account for the planned Strength buff; a spell-only hand ends with an announced 12-base attack. La Rata is an enemy boss, not a selectable player character. Five distinct native relic identities receive explicit enemy combat adaptations, described by their icons and Royal Regalia. Generic director affixes and the regular earlier-boss helper do not stack onto this encounter.

The regular Act 3 finale's additional boss now receives full native multiplayer-scaled HP. This does not broaden the existing helper compatibility allowlist.

## Wandering potion goblin

One randomly selected encounter per three-combat block schedules a goblin visit. Arrival occurs between rounds after enemy turn two or three, before the next player action phase, never on combat entry. The native hook runs after block clearing so a splash of Block Potion remains effective. A short fight can end before its visit. Each living player, sorted by stable NetId, receives a different potion from a 12-item native pool. Eight helpful options and four harmful options retain their original effects. No inventory consumption, permanent-stat modification, potion-choice UI or new network protocol is introduced. Native room history, run seed and fixed RNG salts reconstruct the schedule and choices on combat restart.

## Technical boundaries and remaining design work

The mod identity stays LaRata. BaseLib 3.4.7 supplies custom models, localization, animation conversion and serialized run fields. A small game-version-pinned adapter appends the optional act and intercepts ending transitions. Existing game files and real profiles are not modified.

This prototype requires natural solo/co-op playtesting for pacing, difficulty, party composition and accessibility. Four local clients can demonstrate network consistency in specified scenarios; they cannot establish four-human Steam stability or enjoyable balance. Full layered animation and custom arena production remain future work. See TEST-REPORT.md for actual validation rather than treating design intent as completion evidence.

## 0.6.0 party-sized chaos amendment

The owner requested chaos and variety rather than nearly flat co-op budgets. Multiply solo twist budgets by party size for normal/elite/ordinary boss encounters, distribute effects among enemies and allow budgeted reinforcement alongside other twists. Increase the pool to 31 families, retaining separate limits on direct offense, extra hex sources, defensive stacks and formation size. The exact current rules and remaining test gaps are in UPDATE-0.6.0.md; these supersede earlier generator descriptions.
