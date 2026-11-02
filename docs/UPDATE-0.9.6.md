# La Rata Remix 0.9.6 — Content & Encounters

Requires the Public game branch v0.107.1 and BaseLib 3.4.7. Start a new run and use matching versions throughout a co-op party. The game Beta branch is not supported.

## New Content

- 11 new La Rata cards. Discover their names and effects in game.
- Three new shared relics:
  - **Counterfeit Seal:** The first card you create each turn is upgraded, if possible.
  - **Plague Testament:** When an enemy dies, transfer its Poison to a random enemy.
  - **Usurper's Tooth:** Whenever an enemy gains Strength, gain 2 Vigor.
- Three new shared potions:
  - **Brass Polish:** Gain Vigor equal to your Block.
  - **Grave Distillate:** Swap an enemy’s Poison and Doom.
  - **Amber Hourglass:** Block is not removed at the start of your next 2 turns.
- 10 new encounter twists and 8 additional reinforcement types.
- Boss relic selection expanded to 25 options, with 10 different relics per fight.

## Balancing

- La Rata uses 5 base Energy, increasing to 7 at half HP or below. Lantern and Happy Flower are replaced; retained card/relic resource bonuses remain separate.
- The boss deck contains 41 active cards. Black Tithe, Royal Cleansing, Grave Spark, Sewer Pact and Lord Tibo's Reset are excluded in addition to the previous two exclusions. Player availability is retained. Boss Exposed Crown grants 25 base Block.
- Withering reduces only the next starting hand by one card, does not stack and expires after that hand is drawn. Reinforcement selection avoids recently encountered types when alternatives exist.
- Lord Tibo's Reset costs 2 Energy, or 1 upgraded; both versions Exhaust. All players recover their own unrefunded Energy spent this turn, including actual Reset payments.
- Rebalanced existing cards and upgrades, including discard, Vigor, Poison/Doom and draw synergies. Cleanse is renamed Royal Cleansing.
- Sootbound Ticket makes the first card purchase at each Merchant free and is now Rare. Stairwell Key reduces the first card's cost each turn by 1.
- Brass Scavenger retains its once-per-combat trigger without the former 12-Gold cap. Relic Remover is Uncommon. Several relic descriptions are shorter.
- Rustwater removes all Block and Plating from its target.

## Bug Fixes and Presentation

- Corrected Reset payment/refund accounting across owners, including repeat refunds, actual discounted payments, blocked gains and turn boundaries.
- Added visible damage-ramp indicators for La Rata and both escorts, a Well treasure-contest animation and updated artwork.
- Playable boss cards use a red rarity banner with their normal frame. All ten variants remain in the dedicated La Rata booster prize pool; its single 20% pack roll is unchanged.
- English, German and Simplified Chinese text updated. New boss relic effects have corresponding status descriptions and relevant intents.

## Validation and remaining limits

Targeted native model/command, deterministic replay, localization and resource checks cover the individual content waves. The latest Reset checks cover 262 assertions; the boss-relic checks cover 1024 seeded selections, native effects for 1–4 modeled players, 24 offensive modifier combinations and 96 hand preparations. These are technical fixtures, not live multiplayer sessions.

The obsolete Guard test compared the raw damage callback rather than the actual intent forecast. It now checks native total damage at rounds 1 and 7, including the transition from pending to applied Strength. Production forecast behavior was not changed to satisfy the test. The initial failed default-suite run remains in local evidence; no full-suite pass is claimed.

Final packaging and installation evidence is kept under `artifacts/release-0.9.6/`. Earlier reports remain historical snapshots. No new game starts or live playtests were performed for this release. Full rendered UI, real Steam multiplayer, disk save/load and human balance acceptance remain open. Relic statistics stay disabled, and player trading is deferred.

Relevant reports: [content wave](CONTENT-WAVE-QA-20261102.md), [content revision](CONTENT-WAVE-REVISION-QA-20261102.md), [additional content](CONTENT-EXPANSION-20261102.md), [encounter variety and Withering](ENCOUNTER-VARIETY-20261102.md).

Public Workshop notes are maintained in [EN → CN → DE](../workshop/LaRata/changelog-0.9.6.txt). The existing Workshop title and opening description line are preserved from the live item.
