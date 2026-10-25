# A Thousand Cuts: corrected first-price contract (2026-10-25)

## Installed follow-up: ATC plus concise card text

On 2026-10-25 at 15:12:48 Europe/Berlin, the user-authorized installation completed after fresh zero-process checks and a verified runtime backup. Version remains **0.9.4**. Installed hashes match the prepared package:

- DLL: `63155D592B6F89A8C714872E8ECF35C47CE0F4A310801A1B79025428C79B807E` (exact tested ATC DLL retained).
- PCK: `8B6FE940962D287A9BC676292C96CACAF39A9B3E3C52D0A83BF981E2F4DE8411`.
- Manifest: `1A2834DB1A10732A1B64E880E7D7077F4BE83BA81B1667AE49983996DF13455D`.

The combined PCK additionally removes optional quantity wording from Cinder Receipt, Cold Circuit, Ragpicker's Guard and Scrap Coronation in EN/DE/ZHS. Exactly these four description keys per language changed relative to the earlier ATC package; follow-up lines, conditional effects and variables remain intact. Blood Contract's HP cap remains unchanged. All other packed files, including ATC descriptions and art, are byte-identical to that earlier ATC package. Current source JSON equals extracted packed JSON; 861 native formatter calls and locale validation pass again using the already-built test/DLL snapshot. No gameplay matrix was repeated for this text-only delta.

K2 also cleaned four unused legacy constants in `ComboCardText.cs`. K1 explicitly retained the tested DLL, so that source-only cleanup is **not compiled into this DLL**; the actually loaded card descriptions come from JSON in the PCK. The source/DLL exception is recorded in `source-snapshot.json`. Selection optionality and mechanics are unchanged.

Evidence: `artifacts/atc-cardtext-install-20261025/` contains `preinstall-audit.json`, `installation-audit.json`, `source-snapshot.json`, the clean export/localization logs, the prepared package, and the freshly verified `installed-before-20261025-151247/` backup. Canonical `artifacts/package/LaRata` and `artifacts/LaRata-0.9.4.zip` are synchronized with this installation after document refresh. New relic-art drafts are excluded.

No game was launched, no profiles restored, and no commit or publication performed. Real UI/network/checkpoint acceptance remains open; the earlier candidate 06 ENet disconnect FAIL remains unchanged.

## Initial preparation status and correction

Local 0.9.4 hotfix, technically checked and packaged; installation and live acceptance are pending a safe game-free window. A fresh process inspection found the user's normal game process PID 46732, started at 14:55:24 local time. K6 did not close it, change installed runtime files or restore old profiles.

**Earlier ATC PASS results for increasing prices tested the wrong requirement.** They remain historical evidence of that implementation, not acceptance of the user's desired behavior. This applies to the earlier 0.9.3 increasing-price description and candidate 06 ATC evidence. The correction is a real mechanic and localization change, not merely a tooltip change.

While the buyer owns active A Thousand Cuts, every removal uses the native first-removal base, with current native price modifiers. Successful removal history still increments once per success. Prior removals, later acquisition and a new shop do not increase the active relic's price. Losing the relic restores native price/history and the already-used shop state. Other players retain their own rules.

## Implementation supplied by owning units

- K3: `TreasureAndShop.cs` uses the native private `BaseCost` getter after native `CalcCost`; native `Cost` retains discount hooks and truncation. The actual saved removal counter is not reset. Native `SetUsed` is no longer suppressed; only the active relic overrides the `Used` getter. Cost is recalculated after loss as well.
- K5/K7: only the ATC description key changes in EN/DE/ZHS. English: "You may remove any number of cards at the Merchant. Each removal uses the first removal's base price, with current discounts and price modifiers."
- K6: test fixtures and this evidence. No K6 changes to product code. No new external dependency, version bump, commit, push or publication.

## Reproducible technical checks

```powershell
dotnet build tests/LaRata.ContentChecks -c Release --no-restore
dotnet tests/LaRata.ContentChecks/bin/Release/net9.0/LaRata.ContentChecks.dll --atc
dotnet tests/LaRata.ContentChecks/bin/Release/net9.0/LaRata.ContentChecks.dll --patches-only
dotnet tests/LaRata.ContentChecks/bin/Release/net9.0/LaRata.ContentChecks.dll --wave-localization assets/LaRata/localization
```

Native first prices were independently checked in the installed-version decompilation: 75 normally and 100 with Inflation; native increments without ATC are 25/50. Fixture expectation tables are independent of the product delegate.

| Buyer modifiers | Normal, every ATC cut | Inflation, every ATC cut |
| --- | ---: | ---: |
| None | 75 | 100 |
| Membership Card | 37 | 50 |
| The Courier | 60 | 80 |
| Both | 30 | 40 |

PASS: 32 combinations of owner context (1/2102), prior counter (0/4), Inflation and four modifier sets. Each performs three consecutive public native purchases, cancellation, insufficient gold, serialized counter/new entry, transmitted-price remote completion, relic loss, native non-owner purchase, reacquisition and an exact-gold public purchase. Totals: **128 public ATC successes, 32 public non-owner successes, 32 modeled remote completions**. A separate stale-cache fixture performs three more public purchases at counter 4 with Membership Card: 37/37/37 without intervening UI/Cost/Used reads. Lower-level completion fixtures cover native removal prohibition and late acquisition after the shop was used.

For each public matrix purchase, displayed cost equals the observed `MerchantCardRemovalMessage.goldCost` and actual native gold debit; exactly one card is removed and the counter increments once. Other players' gold/deck/counter stay unchanged. Cancel and insufficient-gold attempts do not mutate successful state. Removing discounts updates the current price immediately. Without the relic the native rising-price controls remain active.

PASS: all Harmony patches activate (4104 patched methods including fixture substitutions). EN/DE/ZHS key/placeholder/UTF-8 validation and 861 native formatter calls pass. The ATC descriptions have no formatting placeholders. Build succeeds with two NU1900 warnings because NuGet vulnerability metadata was unreachable; no claim of a fresh dependency-security audit.

The initial low-level Inflation control failed because the native BaseCost delegate was initialized before the fixture's ascension interception. Installing that test context first resolved it. No product change was made to conceal that fixture failure. The first sandboxed Godot export emitted editor-directory permission diagnostics despite exit 0; it was repeated with approved directory access. The retained clean export contains no such errors.

## Package evidence

Evidence directory: `artifacts/atc-first-price-20261025/`. Separate immutable candidate: `package/LaRata/`; prior installed DLL/PCK/manifest copied to `installed-before/`. Logs: `build.log`, `native-atc.log`, `patch-activation.log`, `localization.log`, `pck-export.log`. Machine-readable hashes and resource differences: `package-audit.json`.

- DLL SHA256: `63155D592B6F89A8C714872E8ECF35C47CE0F4A310801A1B79025428C79B807E`
- PCK SHA256: `066B792A416EA944D539035A076667476B8433DBDC1EB50F8CF86C42BBFC1F23`
- Manifest SHA256: `1A2834DB1A10732A1B64E880E7D7077F4BE83BA81B1667AE49983996DF13455D` (0.9.4)
- K3 source SHA256: `F81C0C5D026014D5DB22085458B7890C84663F4798D7CA9E3913C4E203982435`.

Relative to candidate 06, only `TreasureAndShop.cs` changed among product C# sources. PCK entry sets are identical; changed entries are exactly the three relic locale files and Godot's generated UID cache. Each locale differs only in `LARATA-A_THOUSAND_CUTS.description` and is byte-identical to current source. Packed art, scenes and audio remain unchanged; the six upcoming relic redesigns are not part of this hotfix.

## Outstanding acceptance and installation contract

These fixtures use actual game models/payment/deck/counter and native purchase/message entrypoints, with UI selection/desktop services substituted and singleplayer no-op transport observed. They are **not live UI, real network or full save/load tests**. Serialized `ExtraPlayerFields` is only a field roundtrip. Direct modeled remote completion is not a network delivery test. One message per completed fixture purchase is not proof against simultaneous live clicks.

1. K1 coordinates a user-confirmed regular save/exit. K6 freshly checks actual processes; no forced kill and no stale profile restoration.
2. Verify the prepared candidate hashes and installed-before baseline, take a fresh backup if installed state changed, then install the three runtime files exclusively and hash-compare them. Version stays 0.9.4.
3. Only in the assigned desktop window, use fresh profile/save backups and the established native test launcher. Exercise three real shop removals, late acquisition, a subsequent shop, discounts/Inflation, cancellation/exact gold and relic loss; record displayed price, gold and deck before/after.
4. Reload a supported completed-room checkpoint with removal history and relic ownership retained. Exercise host/client ownership and simultaneous UI actions with real clients. Three-/four-player coverage is still open unless separately executed.
5. Preserve complete logs and strict error gates. Candidate 06's unresolved ENet disconnect **FAIL remains unchanged**; this hotfix supplies no network root-cause fix or release approval.
