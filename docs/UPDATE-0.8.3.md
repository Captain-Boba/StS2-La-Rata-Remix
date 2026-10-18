# La Rata Remix 0.8.3

Requires Slay the Spire 2 v0.107.1 and BaseLib 3.4.7. All co-op players must update together. Start a new run: reinforcement selection has changed, so regenerating an encounter from an older save can produce a different roster.

## Changes

- Keep offscreen intervention cards visible for 3.35 seconds instead of 1.35 seconds, including Instant mode. The boss's own card timing is unchanged.
- Replace the expandable map-effects panel with the native-font La Rata Remix title at the bottom right. Remove the background, toggle and development details.
- Remove the current-act filter that left Corpse Slug as the only eligible reinforcement in Underdocks.
- Offer Inklet, Leaf Slime, Twig Slime and Corpse Slug in act one; add Nibbit, Flyconid and Fuzzy Wurm Crawler from act two.
- Prefer types absent from the current formation and previous combat, using stable ordering, a seeded RNG and native saved history. No new persisted shuffle bag or network messages are required.
- Preserve reinforcement budgets, formation caps, scene exclusions and the special rat formation.
- Add the repository's local/Workshop switcher with a double-click menu, preserved local builds and safeguards against running-game changes or overwritten parked copies.
- Use square emerald throne-room Workshop artwork and retain La Rata Remix as the upload title.

## Validation scope

The gameplay changes passed compilation and the existing content/policy checks before publication preparation. Reinforcement checks cover four early/seven later native model state machines, duplicate/recent-type avoidance, fallback behavior and 128 seeded selections replayed on four simulated peers. The wider policy suite covers encounter budgets and formations, royal decks and cameo scheduling.

These are automated model/policy checks, not a new in-game visual review or a live co-op session. Prior 0.8.2 game-test results remain historical evidence for that version; they do not establish full 0.8.3 compatibility or balance. The source switcher was exercised in an isolated directory for round-trip file preservation, repeated switching, preview mode, missing Workshop files and collision rejection.

## Distribution

Version 0.8.3 was published on 2026-10-18 to the existing [Workshop item 3822192203](https://steamcommunity.com/sharedfiles/filedetails/?id=3822192203). The official uploader confirmed success; the unauthenticated public page returned HTTP 200, the title La Rata Remix and version 0.8.3. Steam had not yet refreshed the local subscription at the immediate post-upload check. The GitHub manual release remains 0.8.2 until a separate release is published. The repository's build script produces the local LaRata-0.8.3.zip package; it does not upload it to GitHub.
