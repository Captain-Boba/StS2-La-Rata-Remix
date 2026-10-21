# 0.8.5 — Solo variety and presentation

Local test iteration, 2026-10-18, for game v0.107.1 / BaseLib 3.4.7. No new dependencies. No game launch or active playtest by the assistant; Workshop publication is unchanged.

## Why solo felt quiet

The owner's 0.8.4 solo log contains seven ordinary act-one plans with one tactical twist each (two also added HP), followed by a boss plan spending 0/30 sixteenth-points. The effects were running, but the fixed prices left much of the reduced solo budget unusable: normal budget 3.75 versus 4 for a reinforcement; ordinary boss budget 1.875 versus at least 2 for its first eligible effect. Act two already shows a Flyconid helper and mixed effects. The starting seven-card booster also adds player options; its effect on overall difficulty has not been measured.

## Same budget, smaller combinations

- Budgets stay exactly 75% of 0.8.3 and linear across party sizes. Four-player roll ordering, prices and strengths are unchanged.
- Below six points, ten families have Lesser variants at half cost and half magnitude, rounded down with a minimum of one. Only explicitly supported families can be reduced; no fractional native stacks, custom powers or new networking protocol.

| Lesser family | Act-one effect | Cost |
| --- | --- | ---: |
| Hexed | Announced 1 Weak, Frail or Vulnerable | 1.5 |
| Bristling | 1 Thorns | 1 |
| Venomous | Announced 1 Poison | 1 |
| Ominous | Announced 2 Doom, original cap retained | 1 |
| Wound Up | 1 Vigor | 1 |
| Fortified | 6 opening Block | 1 |
| Second Wind | 4 Block after the next block clear | 1 |
| Constricting | Announced 1 Constrict, original cap retained | 1.5 |
| Ember Guard | Retaliate for 1 during the first player turn | 1 |
| Hotheaded | 1 temporary Strength for the first enemy turn | 1 |

- For small budgets, the first unreinforced pick leaves room for a second effect when possible; HP/damage-only picks are admitted after two tactical picks. Native compatibility caps still prevent stacking offensive amplifiers, shields, block engines, retaliation or excess hexes.
- Normal solo act-one encounters can roll a Scout reinforcement for 2.5 points, at 35% HP and 50% attack. Inklets retain normal attack. The existing formation allowlist, slot reservations, helper diversity and four-body ceiling remain intact. Regular helpers retain 60% HP / 65% attack and their four-point cost.
- All sampled solo act-one normal plans have at least two tactical changes, counting a helper as one. Ordinary solo act-one bosses no longer receive an empty plan. This is a policy property, not a claim of equal perceived difficulty or human balance acceptance.

## Presentation

- Center the original native Choose cards button bounds. Previously the code widened only its hitbox to 340px while leaving 180px artwork aligned to its left.
- Replace Royal discard / Goblin history text buttons with native-style icon/count controls above the Exhaust pile. Reuse its native count label, typography and outline. Each counter stays absent until its own first recorded entry; empty Exhaust does not hide history. Viewing remains local and does not consume RNG or submit choices.
- Royal cards still use the native card viewer. The potion log shows potion icon, round, recipient seat and character portrait/name, with native effect hover tips; scroll content stops above the native Close button.
- Repaint the potion goblin with angular matte shapes, a muted hood/olive palette and a less generic silhouette. Preserve idle, wind-up, throw and laugh poses and the existing sequential throw timing. Update atlas gutters, foot registration and bottle origins. [Asset and exact image-generation prompt](art-prompts/potion-goblin-0.8.5.md).

## Verification and boundaries

- 384,000 seeded policy cases: unchanged total/linear budgets, valid Lesser pricing, Scout restrictions, deterministic replay, formation and compatibility caps, all 44 families reachable, nonempty boss plans, at least two tactical changes for ordinary solo act one, and no Lesser/Scout rules in four-player plans.
- Existing royal-deck, goblin and intervention scheduling/replay checks pass.
- Content checks cover native Bestiary lifecycle, relic/card serialization, starting-pack deferral, history field roundtrip, Lesser magnitude/price, and the actual damage hook's Scout factor. Game UI, real saves and multiplayer were not run.
- Native scene data was read headlessly to inspect the popup button and Exhaust counter layout. The generated RGBA atlas was visually inspected and wired into the mod. Source/model and resource checks are not in-game visual acceptance.
- New in-game acceptance remains necessary for button centering, counter placement/clickability at different resolutions, recipient log navigation, goblin animation registration, small-budget combat feel and co-op synchronization. Use a fresh run and matching versions across peers.
- NuGet vulnerability-feed lookup was unavailable (NU1900); no dependency was added or updated.
