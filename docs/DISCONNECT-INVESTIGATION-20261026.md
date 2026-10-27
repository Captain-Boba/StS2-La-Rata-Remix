# Disconnect investigation — 2026-10-26

Owner: K6 (independent QA); K3 owns relic identity/metrics, K2 owns booster choices. K1 coordinates the shared build/test environment. This report distinguishes user-log evidence, source analysis, console regression and live acceptance.

## Confirmed user-log evidence

Source directory: `%APPDATA%/SlayTheSpire2/logs/`. These are real Steam sessions, separate from the older synthetic ENet client-window-close failure. Individual log records have no wall-clock timestamps; times below are session-header/local Windows times, not exact disconnect times. Rotation filenames mark the next session, not necessarily the session start.

| Log / session | Evidence | Meaning |
| --- | --- | --- |
| `godot.log`, 26 October 18:13:44–21:03:13 | 26450, checksum 862; local/remote dumps start 26452/26690; disconnect 26946 | `After player turn start`, BOWLBUGS_NORMAL, zero-based act 1. Only difference across 236 dump lines: choice IDs `22,53,60` / `22,52,60`. |
| Same log | 43649, checksum 561; dumps 43651/43765; disconnect 43906 | Exiting VAKUU. Only difference across 112 lines: MUSIC_BOX `RelicModel_LaRataRelicAcquisitionV1` 16 / 15. Both sides already have MEMBERSHIP_CARD acquisition 15. |
| Same log | 65743, checksum 706; dumps 65745/65881; disconnect 66061 | Exiting PAEL. Only difference across 134 lines: PAELS_TOOTH acquisition 23 / 22. |
| `godot2026-10-26T01.15.36.log`, starts 25 October 17:13:41 | 13153, checksum 485; dumps 13155/13221; disconnect 13304 | Exiting PAEL. Only difference across 64 lines: PAELS_FLESH acquisition 11 / 12. |
| `godot2026-10-26T18.13.43.log`, starts 26 October 14:47:01 | 16405/16447 checksum 684; 34131/34156 checksum 526 | Host detects VAKUU and PAEL divergence and disconnects the peer at 16621/34308. Repeated reports of each checksum are not independent incidents. No complete paired dumps here. |

The latest session identifies game v0.107.1, LaRata 0.9.4 and BaseLib 3.4.7, with three players and this user's process acting as client. The explicit disconnect reason is `StateDivergence` (Steam application-close reason 1012), not evidence of a transport outage. Earlier host-role logs confirm the problem also occurs when this user hosts.

SHA-256 of logs inspected:

- `godot.log`: `30EF6402B2F78055C0DFE44AC5195053FAEA9BF87BA91703EDAAD0C5131890B4`
- `godot2026-10-26T01.15.36.log`: `FFACDEF93E0D4659193718C34A395F5B46D38E4996E34E7A7DADA135237AA685`
- `godot2026-10-26T18.13.43.log`: `776A476C7D6731E8F73B2294CD9A8B76668763769AA1948660E2248E0716A841`

Rejoin is separately evidenced by `ClientLoadJoinResponse`, `LobbyBeginLoadedRun`, then `Loading a multiplayer run` at 27002/43984/66111 and subsequent room loading. It demonstrates continuation through a loaded run, not removal of the cause.

## Causal investigation and regression contract

### P0: imported relic identity and owner sequence (K3)

Source path identified by K3: local acquisition assigns ordinal N; native `RewardObtainedMessage` serializes that already obtained relic; remote deserialization preserves N. The old `RelicMetrics.Identity` returns immediately for a nonzero ID without advancing the receiving owner's sequence. The next jointly acquired Ancient relic can therefore obtain N+1 locally and N remotely. This matches the MusicBox/MembershipCard log relationship. A second boundary is native `Player.SyncWithSerializedPlayer`: replacing inventory must also reconcile the persisted owner sequence, including the existing BaseLib payload and removed-relic high-water mark.

Acceptance: native reward packet roundtrip followed by remote acquisition and another Ancient relic must produce equal full native state dumps and checksums. Existing relic IDs and metrics must survive; IDs must not be arbitrarily renumbered. New allocations must exceed imported live/archive/sequence high-water marks. Reads, serialization and uncommitted option inspection must not allocate persistent identities. Identity/metric changes must remain checksum-visible.

### P0: shop booster choice reservation (K2)

Source path identified by K2: native merchant relic purchase awaits acquisition/`AfterObtained` before sending the obtained relic. An already opened booster consequently arrives remotely as `Opened=True`; the remote side skips the choice reservation that the buyer executed. Historical correlation: `godot.log` 26264 reserves choice 52 (next 53), 26322 resolves choice 52, 26324 records the Ironclad booster purchase; dumps 26605/26843 both contain that opened floor-21 booster while their choice counters differ. The native console reproduction below demonstrates this ordering defect independently.

Acceptance: exercise actual merchant purchase completion and native reward packet handling with a pending booster selection. Each modeled peer reserves exactly one choice, agrees on the same acquisition/next-sequence progression, receives one shop purchase transmission, and reaches equal final state after choice resolution. Loading an already opened booster must not reopen it. Preserve payment, purchase-completion and cancel semantics. No suppression of checksum fields or assertions.

### Technical verification status

Completed 27 October after explicit resumption and verification of both owners' freezes. Evidence is under `artifacts/disconnect-qa-20261027/`; `manifest.json` records source/binary/log hashes and command results. No product correction by K6 was needed.

| Final check | Released 0.9.4 baseline | Frozen fixes |
| --- | --- | --- |
| `--disconnect-regression` | 313 assertions, 113 failures | 313 assertions, 0 failures |
| `--shop-booster-regression` | 208 assertions, 72 failures | 224 assertions, 0 failures |
| Existing `--wave-relics` | Not repeated | PASS: metrics, archive, removal, Potion Belt, pending/reopened owner choices |
| Existing `--wave-deferred` | Not repeated | PASS: reward/rest provenance, native action boundaries, replacement and removed-source restoration |

**537 final assertions pass on the fixes**, plus the two existing suites. Baseline failures are actual identity/choice/state differences followed by the runner's deliberate assertion exception (Windows exit `-532462766`), not missing methods/fields. All passing commands exit 0. The additional 16 shop assertions cover repeated purchase/Ancient, non-booster, insufficient gold, send-fault recovery and overlapping owners; they execute only after the primary cases pass. They are therefore not included in the failing baseline's count.

Identity reproduction: native `RewardObtainedMessage.Serialize/Deserialize` imports the acquired ID; native `AddRelicInternal` then acquires the next Ancient relic. Baseline inventory IDs become 3/2 despite equal pre-Ancient states; fixes give 3/3 with equal full dumps/checksums. Twelve combinations cover MusicBox/PaelsTooth/PaelsFlesh and modeled party sizes 1–4. Additional checks cover one-sided preview reads, repeated serialization, checksum sensitivity to changed IDs/metrics, native relic JSON, actual `Player.FromSerializable` and `SyncWithSerializedPlayer` using BaseLib's existing player payload, stale/live/archive sequence bounds, removed-highest sequence, owner isolation and ambiguous legacy archives. Actual `RelicCmd.Obtain`/OldCoin pickup replay yields exactly 300 credited gold on each side, avoiding ledger double counting.

Shop reproduction: actual `OnTryPurchaseWrapper` runs the installed production patch and native purchase/payment/reward transmission. Captured native reward bytes contain `Opened=true` in the baseline; remote acquisition skips its reservation, producing one extra local choice and a different checksum. Fixed packets contain `Opened=false`; the native purchase remains pending until selection resolves, with one relic/gold transmission and one completion. Fresh seven-card and saved five-card offers, keep-two/keep-none, party sizes 1–4, repeated purchase followed by Ancient, opened-relic JSON reload, ordinary relic purchase, rejected payment and an injected send exception are covered. Two owners can keep selections pending together and resolve them in opposite orders without changing ownership or final checksums.

Fixture boundaries: desktop text/seen-marking is substituted before async methods are JIT-compiled. The no-op transport advertises client mode to permit the **native** remote-choice waiter; explicit replay results resolve it. `CardSelectCmd.Selector` is not used. The native packet serializer, full state builder, checksum algorithm, purchase wrapper and choice reservation remain active. One-player cases are modeled client-path controls, not live solo acceptance. Gold-message amounts and deck contents are separately checked; the out-of-combat native checksum does not cover every deck detail. The two-existing-suite checks use their established fixtures, not a live game.

Final binary SHA-256:

- Released baseline DLL: `D354E92F9197DDC123B950BC24CB75E383BF02C4601FCA89917D7AAFD4B8D0C3`
- Fixed Debug DLL: `19716E6529F21AA743A0331CF3A76D9070FE24AA80144DAC41E14E967F7650A6`
- Identical final test runner in both isolated directories: `7696CB1E6CA6F9EDA0FCF3F55482346015D98D6F0751CA11572BB7D6628C87CD`

Commands (workspace root; output/exit files alongside each log):

```powershell
dotnet build tests/LaRata.ContentChecks/LaRata.ContentChecks.csproj --no-restore -v:q
dotnet artifacts/disconnect-qa-20261027/baseline/LaRata.ContentChecks.dll --disconnect-regression
dotnet artifacts/disconnect-qa-20261027/fixed/LaRata.ContentChecks.dll --disconnect-regression
dotnet artifacts/disconnect-qa-20261027/baseline/LaRata.ContentChecks.dll --shop-booster-regression
dotnet artifacts/disconnect-qa-20261027/fixed/LaRata.ContentChecks.dll --shop-booster-regression
dotnet artifacts/disconnect-qa-20261027/fixed/LaRata.ContentChecks.dll --wave-relics
dotnet artifacts/disconnect-qa-20261027/fixed/LaRata.ContentChecks.dll --wave-deferred
```

Build succeeded with zero errors and two NU1900 warnings because the NuGet vulnerability feed was unreachable. This is a Debug technical build, not a release package. Earlier setup failures (missing fixture BaseLib hooks, presentation inlining and singleplayer remote-waiter guard) were corrected in tests only. A baseline runner copy initially met a Windows file lock; after the process ended, both runner hashes were verified equal and the final comparison rerun. Those incomplete attempts are not counted above.

## Separate findings and remaining gaps

- Latest log 76775/76822/76869: `Unexpected act` in native `NRestSiteCharacter._Ready`. Native animation selection supports act indices 0–2; preceding context is Act 4/Kingsguard. This is a separate Act-4 rest-site integration issue; these entries do not establish a combat disconnect cause. No fix in this scope.
- 40741: `NHorizontalLinesVfx._Ready` invalid parent cast after GRAND_FINALE. 75767: invalid index bounds in `NInspectCardScreen.SetCard` during card detail opening. These stacks do not prove a LaRata/BaseLib/other-mod cause for the disconnects.
- The reported Act-4 mid-combat disconnect remains unidentified in these logs. Matching host/client logs and a specific encounter/run are needed.
- Native JSON and in-memory sync checks are not real disk Save/Load, live UI, Steam networking, latency, reconnect or host migration acceptance.
- Later K1-assigned live coverage: 2/3/4 peers, buyer as host/client, different simultaneous selection orders, Ancient after shop rewards, repeated purchases, save/load and rejoin. All peers must agree after each boundary. Reserve the shared installation and desktop for one unit at a time.
- No game start, installation, package/release update, version bump, Git operation or Workshop change is part of this investigation.

## Authorized live follow-up — 2026-10-27 (K6)

The scope above describes the earlier investigation. The user subsequently authorized local installation and targeted co-op tests through K1. K1 assigned K6 the exclusive installation/desktop window and later limited completion to the existing two-client chain, disk reload/rejoin and a new acquisition after loading. No new three/four-client or Act-4 campaign was started.

**Scoped result: PASS for the recorded two-client regression paths; not a general multiplayer or release approval.** These are two real game processes using the game's native local ENet transport, not the transportless 537 console assertions above. Game Public v0.107.1, BaseLib 3.4.7, LaRata 0.9.4, isolated native IDs 1 and 2101, both Ironclad. Steam transport was disabled by the existing `Start-Test.ps1` launcher.

### Installed runtime and evidence

The checked Debug fix remains installed at `E:\SteamLibrary\steamapps\common\Slay the Spire 2\mods\LaRata`. Existing `Switch-ModVersion.ps1`/`Install.ps1` were used. The local mod takes precedence over the same-ID Workshop copy. Version and resources are unchanged; the Workshop copy and canonical package were preserved at their prior hashes. This is a local fix installation, not a refreshed release package or upload.

| Installed file | SHA-256 |
|---|---|
| LaRata.dll | `19716E6529F21AA743A0331CF3A76D9070FE24AA80144DAC41E14E967F7650A6` |
| LaRata.pck | `C6B307CA6C35038828EDF9A45BFCC7206B09F21F3F9023DEAB791138291AA187` |
| LaRata.json | `1A2834DB1A10732A1B64E880E7D7077F4BE83BA81B1667AE49983996DF13455D` |

Evidence directory: `artifacts/disconnect-live-20261027/`. The four unabridged logs are under `artifacts/logs/`: `20261027-014216-Host.log`, `20261027-014233-Client1.log`, `20261027-021628-LoadHost.log`, `20261027-022145-Client1.log`. Launcher metadata, screenshots, native save copies, comparisons and cleanup manifests are retained.

### Recorded scenarios and acceptance

| Scenario | Actual actions and measurable result | Status |
|---|---|---|
| Client shop booster | Actual Silent Booster purchase for 165 gold, 3849 → 3684. Kept SHOCKWAVE and BACKSTAB; deck 10 → 12; purchased stock disappeared. Both peers reserved client choice 1, next 2, and resolved indexes 5,4. | PASS |
| Pending client selection | Host opened/left its shop and voted for the next combat while client selection remained pending. Room did not transition until client completed its selection and voted. Next combat loaded on both peers; native checksum 1 = 749421507. | PASS, observed room/vote boundary only |
| Ancient after shop | Both players acquired MusicBox, PaelsTooth and PaelsFlesh through the actual Ancient UI. PaelsTooth removed/stored five Strikes per owner. | PASS |
| Overlapping native choices | Client opened PaelsTooth selection first, host second; host completed first, client second. Owner-specific choice IDs and remote waiters resolved. This was overlapping Ancient selection, not two pending booster purchases. | PASS |
| Host shop booster | Actual Defect Booster purchase for 189 gold, 515 → 326; kept none, deck stayed 6; stock disappeared. Both peers reserved host choice 2, next 3. Subsequent event exit checksum 7 = 2997453063 matched. | PASS |
| Native disk reload and rejoin | Host Save and Quit, both processes closed normally; fresh LoadHost and Client1 processes resumed the host-owned disk checkpoint. Client booster did not reopen. Full serialized `players` data exactly matched before/after reload. | PASS for saved checkpoint |
| Acquisition after loading | Both selected PaelsHorn through Ancient UI. Host sequence 9 → 10, client 10 → 11; saved IDs and BaseLib sequence agree. Each deck gained exactly two RELAX cards (6 → 8 / 7 → 9), metric `cards_added=2`; all prior relic objects/metrics and gold remained unchanged. Next native checksum 1 = 3528489469 matched on both peers. | PASS |
| Normal shutdown | Host left through Save and Quit, exited lobby, then window close. Client displayed “Host left the game” and was closed normally. No forced termination. | PASS for this exit order, with engine diagnostics below |

Room/Ancient setup used the game's existing networked developer console: `room shop`, `ancient VAKUU MUSIC_BOX`, `ancient PAEL PAELS_TOOTH`, `ancient PAEL PAELS_FLESH`, and after reload `ancient PAEL PAELS_HORN`. The client received `gold 3000` for shop affordability; BowlerHat modified the actual gain. Purchases and choices themselves used native UI. Seven initial shop rooms were visited in total (five before the client purchase, one for the host purchase, one after the following event); this is not a distribution sample. Combat was entered for a checksum, then completed using native `win`, so this does not establish combat balance or a full-playthrough pass.

Setup artifacts: an initial `room merchant` was rejected (correct enum: `shop`); one early debug Ancient was obscured by an unfinished combat-loot overlay and was not counted. The reward overlay was cleared through normal progression, then MusicBox was repeated from a clean event. An interim icon-based “Necrobinder” label was corrected to the logged model `LARATA-SILENT_BOOSTER`.

### Checkpoints, comparison limits and diagnostics

First-session checksum IDs 0–7 matched on both peers: `2291290474`, `749421507`, `3393788112`, `576223647`, `2582107818`, `1381855102`, `3851420461`, `2997453063`. Reload-session IDs 0–1 matched: `278526217`, `3528489469`. See `first-session-comparison.txt` and `resume-session-comparison.txt`. Existing `Compare-CoopChecksums.ps1` was run with explicit minimums 8 and 2 respectively, expected players 2, version 0.9.4 and scenario markers; these are scoped comparisons, not the default ten-checksum full scenario.

`before-reload.save` SHA-256: `893B06F88113DCC56E8924D4CF1113DCC09FC2BFD16BDC6E448DDB6A9DE6CA7E`. The last durable checkpoint contains the client Silent Booster and all three Ancient relics. It **does not contain the later host Defect purchase**, which happened in debug-created rooms after that checkpoint. Therefore host-booster persistence is **NOT TESTED**, not PASS or a demonstrated product save defect. Host owns the native multiplayer save; no second client disk save was expected. `after-rejoin.save`, `after-rejoin-horn.save` and `save-resume-comparison.json` establish preserved player state plus subsequent acquisition/sequence advancement.

No StateDivergence, gameplay ERROR or runtime exception was found in these recorded paths. Each of the four processes emitted 12 Godot shutdown resource-leak ERROR lines. Comparisons explicitly used `-AllowEngineShutdownResourceLeaks`; original diagnostics remain intact. This is **not a clean unrestricted error-log pass** and does not assign those leaks to a particular mod. The earlier PeerNotConnected shutdown storm did not reproduce under this normal host-first exit order, which does not establish its root cause or resolution.

### Cleanup and remaining acceptance gaps

Fresh pre-live backup contained 485 verified files. After normal test shutdown, the four owned test profiles (1/2101/2102/2103) were archived and 53 baseline files restored. All 476 user-data baseline files then matched their original hashes, including Steam profiles; no extra files remained in the backed-up user-data roots. Temporary local BaseLib was moved out of active mods into `temporary-BaseLib-removed/`; its original Workshop dependency remains unchanged. Zero game processes/windows remained. `cleanup-result.json` and `final-integrity.json` document this, including unchanged K2/K3 frozen sources, Workshop runtime and canonical package. The local fixed LaRata installation remains active. No product code, Ideas file, Git history, version or publication was changed by this live follow-up.

Still open: real three/four-player sessions; current-wave solo; Steam/remote-machine transport, latency/loss and host migration; repeated shop purchases by the same owner; simultaneous pending booster choices; host Defect purchase persistence; and the unidentified Act-4 mid-combat disconnect. These remain separate from modeled party-size coverage and from the successful two-client paths above. The shared desktop/installation window is released after cleanup.
