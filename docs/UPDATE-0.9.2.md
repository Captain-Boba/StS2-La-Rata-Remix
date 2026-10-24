# Version 0.9.2 — Royal Rebalance

## Patch notes

- Halve La Rata's base HP from 3,000 to 1,500. With native co-op scaling, he now has 1,500 / 3,900 / 5,850 / 7,800 HP for one / two / three / four players. Kingsguard HP is unchanged.
- Remove Sewer Alchemy and Plague of Crowns from La Rata's boss deck without replacements, leaving 23 cards. Rotting Tribute remains. Playable booster variants are unchanged.
- Remove Sewer Alchemy from La Rata's interventions in regular and elite encounters.
- Add Stolen Hour, Royal Pardon and Nightfall Decree to the intervention pool alongside Royal Flush, Stolen Aegis and Exposed Crown. Intervention frequency, hidden-card warnings and cancellation by defeating the marked enemy are unchanged.
- Preserve existing hidden intervention data while extending its card encoding to support the expanded pool.
- Reduce Ashen Spool from 3 Block to 1 Block per exhausted card. Its first-exhaust card draw each turn is unchanged.
- Remove the Doomweaver/Shroud twist from random encounter generation. Native enemy powers and royal card effects are unchanged.
- Remove Inklet from random reinforcement pools. Three early helper types and six later types remain; native Inklet encounters are unchanged.
- Place potion-history hover tips above the popup's dimming layer and outside the scrolling list's clipping area.

## Intervention cards

| Card | Effect |
| --- | --- |
| Royal Flush | Add 2 Dazed to each living player's draw pile and 1 Burn to their discard pile. |
| Stolen Aegis | The marked enemy gains 24 base Block and 1 Buffer; existing Buffer is not increased. |
| Exposed Crown | The marked enemy gains 16 base Block and 2 Vulnerable. |
| Stolen Hour | Add 2 Dazed to each living player's draw pile; the marked enemy gains 12 base Block. |
| Royal Pardon | Remove Weak and Frail from all living enemies; the marked enemy gains 10 base Block. |
| Nightfall Decree | Give all living enemies 2 Ritual, capped at 6 total. Uses native Ritual timing. |

Block and Buffer use native multiplayer scaling. The intervention is announced before the enemy turn; the card's identity stays hidden until it resolves. Killing the marked enemy cancels it.

## Installation and compatibility

Requires game **v0.107.1** and **BaseLib 3.4.7**. The 0.9.1 starter booster fix is included. Use the same game and mod versions throughout a co-op party; do not update during an active shared session. Start a new run for the revised encounter rules. See [INSTALL.md](INSTALL.md).

## Validation and work in progress

- C# build and native content checks pass. Native scaling confirms all four HP values above; the six intervention IDs resolve to the intended card models and remain hidden in native power state.
- Policy checks pass 384,000 encounter cases, 40,000 royal turns and 120,000 intervention schedules, including deterministic replay, deck conservation, retired-card exclusion, old/new encoding round-trips and goblin timing separation.
- The dependency vulnerability feed was unavailable during the local build (NU1900 warning); compilation had no errors.
- No fresh live gameplay, visual tooltip acceptance, real mid-run save/load or multiplayer synchronization test was performed for 0.9.2. These checks do not establish gameplay or network acceptance. Balance remains work in progress.
