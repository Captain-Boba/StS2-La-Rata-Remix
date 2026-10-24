# Local follow-up: Thorns and A Thousand Cuts

Unreleased source changes after 0.9.2. No installation, package rebuild, version bump, commit, push or Workshop upload is included.

## Changes

- Bristling and Toxic Shell have 25% of the normal selection weight (1 versus 4). Both grant native Thorns; other retaliation families keep their weight. The existing native `Rng.WeightedNextItem` handles selection with the encounter's deterministic RNG callback. Effect strength, budgets, eligibility constraints and reinforcement rules are unchanged.
- A Thousand Cuts recalculates the native removal base price whenever the price is read, before display, affordability and payment. Price recalculation no longer depends on querying `Used` or on a shop UI callback. The original synchronized removal flow remains responsible for incrementing the saved `CardShopRemovalsUsed` counter; native price modifiers still run afterward. No separate price counter or pricing formula is introduced.

## Validation

- Build/content checks pass, with the existing NU1900 vulnerability-feed availability warning. Tests use the native synchronized removal completion method with UI, gold presentation and deck-removal commands simulated. They cover repeated purchases, both inflation tiers, prior removal history, mid-shop relic acquisition, cancelled selection, affordability, removal restrictions and native extra-field serialization/reload.
- Policy checks pass 384,000 encounter cases, plus the existing deck and intervention suites. Exhaustively enumerating the weighted selector's input verifies one selection slot for each Thorns family versus four for every other family.
- In that encounter sample, Bristling occurs 22,129 times and Toxic Shell 18,717 times, versus 70,696 and 66,739 in the 0.9.2 sample. Reducing selection weight by 75% does not guarantee exactly 75% fewer final encounters: budgets, eligibility, sampling without replacement and alternative weights also affect final frequency.
- Logs: `artifacts/local-thorns-cuts-content.log` and `artifacts/local-thorns-cuts-policy.log`.
- No live shop UI, exact reported co-op scenario or network test was performed. Native-method regression checks do not establish live gameplay acceptance. The running game and installed Workshop package were left unchanged.
