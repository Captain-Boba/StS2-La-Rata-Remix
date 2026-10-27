# Silent counterlogs — 2026-10-27

Read-only investigation of the user's `silent-log1.zip` and `silent-log2.zip`. No product edits, installs, game launches or agents. Originals retained; ZIP entries validated as flat `godot*.log` paths before extraction. SHA-256 provenance: `artifacts/silent-logs-20261027/source-hashes.json`. Reproducible comparison: `analyze.py`, full differences: `state-differences.json` in that directory.

## Findings

**The Silent counterlogs confirm the three Act-4 Snecko Skull statistics divergences exactly. No additional independent cause of those combat desyncs appears in these files.** An earlier Vakuu divergence identifies Preserved Fog's acquisition ID, extending the concrete examples of the previously investigated identity-sync issue. Separate UI and post-disconnect errors remain visible and must not be conflated with the first divergence.

### Original modded session

`silent-log1/godot2026-10-26T21.04.14.log`: 80,770 lines; session header line 22 is **26 October 18:16:30**, not the rotation filename's time. BaseLib 3.4.7.0 (208), LaRata 0.9.4.0 (268), game Public v0.107.1. Run AYPN8UGULG, Silent/Ironclad/Necrobinder (420). This is historical evidence predating the local fixes installed on 27 October, not a post-fix test.

| Checksum / event | Silent error line | Dump lengths | Only differing field, Host → Silent | Exact reversed pair in host log |
| --- | --- | --- | --- | --- |
| 561 / exiting Vakuu | 43145 | 112 / 112 | Preserved Fog acquisition ID **15 → 16** | 42932 |
| 70 / Devoted Sculptor, player-turn start | 67314 | 378 / 378 | Snecko Skull `power_value_adjustment` **32 → 33** | 66779 |
| 118 / Kingsguard, player-turn start | 71542 | 438 / 438 | Snecko Skull `power_value_adjustment` **42 → 44** | 70973 |
| 262 / Rat King, player-turn start | 79786 | 441 / 441 | Snecko Skull `power_value_adjustment` **50 → 53** | 79115 |

Host reference: `artifacts/act4-host-logs-20261027/godot2026-10-26T21.04.17.log`. Full line arrays compared, trimming only trailing empty lines from dump boundaries. Every Silent local dump equals the host's remote dump, and every Silent remote dump equals the host's local dump. Each pair differs on exactly one line; remaining recorded state, including powers, HP, RNG and choice IDs, is identical. This is equality of the logged state representation, not proof about unlogged internals.

The three combat cases explicitly disconnect with `StateDivergence`, lines 68104, 72453, 80692. The additional Silent log still does not log the exact preview callback which incremented the counter. The shared field and first disconnect reason are proved; the precise extra invocation is not established by these logs. The prior source investigation identified nested local previews as a candidate path.

Preserved Fog differs only in `RelicModel_LaRataRelicAcquisitionV1`; its `cards_added=1` and `cards_removed=3` statistics agree. This is an identity issue, distinct from benefit tracking. Current `RelicMetrics.AssignCommittedIdentity` advances the owner's sequence before accepting imported IDs, and `RestorePlayer` reconciles it after native load/sync. These existing fixes target this failure class. Existing regression cases use MusicBox/PaelsTooth/PaelsFlesh; this exact historical Preserved Fog event has not been replayed. No claim of a new specific live pass.

### Additional errors and causal ordering

- **Post-disconnect action-queue exceptions:** THUNDERCLAP at 68115, DELAY at 72463, UNMOVABLE at 80702. Each follows the relevant StateDivergence and disconnection. Example: first divergence 67314 → abandon run 68090 → disconnect 68104 → NotInCombat/cancel actions 68107–68109 → old action finishes 68114 → `PopAction` cannot find it at 68115. These are secondary teardown faults in the logged sequence, not evidence that these cards caused the earlier mismatched snapshot. No new fix applied; the earlier ENet-host cleanup fix does not establish coverage of this Steam-client/action-queue path.
- **Act-4 rest-site error:** `Unexpected act` from `NRestSiteCharacter._Ready`, lines 77214, 77261, 77308. Confirms the known separate Act-4 presentation/integration problem. It does not explain the earlier two combat divergences or add a new differing field to the Rat King dump. Remains outside the completed potion/ramp/statistics changes.
- **Combat VFX error:** `NHorizontalLinesVfx._Ready` casts `NParticlesContainer` to `Control`, line 40254. Confirms the previously observed visual error; no demonstrated causal link to a checksum divergence.
- **Other-mod notices:** RegentAnimations 0.50.0 is installed on Silent but not the host (406–408); native join classifies it as non-gameplay and permits entry. Two unrelated manifests lack an `id` and are not loaded. No evidence in these dump differences that those mods caused the investigated desyncs. FMOD parameter warnings and resource leaks at exit also do not establish a new gameplay-state cause.

### Second ZIP: subsequent run with La Rata disabled

`silent-log2/godot.log`: 48,684 lines; starts **26 October 21:04:17** (21). Explicitly skips La Rata because it is disabled (205). Initial join failures: generic `Could not connect` (448), then clear `Mod mismatch: Host mods LaRata-0.9.4 / Local mods empty` (524). That explains the rejected join; it is not a mid-fight desync.

Later run GYX80CEE8S, Ascension 10, Silent/Ironclad/Defect (613), ends with `QuitGameOver` (48619). No StateDivergence message or local/remote divergence dump occurs in this file. BaseLib remains active, so this is specifically a run **without La Rata**, not a claim of a wholly unmodded installation. It cannot validate the La Rata fixes.

## Current implications

- The decision to disable all relic benefit counters is supported independently from both peer perspectives. Current source remains `RelicMetrics.Enabled => false`; observer category remains inactive.
- The known identity-sync fix remains necessary independently of benefit counters, because Relic Remover still uses deterministic acquisition IDs.
- The rest-site/VFX errors and client action-queue teardown deserve separate scoped fixes, with the latter addressing cleanup robustness rather than the initial desync.
- No new code change follows automatically from this read-only review. All peers need the same fixed build for a meaningful future co-op check; neither historical ZIP is evidence of that check.

Subsequent validation: [focused local co-op playtest](DESYNC-PLAYTEST-20261027.md) exercised fresh Preserved Fog acquisition and Silent/Snecko Skull poison in the royal encounter through player turn 3. All 20 native checksum boundaries matched. Its local ENet, setup and shutdown limitations are recorded separately; this does not retroactively validate the historical run.
