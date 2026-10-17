# La Rata Remix design

Updated 2026-10-16 for the authorized 0.3.0 prototype. All mod-added content is English. The earlier 0.2.1 documents are preserved under artifacts/night-20261016.

## Core difficulty

Keep the existing 1–4 player seeded encounter budgets, tactical affixes, compatible reinforcements, Mecha Knight suppression, +25% newly gained gold, +2 starting potion slots, map panel and regular finale duo. No universal large HP/damage multiplier. Existing exact numbers are in README.md.

## Treasure contests

All players claiming the same chest relic privately select Rock, Paper or Scissors before revealing. Ties repeat. Eliminated players watch. Native consolation rewards and relic ownership remain authoritative. Well is a rare co-op-only relic in the shared pool, including eligible reward and shop generation. It automatically beats non-holders; equal seeded probability among multiple holders. No custom network transport or new save format.

## Bonus path

After the complete regular finale, including the second boss on double-boss runs, offer a shared invitation. Every player must vote Enter; any decline selects the regular ending after all votes arrive. Missing votes are never consent. The native multiplayer disconnect/rejoin lifecycle remains in charge.

The accepted topology is YOU (?) → ARE (?) → NOT (?) → PREPARED (Shop) → La Rata (Boss). The three Unknown rooms use native room resolution with Glory encounter/event pools. There is no extra Ancient or rest node. The regular victory remains recorded even if players lose in the bonus. Defeating La Rata leads to the Architect and the ordinary ending. No duplicate victory closure or automatic bonus rewards.

## First boss design

A royal gray rat on a dark throne, black robes, gold crown and green scepter, derived from the supplied poster. The current implementation uses a new transparent sprite and Godot AnimationPlayer states, a mod-owned formation and references the game's Queen background. It does not redistribute extracted game art. The sprite is a flat animated illustration, not a layered character rig.

430 base HP plus native party scaling, with two native Two-Tailed Rats. A fixed four-move cycle gives predictable response windows: Royal Decree (2 Weak + 2 Frail to living players), Tax Collection (20 damage), Scepter Strikes (7 × 3), Exposed Crown (25 base Block, natively scaled in co-op, and self 2 Vulnerable). The rats create immediate target-priority pressure; Crown offers a burst window. Native intents show attacks and custom tooltip text explains both nonattack actions. Generic director affixes and the regular earlier-boss helper do not stack onto this encounter.

## Technical boundaries and remaining design work

The mod identity stays LaRata. BaseLib 3.4.7 supplies custom models, localization, animation conversion and serialized run fields. A small game-version-pinned adapter appends the optional act and intercepts ending transitions. Existing game files and real profiles are not modified.

This prototype requires natural solo/co-op playtesting for pacing, difficulty, party composition and accessibility. Four local clients can demonstrate network consistency in specified scenarios; they cannot establish four-human Steam stability or enjoyable balance. Full layered animation, custom arena production and broader boss mechanics remain future work. See TEST-REPORT.md for actual validation rather than treating design intent as completion evidence.
