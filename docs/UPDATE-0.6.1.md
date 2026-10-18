# La Rata Remix 0.6.1 — starting Bowler Hat

2026-10-17. Replace the global +25% gold patch with the original Bowler Hat relic for every player at the start of a new run.

- Await the native `RunManager.FinalizeStartingRelics` routine, then award `ModelDb.Relic<BowlerHat>` through `RelicCmd.Obtain` in stable NetId order. Existing owners are skipped.
- Use the original relic's native +25% gold effect, icon, tooltip, ownership and callbacks. Delete the separate `Hook.ModifyGoldGained` patch entirely, so La Rata no longer stacks a second multiplier onto the hat.
- Native `RelicCmd.Obtain` manages inventory, relic history, non-stackable grab-bag removal and `AfterObtained`. Native relic serialization handles saves and multiplayer state. No custom save flag, random choice or network message is added.
- The inspected game v0.107.1 calls this finalization routine on new-run paths (normal NGame startup and the two bootstrap multiplayer paths). `NGame.LoadRun` does not call it. Loading an existing save therefore neither adds a hat nor replaces a lost hat. No old-save migration is performed; use a new run with matching mod versions on every peer.
- The map panel now says `Starting relic: Bowler Hat`. The two extra starting potion slots remain unchanged.

Source review covered the installed game's BowlerHat, RelicCmd, RunManager and new-run/load call sites. Interactive tests remain paused. New solo/co-op starts, visible relic inventory, save/reload and actual gold gains still require a game test; a successful build alone does not validate those behaviors.

GitHub release v0.6.0 is unchanged. This update does not authorize another commit, push or public release.

Release compilation and official Godot resource export succeeded with zero warnings/errors (`artifacts/update-0.6.1/build.log`). The package is `artifacts/LaRata-0.6.1.zip`. The first installation attempt detected a running game and stopped before copying any files; `installation-not-applied.json` preserves that earlier result. After the owner authorized installation and the game was closed, installation succeeded. `artifacts/update-0.6.1/install.log` and `release-verification.json` record matching installed DLL/manifest/PCK hashes. No game was closed or controlled. No commit, push or release was made.
