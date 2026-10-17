# La Rata Remix 0.4.0

2026-10-17, Europe/Berlin. Implementation is compiled; interactive acceptance is incomplete. The owner explicitly paused UI testing after the initial solo checks. No four-player runtime evidence from earlier releases is reused as validation for this update.

## Implemented

- Encounter budgets increased by exactly 25% using integer quarter-points: 5 / 6.25 for small / large normal parties, 3.75 for elites, 2.5 for ordinary bosses. The finale duo remains exempt.
- Twenty affix families including reinforcement, up from eight. Added native Thorns, Regen, Ritual, Demon Form, Barricade, Buffer, Strength, Poison, Doom, Burn, Dazed and Wound combinations. HP changes are 10–18%; damage changes 8–16%. Offense exclusions and finite budgets prevent unrestricted stacking.
- Eligible normal formations have a 50% chance of a small extra monster from the current act's compatible pool. The special rat formation remains forced. Fixed scenes/slots remain protected. Native generated-monster asset discovery includes the extra monster.
- Solo nonempty chests offer three native relic choices and award one. Empty/suppressed rewards remain empty.
- A Thousand Cuts is a rare shared-pool relic. Repeat removal uses native selection, payment, synchronization and saved price escalation. Native removal restrictions retain priority.
- Bonus route: unseen native Ancient with normal entry healing, YOU shop, ARE unknown, NOT elite, PREPARED rest site, then La Rata. Previously visited Ancients and Neow are excluded. Native Ancient healing retains Ascension restrictions.
- La Rata has 540 base HP, two native rat companions, a ten-card draw/discard deck, three cards drawn each turn and 3 Energy (4 at half HP). Native card models render the hand; native monster commands execute it. Attacks precede new offensive buffs/debuffs. The card list is in README.
- Six standing sprite poses run through native Godot animation, with new A Thousand Cuts artwork. No new external runtime dependency, save format or network transport.

## Automated checks

`dotnet run --project tests/LaRata.PolicyChecks -c Release` passes:

- 120,000 native-Rng encounter policies: deterministic replay, nonempty policies, exact budget ceiling, cost limits, 1–3 affixes, stat/debuff caps and offense exclusions.
- 40,000 paired royal turns: identical seeded hands/plans, affordable plays, reshuffling, no lost/duplicated cards and all ten card types reachable.
- The sampled reinforcement rate was 20,031 / 40,000 eligible normal cases (50.08%). This is a policy sample, not measured encounter frequency during a natural run.

Evidence: `artifacts/update-0.4.0/policy-checks.log`. .NET Release compilation and official Godot resource export pass with no compiler warnings/errors; evidence in `artifacts/update-0.4.0/build.log`. These checks do not execute combat effects or prove multiplayer synchronization.

## Observed solo runtime checks

Game v0.107.1 (59260271), BaseLib 3.4.7 and La Rata only. Isolated Steam-off profile 2000. The base game was German; added content was English. Native console commands accelerated the tests. This was not a natural balance run.

Log: `artifacts/logs/20261017-045813-Solo.log`.

- A chest displayed Gremlin Horn, Miniature Cannon and Vajra. Selecting Vajra awarded exactly that relic and closed the choices.
- A Thousand Cuts was granted with the native console. In one shop, the first removal charged 75 gold (1395 → 1320; deck 10 → 9). Cancelling the next selection kept 1320 gold and nine cards. A second completed removal charged 100 (1320 → 1220; deck 9 → 8), and the available next price became 125.
- The custom encounter entered with La Rata at 540 HP and two rats at 21/18 HP. The standing sprite and three native card faces appeared.
- The first seeded hand was Exposed Crown / Borrowed Tomorrow / Scepter Storm. Execution spent energy 3 → 2 → 0 → 0 for Scepter Storm, Borrowed Tomorrow and Exposed Crown. The player received Doom and Weak; La Rata gained 16 Block and self-Vulnerable. The next hand was Royal Tax / Royal Flush / Stolen Aegis, planning Royal Tax and Royal Flush.
- No gameplay exception was found in that scenario. Godmode was used to observe the enemy turn; damage balance was not evaluated.

The first boss view exposed overlap between the cards and the boss intents, plus a dark heading. The final code moves the hand left, increases card size and gives the heading a light outline. It uses native preview holders and the native card-inspection screen for readable details. Those final presentation changes compiled, but their in-combat appearance and click behavior have **not** been rechecked.

## Failures retained and test pause

The first 0.4.0 startup (`20261017-045736-Solo.log`) failed because enemy-only native card models were automatically entering BaseLib's player content registration without a pool attribute. They now register their model types without adding themselves to player reward pools; the next client initialized and ran the above scenario.

The corrected-view client (`20261017-051356-Solo.log`) initialized and loaded the last ordinary Neow checkpoint. Debug `room`/`fight` jumps had not created normal map checkpoints, so this reload does **not** validate persistence of the new chest, relic or boss deck. The window was resized and then closed normally before another fight. Its shutdown log contains engine resource-leak diagnostics after FMOD shutdown; these are retained, not reported as a clean full-log run.

The owner then requested: **pause UI tests**. No further game launch or UI interaction was performed after that request.

## Required follow-up before acceptance

1. Inspect the corrected hand, energy/cost readability, native card detail screen and all attack/cast/hit/death poses in combat.
2. Traverse the actual bonus route from the regular finale. Check unseen Ancient selection, healing, labels, shop, elite, rest and Architect transition. Recheck bonus-loss victory preservation.
3. Exercise all ten card effects, the half-HP energy increase and deck reshuffle, including dead players and native Artifact/Buffer interactions.
4. Load ordinary native checkpoints for chest offers, A Thousand Cuts and the boss. Verify deterministic encounter reconstruction and persistent removal costs. Debug fight commands alone are insufficient.
5. Run four local clients on identical files. Compare native checksums plus complete ROYAL PLAN / ROYAL PLAY sequences, new twist outcomes and route selection. Then test genuine four-machine Steam play separately.
6. Check reinforcement layouts across acts, particularly weak encounters and enemies that summon later. The broader eligibility has source checks but no new runtime formation coverage yet.
7. Check removal without A Thousand Cuts, acquiring it after the shop's removal was already used, multiplayer ownership and natural reward/shop availability.
8. Recheck optional mods on a fresh run. Earlier 0.3.x compatibility evidence is historical; 0.4.0 has only been run with BaseLib.

Start a new run for 0.4.0. Encounter seed rules and bonus map topology changed; do not upgrade an active 0.3.x bonus run in place. No real Steam profile, Workshop source file, original game binary or Steam launch option was changed. No commit or push was made for this update.
