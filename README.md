# La Rata Remix

**Version 0.9.5** — seeded encounter chaos, seven-card boosters, an expanded card collection, relic removal and an optional Rat King finale for **1–4 players**. Requires **Slay the Spire 2 v0.107.1** and **BaseLib 3.4.7**.

**Deutsch und 简体中文: kurze Hinweise stehen [weiter unten auf Deutsch](#deutsch) und [简体中文](#简体中文).** In-game text supports English, German and Simplified Chinese through the game's language setting.

Version 0.9.5 adds Ashbrand, Rustwater and Crown Lacquer, gradual attack damage for La Rata and his two bodyguards, co-op synchronization fixes, clearer twist indicators and updated artwork. All relic run statistics are temporarily disabled. The existing card and relic collection remains available.

**We recommend starting without Ascension levels.** Start a new run after updating; co-op players need identical game/mod versions. Only the Public game branch v0.107.1 is supported. See [0.9.5 changes and validation](docs/UPDATE-0.9.5.md) and the [focused local co-op playtest](docs/DESYNC-PLAYTEST-20261027.md). Gameplay checksums matched and orderly disconnect completed without network exceptions in that test. Broader Steam/Internet multiplayer, forced disconnect during active actions and full balance acceptance remain open; engine resource diagnostics still occurred on exit.

## Installation

### Steam Workshop (recommended)

Subscribe to [La Rata Remix](https://steamcommunity.com/sharedfiles/filedetails/?id=3822192203) and its required dependency [BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127). Let Steam finish downloading, enable both in the game's mod menu and restart when prompted. Remove `--nomods` from Steam launch options if present. No manual `mods` folder is needed for Workshop installation.

**Moving from a manual install:** close the game and move `mods/LaRata` outside the game's `mods` directory. Do the same for `mods/BaseLib` if switching BaseLib to its Workshop version. A local copy takes priority over Workshop and can silently keep you on an older version. Keep only one installation source per mod.

Version 0.9.5 requires **game v0.107.1 and BaseLib 3.4.7**. Start a new run after changing versions; all co-op players need matching game and mod versions. Steam manages Workshop updates, so compare versions before resuming a shared run. See the [installation guide](docs/INSTALL.md) and [Workshop verification](docs/WORKSHOP-0.8.2.md).

### Manual installation

**Local development:** double-click `Switch La Rata.cmd` in this checkout to switch between a local test build and the subscribed Workshop build. See [switching instructions](workshop/README.md#switching-the-local-development-installation). This does not publish an update.

Use the compiled **[LaRata-0.9.5.zip](https://github.com/Captain-Boba/StS2-La-Rata-Remix/releases/download/v0.9.5/LaRata-0.9.5.zip)** from the [0.9.5 release page](https://github.com/Captain-Boba/StS2-La-Rata-Remix/releases/tag/v0.9.5). GitHub's **Code → Download ZIP** and automatic **Source code** assets contain development sources and cannot be installed directly. No compilation is needed for the compiled package. A [standalone installation guide](docs/INSTALL.md) is included.

1. Close Slay the Spire 2. This version requires **game v0.107.1** and **BaseLib 3.4.7**.
2. Open the game's installation directory through Steam's **Browse local files** action. On Windows, this is the folder containing `SlayTheSpire2.exe`. It is not your save folder or the cloned GitHub repository.
3. Inside that game directory, create a folder named **`mods`** if it does not exist. The name is plural: `mods`, not `mod`.
4. Extract the compiled ZIP and copy its **`LaRata`** folder into `mods`. The three runtime files must be directly inside `mods/LaRata`, without an extra nested `LaRata` folder.
5. Install [BaseLib **3.4.7**](https://github.com/Alchyr/BaseLib-StS2/releases/tag/v3.4.7) separately, using the author's `BaseLib.3.4.7.zip` package and instructions. It is required and is not bundled with La Rata.
6. Remove `--nomods` from Steam's launch options, start the game and enable La Rata and BaseLib in the mod menu. Follow any restart prompt.
7. Start a **new run**. Every co-op player needs the same game version and identical mod files. Do not change mod versions during an active run.

Required La Rata layout on Windows:

```text
Slay the Spire 2/
├── SlayTheSpire2.exe
└── mods/                     ← create this folder if missing
    └── LaRata/               ← copy this folder from the compiled ZIP
        ├── LaRata.dll
        ├── LaRata.json
        └── LaRata.pck
```

To update, close the game and replace those three La Rata files with the matching files from the new package. Keep BaseLib installed. If the mod does not appear, check the folder nesting, the three filenames, BaseLib and the `--nomods` launch option first.

The game uses separate modded profiles. This mod does not erase, reset, migrate or unlock progress. Original game files are never modified. Disable La Rata in the game's mod menu to turn it off; removing a manual folder alone does not disable an installed Workshop copy. Keep La Rata and BaseLib installed while a run contains custom relics, events or the bonus act.

## Run effects

- **Starting Bowler Hat for every player**, awarded once during native new-run relic finalization. The original relic supplies its native +25% gold effect, icon, tooltip and interactions. There is no additional La Rata gold multiplier. Existing Bowler Hat owners receive no duplicate; loading a save does not grant another hat.
- **Starting Potion Belt and White Beast Statue for every player**, through native relic acquisition. Potion Belt supplies +2 slots (normally five total); White Beast Statue guarantees potion rewards after combat. The independent +2-slot patch is removed. Existing owners get no duplicate, and loading a save grants nothing extra. Older runs retain their saved capacity; start a new run for the new relic setup.
- **Compact map title:** only `La Rata Remix` remains at the bottom right, using the native map-legend font. The development effects panel and expansion control are removed.
- **Controlled chaos:** seeded encounter changes within a shared difficulty budget, with at least one tactical effect instead of only raising health and damage.
- **Varied reinforcements:** eligible ordinary fights draw from three small native helper types in act one (Leaf Slime, Twig Slime and Corpse Slug), with Nibbit, Flyconid and Fuzzy Wurm Crawler joining from act two. Inklet is excluded from random reinforcements. The pool is not restricted to enemies from the current act. Existing enemy types and types from the previous combat are avoided when alternatives exist; the special rat formation retains its additional rat. Budget and formation caps are unchanged.
- **Final boss reinforcement:** the regular final boss gains a compatible boss from an earlier act that has not appeared in this run. The helper retains native mechanics and now starts at **100% of its normal multiplayer-scaled HP** in every party size. The duo receives no additional generator affixes.
- **Double boss spoils:** the reinforced regular finale gives each player two native boss reward batches before the optional invitation: both gold rewards, two independent three-card boss offers and two potion rolls (both guaranteed with White Beast Statue). A finale without a compatible helper gives one batch. Room-specific extra rewards are included once; La Rata's own ending is unchanged.
- **Barricade:** the vanilla Ironclad card is Uncommon instead of Rare while La Rata Remix is enabled. Its effects, costs and upgrade are unchanged. It uses normal Uncommon selection and pricing; Bowler Hat retains its original gold effect.
- **Less frequent Thorns:** Bristling and Toxic Shell have one quarter of the normal twist selection weight. Their strength and encounter budgets are unchanged.
- **Potion goblin:** one encounter per seeded block of three is selected for a visit between rounds, after the second or third enemy turn. Each living player receives a different randomly selected native potion splash. Short fights that end before the arrival skip the visit. 58 audited native potions form the supported pool, including native choice effects and class mechanics; no inventory slot is consumed. Revival, a deprecated placeholder and four incompatible area effects are excluded. Situationally ineffective potions are skipped. See the [full eligibility rules](docs/UPDATE-0.8.2.md#goblin-potions). The splash resolves before the next player action phase, after block clearing, so Block Potion remains useful. His four poses show arrival, wind-up, throwing and laughter, with the selected native potion visible in his hand. The initial standing pause is 0.2 seconds; co-op awaits each throw, effect and any native player choice before the next recipient. Instant mode no longer skips the appearance.

## The King's Gambit

Version 0.8.3 holds intervention cards for **3.35 seconds**, two seconds longer than release 0.8.2, including Instant mode. La Rata's own boss-turn card timing is unchanged. The compact map title and reinforcement changes above are included in 0.8.3.

One fight per seeded block of three ordinary/elite fights schedules a royal intervention on player turn 2 or 3. Bosses and the Kingsguard are excluded. A black-and-emerald version of the native card-debuff symbol appears above one living enemy. **Only the coming intervention is announced, never the card's identity.** Defeat that enemy before the enemy turn to cancel it; the mark never transfers. Short fights can finish before an intervention happens.

After enemy block clearing and before any enemy attacks, the hidden card is revealed through the native play animation, accompanied by an original ElevenLabs ratlike chuckle. La Rata's character never appears. The six-card pool uses Royal Flush, Stolen Aegis, Exposed Crown, Stolen Hour, Royal Pardon and Nightfall Decree with their actual royal-card effects. Status cards affect the living party; Block/Buffer/self-Vulnerable affect the marked enemy; Royal Pardon removes Weak/Frail from living enemies and Nightfall Decree grants them capped Ritual. It excludes direct attacks, Poison, Doom and immediate Strength grants. Each fight has at most one intervention, and it never shares a round with the goblin. The reveal remains visible in Instant mode.

## Neutral cards

**Cleanse** is a rare neutral Skill in the native colorless pool: **2 Energy, Exhaust; upgraded cost 1**. It removes active Debuffs from yourself and every other living player, including negative Strength/Dexterity and temporary stat reductions. It also removes Curse cards from every living player's combat piles for this fight only, using the native removal command. Status cards, permanent deck originals and beneficial powers remain; it does not heal HP. It uses La Rata's frame, the native Rare banner and a spectral fountain portrait. Native colorless shops/rewards/events can offer it. The existing base-game card with the same English name is unchanged; the new card has its own BaseLib-prefixed model ID.

## Card boosters and combo relics

Every player starts a **new run with one booster for their class**. Selecting the initial Ancient relic opens that player's booster first; the chosen relic resolves after the pack is completed. Players can complete their own packs independently. This applies to all five regular characters in solo and co-op. Third-party characters without a dedicated pack receive a La Rata booster instead. Pending openings are saved; loading does not grant a second pack. Existing runs receive no retroactive starter pack. See the [three-client startup investigation](docs/COOP-START-20261023.md) for evidence and limitations.

Six common relics offer **Ironclad, Silent, Defect, Regent, Necrobinder or La Rata boosters**, regardless of your character. Each opens once upon pickup and remains as a memento. Five cards come from the named pool; two additional distinct cards come from the combined regular colorless and La Rata neutral pools. Previously saved five-card packs keep their original contents. Open the pack, hover over the seven facedown cards to see their rarity glow, then click each to turn it over. Common, uncommon and rare reveals have different colors and bursts; royal boss prizes glow emerald. After all seven are revealed, the native selection screen lets you keep **any number, including all seven or none**.

Class packs draw five distinct unlocked cards from that class's regular pool. The La Rata pack draws five main cards from all **23 regular La Rata neutrals**, using **75% Uncommon / 25% Rare**. The two mixed neutral bonus slots of every pack use the same 75/25 weights; class main slots retain **60% Common / 30% Uncommon / 10% Rare**. Only eligible nonempty rarity groups participate, and IDs cannot repeat across the seven-card offer. A separate roll makes **20% of La Rata packs** replace one main slot with one uniformly chosen playable boss-card variant; there is no per-slot 20% roll or guaranteed Rare. The ten available prizes are Scepter Storm, Sewer Alchemy, Stolen Aegis, Blood Contract, Emerald Tempest, Black Tithe, Plague of Crowns, Kneel Before the Crown, Soul Foreclosure and Nightfall Decree, adapted to player energy and targets. [New prize costs and upgrades](docs/UPDATE-0.8.7.md#rare-playable-booster-variants). Boss-only models never enter player decks.

The original ten colorless additions support energy/draw, exhaust, discard, Vigor, Poison/Doom, team Block and powers: **Grave Spark, Umbral Stitch, Sewer Pact, Phantom Parade, Borrowed Breath, Crown Exchange, Midnight Rehearsal, Emerald Ward, Second Shadow and Open the Gates**. All have their own art and upgrades. Original costs/effects are in [the 0.8.0 notes](docs/UPDATE-0.8.0.md). Borrowed Breath+ heals 6 HP and draws 3. In [0.8.2](docs/UPDATE-0.8.2.md#player-cards-and-relic), Emerald Ward costs 2 and grants all living players 5/10 Block plus 1/2 Energy; playable Scepter Storm costs 2 and applies 1/2 Weak and Vulnerable before dealing 10/20 damage. Regent Booster artwork is orange/gold. Cleanse and all ten playable boss prizes also have upgrades; the enemy-only royal cards do not.

| Relic | Rarity | Effect |
| --- | --- | --- |
| Ashen Spool | Uncommon | Each Exhaust grants 1 Block; the first each turn also draws 1 |
| Crown Capacitor | Rare | First Power each turn grants 1 Energy and 4 Vigor |
| Crossroads Compass | Uncommon | First pair of different card types in a turn grants 4 Block and draws 1 |
| Bottled Echo | Rare | Using a Potion in combat grants 1 Strength and immediately draws 2 cards |
| Smuggler's Thread | Uncommon | First discard during your turn grants 1 Energy |

All five belong to the shared relic pool. They add opportunities without character restrictions or removing existing mechanics.

Version 0.9.4 introduced **The Relic Remover, Sootbound Ticket, Stairwell Key, Mocking Bell, Brass Scavenger and Moonlit Stopper**, with six individually approved redesigned icons. The Relic Remover uses an owner-only selection; relic statistics are disabled in 0.9.5, while legacy saved counters are preserved for compatibility. The complete collection contains 60 model entries: 25 player cards (23 neutral, two class-specific), ten playable boss prizes and 25 boss-exclusive entries. Boss-exclusive presentation does not make those cards eligible player rewards.

## Treasure contests and Well

In solo play, ordinary nonempty chests offer **three relics; choose one** through the game's existing chest interface. Effects that suppress the chest reward remain effective.

**A Thousand Cuts** is a rare shared-pool relic for solo and co-op. Its owner may purchase any number of card removals at the native first-removal base price: normally 75 gold, or 100 with Inflation, before current discounts/modifiers. Further cuts do not increase that price while the relic is active, including after earlier shops or late acquisition. Successful-removal history is still recorded; losing the relic restores native pricing and shop-use rules. Native restrictions apply, and cancelling does not charge gold.

When several players choose the same chest relic, each contender privately chooses Rock, Paper or Scissors. After everyone locks in, the game's original character hands shake and reveal the chosen signs. Losing hands play the native loss animation; the winner grabs the relic without a second fight announcement. One or three distinct hands tie; with two distinct hands the losing hand is eliminated. Survivors choose again until one remains. Uncontested relics and the game's consolation rewards keep their native behavior.

**Well** is a rare shared-pool relic available only in co-op. A contender with Well wins automatically against contenders without it. Multiple Well holders have equal seeded chances: two holders means 50/50. The losing holders still keep their Well. Choice submission uses the game's own player-choice synchronization, not a separate network protocol. Decisions are hidden in the UI, not cryptographically protected against a modified client.

## Optional endgame

After the regular finale, a shared invitation offers the bonus path. Entry requires every player to vote yes. A no vote takes the party to the Architect. Pending invitations restart voting after loading; a completed decision persists.

The path contains **an unseen Ancient → YOU (shop) → ARE (?) → NOT (elite) → PREPARED (rest site) → La Rata**. The Ancient is drawn from native Ancients across all acts, excluding Neow and anyone already visited this run. Its normal entry healing and reward logic apply, including native Ascension healing restrictions. The unknown room uses the game's normal resolver and Glory content pools. The regular victory is retained in progression and run history if the party dies during the bonus. Defeating La Rata leads to the Architect and the ordinary ending.

La Rata starts with **1,500 base HP**, accompanied by two distinct random Kingsguard knights. Native boss HP scaling adjusts this for party size. His scene uses **ten separately authored poses** with native Idle, Attack, Cast, Hit and Death animation tracks, including wind-up, impact and a staged fall; this replaces the earlier twelve-frame atlas presentation.

The seeded combat deck contains **46 active families**: 23 retained boss families and all 23 regular La Rata neutral families with explicit enemy-side execution. The retired Sewer Alchemy and Plague of Crowns boss entries remain excluded; playable prize variants do not duplicate their boss identities. Each hand normally draws five cards and spends **6 Energy, increasing to 8 at half HP or below**, with a ten-card hand limit and eight-play limit. Draw/Energy gains for the next hand, Exhaust, persistent Powers and material-card requirements use explicit boss rules. The hidden hand is shown through played-card animations and relevant intents, not revealed in advance.

La Rata's direct card Block bases are halved and most card healing bases are reduced to one quarter (rounded down, minimum 1 for positive healing). Grave Levy heals exactly **1% of current maximum HP**, without applying the quarter rule twice. Kingsguard healing directed at La Rata is also quartered; healing of other guards retains its native rule. Plating, Buffer and unrelated player effects are not silently reduced by this card-Block rule.

Each encounter grants **ten distinct combat relics** from an eighteen-relic native selection pool, with explicit enemy adaptations. Two rows of native icons below the power rows explain those effects. Selection reconstructs from the encounter seed. Skills and powers resolve before attacks; pending Strength is reflected in intents. A hand without an attack ends with an announced 12-base-damage fallback attack. Generic encounter twists and earlier-boss helpers do not stack onto this custom finale.

The earlier card-by-card tables remain in the [historical update reports](docs/UPDATE-0.8.7.md); they are not current 0.9.5 balance values. See [current release evidence](docs/UPDATE-0.9.5.md) for the expanded deck and bounded live observations. Full animation sequencing, natural difficulty and four-human balance still require playtesting.

### The Kingsguard

The **NOT** elite is now a fixed four-knight formation: two **Flail Knights**, one **Magi Knight** and one **Spectral Knight**, using the installed game's original animated models. A mod-owned material remaps their armor to dark steel and their accents to emerald. Their original Hex/Dampen/attack rotations are replaced completely; the regular Knights elite is unchanged.

| Guard | Native model | Base HP / starting Plating | New role |
| --- | --- | --- | --- |
| Crown Bulwark | Flail Knight | 220 / 30 | Shields allies and adds Plating; alternates double-flail and armored ram attacks |
| Crown Headsman | Flail Knight | 210 / 24 | Opens with two strikes; reinforces allies' Plating and shields itself between assaults |
| Crown Armorer | Magi Knight | 165 / 18 | Repairs Plating, shields allies after striking, heals the most injured other knight for 25% max HP on turn 3 |
| Crown Herald | Spectral Knight | 155 / 18 | Alternates spectral strikes, a 10% max-HP group heal on turn 2 and group protection |

HP, Block and Plating retain native multiplayer scaling. All Kingsguard damage is doubled, with matching intents; all Plating grants are tripled. Plating grants stop at 42 base-equivalent stacks per knight; there is no unlimited armor growth. Heals never revive dead guards. Removing a support knight breaks that part of the formation's sustain. Generic encounter twists and royal interventions do not stack onto this elite. A targeted four-local-client test verified formation, starting stats, attack intents and both healing actions; natural difficulty and four-human balance still require playtesting.

## Encounter budget

For budgets below **6 points**, ten families use **Lesser** variants at half price and half magnitude (rounded down, minimum 1): Hexed, Bristling, Venomous, Ominous, Wound Up, Fortified, Second Wind, Constricting, Ember Guard and Hotheaded. Flat defense still scales by act after the reduction. Low-budget rolls reserve room for a second tactical effect when possible; stats-only additions come later. Every normal solo act-one plan now has at least two tactical changes, counting a reinforcement as one. Ordinary solo act-one bosses can receive a Lesser effect. **Total budgets are unchanged, and four-player prices, effect strengths and roll ordering are unchanged.** The table below lists full variants.

| Affix | Effect | Cost |
| --- | --- | --- |
| Sturdy | +10–18% starting HP | 1 |
| Keen | +8–16% base attack damage, reflected in intents | 2 |
| Warded | 1 Artifact on that enemy | 2 |
| Empowered | 2 Vigor and +1 to its small Strength applications | 2 |
| Echoing | An extra announced hex and +1 to its short Weak/Frail/Vulnerable applications, capped at 3 | 2 |
| Hexed | One native action gains an announced 2 Weak, Frail or Vulnerable | 3 |
| Bristling | 2 Thorns on the selected enemy | 2 |
| Regenerating | Up to 3 Regen, capped at 10% max HP (minimum 1); Inklets max 1; no extra party multiplier | 2 |
| Cultist / Demonic | 1 Ritual / Demon Form | 3 |
| Entrenched | Barricade and 8 base Block | 3 |
| Buffered | 1 native Buffer | 2 |
| Furious | 1 Strength | 2 |
| Venomous | An announced action adds 2 Poison | 2 |
| Ominous | An announced action adds 4 Doom, capped at 25% of target max HP | 2 |
| Cinders / Disorienting / Lacerating | Add a Burn / Dazed / Wound through an announced action | 2 |
| Reinforcement | One additional existing small enemy at 60% HP and 65% base damage | 4 per extra |

Act-one budgets scale linearly with **1/2/3/4 players**. Budgets are 25% below 0.8.3. Normal: **3.75/7.5/11.25/15** points; elite: **2.8125/5.625/8.4375/11.25**; ordinary boss: **1.875/3.75/5.625/7.5**. Multiply by **1.0 / 1.4 / 1.8 / 2.2** for acts 1 / 2 / 3 / 4+. Flat defensive grants use **1 / 1.5 / 2 / 2.5**, rounded upward, before applicable native party scaling. Sturdy gains 4 percentage points per act; Keen gains 2. The table lists act-one values. Select up to three changes per player, plus one per later act, within budget and compatibility limits. Twists spread across the least-loaded eligible enemies. Direct damage is not multiplied by party size. At most one added offensive amplifier and one extra hex source (two with 3–4 players); at most one added shield, block engine and retaliation family per enemy. Immunity durations and debuff caps remain bounded.

Eligible normal formations have a **70/80/90/100% reinforcement chance**, plus 5 percentage points per later act, capped at 100%. Regular extras cost 4 points. In a normal encounter with less than 4 points (solo act one), a **Scout reinforcement** instead costs **2.5 points**, uses **35% HP and 50% base damage**, and leaves room for a small effect. Two regular extras are available with 2+ players (solo from act 3); three with 3+ players from act 2, subject to extra rolls, remaining budget and space. Reinforced formations start with at most four enemies. Fixed scenes/slots remain excluded except **Two-Tailed Rats Normal**, which prioritizes its fourth rat in `second` and preserves `first` for native summons. The early cross-act helper pool is Small Leaf Slime, Small Twig Slime and Corpse Slug; Inklet is excluded. Nibbit, Flyconid and Fuzzy Wurm Crawler join from act two. Present/recent monsters are avoided when alternatives exist. Reinforcement can coexist with other twists.

The following ten families remain included:

| Affix | Effect | Cost |
| --- | --- | --- |
| Phased | 1 Intangible | 3 |
| Wound Up | 2 Vigor for the next attack | 2 |
| Agile | 2 Dexterity and 6 base Block | 2 |
| Fortified | 12 base opening Block | 2 |
| Second Wind | 8 Block after the next block clear | 2 |
| Reservist | 6 Block after each of the next two block clears | 3 |
| Shackling | Announced temporary -1 Strength, at most one stack | 2 |
| Sapping | Announced -1 Dexterity, stopping at -1 total | 2 |
| Constricting | Announced 2 Constrict, capped at 4 | 3 |
| Eroding | Announced 1 Disintegration, capped at 2 | 3 |

Reactive families retained from 0.8.1: **Venom Coated, Ember Guard, Glass Crown, Mirror Guard, Blood Feud, Hotheaded and Afterglow**. **Armored, Slippery, Spiteful/Rupture, Skill Hunter/Enrage and Doomweaver/Shroud are removed from random selection.** Empowered no longer grants or amplifies Plating. Native enemy abilities and the Kingsguard remain separate.

The active pool now contains **43 twists / 44 families including reinforcement**. The eight families introduced in 0.8.1 remain available:

| Affix | Effect (act-one values) | Cost |
| --- | --- | --- |
| Reckless | 2 Strength, but 1 Vulnerable | 3 |
| Brittle Ward | 18 opening Block, but 2 Vulnerable | 2 |
| Toxic Shell | 3 Thorns, but 3 starting Poison | 2 |
| Last Stand | 1 Demon Form, but 1 starting Weak | 3 |
| Fading Might | 5 temporary Strength and 1 Weak for the first enemy turn | 3 |
| Leaden | Announced action adds 1 Slimed to discard | 2 |
| Static Hex | Announced action shuffles 1 Void into draw | 4 |
| Crossfire | Announced action applies 1 Weak and 1 Vulnerable, capped at 2 each | 4 |

Two compact clickable **icon/count counters above the Exhaust pile** replace the history text buttons. Each appears only after its own first entry, independently of whether the Exhaust pile is empty. The royal counter opens the native card viewer for cards actually played by La Rata, with rounds on hover. The potion counter opens a scrollable log with potion icons, round, recipient seat and character portrait/name, plus native effect tooltips. Both reset after combat. Native save/load restarts unfinished combat at its entry checkpoint, so history restarts too and is rebuilt as actions replay; future cards from an abandoned attempt are never retained. History data uses BaseLib save fields, without a separate save file or network protocol.

Added Weak/Frail/Vulnerable actions cap at three stacks, Venomous at six Poison and Doom at 25% max HP. Venom Coated instead uses native Envenom: one Poison per unblocked powered hit, without the separate Venomous action cap. Existing higher values are not reduced. The finale duo, Kingsguard and La Rata retain their separate rules.
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

`Compare-CoopChecksums.ps1` requires complete logs from one scenario (four by default; use `-ExpectedPlayers 2` or `3` for smaller groups) and compares native checksum IDs, values and contexts. It normalizes only the process-local object hash embedded in native PlayCardAction context text; card IDs, indices, targets and actual checksum values remain strict. Its default expected version remains `0.2.0` for the historical scenarios; pass the tested version with `-ExpectedModVersion` (`0.9.5` for this release) for new logs. Local processes do not replace four human players over Steam/Internet. See `TEST-REPORT.md` for exact evidence and remaining gaps, including the separate gameplay-window and shutdown results.

## Deutsch

La Rata Remix 0.9.5 unterstützt deutsche Spieltexte über die Spracheinstellung des Spiels. Benötigt werden Spiel v0.107.1 und BaseLib 3.4.7. Installiere das kompilierte Paket oder abonniere den Workshop-Eintrag; beginne nach dem Update einen neuen Run. Im Koop müssen alle dieselben Versionen verwenden. A Thousand Cuts behält den Erstpreis; weitere Entfernungen erhöhen ihn mit aktivem Relikt nicht. Die technische Prüfung ersetzt keine vollständige Spiel- oder Netzwerkabnahme.

## 简体中文

La Rata Remix 0.9.5 支持简体中文，可在游戏语言设置中选择。需要游戏 v0.107.1 和 BaseLib 3.4.7。请安装编译好的压缩包或订阅创意工坊条目；更新后开始新的一局。联机玩家必须使用相同版本。持有 A Thousand Cuts 时，每次移除卡牌均使用首次移除的基础价格，并应用当前折扣。技术检查不代表已完成全面的游玩或联机验证。

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
