# Version 0.9.5 - Fixes & Potions

## Changes

- Three shared-pool potions: **Ashbrand** exhausts a non-X hand card and grants its Energy cost before temporary discounts; **Rustwater** removes all Block from one enemy; **Crown Lacquer** removes a chosen hand card's Exhaust keyword for this combat. English, German and Simplified Chinese texts and new artwork are included.
- La Rata and the two accompanying bodyguards start at 40% direct attack damage, gaining 10 percentage points each round to reach 100% in round 7. Forecasts use the same native hooks. Poison and other indirect effects, standalone guards and player attacks are unaffected.
- All relic run statistics and their observer patches are disabled. Saved legacy counters remain compatible but are not updated or displayed. Gameplay relic effects and persisted instance identities remain active.
- Corrected relic acquisition identity progression after native reward transfer/load/sync, synchronized shop-booster choice reservations, and narrowed native ENet disconnect/reentrant shutdown handling.
- Corrected initial booster card hover/focus and lingering tooltips. Expanded twist status and next-attack indicators, including Sturdy's red-cross HP percentage.
- Simplified card descriptions and removed rounding explanations. Updated The Kikilyst and Venomous Encore artwork and native-size relic outlines.
- Co-op trading remains deferred.

## Release verification — 2026-10-27

PASS: Release product and ContentChecks builds, zero warnings/errors; clean Godot export. Final PCK is byte-identical to the already resource-validated and locally playtested amber Crown Lacquer candidate.

| Check | Result |
| --- | --- |
| Royal potions and ramp | PASS: 120 assertions, including 45 native damage/intent resolutions; native selectors, command execution, potion pool/save identity, X exclusions and combat keyword lifetime. |
| Statistics disabled | PASS: 94 checks, 42 inactive observer classes, native Poison, 36 previews, inert legacy ledgers and working Relic Remover. |
| Disconnect regression | PASS: 314 assertions for native reward transfer, owner identity sequencing, checkpoint restore and checksums at 1–4 modeled players. Historical ledgers are now seeded directly in the fixture because runtime recording is disabled. |
| Shop booster regression | PASS: 224 assertions for native packets, choice reservations and checksums. |
| Patch activation | PASS: 83 patched methods in the fixture, including 17 scoped ENet lifecycle checks. |
| Twist presentation | PASS: native status/trigger and HP/damage checks, with modeled 1/2/4-player cases. |
| Localization | PASS: strict JSON/key/placeholder parity and 2031 native formatter calls across EN/DE/ZHS. |
| Asset paths | PASS: declared card/relic/potion paths and native relic-icon contracts. |

Console fixtures do not establish live UI, Steam network or full-save acceptance. No game was launched during release preparation. Logs and hashes are retained locally under `artifacts/release-0.9.5/`.

The preceding [focused local two-client ENet playtest](DESYNC-PLAYTEST-20261027.md) exercised starter boosters, a native Ancient acquisition and the royal fight with Silent Poison/Snecko Skull. All 20 native checksums matched. An orderly guest disconnect and host Save and Quit produced no network exception. It used the same gameplay source before the version bump; the final Release DLL was checked separately above.

## Compatibility and known limits

- Public game branch **v0.107.1**, **BaseLib 3.4.7**. Game Beta branch unsupported. Start a new run and use identical builds on all peers.
- Full logs from the playtest still contain engine resource-leak diagnostics on shutdown. This release does not claim clean engine shutdown or universal disconnect safety.
- Three/four live clients, Steam Internet latency, disconnect during an executing action, rejoin/save-reload and a full Act-4 route/boss defeat were not covered by that playtest. Historical Act-4 rest-site/VFX/action-queue findings remain open.
- Full potion UI, broad multiplayer, natural balance and visual acceptance remain ongoing.

## Related evidence

[Potions and ramp](ROYAL-POTIONS-RAMP-20261027.md) · [Statistics disabled](RELIC-STATISTICS-DISABLED-20261027.md) · [Silent/host log analysis](SILENT-DESYNC-LOGS-20261027.md) · [Identity and shop synchronization](DISCONNECT-INVESTIGATION-20261026.md) · [ENet lifecycle](ENET-TEARDOWN-20261027.md) · [UI and artwork](FEEDBACK-WAVE-20261027.md)
