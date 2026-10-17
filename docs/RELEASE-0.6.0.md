# La Rata Remix 0.6.0 — Party-Sized Chaos

A standalone difficulty mod for solo and 2–4 player co-op. This release includes the compiled mod; no development tools or compilation are required.

## Download and install

1. Download **[LaRata-0.6.0.zip](https://github.com/Captain-Boba/StS2-La-Rata-Remix/releases/download/v0.6.0/LaRata-0.6.0.zip)** below. Do not choose the automatically generated **Source code** files.
2. Close the game and open its installation directory through Steam's **Browse local files** action.
3. Create **mods** next to `SlayTheSpire2.exe` if that folder is missing.
4. Extract the ZIP and copy its **LaRata** folder into **mods**. The final path must be `mods/LaRata/LaRata.dll`, alongside `LaRata.json` and `LaRata.pck`.
5. Install **[BaseLib 3.4.7](https://github.com/Alchyr/BaseLib-StS2/releases/tag/v3.4.7)** separately using the author's compiled package.
6. Remove **--nomods** from Steam launch options, enable both mods in-game and follow any restart prompt.
7. Start a **new run**. Every co-op player needs identical mod files and the same game version.

**Requires Slay the Spire 2 v0.107.1 and BaseLib 3.4.7.** Other game versions are rejected. Read **[INSTALL.md](https://github.com/Captain-Boba/StS2-La-Rata-Remix/releases/download/v0.6.0/INSTALL.md)** for the complete folder layout and troubleshooting. The guide is also inside the ZIP. `SHA256SUMS.txt` contains download checksums.

## Included features

- Add 31 encounter twist families with budgets scaling linearly for one to four players.
- Distribute twists across enemies and combine modifiers with budgeted reinforcements.
- Raise eligible normal-encounter reinforcement chances to 50/65/80/95% by party size, with up to two additional enemies in larger parties.
- Add defensive, growth, poison, Doom, status-card and stat-reduction mechanics, including eleven new twist families in 0.6.0.
- Limit added offensive stacking, defensive combinations and debuff accumulation; retain native attack previews and announced extra actions.
- Grant 25% more newly gained gold and two additional starting potion slots, with a collapsible map panel showing run effects.
- Pair compatible regular final bosses with a previously unseen earlier-act boss at full native HP.
- Add a unanimous optional bonus route through an unseen Ancient, shop, unknown room, elite and rest site to La Rata; preserve the regular victory on bonus defeat and return to the Architect after bonus victory.
- Give La Rata a ten-card deck, energy, five randomly selected combat relic adaptations, hidden-hand card animations and a dark emerald royal court.
- Add an animated potion goblin who appears between rounds and throws a different random potion at each living player.
- Add Cleanse, a rare neutral card that removes active debuffs from all living players, including negative and temporary stat reductions.
- Offer three relic choices in solo chests; add A Thousand Cuts for repeated paid shop removals with normal price escalation.
- Let treasure contenders choose Rock, Paper or Scissors privately and play native reveal animations; add Well to win contests against nonholders, with equal seeded chances between Well holders.
- Include updated relic artwork, standing boss animation, smoother idle motion and a slower four-pose goblin appearance.

## Current validation limits

This is a playable prototype, not a claim of complete compatibility or balance coverage. The 0.6.0 build/resource export passed, as did 240,000 seeded encounter-policy cases and the existing deck/goblin/relic simulations. Those checks do not execute all native combat effects. The latest twist mechanics, crowded formations, checkpoint behavior and four-peer synchronization still require gameplay testing; older multiplayer results apply only to their recorded versions.

Start a new run for this generator version. If another mod causes errors, first reproduce with only BaseLib and La Rata. See the included `TEST-REPORT.md` and `docs/UPDATE-0.6.0.md` for the evidence and remaining checks.

BaseLib and original game files are not bundled. The MIT license covers original code; raster artwork and branding are excluded from that license.
