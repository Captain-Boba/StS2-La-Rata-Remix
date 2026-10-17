# La Rata Remix 0.6.0 — party-sized chaos

2026-10-17. The owner requested linear party-size twist budgets, broader enemy coverage and more variation. This changes encounter generation; start a new run and use identical 0.6.0 files on every peer. Existing active runs must not be upgraded in place.

## Rules

- Preserve the previous solo budget and multiply it by 1 / 2 / 3 / 4 for party size, for normals, elites and ordinary bosses alike. In points: normals 5/10/15/20; elites 3.75/7.5/11.25/15; bosses 2.5/5/7.5/10. Integer quarter-points keep decisions exact.
- The regular finale boss duo and La Rata's custom encounter retain their separate rules. No additional generic twists are added there.
- Select up to three changes per player, subject to available budget and compatibility. Budget is a ceiling, not a requirement to spend every point. Each family appears at most once per encounter. HP/damage can only be supplementary choices.
- Assign twists to the least-loaded eligible enemies with seeded tie-breaking. Original monsters and added small reinforcements can receive effects. HP/damage bonuses now belong to the selected enemy, rather than being applied encounter-wide for a single cost. Later native summons do not silently inherit twists.
- Additional enemy chance: 50/65/80/95 percent for eligible normal formations. Each extra costs 5 points, at 60% native HP and 65% base damage before any assigned twist. Three/four players can receive a second extra on a further 50% roll if space permits. Maximum four starting enemies for reinforced formations. Original larger formations are untouched.
- Fixed scenes/slots stay excluded except the established fourth rat in Two-Tailed Rats Normal, with its existing summon slot preserved. Additional small creatures use the existing native allowlist and native positioning. New two-extra layouts still require visual review.
- At most one added offensive amplifier per encounter (Keen, Empowered, Ritual, Demon Form, Strength or Vigor). Individual caps remain +18% HP / +16% base damage; player count does not multiply damage.
- At most one extra hex source for 1–2 players, two for 3–4, on different enemies. Each assigned native move retains its original transitions and gains the native Debuff/Status intent. No new action-blocking power is added.
- Per monster, at most one added Buffer/Intangible/Slippery family and one added block-engine family. These exclusions concern mod-added twists; original enemy powers and other mods can still interact.
- Added Weak/Frail/Vulnerable actions are bounded to three stacks, Poison to six, Doom to 25% max HP; existing larger stacks are never reduced. The newer harmful effects have the caps described below. Cards added to piles still use native commands and run RNG.

## Eleven additional families (31 total including reinforcement)

| Twist | Effect | Cost |
| --- | --- | --- |
| Phased | 1 native Intangible, expiring at enemy turn end | 3 |
| Slippery | 1 native Slippery; absorbs an unblocked hit down to 1, with native party-size charges | 2 |
| Wound Up | 2 Vigor for the next powered attack | 2 |
| Agile | 2 Dexterity and 6 base opening Block | 2 |
| Fortified | 12 base opening Block | 2 |
| Second Wind | Self-Forming Clay: 8 Block after the next block clear | 2 |
| Reservist | Toric Toughness: 6 Block after each of the next two block clears | 3 |
| Shackling | Announced temporary -1 Strength via Shackling Potion's native power, at most one stack | 2 |
| Sapping | Announced -1 Dexterity per execution, stopping at -1 total Dexterity | 2 |
| Constricting | Announced 2 Constrict, stopping at 4; native removal when its applier dies | 3 |
| Eroding | Announced 1 Disintegration, stopping at 2 | 3 |

Native player powers were inspected before reuse. Noxious Fumes targets the enemy list even with an enemy owner; Juggernaut and Furnace require a Player owner; Curl Up requests a monster-specific animation. They were deliberately not added. The selected powers use existing native commands, duration hooks, tooltips and cleanup. No new dependency, transport, save payload or custom power implementation.

## Determinism and validation

The director's dedicated seed namespace is now `LaRata/v4`. Array order and sorted pools determine target assignment and reinforcement choice; dictionary iteration and cosmetic RNG do not choose gameplay. Native combat restart reconstructs the same plan from seed, floor, act, encounter and party size. Runtime plans reference the exact generated monster objects, preserving ownership even if native combat sorting changes.

`artifacts/update-0.6.0/policy-checks.log`: 240,000 native-Rng policies passed across 1–4 players, three encounter tiers and 1–4 initial enemies. Checks cover replay, linear budgets, exact cost accounting, target bounds, unique families, formation limits, offensive/defensive/hex exclusions, all 30 non-reinforcement families, forced rat eligibility and reinforcement rates. Observed rates over 5,000 eligible samples per party size: 49.48%, 63.96%, 79.46%, 94.20%. Largest observed four-player plan: 10 changes. The existing royal-deck, goblin and relic policy checks also passed in the same test executable.

These simulations do not execute native powers, draw the UI or test network synchronization. Assistant-driven game/UI tests remain paused. Required follow-up: native effect execution and expiry, intents versus actual damage, Cleanse interactions, native checkpoint reconstruction, crowded formations, four-peer checksums and genuine Steam co-op balance. Earlier release tests do not validate this ruleset.

Release build and official Godot 4.5.1 resource export completed with zero warnings/errors (`artifacts/update-0.6.0/build.log`). Installed DLL, manifest and PCK match packaged SHA-256 hashes (`release-verification.json`). Only the mod's three runtime files were installed. No game launch, profile/save edit, commit or push was performed. The current package is `artifacts/LaRata-0.6.0.zip`.
