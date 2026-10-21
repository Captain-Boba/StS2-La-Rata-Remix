# 0.8.6 — Royal map and history layout fixes

Local iteration, 2026-10-20, for game v0.107.1 / BaseLib 3.4.7. No new runtime dependencies.

## Changes

- Replace the full-color throne portrait on the bonus map with a crowned-rat bust drawn in the pen-and-ink style of the native boss map icons. Supply a separate white silhouette for the native background tint. [Artwork, source and exact prompt](art-prompts/map-boss-0.8.6.md).
- Keep the original boss button and its native hover, selection, controller and co-op voting behavior. Center it on the path and move it above PREPARED with enough separation for the full native hitbox and hover artwork. The previous 374×306 boss hitbox overlapped the final campfire; attempting to shrink its child picture also conflicted with the scene's 374×350 minimum size.
- Fix the oversized royal-history icon by enabling `TextureRect.IgnoreSize` before assigning its large card texture and final 58×66 size. Previously the texture clamped the requested size to 1058×1487; changing expansion afterward did not shrink it. Both history counters retain their existing visibility, count labels and native viewers.
- Encounter plans, combat mechanics, saved map coordinates and map connections are unchanged. The layout is rebuilt when opening/loading the bonus map, including an existing save paused after the Kingsguard. No save migration is required; an already abandoned run is not restored.

## Verification

- Release compilation: zero errors and zero warnings.
- Existing native content/model checks pass. NuGet vulnerability-feed lookup remains unavailable (NU1900); no dependency was changed.
- Headless Godot 4.5.1 reproduces the old oversized icon and confirms 58×66 bounds after scene attachment and texture replacement.
- Native installed scene data confirms the boss hitbox and artwork dimensions. Geometry checks reproduce the old campfire overlap and verify the corrected path alignment and non-overlap at viewport scales corresponding to 1280, 1920, 2560 and 3840 widths. Native 1.05 hover scale leaves 73.25 logical pixels before the campfire hitbox.
- Both imported masks have transparent exterior pixels and pure white visible pixels. Original drawing alpha is preserved exactly and every ink pixel is covered by its background silhouette. The final drawing was compared with native icons using the native Glory map colors.
- Evidence: `artifacts/map-fix-0.8.6/` (layout checks, masks, import/build logs and asset preview). The initial inspection also attempted a nonexistent atlas directory and logged that diagnostic; subsequent focused checks do not use it.

No game was launched, no active playtest was performed, and no real save or multiplayer session was exercised. In-game visual/input acceptance is still pending. Workshop remains 0.8.3 and the public GitHub manual release remains 0.8.2; this change does not publish either.
