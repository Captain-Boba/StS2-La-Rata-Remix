# La Rata Remix 0.8.2 — Gifts from the throne

2026-10-18. Local development build for game v0.107.1 and BaseLib 3.4.7. English mod content. The user explicitly reopened local in-game testing during this update. No commit or publication is authorized.

## Starting relics

Every player receives native **Bowler Hat**, **Potion Belt** and **White Beast Statue** during new-run relic finalization. Their actual native effects apply: +25% gold, +2 potion slots (normally five total), and guaranteed potion rewards after combat. The independent +2-capacity patch is removed. Existing owners do not receive duplicates. Native saves preserve inventory and capacity; loading/reconnecting does not run starting relic finalization. Existing runs retain their saved capacity and do not retroactively receive the new relics. Start a new run for the complete setup. The map legend names the three starting relics and still shows actual potion capacity.

## Player cards and relic

| Card | Energy, base / upgraded | Base | Upgraded |
| --- | --- | --- | --- |
| Emerald Ward | 2 / 2 | All living players gain 5 Block and 1 Energy | All living players gain 10 Block and 2 Energy |
| Scepter Storm (playable booster prize) | 2 / 2 | Apply 1 Weak and 1 Vulnerable, then deal 10 damage | Apply 2 Weak and 2 Vulnerable, then deal 20 damage |
| Cleanse | 2 / 1, Exhaust | Remove all Debuffs and all combat Curse cards from all living players | Same effect at lower cost |

Emerald Ward uses native Block modifiers and Energy gain. Scepter Storm's damage follows its Vulnerable application: against an unprotected target the usual Vulnerable multiplier can make the hit 15/30 before other modifiers. The enemy-only Scepter Storm remains a separate 1-Energy, 7×3 attack.

Cleanse selects Curse copies from Hand, Draw, Discard, Exhaust and Play, then calls native `CardPileCmd.RemoveFromCombat`. Status cards are not selected. Permanent deck originals are never removed; the Curses return in later combats. Existing Debuff removal, including negative/temporary stats, remains in place.

**Bottled Echo** now gives 1 Strength and draws 2 cards immediately after drinking a potion in combat. It uses the native drinking action's choice context if drawing triggers another player choice. NPC potion splashes still do not count as drinking an inventory potion.

## Goblin potions

The audited native pool expands from 12 to **58**. Class mechanics, card generation, upgrades, draw, energy, stars, forging, Osty, potion generation, Poison and Doom are included. Native card-selection screens handle choice potions; no invented choices or new network messages. Choice effects block the between-round visit until the recipient has answered, then the next throw proceeds. Both helpful and harmful effects retain their native values.

All 64 non-mock native potion classes are classified. Excluded:

- Fairy in a Bottle: automatic revival, explicitly excluded.
- Deprecated Potion: obsolete no-op model.
- Explosive Ampoule, Potion of Binding and Shackling Potion: native effects target the owner's enemies rather than the individual splash recipient.
- Foul Potion: native combat effect damages the entire battlefield rather than the individual recipient.

Conditional eligibility avoids empty selections: Essence of Darkness/Focus require orb capacity; Fortifier requires Block; Liquid Memories requires a discard; Droplet of Precognition requires a draw pile; Ashwater/Gambler's Brew require a hand; Blessing of the Forge requires an upgradable hand card; Soldier's Stew requires a Strike; Touch of Insanity requires a card with an energy/star cost; Entropic Brew requires a free potion slot. Potions from other mods are not automatically executed without auditing their callbacks.

Recipients are sorted by NetId. A seeded v2 potion stream selects different eligible bottles before any presentation/effects occur. Throws and effect completion are awaited sequentially. Entrance standing time drops from 1.2 seconds to 0.2; the one-second entrance and readable wind-up, flight and reaction remain. The schedule is unchanged: one eligible encounter in each three-combat block, arriving between rounds. Short fights can end before the visit.

## Kingsguard

All Kingsguard Plating grants and their cap are tripled; all attacks and their matching intents are doubled. Native multiplayer scaling remains. These multipliers do not apply to director twists, ordinary knights or La Rata.

| Knight | Base HP | Starting base Plating | Three-turn cycle |
| --- | --- | --- | --- |
| Crown Bulwark | 220 | 30 | Other guards gain 14 Block, all gain 6 Plating → 18×2 damage → 36 damage and 10 self Block |
| Crown Headsman | 210 | 24 | 20×2 damage → other guards gain 9 Plating, self gains 16 Block → 36 damage |
| Crown Armorer | 165 | 18 | All gain 9 Plating → 16 damage and 8 Block to other guards → heal the most injured other guard for 25% max HP and give it 12 Plating; if alone, 12 self Block |
| Crown Herald | 155 | 18 | 10×2 damage → heal all living guards for 10% max HP → all gain 10 Block, other guards gain 6 Plating |

Heals round upward and do not revive. Plating cap is 42 base-equivalent stacks. Native Plating scaling is 1/3/5/7 for 1/2/3/4 players: four-player starting Plating is 210/168/126/126, capped at 294. This is deliberately a substantial sustain increase, with balance still untested.

La Rata's stats are unchanged. His 1,080 base HP become about 5,616 in four-player final-boss scaling; he still draws 3 cards with 3 Energy, rising to 4 below half HP. More four-player action pressure is a possible next balance change, not implemented or claimed tested here.

## Art and presentation

Regent Booster uses orange/amber foil with the existing gold crown motif, based on the native Regent palette (#E36600). The royal intervention marker grows from 72 to 88 pixels, uses vivid emerald/mint shading and never pulses below 90% opacity; the hidden card is still not revealed in advance.

The invitation now uses a new wide throne-room painting, with La Rata's face in shadow and glowing emerald eyes. Its native event portrait fills the viewport with aspect-preserving cover, behind the original title, text, choice buttons and votes. The right side of the painting is intentionally quiet for readability. No custom voting or menu replacement. The original title art is untouched.

Generated images use built-in OpenAI image generation, are saved inside the project and retain their original pixels. Prompts, source filenames and hashes are in `docs/art-prompts/manifest-0.8.2.json`. No new dependencies.

## Verification and limits

The native-model content suite checks 36 card models, all 16 player upgrades with canonical isolation and native JSON reload, precise Emerald Ward/Scepter Storm values, 11 custom relic saved-property roundtrips, coverage/entrypoints for all 64 native potion types, and the six doubled Kingsguard attack intents. These are not full battle tests.

An attempted standalone Cleanse combat fixture could not initialize native Player/SaveManager without Godot's engine bindings and terminated before executing Cleanse. It was not treated as a pass or replaced with fake players. Curse-removal behavior is source-verified against the native command. Live combat, potion-choice synchronization, save/rejoin, full-screen UI and actual four-player balance require separate engine tests; results will be recorded below.

Final build, policy and exported-resource results are recorded in `artifacts/update-0.8.2/`. All peers need matching files; start a new run after updating. No original game files or saves are modified.

### Live solo checks, 2026-10-18

Steam-disabled test client 2000, separate Profile 2, Ironclad A0, seed AVA9PKEM8Y. Log: `artifacts/logs/20261018-053700-Solo.log`. BaseLib and La Rata only; this is not a Workshop compatibility test.

- New-run inventory visibly contained Bowler Hat, Potion Belt and White Beast Statue, with exactly five potion slots. The map legend listed these benefits.
- Cleanse removed 3 Weak and combat copies of Greed in Draw and Regret in Discard. Wound remained in combat. The permanent deck still showed Greed afterward. Hand/Exhaust/Play curse cases and other players were not exercised in this solo case.
- Emerald Ward changed Energy 3 → 2 and granted 5 Block. Emerald Ward+ changed Energy 2 → 2 and Block 5 → 15. Both remained playable and the next turn completed.
- Bottled Echo triggered from an Energy Potion in turn 3: Strength 0 → 1, hand 5 → 7, draw pile 6 → 4, immediately before ending the turn. No action queue stall occurred.
- A natural hidden royal intervention announced in turn 2 and resolved Sewer Alchemy before enemy moves. Marker readability was not assessed in that sequence.
- Found and fixed missing Energy icons in Emerald Ward and Grave Spark: the native SmartFormat formatter requires `energyIcons()`. Added native formatter regression checks for one and two icons, which passed. The corrected DLL was installed before the subsequent Koop test.
- The native `card` command cannot select the pool-excluded boss cards/prizes; failed console lookup is not counted as a gameplay pass for Scepter Storm.

The physical Escape interruption was cleared by the user's explicit instruction to resume. UI automation occasionally needed a second click to confirm native multi-creature targeting; once confirmed, the cards resolved normally. No gameplay exception was observed in this solo scenario. Four-peer and remaining presentation results follow separately.

### Four-local-client gameplay, 2026-10-18

Native ENet, Steam disabled, host 1 and clients 2101/2102/2103, seed 33BHIGM9R5. Only BaseLib 3.4.7 and La Rata 0.8.2 were loaded. An existing native four-player invitation checkpoint was used; the previous isolated test profiles were backed up first. Logs: `artifacts/logs/20261018-060635-LoadHost.log`, `20261018-060735-Client1.log`, `20261018-060844-Client2.log`, `20261018-060933-Client3.log`.

- The new throne-room painting filled the 1282 × 752 test windows on all four peers, behind readable native text/buttons/votes. Three yes votes waited for the fourth; unanimous acceptance entered bonus act index 3 while retaining regular finale index 2.
- Entering the Ancient healed three players from 34/80 to 80/80 and one from 22/68 to 68/68. Orobas choices resolved through native interfaces: two players received Black Blood; two received Archaic Tooth and the Bash-to-Break transformation. The map showed YOU shop, ARE unknown and NOT elite.
- Native `fight LARATA-KINGSGUARD_ENCOUNTER` started a targeted combat. In Bulwark/Headsman/Armorer/Herald order, max HP was 1056/1008/792/744 and starting Plating 210/168/126/126. The doubled attack intents were visible across the three-turn cycle. Test-only Buffer and controlled damage protected the party and exposed heals; this was not a natural difficulty test.
- After controlled damage, HP 766/676/418/370 became 872/777/498/445 on Herald turn two: exactly ceil(10% max HP) each. Armorer turn three healed the most injured other guard, Herald, from 445 to 631: 186 HP, exactly 25% of 744.
- Emerald Ward cost 2 and gave every living player 5 Block/1 Energy; the host's Energy changed 3 → 2 and other players 3 → 4. Ward+ then gave another 10 Block/2 Energy; host Energy stayed 2, others reached 6, and all had 15 Block. The corrected Energy icons were visible.
- A naturally scheduled goblin arrived between enemy turn three and player turn four, combat ordinal 4. All peers recorded the same recipient/potion order: 1 Heart of Iron; 2101 Ghost in a Jar; 2102 Distilled Chaos; 2103 Vulnerable Potion. Effects resolved in sequence, including Distilled Chaos autoplay. Turn four became playable; no action queue stall occurred.
- Player 2103 then played Cleanse. It removed the splash's 3 Vulnerable, Regret from Hand and Doubt from Exhaust. The Exhaust pile afterward contained only Cleanse. Beneficial Buffer remained; enemy intent damage returned from Vulnerable-adjusted 30×2/15×2 to 20×2/10×2. All four recipient callbacks completed.

All 31 native checksum IDs 0–30 and their values matched on all four peers. Native `PlayCardAction.ToString()` includes `AbstractModel.ToString()`'s process-local `RuntimeHelpers.GetHashCode`; the comparison script now normalizes only that text field. Card ID, combat index, target and actual checksum are still compared strictly. Regression probes accepted differing object identities but rejected changed checksum/card/index/target. No network or gameplay state was rewritten.

The live comparison passed before closing clients (`artifacts/update-0.8.2/coop-checksums.log`). Reproducible log prefixes through checksum 30 are retained beside `coop-checksums-gameplay-snapshot.log`; they cover exactly the gameplay window above. Complete original logs remain under `artifacts/logs`.

**Teardown limitation:** after manual client closure, the first host log records native ENet `Peer not connected` from `NetQualityTracker.Update`/`NetHostGameService.SendMessageToClientInternal`, followed by null-peer/resource-cleanup diagnostics. These occurred after the final gameplay checkpoint and are not hidden by the comparator. The complete-log error gate therefore does not pass, and the session is not described as wholly error-free.

### Four-client saved bonus-path reload

Logs: `artifacts/logs/20261018-064816-LoadHost.log`, `20261018-064909-Client1.log`, `20261018-064928-Client2.log`, `20261018-064945-Client3.log`. Native checkpoint copy: `artifacts/update-0.8.2/bonus-ancient-reload.save`.

All four peers rejoined through the native load lobby, readied and loaded the completed Orobas event in bonus act 3, coordinate (3,0). Full HP and the selected relic rewards persisted; the completed event offered Continue. Saved custom fields retained endgame version/state 4 and regular finale 2. No error was observed through this reload entry. The host subsequently used Save & Quit before all test windows were closed.

The console-started Kingsguard fight was not a saved map node. This validates the native bonus-path checkpoint and completed Ancient rewards, **not restoration in the middle of combat**. Fresh-run starting relics were checked in solo; this historical co-op checkpoint was not retroactively granted them.

### Remaining coverage

No full natural run or four-human balance claim; La Rata's unchanged four-player stats were not fought in this update. The 58 supported potion models are technically audited, but only the four listed splash effects were observed here; selection-potion synchronization, disconnect/rejoin during a choice, and mixed Workshop compatibility remain untested. Scepter Storm gameplay, the brighter intervention marker's readability, Regent pack opening presentation, and the Cleanse Play-pile Curse case remain outside these live checks. No Steam profile or original game file was edited. No Git commit, push or GitHub release was made.
