# La Rata Remix design

Updated 2026-10-18 through the authorized 0.8.2 prototype. All mod-added content is English. Historical validation remains in the versioned test reports.

## Core difficulty

Increase the existing 1–4 player seeded encounter budgets by 25%, with a wider pool of native powers/status effects, bounded HP/damage changes and more eligible multi-enemy encounters. Preserve Mecha Knight suppression, map panel and regular finale duo. Version 0.8.2 grants native Potion Belt and White Beast Statue at new-run start instead of an independent +2-slot bonus. Starting with 0.6.1, each player receives the native Bowler Hat instead of a separate gold multiplier. No universal large HP/damage multiplier. Exact numbers and exclusions are in README.md.

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

One randomly selected encounter per three-combat block schedules a goblin visit. Arrival occurs between rounds after enemy turn two or three, before the next player action phase, never on combat entry. The native hook runs after block clearing so a splash of Block Potion remains effective. A short fight can end before its visit. Each living player, sorted by stable NetId, receives a different potion from 58 audited native effects, with recipient eligibility checks. Native choice UI handles selection potions; throws resolve sequentially. Native effects may change inventory or permanent max HP (Entropic Brew, Fruit Juice). Revival and incompatible global effects are excluded. No new network protocol is introduced. See UPDATE-0.8.2.md for the complete rules. Native room history, run seed and fixed RNG salts reconstruct the schedule and choices on combat restart.

## Technical boundaries and remaining design work

The mod identity stays LaRata. BaseLib 3.4.7 supplies custom models, localization, animation conversion and serialized run fields. A small game-version-pinned adapter appends the optional act and intercepts ending transitions. Existing game files and real profiles are not modified.

This prototype requires natural solo/co-op playtesting for pacing, difficulty, party composition and accessibility. Four local clients can demonstrate network consistency in specified scenarios; they cannot establish four-human Steam stability or enjoyable balance. Full layered animation and custom arena production remain future work. See TEST-REPORT.md for actual validation rather than treating design intent as completion evidence.

## 0.6.0 party-sized chaos amendment

The owner requested chaos and variety rather than nearly flat co-op budgets. Multiply solo twist budgets by party size for normal/elite/ordinary boss encounters, distribute effects among enemies and allow budgeted reinforcement alongside other twists. Increase the pool to 31 families, retaining separate limits on direct offense, extra hex sources, defensive stacks and formation size. The exact current rules and remaining test gaps are in UPDATE-0.6.0.md; these supersede earlier generator descriptions.

## 0.7.0 Kingsguard and offscreen interference

The NOT elite becomes The Kingsguard: four dark knights with emerald accents, using two native Flail Knight models plus Magi Knight and Spectral Knight. Preserve native skeletal animation but replace their mechanics with coordinated protection, bounded Plating, substantial HP and healing. The regular knight elite is unchanged.

La Rata occasionally intervenes from offscreen, once in roughly three normal/elite fights. Exclude bosses and Kingsguard and avoid the goblin's arrival round. Announce an unknown card above one enemy at the start of a player turn; never reveal which card before play. Killing the marked enemy cancels it. Resolve the card before enemy attacks, with the native card-play presentation and original ElevenLabs ratlike laughter, no visible boss. See UPDATE-0.7.0.md for exact implementation and remaining test boundaries.

## 0.8.0 royal collection

The owner authorized a 20-card royal boss deck with individual card artwork, ten additional neutral cards, six shared-pool boosters and five enabler relics. Boosters open once, keep their relic memento, offer five different cards, and permit retaining any subset. Royal packs have a 20% per-pack chance of one playable boss-card variant. The opening starts with five facedown cards, shows rarity shimmer on hover/focus and requires individual flips with rarity-dependent effects. Larger Kingsguard knights gain flowing emerald breastplate energy. See UPDATE-0.8.0.md for exact content, native integration and remaining gameplay/UI/network tests. Interactive game tests remain paused.

## 0.8.1 progression and rare twists

Borrowed Breath+ heals 6 HP and draws 3 cards. Lighter charcoal/jade card frames retain native sculpted details with emerald highlights. Twist budgets grow by act while remaining linear in party size; flat defenses scale separately. Reinforcements are more frequent and can use up to three extra slots in later large-party normal fights, always capped at four starting enemies. Ten additional audited native-power families expand the pool to 40 kinds plus reinforcement. Exact values, exclusions and untested gameplay boundaries are in UPDATE-0.8.1.md. Well already has seeded contest resolution but still has no dedicated well animation. No installation into the active co-op session.
