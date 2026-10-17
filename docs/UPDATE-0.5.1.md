# La Rata Remix 0.5.1 — Cleanse

2026-10-17. Adds one player card; all 0.5.0 features remain. UI tests are still paused.

- Cleanse: rare neutral Skill, 2 Energy, Exhaust. Upgrading reduces cost to 1.
- Registered through BaseLib's established `[Pool(typeof(ColorlessCardPool))]` mechanism, eligible wherever native rare colorless cards are offered. The boss's ten-card deck does not gain this card.
- Removes active Debuffs from all living players in stable NetId order, including the caster. Solo naturally targets only the caster. The native `TypeForCurrentAmount` classification includes negative Strength/Dexterity while retaining positive powers. Curse/Status cards, HP loss and potion inventory are unaffected.
- Temporary native stat-reduction powers are reduced to zero with `PowerCmd.ModifyAmount` first, letting their existing change hook restore the underlying stat. Remaining Debuffs use `PowerCmd.Remove`, retaining native removal callbacks. This prevents stale temporary reductions and avoids deleting restored positive Strength/Dexterity.
- Shared black/emerald frame and banner materials match the royal cards through BaseLib's supported per-card material hooks. The neutral gameplay pool remains the native colorless pool.
- New spectral basalt fountain artwork replaces the rejected hands/chains motif. No base-game card or texture is replaced. Native Cleanse is a separate Necrobinder card and is left intact; BaseLib prefixes this mod's model ID.

Release compilation passed with zero warnings and zero errors, and official Godot 4.5.1 exported the new portrait and existing resources successfully (`artifacts/update-0.5.1/build.log`). Installed DLL, manifest and PCK match the package SHA-256 hashes (`release-verification.json`). No new dependency, custom network message or save format. No game launch, UI test, profile edit, commit or push is part of this update.

Runtime acceptance remains pending: solo and four-player removals, dead players, negative and temporary stats, removal callbacks, upgrade cost, Exhaust, native colorless availability, save/load and card readability. Earlier policy simulations do not execute this new card.

## Owner-run error investigation

The owner subsequently tested 0.5.1 with other installed mods and reported unplayable cards. The captured game log is retained privately at `artifacts/update-0.5.1/user-run-errors.log`.

- The installed TheHeroExpansion 1.7.0 fails its initializer: its ConquerorPower patch requests a `cardPlay` parameter absent from the installed v0.107.1 method.
- Its `HeroBetaArt` static initializer also throws `ArgumentNullException` while constructing a Harmony method delegate. The first occurrence is during a card transform at Neow. Later occurrences interrupt native `CardPileCmd.Draw` / `CombatManager.SetupPlayerTurn`, explaining the aborted player-turn setup and unavailable card actions.
- The log reports the current branch as `public`, while the expansion supports `public-beta`. The author's Workshop description explicitly says it only works on the beta branch: https://steamcommunity.com/sharedfiles/filedetails/?id=3749294247 .
- No GOBLIN resolution entry or LaRata stack frame appears in this captured failure. This identifies the observed blocker; it does not validate the untested goblin event or rule out unrelated future issues.

Recommended isolation: disable The Hero Expansion, restart the application and use a new run with BaseLib + La Rata. A beta-branch switch would conflict with La Rata's explicit v0.107.1 gate. No third-party mod, profile or save was modified during diagnosis. Successful retest remains outstanding.

## 0.5.2 — goblin readability and animation

The owner subsequently observed the goblin in a run and reported that its appearance was too brief. This is limited observation of an appearance, not full gameplay or multiplayer acceptance.

- New transparent 2-by-2 sprite sheet: chunkier proportions, bold contours and simplified hand-painted shading, with idle, wind-up, throw and laughing poses. The old single-pose asset is retained as source history.
- One-second entrance, 1.2-second introduction pause, 0.65-second wind-up with the actual selected potion held visibly, 0.85-second native throw arc, one-second reaction and 0.8-second departure. Solo presentation takes about 5.55 seconds plus native effect resolution. Co-op repeats the per-player wind-up/throw/reaction.
- Native Godot tweens animate lean, recoil and a small laughing bounce. Native `TweenHelper.AwaitFinished(owner)` keeps the sprite alive until presentation finishes and cancels on scene exit. It replaces `Cmd.Wait` for this presentation, because the latter skips waits entirely in Instant mode while the Godot sprite tweens continue running.
- Native atlas regions keep the throwing foot intact across the nominal cell boundary. Per-pose offsets align planted feet; the held bottle and native projectile use the same 48-pixel size.
- Uses the native item-throw scene's exported flight-time property. No new animation runtime, gameplay RNG, potion selection, encounter schedule or network format.

Release compilation and official Godot resource export succeeded (`artifacts/update-0.5.2/build.log`): zero errors, two NU1900 warnings because NuGet vulnerability metadata could not be reached. No dependency was added or changed. The new RGBA source sheet was inspected and its pose bounds measured. New sprite alignment, in-game timing, Instant-mode presentation and room-exit behavior still require an interactive recheck; assistant-driven UI tests remain paused.

Installed DLL, manifest and resource pack match the packaged SHA-256 hashes (artifacts/update-0.5.2/release-verification.json). Installation changed only this mod's three runtime files; no game launch, profile edit, commit or push was performed.
