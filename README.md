# StS2 - La Rata Remix

Standalone difficulty mod for **Slay the Spire 2 v0.107.1 (59260271)**. Version **0.6.0** adds party-sized chaos: 31 twist families, linear party budgets, distributed enemy effects and up to two small reinforcements. The animated potion goblin, Cleanse and royal encounter remain included. Supports 1–4 players. All mod-added text is English. The base game's language remains your choice. See [current implementation and validation limits](docs/UPDATE-0.6.0.md) and [the previous update](docs/UPDATE-0.5.0.md).

The optional endgame path and the first La Rata boss are included in this prototype. See the [accepted design brief](docs/DESIGN.md) and [endgame integration study](docs/ENDGAME-INTEGRATION.md). The supplied [title artwork](La%20Rata%20Remix%20-%20Titelbild%20-%20V1.png) is the visual reference for the new mod-owned combat sprite.

## Installation

Close the game. Copy the `LaRata` folder containing `LaRata.dll`, `LaRata.json` and `LaRata.pck` into `Slay the Spire 2/mods`, install BaseLib 3.4.7, then enable both mods in-game. Remove `--nomods` from the launch options when you want to play with mods. Every player needs the same game version and identical mod files. Start a **new run** to receive the two additional potion slots. Do not change mod versions during a run.

The game uses separate modded profiles. This mod does not erase, reset, migrate or unlock progress. Original game files are never modified. Removing the LaRata folder disables it for subsequent runs. Keep La Rata and BaseLib installed while a run contains custom relics, events or the bonus act.

## Run effects

- **+25% newly gained gold**, through the native gold hook, including combat, event and relic gains. Native rounding to whole coins applies: 150 becomes 187. Starting gold and existing balances are unchanged; native prevention such as Ectoplasm remains effective.
- **+2 potion slots at run creation**, normally five total. Other native effects may still alter capacity. Loading a save does not add another two slots.
- **Collapsible map panel** below the map legend. Click `La Rata Remix` to expand or collapse it. The panel uses the native map-legend parchment and fonts. It stays fixed while scrolling and shows permanent effects and current potion capacity for each player.
- **Controlled chaos:** seeded encounter changes within a shared difficulty budget, with at least one tactical effect instead of only raising health and damage.
- **Final boss reinforcement:** the regular final boss gains a compatible boss from an earlier act that has not appeared in this run. The helper retains native mechanics and now starts at **100% of its normal multiplayer-scaled HP** in every party size. The duo receives no additional generator affixes.
- **Potion goblin:** one encounter per seeded block of three is selected for a visit between rounds, after the second or third enemy turn. Each living player receives a different randomly selected native potion splash. Short fights that end before the arrival skip the visit. Eight helpful and four harmful potions form the supported pool; no inventory slot is consumed. The splash resolves before the next player action phase, after block clearing, so Block Potion remains useful. His four poses show arrival, wind-up, throwing and laughter, with the selected native potion visible in his hand. Presentation takes about 5.6 seconds solo plus native effect delays; co-op repeats each throw. Instant mode no longer skips the appearance.

## Neutral cards

**Cleanse** is a rare neutral Skill in the native colorless pool: **2 Energy, Exhaust; upgraded cost 1**. It removes active Debuffs from yourself and every other living player, including negative Strength/Dexterity and temporary stat reductions. Beneficial powers remain; this does not remove Curse/Status cards from decks or heal HP. It uses La Rata's black frame and dark emerald banner, with a spectral fountain portrait. Native colorless shops/rewards/events can offer it. The existing base-game card with the same English name is unchanged; the new card has its own BaseLib-prefixed model ID.

## Treasure contests and Well

In solo play, ordinary nonempty chests offer **three relics; choose one** through the game's existing chest interface. Effects that suppress the chest reward remain effective.

**A Thousand Cuts** is a rare shared-pool relic for solo and co-op. Its owner may purchase card removal repeatedly in the same shop. Every removal uses the native payment and permanent price counter: normally 75, 100, 125 gold, and so on. Native discounts and restrictions still apply. Cancelling the card selection does not charge gold.

When several players choose the same chest relic, each contender privately chooses Rock, Paper or Scissors. After everyone locks in, the game's original character hands shake and reveal the chosen signs. Losing hands play the native loss animation; the winner grabs the relic without a second fight announcement. One or three distinct hands tie; with two distinct hands the losing hand is eliminated. Survivors choose again until one remains. Uncontested relics and the game's consolation rewards keep their native behavior.

**Well** is a rare shared-pool relic available only in co-op. A contender with Well wins automatically against contenders without it. Multiple Well holders have equal seeded chances: two holders means 50/50. The losing holders still keep their Well. Choice submission uses the game's own player-choice synchronization, not a separate network protocol. Decisions are hidden in the UI, not cryptographically protected against a modified client.

## Optional endgame

After the regular finale, a shared invitation offers the bonus path. Entry requires every player to vote yes. A no vote takes the party to the Architect. Pending invitations restart voting after loading; a completed decision persists.

The path contains **an unseen Ancient → YOU (shop) → ARE (?) → NOT (elite) → PREPARED (rest site) → La Rata**. The Ancient is drawn from native Ancients across all acts, excluding Neow and anyone already visited this run. Its normal entry healing and reward logic apply, including native Ascension healing restrictions. The unknown room uses the game's normal resolver and Glory content pools. The regular victory is retained in progression and run history if the party dies during the bonus. Defeating La Rata leads to the Architect and the ordinary ending.

La Rata starts with **1,080 base HP**, accompanied by two Two-Tailed Rats at **double their native HP**, including multiplayer scaling. The rats retain native skeletal animation with a dark emerald tint and green spirit particles. His standing sprite uses native Godot animation, with an 18-second smooth breathing, sway and glow cycle instead of frequent idle frame switches. Each turn he draws three cards from a seeded ten-card draw/discard deck, prioritizes expensive affordable cards, and spends **3 Energy, increasing to 4 at half HP or below**. Unplayed cards are discarded; the discard pile reshuffles when needed. His hand is hidden. Black cards with dark emerald banners appear only as they are played, using the native play-pile animation. Native attack intents still announce damage.

Every encounter gives him **five distinct combat relics**, selected from Vajra, Oddly Smooth Stone, Anchor, Bronze Scales, Horn Cleat, Captain's Wheel, Orichalcum, Mercury Hourglass and Lantern. Five native icons and the Royal Regalia tooltip explain the selected enemy adaptations. Lantern adds one Energy to his first hand. The selection replays on loading the same encounter; it does not reroll through save-scumming.

| Card | Energy | Base effect |
| --- | --- | --- |
| Royal Tax | 1 | 12 damage to every player |
| Scepter Storm | 1 | 7 damage three times to every player |
| Off With Their Heads | 3 | 34 damage to every player, then 2 Vulnerable after all attacks |
| Sewer Alchemy | 2 | Double player Poison, add 4; cap the resulting Poison at 14 |
| Borrowed Tomorrow | 2 | 10 Doom and 1 Weak to every player |
| Royal Flush | 2 | 2 Dazed into each draw pile and 1 Burn into each discard pile |
| Stolen Aegis | 2 | 24 base Block and 1 Buffer if none remains; native co-op scaling |
| Blood Contract | 0 | Up to 12 nonlethal self-damage; 2 Strength for the living royal court |
| Exposed Crown | 0 | 16 base Block and 2 self-Vulnerable |
| Emerald Tempest | 2 | 5 damage four times to every player and 1 Burn each |

These are native card models for presentation, with enemy execution through the native monster action system. They do not enter player reward pools. **Spells and powers resolve first, then attacks.** Pending Blood Contract Strength is included in attack previews and removed from the preview adjustment when the actual Strength is applied. Off With Their Heads applies Vulnerable only after all announced attacks. A hand without an attack still finishes with an announced 12-base-damage monster attack.

The rats retain their native mechanics. Generic encounter affixes and the earlier-boss reinforcement do not stack onto this custom finale. This is an initial gameplay design, not a finished balance claim.

## Encounter budget

| Affix | Effect | Cost |
| --- | --- | --- |
| Sturdy | +10–18% starting HP | 1 |
| Keen | +8–16% base attack damage, reflected in intents | 2 |
| Armored | 2–4 native Plating on the selected enemy | 2 |
| Warded | 1 Artifact on that enemy | 2 |
| Empowered | 2 starting Plating and +1 to its small Strength/Plating applications | 2 |
| Echoing | An extra announced hex and +1 to its short Weak/Frail/Vulnerable applications, capped at 3 | 2 |
| Hexed | One native action gains an announced 2 Weak, Frail or Vulnerable | 3 |
| Bristling | 2 Thorns on the selected enemy | 2 |
| Regenerating | 3 Regeneration | 2 |
| Cultist / Demonic | 1 Ritual / Demon Form | 3 |
| Entrenched | Barricade and 8 base Block | 3 |
| Buffered | 1 native Buffer | 2 |
| Furious | 1 Strength | 2 |
| Venomous | An announced action adds 2 Poison | 2 |
| Ominous | An announced action adds 4 Doom, capped at 25% of target max HP | 2 |
| Cinders / Disorienting / Lacerating | Add a Burn / Dazed / Wound through an announced action | 2 |
| Reinforcement | One additional existing small enemy at 60% HP and 65% base damage | 5 per extra |

Budgets scale linearly with **1/2/3/4 players**. Normal: **5/10/15/20** points; elite: **3.75/7.5/11.25/15**; ordinary boss: **2.5/5/7.5/10**. Select up to three changes per player within budget and compatibility limits. Twists are distributed across the least-loaded eligible enemies, including reinforcements. HP and damage bonuses affect their assigned enemy only. Direct damage is not multiplied by party size. At most one added offensive amplifier, and one extra hex source (two with 3–4 players). Each enemy can receive at most one added damage-prevention family and one block-engine family. Native power scaling remains active.

Eligible normal formations have a **50/65/80/95% reinforcement chance**. Each extra costs 5 points; 3–4 players can receive a second on a 50% roll if space permits. Reinforced formations start with at most four enemies. Fixed scenes/slots remain excluded except **Two-Tailed Rats Normal**, which always adds its fourth rat in `second` and preserves `first` for native summons. The existing autonomous pool remains Inklet, Small Leaf Slime, Nibbit, Flyconid, Corpse Slug and Fuzzy Wurm Crawler from the current act, with Inklet as fallback. Co-op reinforcement can coexist with other twists.

Eleven new families bring the total to **31 including reinforcement**:

| Affix | Effect | Cost |
| --- | --- | --- |
| Phased | 1 Intangible | 3 |
| Slippery | 1 Slippery, with native co-op charges | 2 |
| Wound Up | 2 Vigor for the next attack | 2 |
| Agile | 2 Dexterity and 6 base Block | 2 |
| Fortified | 12 base opening Block | 2 |
| Second Wind | 8 Block after the next block clear | 2 |
| Reservist | 6 Block after each of the next two block clears | 3 |
| Shackling | Announced temporary -1 Strength, at most one stack | 2 |
| Sapping | Announced -1 Dexterity, stopping at -1 total | 2 |
| Constricting | Announced 2 Constrict, capped at 4 | 3 |
| Eroding | Announced 1 Disintegration, capped at 2 | 3 |

Added Weak/Frail/Vulnerable actions cap at three stacks, Poison at six and Doom at 25% max HP. Existing higher values are not reduced. See [0.6.0 rules and validation boundaries](docs/UPDATE-0.6.0.md). The finale duo and La Rata keep their separate rules.
**Mecha Knight** retains the original prototype action: after the flamethrower and before winding up, it announces a strong debuff action that applies 3 Weak and 3 Frail to living targets. The extra action also gives players a response window; its net difficulty remains a playtesting question.

## Boss compatibility

Supported regular finales are Queen, Test Subject and Aeonglass. On a double-final-boss run, only the second receives a helper. Candidates come from the actual earlier acts' boss pools, filtered against saved room and monster history and sorted before the seeded choice.

Allowed helpers: Vantom, Lagavulin Matriarch, Waterfall Giant and Knowledge Demon. The Kin and Kaiser Crab require special formations/scenes; Soul Fysh and The Insatiable have special combat/card transitions. Ceremonial Beast remains outside the existing compatibility allowlist pending a separate phase/formation audit, despite the removal of the HP reduction. If no compatible unseen helper remains, the original finale is kept and the reason is logged.

The Queen's native markers are preserved and a helper marker is added in memory. Duo camera scaling is adjusted to fit the larger formation. The main boss owns the soundtrack; helper-specific music parameters are suppressed only in these duos, while native helper sound effects and combat actions remain intact. The compatibility list is not a claim that every combination has passed an entire playthrough.

## Determinism and saves

The director uses the game's `Rng`, derived from run seed, floor, act, encounter ID, party size and ruleset version. It consumes no draws from the native run RNG streams. Per-encounter associations use weak references and are reconstructed from the saved run state. There is no custom network transport or save format.

Enemies, powers, action queues and potion capacity use native combat, networking and serialization. The base game resumes an ongoing battle from its beginning, not an arbitrary mid-turn snapshot. The current history entry is excluded when selecting a helper so that reloading does not mistake the current helper for an earlier sighting.

Native debug `fight` normally randomizes an encounter with local wall-clock time. La Rata disables that debug randomization so targeted co-op tests use the normal run-seeded path as well.

## Build and local tests

Prerequisites for a fresh checkout:

1. Install the .NET 9 SDK and Slay the Spire 2 v0.107.1 for Windows.
2. Obtain BaseLib 3.4.7 from the project linked below, install it in the game, and copy its `BaseLib.dll` to `.research/dependencies/BaseLib.dll` in this checkout. This local dependency is ignored by Git and is not bundled.
3. Extract the official Godot 4.5.1 Windows editor into `.tools/Godot-4.5.1`, with `Godot_v4.5.1-stable_win64_console.exe` at that path. The editor imports the mod's source PNGs and exports the resource pack.
4. The scripts default to `E:\SteamLibrary\steamapps\common\Slay the Spire 2`. For another installation, pass `-GamePath 'C:\path\to\Slay the Spire 2'` to each script. Set `Sts2Path` in `local.props` for direct `dotnet` commands.

```powershell
./scripts/Build.ps1
./scripts/Install.ps1 # Close the game first

dotnet run --project tests/LaRata.PolicyChecks -c Release
./scripts/Start-Test.ps1 -Role Solo
```

Copy `local.props.example` to `local.props` if your game installation differs. Game assemblies are referenced locally and are not distributed. The runtime requires BaseLib 3.4.7 and the game with its bundled Harmony. Building resources uses the official Godot 4.5.1 editor; see scripts/Build.ps1 for its expected local path.

For targeted tests inside an active test run:

```text
fight TWO_TAILED_RATS_NORMAL
fight MECHA_KNIGHT_ELITE
```

Open the native console using the game's console key (Shift+8 on the tested German keyboard), then use regular Enter. Numpad Enter is not handled by this game version. `act 3` and `travel` are native debug commands for reaching the selected final boss. Debug commands modify the test run; `fight` itself does not create a normal map-node checkpoint.

`Start-Test.ps1` uses native ENet/`-fastmp`, Steam disabled, and separate profiles: Solo 2000, Host/LoadHost 1, clients 2101/2102/2103. Prepare those test profiles with the mod enabled and complete the initial game prompts. The host must use ID 1 because native fast hosting assigns network ID 1 and native save validation requires it. Logs are written to `artifacts/logs`; checksum auditing is opt-in through `-larata-audit`.

`Compare-CoopChecksums.ps1` requires four complete logs from one scenario and compares native checksum IDs, values and contexts. Its default expected version remains `0.2.0` for the historical scenarios; pass the tested version with `-ExpectedModVersion` (currently `0.6.0`) for new logs. Local processes do not replace four human players over Steam/Internet. See `TEST-REPORT.md` for exact evidence and remaining gaps.

## References and dependencies

- [ModTemplate-StS2](https://github.com/Alchyr/ModTemplate-StS2), inspected at `55ca2c606e6c78dd39689a5cf979b243a49652e7`: native loader structure and debugging conventions.
- [BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2), `c070755d11ec062f6c97c4dda1a205d975ed066a`: established MIT modding library, required at version 3.4.7 for custom models, localization, animations and native save/packet extensions. It executes inside the game process and is version-sensitive; integration cost is moderate.
- [BuffTheSpire](https://github.com/hex3gc/BuffTheSpire), `334303b05af196f016d507de215c7074f957b583`: difficulty-patch reference; no source code copied.
- Harmony is an established MIT patching library already bundled with the game. Patches run inside the game process and depend on version-specific APIs; La Rata rejects other game versions and rolls back its patches if initialization fails.
- ILSpyCmd 9.1 (MIT) was used only for local API analysis. BaseLib is the only added runtime dependency; no downloaded service or custom multiplayer infrastructure is used. Decompiled sources, game assemblies, test profiles and logs are excluded from the release package.

## Optional installed mods

A focused fresh-run solo test loaded La Rata with BaseLib, Unified Save Path Forked, Limitless, Who Carried and the installed Ironclad/Defect/Necrobinder animation mods. See TEST-REPORT.md for versions and boundaries. Do not add Limitless to an already-running La Rata save: that combination failed in Limitless's saved configuration during testing. La Rata's ending takes precedence over Limitless's endless-door route; its other features may still alter rewards and balance. A four-player test of this complete external mod set remains outstanding.

Unified Save Path Forked changes the game's profile routing; La Rata itself neither requires nor performs profile migration. If you use that optional mod, vanilla and modded progression share its selected profile path. The standalone La Rata default remains the game's separate modded profile.

## License and assets

The original mod code, build scripts, scene definitions and documentation are available under the [MIT License](LICENSE). Raster artwork and branding are excluded from that grant; no general reuse license is granted for those files. See [asset provenance](docs/ASSET-PROVENANCE.md). Slay the Spire 2 and its referenced assets belong to their respective owners and are not distributed here. This is an unofficial fan mod.

Research files, downloaded tools, game assemblies, local profiles, raw test logs and build output are intentionally excluded from Git. Test reports preserve evidence summaries and local artifact names; those local artifacts are not part of the public repository.
