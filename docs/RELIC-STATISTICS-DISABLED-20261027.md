# Relic statistics disabled — 2026-10-27

User request: disable all relic statistics, run focused technical checks and install locally. No game starts or in-game testing. Implemented directly in the existing chat, without agent delegation.

## Change

- All 42 statistics-only Harmony patch classes now belong to the inactive `LaRataRelicStatistics` category. This includes effect contexts, scalar/numeric observers, payment/deferred reward tracking and tooltip result injection.
- Production startup and the relevant console fixtures share `ModEntry.ApplyPatches`. It activates normal gameplay patches and Reset's energy accounting, but never the statistics category.
- `RelicMetrics.Enabled` is hard-disabled for every peer, with no local settings toggle. Direct metric recording, deferred flags, archival and fresh statistics-ledger creation are also inert. Calls embedded in gameplay code cannot bypass the disabled observers.
- `RelicStatsPresentation.ResultLines` returns no statistics, including for an old ledger. Ordinary relic descriptions and gameplay effects remain available.
- Saved fields remain registered for old-run compatibility; old ledgers are retained unchanged. The deterministic acquisition identities and load/sync restoration remain active because Relic Remover's reopen request identifies its instance through them. These IDs are not benefit counters. No checksums were excluded or bypassed.
- No artwork/localization/resource changes or new dependency. No commit, push, version bump or publication.

## Focused validation

Evidence: `artifacts/metrics-disabled-20261027/`.

| Check | Result |
| --- | --- |
| Debug product and existing ContentChecks build | PASS, zero errors. Two existing NU1900 warnings: NuGet vulnerability feed unavailable. No new dependency requested. |
| `--metrics-disabled` | PASS: 94 assertions; all 42 observer/UI patch classes absent, required identity/remover hooks present. |
| Native Snecko Skull poison application and stacking | PASS: 5 base Poison becomes 6; second application reaches 12. The actual modifier remains effective. |
| Native `PowerVar<PoisonPower>` previews | PASS: 36 calls, including callbacks inside first power application and stacking; preview remains 6 and old statistics payload remains byte-identical. Console combat setup uses the native `IsStarting` phase; no UI/engine session claimed. |
| Direct recording / deferred / removal / fresh acquisition paths | PASS: existing ledger unchanged, new relic ledger empty, no removed-statistics archive created. |
| Old relic JSON roundtrip | PASS: native serializer + BaseLib preserve old ledger and gameplay identity. Not a full on-disk gameplay load test. |
| Old Coin and Relic Remover | PASS: actual +300 gold, no tracking; empty removal does not consume; confirmed removal works and keeps granted gold. |
| Existing remover-choice fixture | PASS: overlapping owner choices, duplicate-start guard, cancellation, eight reopens, sender/sequence/instance/location checks and native sequence JSON. Predetermined selection UI and singleplayer transport; no live multiplayer claim. |
| `--patches-only` | PASS: 83 patched methods including fixture desktop substitutions; both booster focus hooks, statistics exclusion and existing 17 ENet lifecycle checks. |
| Game starts / live tests | NOT RUN, explicitly excluded by user. |

The first new-fixture attempt correctly failed its positive poison-bonus assertion because the console setup had not enabled a native combat hook phase. Corrected fixture setup to `IsStarting`, restored afterward; no product change was needed. Final build and logs above are the successful rerun. Historical tests requiring enabled statistic accumulation were not run as acceptance for this disabled feature.

## Local installation

Installed with the existing `scripts/Install.ps1` into `E:/SteamLibrary/steamapps/common/Slay the Spire 2/mods/LaRata`. No game was running. Previous installed files backed up under `installed-before/`; the canonical package was temporarily staged for the installer and restored byte-for-byte from `canonical-before/`. Candidate files remain under `candidate/` for provenance.

| File | Installed SHA256 |
| --- | --- |
| LaRata.dll | `4FFA62736EB918773A0BFA1038D968D47B4FD7818EC8583693FC6ED46133165D` |
| LaRata.pck | `1E164A2C8647096F77BF806BD1EED71CF06AD00235B315971CB101D626F43466` |
| LaRata.json | `1A2834DB1A10732A1B64E880E7D7077F4BE83BA81B1667AE49983996DF13455D` |

All installed hashes match the tested candidate. PCK and manifest are unchanged from the preceding local installation. Version remains 0.9.4; Workshop/GitHub copies are not updated. For a co-op test, every player must use this same fixed build; the shared version string alone does not establish identical binaries. No user saves were edited.
