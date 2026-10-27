# Focused local co-op desync playtest — 27 October 2026

## Installed candidate

Public v0.107.1, BaseLib 3.4.7, La Rata 0.9.4. The installed candidate already matched the requested fixed build; no redundant reinstall or source change was needed. Hashes were verified before launch and after cleanup:

- DLL: `7359D6529E37EF50622ECB831C0C0BF323BB71ED8392FCAA0BF958C3A0274426`
- PCK: `2F1317DFDB8B7540AF1623E76B0C14630797C0C0B38A8F4D0AD159E156D0A494`
- Manifest: `1A2834DB1A10732A1B64E880E7D7077F4BE83BA81B1667AE49983996DF13455D`

## Scenario and results

Two actual local ENet clients, Ironclad host (1), Silent guest (2101), separate backed-up native test profiles. Setup used the game's networked developer console, followed by native UI choices, card plays and End Turn actions. This was a focused reproduction attempt, not a complete campaign or balance test.

- **PASS:** Both class starter boosters opened, all seven cards revealed, native keep-none choice completed, and Neow rewards resolved.
- **PASS:** Native `ancient VAKUU PRESERVED_FOG` event; both players acquired Preserved Fog, removed three cards and received Folly. Subsequent native checksums matched. This covers fresh acquisition after starter boosters, not the historical save/import sequence verbatim.
- **PASS:** Silent acquired Snecko Skull; native `fight LARATA-RAT_KING_ENCOUNTER` entered the boss encounter with both bodyguards. The encounter was entered directly from Act 1, not reached through the Act-4 campaign route.
- **PASS:** Deadly Poison was previewed and played through UI on La Rata in turn 1 (6 Poison), then drawn again and played on Crown Headsman in turn 3 (3 → 9 Poison).
- **PASS:** Both enemy phases completed and player turn 3 began. Noxious Fumes applied 3 Poison to each enemy with Snecko Skull at turn 3 start. Its 2-stack power was installed with the native `power` command in turn 2; the Noxious Fumes card itself was not successfully played by the mouse automation. One native heal per player in turn 2 kept the setup alive.
- **PASS:** All **20 native checksum IDs, values and normalized contexts** matched, as did private Reset-owner balances at all 20 boundaries. No state divergence, unexpected exception, script error or gameplay ERROR occurred.
- **PASS, scoped:** Guest used Pause → Disconnect → Yes during an idle player turn. Host showed the disconnected player and then used Save and Quit. No `Peer not connected`, null-peer or action-queue exception occurred on this path. The native `-fastmp join` flag attempted an automatic rejoin, rejected normally because the run was already started. Both processes then closed normally with Alt+F4.
- **FAIL, shutdown cleanliness:** Each full log contains 12 engine resource-leak ERROR diagnostics after `FMOD Sound System: System released`. These remain in the evidence. The full-log checksum check passes only with the existing explicit engine-shutdown classification switch; this is not a claim of error-free shutdown.

## Evidence and limits

Evidence directory: `artifacts/desync-playtest-20261027/`.

- `gameplay-Host.log` / `gameplay-Client1.log`: complete unfiltered snapshots copied before intentional disconnect; `gameplay-checksums.txt` records strict PASS.
- `full-Host.log` / `full-Client1.log`: complete logs including exit errors; `full-checksums-classified.txt` records the explicit shutdown-resource classification and checksum PASS.
- Original logs: `artifacts/logs/20261027-180831-Host.log` and `20261027-180849-Client1.log`.
- `cleanup-result.json`: 28 test-profile files restored, 369 Steam-profile files hash-verified unchanged, temporary local BaseLib removed, zero game processes.

**NOT TESTED:** Three/four players, Steam Internet transport/latency, disconnect while an action is executing, save/reload/rejoin, full Act-4 route/rest site, full boss defeat, complete new-content/potion coverage. Existing historical rest-site/VFX/action-queue findings are not closed by this test. All participants still need the identical fixed build for a real remote co-op run.

No source changes, commit, push or publication in this playtest task. Existing worktree edits preserved.
