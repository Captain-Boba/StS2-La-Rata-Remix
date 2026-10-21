# 0.8.4 — First playtest feedback and starting boosters (local test build)

2026-10-18. Game v0.107.1 / BaseLib 3.4.7. Prepared for the owner's requested local installation and solo test. No game windows were started and no active playtests were performed by the assistant. Workshop publication remains unchanged. No new dependencies.

## Starting class booster

- Every player receives their native class's booster during new-run starting relic acquisition: Ironclad, Silent, Defect, Regent or Necrobinder. Third-party characters without a dedicated pack receive the La Rata pack instead.
- Native `RelicCmd.Obtain` supplies the relic, grab-bag removal and callbacks. Opening is deferred until the first room's native fade-in has completed, when the run UI and synchronized choices are available, alongside the initial Ancient relic choice. It uses the existing animated opening and native reward-selection protocol.
- The deferred opening flag uses BaseLib/native saved properties. Completed packs never reopen, pending packs resume after the room transition, and loading a run never grants an additional starter. Existing runs are not retroactively changed.
- Starting packs also contain seven cards: five class cards and two mixed regular/La Rata neutrals, with any number kept. Native character mapping, pickup deferral and pending/opened save-property roundtrips are covered by model checks; the actual first-room UI and co-op flow still require playtesting.

## Fixes and presentation

- Bestiary previews build monster move states before attaching a Creature. The hex patch now skips that preview lifecycle, including NullCombatState, while retaining native debuff/status intents in actual encounters. La Rata's preview no longer attempts to select combat relics without an encounter.
- Royal intervention tooltips anchor beside the green marker and follow its owner. Royal relic tooltips also use explicit native alignment.
- RPS keeps the native mouse cursor visible while its choice popup is open, even when SharedRelicPicking screen notifications request hiding it. Closing restores the previous visibility request; controller cursor behavior stays native.
- Boosters are Common and contain seven distinct cards: the original five plus two from the combined unlocked regular colorless / La Rata neutral pool. The Royal pack's 20% boss-prize roll still replaces one of the original five slots. Previously saved five-card packs retain their contents and can still be opened.
- Opening uses seven evenly sized slots. The flip animates a separate face container, leaving its glow and hit area stable. Soft textured particles replace untextured square particles; all card faces and bodies explicitly use the same scale. Revealed cards remain hoverable, with a larger native card tooltip and keyword effects. Native synchronized reward selection still decides which cards enter the deck.

## Combat histories

- **Royal discard** opens the native arbitrary-card viewer with cards already played by La Rata, including normal-combat interventions. Card hover tips include the round. Planned, cancelled and unplayed cards are excluded.
- **Goblin history** opens a native popup listing potion icons, names, rounds, recipient seats/characters and native effect tooltips.
- History buttons are unavailable during action-disabled transitions. Views close when the combat resets/ends or their room exits. Local viewing does not submit a player choice or consume RNG.
- BaseLib's existing saved run fields carry the journal; no separate storage or protocol. Native unfinished-combat saves resume at the combat-entry checkpoint, not mid-turn: the journal resets with that checkpoint and is rebuilt by replayed actions. This does **not** promise retention of later turns from an abandoned combat attempt. A genuine game save/reconnect test remains pending.

## Chaos balance

The total budget is exactly **75% of 0.8.3**, using integer sixteenth-point units to avoid per-player rounding differences. Party sizes 1/2/3/4 receive exactly 1/2/3/4 times the solo budget. Act multipliers remain 1 / 1.4 / 1.8 / 2.2.

| Act-one encounter | 1 player | 2 players | 3 players | 4 players |
| --- | ---: | ---: | ---: | ---: |
| Normal | 3.75 | 7.5 | 11.25 | 15 |
| Elite | 2.8125 | 5.625 | 8.4375 | 11.25 |
| Ordinary boss | 1.875 | 3.75 | 5.625 | 7.5 |

Existing affix and reinforcement prices are unchanged. As a consequence, a solo act-one encounter cannot afford a 4-point reinforcement, including the formerly forced fourth rat; solo reinforcement resumes from act two. An ordinary solo act-one boss can receive no added twist because its budget is below the cheapest eligible first twist. Neither case silently exceeds the reduced budget. Finale duos, Kingsguard and La Rata keep their separate rules.

Removed from random selection: **Armored/Plating, Slippery, Spiteful/Rupture and Skill Hunter/Enrage**. Empowered now gives 2 Vigor and amplifies small Strength applications only. No random twist grants or amplifies Plating. Native monster mechanics and royal cards are separate from the twist pool.

Inklet reinforcements retain their original attack damage instead of the generic 65% helper multiplier. A Regen twist on any Inklet is capped at **1**. On other monsters it is capped at the lesser of its act-scaled grant and 10% max HP, minimum 1. Twist Regen does not receive another native party multiplier, and does not top up an existing amount above its cap. The scaling exemption applies only to the exact native power instance being granted by this twist; other Regen sources retain their own behavior. Native Inklets retain their intrinsic Slippery mechanic.

Eight new twists use existing native powers/status cards and the existing intent extension:

| Twist | Effect | Cost |
| --- | --- | ---: |
| Reckless | 2 Strength, but 1 Vulnerable | 3 |
| Brittle Ward | 18 act-scaled opening Block, but 2 Vulnerable | 2 |
| Toxic Shell | 3 Thorns, but 3 starting Poison | 2 |
| Last Stand | 1 Demon Form, but 1 starting Weak | 3 |
| Fading Might | 5 temporary Strength and 1 Weak for the opening enemy turn | 3 |
| Leaden | Announced action adds 1 Slimed to each target's discard | 2 |
| Static Hex | Announced action shuffles 1 Void into each target's draw pile | 4 |
| Crossfire | Announced action applies 1 Weak and 1 Vulnerable, capped at 2 each | 4 |

This leaves **44 active twists**, plus reinforcement. Existing offense, hex, shielding, block-engine and retaliation compatibility limits still apply. A 25% budget cut is not a measured 25% reduction in combat difficulty; native power scaling and encounter mechanics still matter.

## Verification and remaining acceptance

- Release C# build against the installed native assemblies.
- 384,000 deterministic encounter policy cases: exact 25% budget cut, party-linear budgets, stage growth, cost accounting, target/formation limits, excluded families, all 44 active families reachable.
- 40,000 royal turns and existing goblin/intervention schedule/replay checks.
- Content/model checks include an actual Harmony-patched native Bestiary setup with no Creature and with NullCombatState; a real encounter still receives its extra debuff intent. These checks do not instantiate the Bestiary's Godot screen.
- Scoped Regen scaling and Inklet helper damage checks; existing card upgrades, potion classification, Kingsguard intent values and helper models.
- Pending booster contents round-trip with five and seven cards; Common rarity and stable mixed-neutral pool exclusion checks. Actual animated opening/selection is not exercised by these model tests.
- Journal JSON and BaseLib save-field export/import, combat-key isolation, malformed optional-data fallback, reset. No real save/reconnect or network claim.
- NuGet's vulnerability-feed lookup is unavailable in the restricted environment (NU1900); no dependency was added or updated.

**Pending:** in-game starting-booster timing, Bestiary navigation, RPS mouse/controller behavior, seven-card layout/hover/flip at different resolutions, combat-history layout and lifecycle, save/reload/reconnect, other-mod compatibility, and four-human difficulty/desynchronization playtests. Automated tests are not visual or gameplay acceptance. Local installation is authorized for the owner's solo test; Workshop publication is not part of this update. Use a fresh run for the starting booster and changed ruleset, with every co-op peer on the same mod version.
