# Relic measurements and removal integration

Scope: La Rata Remix, native game v0.107.1, existing BaseLib 3.4.7. This is an implementation contract, not a statement of live gameplay acceptance.

## Later local-test revision: five decision relics and minimal result tooltips

The user's later local-test feedback supersedes the previous visible diagnostic tooltip contract. In-game text contains only positive, supported actual result quantities and their kind followed by "this run" (localized). Owner, acquisition, coverage, gaps, attribution labels, generic event counters and modifier contributions stay internal. Unsupported or ambiguous values are hidden, never replaced by zero. Existing ordinary relic descriptions remain intact. K5 owns that presentation and EN; K7 owns DE/ZHS. The stored DTO/ledger remains available for internal diagnostics and checkpoint identity.

The new content is exactly five additional SharedRelicPool relics in `DecisionRelics.cs`, separate from the previous combo relics and The Relic Remover. Native BaseLib autoAdd plus `[Pool]` performs registration; K2 must not pre-register these types, which could prevent automatic pool insertion. No new powers, startup fields, runtime packages, custom RNG or shop telemetry messages are required.

| Relic / rarity | Decision and native difference | State and hard limits |
|---|---|---|
| Sootbound Ticket / Uncommon | Exhaust a base-Energy-2+ card to recover a random existing Discard card. Unlike Burning Sticks it neither duplicates the exhausted Skill nor generates a new card; unlike Joss Paper/Charon's Ashes it is not a fixed draw/damage exhaust proc. | Saved TriggeredThisTurn, max once per own active turn; not X/Ethereal/end-turn; empty Discard/full Hand does not reserve. Native CombatCardSelection RNG and successful CardPileCmd.Add only. |
| Stairwell Key / Rare | Order three manual payments with strictly decreasing Energy spent (e.g. 2,1,0) for 1 Energy and 2 drawn cards. Unlike Nunchaku/Kusarigama/Shuriken or Crossroads Compass, this is a paid-resource sequence rather than an Attack/card-type count. | Saved LastEnergySpent, DescendingCount, TriggeredThisTurn; max once per own turn; nondecrease restarts the chain; autoplay/replay/fellow players do not advance it. |
| Mocking Bell / Uncommon | A successfully applied own Debuff to enemy A gives 1 Weak to a different random enemy B. This creates target-order decisions rather than Paper Krane's Weak multiplier. | Saved TriggeredThisTurn; max once per own turn; original Artifact rejection/one enemy/no other hittable target do not proc; native Debuff amount typing, Applier ownership and CombatTargets RNG; reserve before recursive Weak hook. |
| Brass Scavenger / Uncommon | Convert actual shield absorption on one own Attack that breaks enemy Block into up to 12 Gold. Unlike Hand Drill this gives no Vulnerable; unlike flat kill gold its variable result depends on choosing a shielded target and actually breaking it. | Saved UsedThisCombat; once/combat, cap 12; own Attack card and native DamageResult.WasBlockBroken/BlockedDamage required; no Expose, ordinary Block expiry, Skill damage or another owner's hit. |
| Moonlit Stopper / Rare | Sacrifice a Potion by explicitly discarding it to make the next manual non-X card of that turn free in Energy/Stars. Unlike Sacred Bark/Belt Buckle/Mummified Hand/Brilliant Scarf it trades a consumable for a deliberately timed payment window. | Saved UsedThisCombat and Armed; once/combat; own active turn only; potion use does not arm; autoplay and X do not consume; ordinary free card does consume; Armed expires at own turn end. |

There are no relic upgrades. Per-turn state resets only when native participants contain the owner; combat state resets at native start/end. Each cap is reserved before awaits, so recursion cannot create an unbounded loop. These states serialize on the real relic acquisition; removal suppresses subsequent hooks and a new acquisition starts separate state/metrics. Native commands and synchronized action ordering supply gameplay behavior.

Retained minimal result keys are self Block gained, HP restored/max HP gained, Gold/Energy/Stars gained, cards drawn/removed/recovered, and unambiguous Energy/Stars saved. `cards_recovered` is recorded only after the native return-to-Hand command succeeds. Positive actual power stacks can be displayed with their native name only for own Buffs or other-target Debuffs; unknown types and all intermediate Contributions are hidden. The existing payment observer includes LaRata's own cost-hook assembly so Moonlit Stopper uses the same actual-debit proof instead of printed-cost estimates.

K6 exclusively owns builds, backup/install, targeted native checks and the newly authorized real local multiclient tests. Native/model checks, real clients, full save/reload and visible UI acceptance must be reported separately. The following older candidate sections retain the preceding implementation evidence; they do not claim this new five-relic revision has been tested.

K6's night-wave candidate 02 subsequently passed the five targeted relic groups and nine existing payment groups. Evidence: `artifacts/night-wave-20261025/candidate-02-relics.log` and `candidate-02-payment.log`; DLL SHA-256 `2408B31A0E30CB556213C5B5808D6F64375A9D3EE429F511C4B7E0786FD661E2` (also read independently by K3). Tests include native CardCmd.Exhaust/pile recovery, native Poison/Weak/Artifact dispatch, native PotionCmd.Discard followed by Energy/Stars payment, owner/cap/negative cases, cloning/reacquisition and native SavedProperty JSON intermediate states. Brass Scavenger's Gold command is native, while its DamageResult trigger boundary is supplied in the fixture rather than proving an entire attack. Desktop/combat guards and choices remain substituted where identified; these are not live-client or disk-save results.

Candidate 01 reproduced a real Mocking Bell fractional-stack fault: native Poison 0.5 became 0 integer stacks but still invoked AfterPowerAmountChanged and triggered extra Weak. Candidate 02's single-line integer guard rejects that zero effective amount; the native fractional and Artifact negative cases pass. This is a corrected product bug, separate from earlier missing fixture setup. K5 delivered five matching RGBA icon assets and EN text; K7 delivered DE/ZHS, including the non-X condition. K3 inspected the five actual PNGs individually for their requested clear silhouettes. PCK/installation, real local multiclients, full save/reload and human visual acceptance remain K6's separate next stage.

### Later Lord Tibo's Reset integration boundary

K2 owns the additional card and its ResetEnergyLedger; it does not become a relic source. The only K3 code delta is in the existing `RelicPaymentMetrics.Assigned` callback: forward the exact native field change to K2's narrowly armed `ObserveEnergyChange(object,string,int,int)` before the payment-null guard; after first actual positive payment debit is captured, call `RecordPayment(CardModel,int)` once. Mark EnergyPaid before forwarding so reentrant callbacks cannot repeat it. There is no new event bus, second scalar patch, shop protocol or generalized resource ledger.

Native History.EnergySpent and AfterEnergySpent contain the requested amount; the history entry has no CardModel. The actual post-stfld difference is therefore the payment evidence, including clamping/Stars substitution. Forwarding at SpendResources completion would be too late when AfterEnergySpent awaits and another player's Reset runs in the meantime. Zero payment, previews, energy gains, Stars and ordinary energy-loss commands do not create payment receipts.

K2 excludes all Reset card IDs, reserves all owners' pending refunds before awaiting, and handles native combat/turn clearing. Its native PlayerCmd.GainEnergy callsite wrapper preserves modifiers, awaits and clamping; the existing field observer reports actual credit only while that concrete refund write is armed. Root command identity separates modifier-recursive gains from the refund, and the marker is consumed before EnergyChanged callbacks. Blocked/capped portions remain unrefunded rather than being inferred from a task's net before/after balance. Existing card-effect attribution masks ensure these card refunds are not labeled as relic benefits.

The combined Reset revision requires K6's separate tests and shared source freeze; the previous five-relic/payment candidate results do not establish this later card's behavior.

K6 night-wave candidate 04 later passed the targeted Reset and supplied modifier-await/concurrency cases (`candidate-04-cards-01.log`), the existing payment regression groups (`candidate-04-payment.log`), and 861 native EN/DE/ZHS formatter calls (`candidate-04-localization-01.log`). Its DLL SHA-256 is `0780E9366A8D77E09D80EC27D72EE183D213FB26EFC065DCC724D6BC50A242B5`, independently read by K3 from the already built file and matched to the K6 binary manifest. No K3 build/test was run. The concurrency gate and card-hook/active-combat boundaries are supplied fixtures; these logs do not establish real simultaneous clients, a disk save/restart or live UI. The narrow observer delta remains frozen for K6's shared installation/multiclient stage.

Current 0.9.4 scope follows the user's later prioritization: obvious, reliably attributable values first. Full benefit coverage of all 298 native relics and complicated shop/payment attribution are explicitly deferred. The source inventory is retained for later work; its size is not a claim of implemented or verified coverage.

Retained measurements include actual scalar/resource/power changes, completed draws/deck mutations and successful potion/relic grants, native upgrade/enchantment and generation outcomes, finite orb/forge/auto-play/retention events, explicit La Rata combo triggers and Well contest outcomes, completed A Thousand Cuts shop removals, and the narrowly exact card-payment cases described below. Their units and attribution stay separate; there is no total relic score. Numeric modifier contributions remain explicitly before later modifiers/limits, not final damage or block.

Deferred areas include multiplayer shop gold-savings correlation, ambiguous card payments, complete downstream attribution of shared powers/pets, and complicated replay/retention/map/reward-option consequences. Deferral does not erase an already recorded direct gain or turn it into a zero. `shop_savings_deferred` is explicit for MembershipCard/TheCourier; an actually encountered ambiguous payment adds `complex_payments_deferred` to its original source ledger/archive.

## Ownership

- K3 owns player relic effects, shop pricing and measurement code.
- K5 owns the native tooltip and relic-selection presentation and English text.
- K7 owns German and Simplified Chinese translations.
- K6 owns test files, technical test execution and build evidence.
- K2 owns the central initialization call in `ModEntry.cs` and booster contents.
- K4's Royal Regalia uses boss adapters, not player relic instances. It must not update player counters.

## Measurement storage

`RelicMetrics.Register()` initializes uniquely named BaseLib save fields before Harmony patches are applied.

- Each mutable `RelicModel` carries its acquisition ordinal and a versioned measurement ledger.
- Each native player carries its next ordinal. The native run carries an archive of removed relic instances.
- Ownership comes from the actual relic owner, not a process-global local player.
- The run save is the identity boundary; owner ID plus ordinal identifies an acquisition. Simultaneous acquisitions by different players do not share an order-sensitive global sequence.
- Reacquiring a relic creates a new ordinal and a new ledger. A model ID alone is not an instance key.
- Existing relics without measurements are not assigned retrospective zero totals.
- Loading native relic/run fields replaces the corresponding state. No profile sidecar is merged into a restored checkpoint.
- The presentation getter does not allocate ordinals or modify measurements.

`RelicRunStats.Coverage` describes when recording began, not complete coverage of every consequence of that relic. `Gaps` remains independent of recorded values and available for internal inspection; the later minimal-tooltip instruction explicitly hides these diagnostics in-game. Canonical compendium/shop models have no owned run totals.

The full native source inventory is `RELIC-METRICS-COVERAGE.json`. Its 298 rows identify native types, overrides, commands and candidate measurements. A candidate is not proof of a working observer, and an implemented observer is not a live acceptance result.

## Measurement semantics

Direct scalar observations occur immediately after native field assignment, before nested `Changed` callbacks:

| Native state | Quantity |
|---|---|
| Creature Block | Actual integer block increase/decrease after clamping |
| Creature CurrentHp | Actual HP restored/lost after caps |
| Creature MaxHp | Actual maximum HP increase/decrease |
| Player Gold | Actual gold gained/lost |
| PlayerCombatState Energy/Stars | Actual resource increase/decrease |
| PowerModel amount | Actual power amount increase/decrease, qualified by power model ID |

Block gained is not damage prevented. A power amount increase/decrease is not automatically a benefit. Increasing Strength does not assign later card damage to the relic. HP damage and damage absorbed by block remain distinct quantities. There is no combined value score.

Native history and inventory commands provide completed draw and permanent deck-add/remove events. UI offers, selection candidates and printed effect values are not counted as completed gains.

An effect-local observation context is restored at every async continuation boundary. A nested card/power hook masks the initiating relic; a different relic establishes its own owner-bound context. Original gameplay exceptions are preserved. No statistic is inferred from relic flashes.

K6 candidate 02 checked actual interleaved OldCoin async callbacks, the seven real scalar assignments, native/BaseLib save payload roundtrips and a two-target native damage command with repeated preview queries. These cases establish their bounded technical behavior, not all-relic or live acceptance. Pure modifier queries, delayed reward selections, pets, generated cards, enchantments, map restrictions and other indirect effects require explicit resolution-specific observers or an honest gap. Merely listing such a gap does not complete the requested full benefit coverage.

### Deferred reward and option provenance (candidate 03)

Native reward construction records only an owner-local acquisition ordinal in an existing BaseLib `SavedSpireField<Reward,int>`. The native reward checkpoint and packet payload carries this marker; no offer or constructor increments a benefit counter. The native reward subtype's actual `OnSelect` and its async continuation establish the source for real scalar/inventory mutations. A skipped reward never enters this context. A failed full-inventory potion procurement produces no potion gain. Nested unrelated rewards clear the parent source.

Resolution finds the exact currently owned acquisition, or its existing removed-relic archive entry. Reacquiring the same model cannot redirect an older pending reward to the new instance. If neither identity exists, it records no invented result. Archived sources continue their existing ledger after removal and reload; no replacement model or sidecar is constructed.

Native rest-site options get a transient owner/acquisition marker when constructed by a relic's `TryModifyRestSiteOptions`. Native regeneration recreates this provenance; there is no unsupported custom option save format. Their real `OnSelect` observes the actual effects and adds `rest_actions_completed` only on success. Ordinary Heal/Smith options do not inherit another option's origin.

Additional finite observations distinguish completed auto-plays from the damage the card deals, actual Sovereign Blade base-damage changes from final damage, direct completed extra orb passive calls from their eventual consequences, and Runic Pyramid hand retention from cards with independent Retain. Multiple independent flush preventers are not arbitrarily split. Candidate 03 requires targeted K6 verification separately from candidate 02.

K6 candidate 03b subsequently passed real Gold/Card/Relic reward JSON and packet roundtrips, removed-source archive rollback/reacquisition, the native Amethyst/BlackStar/PrayerWheel gain paths, full-inventory potion failure, rest-option regeneration, the action boundaries and suspended SwordOfStone self-replacement. Native UI alternatives/choices remain predetermined fixtures, not live interaction. The initial GoldReward override omission was reproduced and corrected by preparing the existing public BaseLib extended payload for native concrete reward serialization overrides.

### Actual card payments (candidate 04)

Native local energy modifiers retain provenance at construction, actual native list attachment and clone. Temporary star-cost objects retain it when attached. These transient combat objects use no additional persistence format; native combat reload semantics apply to the modifiers themselves. Discount application, expiry, discard, and preview reads do not increment savings.

`CardModel.SpendResources` is the payment boundary. Cost steps are observed during its actual first energy/star calculation; unrelated card calculations are masked. Native private SpendEnergy/SpendStars identify debit requests, and the immediate scalar assignments provide actual debits before any resource-change callbacks. A completed payment is committed once for that invocation and current run.

`energy_saved`/`stars_saved` or a paid surcharge is Exact only when one changed cost step has a known relic source and the final requested cost, actual debit, and step result agree. Under the narrowed scope, overlapping modifiers, later replacements, clamping and currency substitution produce no payment metric and no invented zero. Instead, `complex_payments_deferred` is retained in the existing ledger's optional Deferred array and surfaced through DTO Gaps, including the removed-source archive. Existing version-1 ledgers without the optional field remain readable. Already granted cost modifiers can finish attribution to their original acquisition after source removal; reacquiring the same relic cannot inherit that provenance.

Candidate 04b exposed a patch-order fault: the Bookmark hook had the correct source, but its native relative modifier was untagged. K6 reproduced that only recompiling that same hook after installing observers made it pass; the diagnostic extra patch is not part of the product. Candidate 04d uses the standard Harmony category API: install uncategorized mutation observers first, then the LaRataRelicEffectContexts category. K2 owns that initializer change; K3 owns the category declaration. This is a targeted initialization correction, not a generic repatching subsystem. See the [Harmony category documentation](https://harmony.pardeike.net/articles/annotations.html).

K6 candidate 04d passed the payment cases using the exact product initialization sequence and no diagnostic repatch: native Hook.AfterFlush/Bookmark 3 to 2, Mummified Hand energy and stars plus expiry, Brilliant Scarf, Spiked Gauntlets, owner isolation, native cost cloning, removed source/reacquisition, and native relic/run-archive gap checkpoint roundtrips. Ambiguous combinations, absolute overwrites and clamped debit produce no numeric value and retain the deferred flag. Logs: `artifacts/dev-wave-20261025/candidate-04d-payment-01.log` and `candidate-04d-localization.log`. The latter checks 789 formatter calls including the deferred tooltip keys. Existing scalar/context/reward/remover regressions under this final initialization sequence are tracked separately by K6; neither these cases nor earlier 03b checks establish live play/network/full-save acceptance.

K6 subsequently passed the bounded existing scalar/context/remover and deferred-reward/action/save-payload regression sets with the same productive initialization sequence (`candidate-04d-relics.log`, `candidate-04d-deferred.log`). Final candidate DLL SHA-256 is `35E9EA15F3D5FC5ED5E8AA177FF996195AC111975BD217F468D497041C954639`; K3 independently read that hash from the existing built file without executing a build. K6 owns the build/test evidence. Product sources remain frozen pending K1's next assignment. No live game, real peer/reconnect, full save/load, packaged PCK/installation or human visual acceptance is implied.

### Deferred shop measurement research (not implemented)

The user's later 0.9.4 prioritization supersedes full native benefit coverage: keep obvious, reliably attributable measurements and defer complicated cases. The following shop protocol research is retained as analysis history only. Work on challenge/epoch/stamp correlation has stopped; no shop telemetry transport or new networking code was implemented. Multiplayer shop savings are explicitly deferred and are not a 0.9.4 acceptance blocker.

The native purchase wrapper reports local success and gold spent. Native GoldLostMessage and RewardObtainedMessage have no shared purchase ID; RunLocation has no checkpoint generation. MembershipCard also uses LocalContext, so a peer's inventory price is not an authoritative undiscounted baseline. Reversing discount formulas is not an accepted substitute.

K1 authorized a narrowly scoped measurement correlation through existing BaseLib messages, with no gameplay-price mutation. A proposed host epoch plus fresh buyer/receiver challenges binds measurements to current native RunManager/INetGameService objects. A bare new GUID is insufficient: it does not version an older unversioned native gold event.

The revised proposal places a finite ShopNativeEventStamp immediately before the actual native gold/relic/card/potion sync send. It includes the authenticated owner, current host/buyer session, persisted owner-local native-event ordinal, native phase, amount/model and location. Receivers must pair that stamp with its exact next matching native event and observe the real debit/grant. Missing, duplicate, conflicting or replaced stamps cannot fall through onto a subsequent same-price purchase. A second unmatched stamp invalidates the measurement window rather than overwriting the first. Native game messages and their gameplay behavior remain unchanged.

Native source evidence: BaseLib's wrapper and the native GoldLost/RewardObtained messages use Reliable; NetTransferModeExtensions maps Reliable to channel 0. NetHostGameService relays the received channel/sender before invoking handlers. SteamHost/Client use SendMessageToConnection with Reliable=8; ENetHost uses peer.Send on that channel with the reliable flag. The ordering guarantees are documented by [Steamworks](https://partner.steamgames.com/doc/api/ISteamNetworkingSockets#ReceiveMessagesOnConnection) and [ENet](https://github.com/lsalzman/enet/blob/master/docs/design.dox). These are source/transport contracts, not a live peer test.

Combat buffering needs separate first-arrival and replay handling: consume and bind the original stamp at first native arrival; preserve that binding through the native buffered replay; increment an ordinal once and never stamp replay with a fresh session. Calling TaskHelper.RunSafely is not proof that a debit or grant completed. In-flight proof records remain transient and bounded; restored checkpoints never reactivate them. Confirmed correlation IDs and metric ledgers share the native checkpoint. An event before save followed by a dataset after reload must not commit without a new exact proof. Rejoin changes the buyer challenge; host changes invalidate the old host epoch.

The dataset contains the actual raw native price and ordered Membership/Courier before/after trace, not a free-form claimed savings total. Match requires wrapper success, the original source acquisition identities, real debit and inventory/removal completion, and the same current session/ordinal. For supported nonnegative native prices, each marginal contribution is trunc(before)-trunc(after); their sum must equal raw price minus actual price paid. These are ordered marginal savings, not each relic's independent standalone savings. The randomized raw price remains buyer-authoritative, as in the existing native shop trust boundary.

OneOff merchant removal requires its own native action ordinal, transmitted cost, actual deck removal and committed CardShopRemovalsUsed increase; cancellation must not produce a measurement. Other native GoldLost events, zero-cost events, identical-price successive purchases, reordered datasets, mismatch, duplicate/conflicting data, host change and rollback require targeted K6 counterexamples before acceptance. Unsupported or ambiguous windows produce no invented savings value.

An additional native source hazard is MerchantRelicEntry rereading Cost after RelicCmd.Obtain for SyncLocalGoldLost and its returned gold amount. Buying MembershipCard can therefore change that reread relative to the earlier actual debit. Such a mismatch must fail measurement correlation. This source finding is separate from A Thousand Cuts, whose removal path captures the transmitted cost before awaiting selection; no general shop gameplay fix is included here.

## Relic Remover

The first selection is offered from native `AfterObtained`. No earlier card, gold, maximum HP or potion-capacity grant is reversed. Start and Ancient relics are selectable; the native trader's `IsTradable` filter is not used as a hidden restriction.

- Cancel or an empty selection removes nothing and does not consume the relic.
- The unused relic can be reopened explicitly outside combat.
- An aborted attempt does not consume a limited retry counter or create an unusable state.
- Exactly one successful nonempty removal selection consumes the relic.
- All indices and selected instances are validated before the first removal.
- Removal uses `RelicCmd.Remove`, including native inventory events, history and `AfterRemoved`.
- Selecting the remover itself is allowed; it is removed last.
- Native removal callbacks are not a universal rollback transaction. A gameplay exception is not silently swallowed or repaired by inventing compensating pickup effects.

### Existing network extension

Reopening uses the existing BaseLib `ICustomTargetedMessage` and `CustomTargetedMessageWrapper.Send`. There is no additional runtime dependency, transport, generic action registry or repurposed native message.

The native `INetGameService.SendMessage` contract sends host messages to all clients. A client sends to the host; `ShouldBroadcast=true` relays the request to the other clients. The BaseLib wrapper forwards this flag. The local sender executes its request separately, following the native one-off synchronization pattern.

The request carries owner ID, persisted acquisition ordinal, monotonically increasing selection sequence and native run location. The receiver validates sender/owner, current location, unspent ownership, sequence and absence of another open selection. The owner alone displays the local UI; selection results use the native `PlayerChoiceSynchronizer`. Run-object identity, room location and availability are checked again before commit.

This source-level contract and technical packet tests do not establish real peer synchronization, reconnect behavior or live UI usability.

## A Thousand Cuts

The user's binding correction on 2026-10-25 supersedes the previous escalating-price requirement. While a player actively owns A Thousand Cuts, every merchant removal uses the native first-removal base price (effective pricing index zero), regardless of earlier removals, earlier shops or acquisition time. Current native discounts, Inflation and removal restrictions still apply. There is no price increase from subsequent cuts. Earlier patches and PASS results for escalating prices implemented and checked the wrong requirement; they do not accept this corrected behavior. Historical reports remain intact.

The corrected source in `TreasureAndShop.cs` uses a narrow CalcCost postfix to read the existing native BaseCost getter (75 normally, 100 with Inflation), then leaves MerchantEntry.Cost's normal modifier hooks and integer conversion intact. Every removal Cost read recalculates, including after relic loss, so no cached ATC discount survives. The native SetUsed value is no longer suppressed; only the Used getter reports an active owner's slot as available when native restrictions permit. This preserves the actual used-shop state for relic removal/melting and supports acquisition after an earlier purchase. No saved counter, run history, packet format, selection or purchase command is replaced.

Corrected acceptance: shown price = transmitted price = actual Gold debit on each successful purchase; normal no-discount sequence 75,75,75 (Inflation 100,100,100); native Membership/Courier and combined discounts retain their current result. Successful CardShopRemovalsUsed still increments once per actual removal and persists through save/load. Late acquisition at a nonzero counter immediately uses the native first price; a new shop retains that rule; another owner without ATC keeps native pricing/one-use behavior. Losing/melting ATC restores native counter-based pricing on the same entry and the true shop-used state. Cancellation, insufficient Gold and repeated activation during an open native selection must not produce a debit, successful count or duplicate removal. K6 exclusively owns corrected technical/live verification, build and local installation; source implementation alone is not acceptance.

The earlier passive 0.9.3 snapshot found the build/package/staging/downloaded Workshop DLLs equal (SHA-256 `94A3456C0F7E4F499693EA63FCF1EFC2477D036433EED00A4E7E839F93457474`); the manual active directory was absent and the parked manual copy was 0.9.1. Existing game logs identified the Workshop 0.9.3 load and repeated native removals, but did not record prices/counter values. These are historical passive findings before the current source candidates, not a claim that a 0.9.4 candidate was installed or live-tested.

Native pricing is base 75 plus 25 per saved removal, or inflation base 100 plus 50; MembershipCard and Courier act through native merchant-price modifiers and the final integer truncation. Native DoMerchantCardRemoval receives the captured transmitted gold cost, waits for the owning player's native choice, loses gold, removes the chosen deck card and increments CardShopRemovalsUsed only on completion. Cancellation does not increment. The counter belongs to ExtraPlayerFields and its native JSON/packet serialization. No observed source assignment resets it in that path.

K6's historical `atc-baseline-02.log` covers 48 sequential native-wrapper cases under the now-rejected escalating-price contract, with owner separation, native merchant-room discounts/inflation, cancellation, gold limits and saved-counter/remote-transmitted-price cases; its selection/UI and transport substitutions remain explicit. Retain it as historical evidence, never as a PASS for the corrected constant-first-price contract. The corrected candidate requires its own price/Gold/counter/location/player/hash evidence and real client cases.
