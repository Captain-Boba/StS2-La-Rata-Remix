# StS2 - La Rata Remix

Standalone difficulty mod for **Slay the Spire 2 v0.107.1 (59260271)**. Version **0.3.1** adds native-styled menus and per-round hand animations to the treasure contests, Well and optional endgame prototype for 1–4 players. All mod-added text is English. The base game's language remains your choice.

The optional endgame path and the first La Rata boss are included in this prototype. See the [accepted design brief](docs/DESIGN.md) and [endgame integration study](docs/ENDGAME-INTEGRATION.md). The supplied [title artwork](La%20Rata%20Remix%20-%20Titelbild%20-%20V1.png) is the visual reference for the new mod-owned combat sprite.

## Installation

Close the game. Copy the `LaRata` folder containing `LaRata.dll`, `LaRata.json` and `LaRata.pck` into `Slay the Spire 2/mods`, install BaseLib 3.4.7, then enable both mods in-game. Remove `--nomods` from the launch options when you want to play with mods. Every player needs the same game version and identical mod files. Start a **new run** to receive the two additional potion slots. Do not change mod versions during a run.

The game uses separate modded profiles. This mod does not erase, reset, migrate or unlock progress. Original game files are never modified. Removing the LaRata folder disables it for subsequent runs. Keep La Rata and BaseLib installed while a run contains custom relics, events or the bonus act.

## Run effects

- **+25% newly gained gold**, through the native gold hook, including combat, event and relic gains. Native rounding to whole coins applies: 150 becomes 187. Starting gold and existing balances are unchanged; native prevention such as Ectoplasm remains effective.
- **+2 potion slots at run creation**, normally five total. Other native effects may still alter capacity. Loading a save does not add another two slots.
- **Collapsible map panel** below the map legend. Click `La Rata Remix` to expand or collapse it. The panel uses the native map-legend parchment and fonts. It stays fixed while scrolling and shows permanent effects and current potion capacity for each player.
- **Controlled chaos:** seeded encounter changes within a shared difficulty budget, with at least one tactical effect instead of only raising health and damage.
- **Final boss reinforcement:** the regular final boss gains a compatible boss from an earlier act that has not appeared in this run. The helper retains native mechanics and starts with 45/50/55/60% of its normal, already multiplayer-scaled HP for 1/2/3/4 players. The duo receives no additional generator affixes.

## Treasure contests and Well

When several players choose the same chest relic, each contender privately chooses Rock, Paper or Scissors. After everyone locks in, the game's original character hands shake and reveal the chosen signs. Losing hands play the native loss animation; the winner grabs the relic without a second fight announcement. One or three distinct hands tie; with two distinct hands the losing hand is eliminated. Survivors choose again until one remains. Uncontested relics and the game's consolation rewards keep their native behavior.

**Well** is a rare shared-pool relic available only in co-op. A contender with Well wins automatically against contenders without it. Multiple Well holders have equal seeded chances: two holders means 50/50. The losing holders still keep their Well. Choice submission uses the game's own player-choice synchronization, not a separate network protocol. Decisions are hidden in the UI, not cryptographically protected against a modified client.

## Optional endgame

After the regular finale, a shared invitation offers the bonus path. Entry requires every player to vote yes. A no vote takes the party to the Architect. Pending invitations restart voting after loading; a completed decision persists.

The path contains **YOU (?) → ARE (?) → NOT (?) → PREPARED (shop)**, followed by La Rata. Unknown rooms use the game's normal resolver and the Glory content pools. No extra Ancient reward is added. The regular victory is retained in progression and run history if the party dies during the bonus. Defeating La Rata leads to the Architect and the ordinary ending.

La Rata starts with 430 base HP before native multiplayer scaling, accompanied by two Two-Tailed Rats. Its fixed, announced cycle is:

1. **Royal Decree:** apply 2 Weak and 2 Frail to every living player.
2. **Tax Collection:** attack for 20.
3. **Scepter Strikes:** attack for 7 three times.
4. **Exposed Crown:** gain 25 base Block (native co-op scaling applies; 130 in the tested four-player finale) and apply 2 Vulnerable to itself, opening a counterattack window.

The rats retain their native mechanics. Generic encounter affixes and the earlier-boss reinforcement do not stack onto this custom finale. This is an initial gameplay design, not a finished balance claim.

## Encounter budget

| Affix | Effect | Cost |
| --- | --- | --- |
| Sturdy | +8–12% starting HP | 1 |
| Keen | +8–12% base attack damage, reflected in intents | 2 |
| Armored | 2–4 native Plating on one selected enemy | 2 |
| Warded | 1 Artifact on that enemy | 2 |
| Empowered | 2 starting Plating and +1 to its small Strength/Plating applications | 2 |
| Echoing | An extra announced hex and +1 to its short Weak/Frail/Vulnerable applications, capped at 3 | 2 |
| Hexed | One native action gains an announced 1–2 Weak, Frail or Vulnerable | 3 |
| Reinforcement | One additional existing small enemy at 60% HP and 65% base damage | Entire budget |

Normal encounters have budget 4 with 1–2 players, or 5 with 3–4. Elites have 3 and bosses have 2. Each eligible encounter receives 1–3 changes as the budget permits. Damage amplification and minor-buff amplification cannot occur together. Plating retains the game's own multiplayer scaling and decay. The selected enemy is called the leader only for describing affix ownership; it is not a new monster type.

Additional normal enemies are restricted to compatible formations and a small list of autonomous creatures. **Two-Tailed Rats Normal** always adds a fourth rat in the existing `second` slot, leaving `first` available for native summons. Other eligible reinforcements are drawn from Inklet, Small Leaf Slime, Nibbit, Flyconid, Corpse Slug and Fuzzy Wurm Crawler already present in the formation. Reinforcement consumes the entire budget.

**Mecha Knight** retains the original prototype action: after the flamethrower and before winding up, it announces a strong debuff action that applies 3 Weak and 3 Frail to living targets. The extra action also gives players a response window; its net difficulty remains a playtesting question.

## Boss compatibility

Supported regular finales are Queen, Test Subject and Aeonglass. On a double-final-boss run, only the second receives a helper. Candidates come from the actual earlier acts' boss pools, filtered against saved room and monster history and sorted before the seeded choice.

Allowed helpers: Vantom, Lagavulin Matriarch, Waterfall Giant and Knowledge Demon. The Kin and Kaiser Crab require special formations/scenes; Soul Fysh and The Insatiable have special combat/card transitions. Ceremonial Beast is excluded because its absolute HP phase threshold conflicts with reduced starting HP. If no compatible unseen helper remains, the original finale is kept and the reason is logged.

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

`Compare-CoopChecksums.ps1` requires four complete logs from one scenario and compares native checksum IDs, values and contexts. Its default expected version remains `0.2.0` for the historical scenarios; pass the tested version with `-ExpectedModVersion` (currently `0.3.1`) for new logs. Local processes do not replace four human players over Steam/Internet. See `TEST-REPORT.md` for exact evidence and remaining gaps.

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
