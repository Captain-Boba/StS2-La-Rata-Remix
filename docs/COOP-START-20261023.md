# Multiplayer startup investigation — 2026-10-23

## Status

The startup fix was tested on top of `c71a2f3` / La Rata 0.9.0 and is included in hotfix 0.9.1. The temporary candidate used the 0.9.0 version label; its hashes below identify those exact test bytes. The installed candidate was restored to the original published DLL after testing. The subsequent 0.9.1 package has updated version metadata and documentation with the same startup fix.

The user reported a fatal crash at the first fight in a three-player run and one player unable to open their starting booster. No logs, exact crash timing, dependency versions or other-mod list were available from that run. The reported fatal crash has **not** been reproduced or conclusively attributed to this defect.

## Confirmed defect and change

Published 0.9.0 opened starting packs from `RunManager.FadeIn`, iterating players sequentially. This attached gameplay choices to a local visual transition, outside the Ancient event's tracked option task. In the baseline local three-client test, the third player could select an Ancient reward and enter a separate native card selection while their starting pack still waited for another player's pack. This explains an apparently inaccessible pack and exposes overlapping selection / room-transition risks.

`StartingBoosterChoicePatch` now wraps the native Ancient relic option callback. It opens only that event owner's pending starter packs before invoking the original callback. `EventSynchronizer` already runs the option on peers and tracks its task; `EventRoom.Exit` awaits those tasks before combat synchronization. Each player can finish independently, and a card-selecting Ancient reward cannot overtake that player's pack. Native card selection, RNG, saved properties and networking remain in use. No new dependency or protocol was added.

The existing checksum comparison script now accepts an explicit `-ExpectedPlayers` value from 2 through 4; its default remains 4. Scenario markers still need to match the requested test.

## Validation

- Release C# build: zero warnings and errors.
- Full content checks passed, including native pending/opened pack property roundtrips and a new three-owner asynchronous ordering regression. The latter replaces interactive selection with controllable tasks and is explicitly not a network test.
- Live test: three separate local game processes using the game's native ENet transport, Steam disabled, isolated profiles 1 / 2101 / 2102, game v0.107.1 (59260271), BaseLib 3.4.7, La Rata candidate, no other gameplay mods. All three characters were Ironclad, Ascension 1. Seed: `7SHI4K3HSW`.
- All three seven-card packs opened and completed in player order 2102 / 1 / 2101. Client 2102 retained four cards, the host retained Grave Spark, and client 2101 retained Hemokinesis. All peers logged identical offers and retained cards for every owner.
- The host chose Lead Paperweight: its separate native card reward appeared only after the starter pack completed, and Dark Shackles was selected successfully. The other two Ancient choices were Cursed Pearl and Small Capsule.
- The party entered its first fight normally by voting on the map. `SLUDGE_SPINNER_WEAK` gained Twig Slime S and Leaf Slime S reinforcements, Ember Guard on the spinner and Sturdy on the leaf slime (budget 176/180 sixteenths).
- Client 2102 played Phantom Parade; the host and client 2101 each played Defend. All three ended their turns. The enemy turn completed and player turn 2 began.
- **10 identical native checksum IDs and normalized contexts (0–9) on all three peers**, from exiting Neow through player turn 2. No gameplay errors or state divergence were recorded in this candidate session.
- One nonfatal Godot `GrabFocus` warning occurred while revealing the first pack; selection and the fight continued. It is not the reported fatal crash.

The test did not finish the combat or exercise a live save/reload, reconnect, simultaneous clicks, mixed character parties, four peers, separate computers, Steam transport or other-mod compatibility. The observed startup pass does not certify those scenarios. For another fatal crash, retain each participant's `godot.log` immediately after the failure, together with game/mod versions and the active mod list.

Older saves already past the initial Ancient with an unopened starter were not migrated or tested. This fix targets the new-run startup path; it does not retroactively grant another pack.

## Evidence retained locally

Baseline logs (published DLL):

- `artifacts/logs/20261023-002722-Host.log`
- `artifacts/logs/20261023-002740-Client1.log`
- `artifacts/logs/20261023-002740-Client2.log`

Candidate logs:

- `artifacts/logs/20261023-003027-Host.log`
- `artifacts/logs/20261023-003052-Client1.log`
- `artifacts/logs/20261023-003053-Client2.log`

Additional evidence: `artifacts/coop-start-20261023/content-checks.log` and `checksum-comparison.txt`. These local artifacts are ignored by Git.

Candidate DLL SHA-256: `76924BFB0DEC7254501365EF1EAA29E2BF22DE528702D6C1284883E63478535C`.

Restored published DLL SHA-256: `1EA92D4D62145D0D4A16615E4C12A89E6A8A330E445906CB6800C11BEC636BFE`.

All three agent-launched test processes were stopped. The temporary local copy of the installed Workshop BaseLib was moved out of the game's mods directory into the test artifact folder. Workshop files and normal Steam saves were not changed.

To repeat the recorded comparison from the repository root:

```powershell
./scripts/Compare-CoopChecksums.ps1 `
  -Logs @('artifacts/logs/20261023-003027-Host.log',
          'artifacts/logs/20261023-003052-Client1.log',
          'artifacts/logs/20261023-003053-Client2.log') `
  -ExpectedPlayers 3 -ExpectedModVersion '0.9.0' `
  -RequiredMarkers @('encounter=SLUDGE_SPINNER_WEAK players=3',
                     'BOOSTER player=1 ', 'BOOSTER player=2101 ',
                     'BOOSTER player=2102 ', 'CARD.LARATA-PHANTOM_PARADE')
```
