# Endgame and treasure integration, 0.3.0

Historical implementation notes. The current topology, enemy deck and validation status supersede the presentation details below; see [the current update report](UPDATE-0.5.0.md) and [design](DESIGN.md).

Implementation notes, 2026-10-16. The original research-only 0.2.1 study is retained in artifacts/night-20261016/docs. Runtime evidence is tracked separately in TEST-REPORT.md.

## Established interfaces

BaseLib 3.4.7 provides CustomActModel, CustomEventModel, CustomEncounterModel, CustomMonsterModel, CustomRelicModel, model registration, localization and AnimationPlayer conversion. Its SavedSpireField mechanism extends native run saves, canonicalization and multiplayer packets. There is no mod-owned save file or network transport. Godot 4.5.1 exports the resource pack through the official editor.

The existing Relic RPS Choice project was investigated, but no supported public Well extension was found. Its implementation was not copied or redistributed. La Rata uses native PlayerChoiceSynchronizer for interactive hands and retains native TreasureRoomRelicSynchronizer awards and consolation behavior. Do not combine two mods replacing chest RPS without a dedicated interoperability adapter.

## Version-pinned adapters

The mod rejects game versions other than v0.107.1. Harmony adapters are scoped to La Rata content wherever possible:

- PickRelicAction defers final chest resolution while all contenders choose through reserved native player-choice IDs. Stable player/relic order is identical on every peer. An authoritative vote snapshot is used, not local predicted votes. Native GenerateRelicFight receives the chosen result, and native relic award presentation performs ownership changes.
- Well uses an isolated native Rng domain based on seed, act, floor, slot and relic identity. It consumes no ordinary run-stream draws. Cosmetic delays do not select winners. Leaving the run cancels pending choice tasks.
- RunManager.EnterNextAct intercepts the completed regular finale before native WinRun can close it. The invitation is a registered native shared event. A narrowly scoped shared-event resolver sends the native selected-option message only after every vote; all yes accepts, otherwise decline.
- ActChangeSynchronizer handles readiness after the committed invitation. EnterRoom is not called from inside shared-event option callbacks, avoiding the native pending-option exit deadlock.
- On acceptance, one initialized RatDomain is appended using the version-pinned private Acts setter. Only that act's rooms are generated with an isolated native RNG; existing acts and their regular double-boss selection are retained.
- Phase and original-final-act index use versioned SavedSpireField values. SavedActMap stores the actual four-node topology. Loading a pending/committed invitation reconstructs the event, not the defeated regular boss. Loading phase 5 reconstructs the Architect.
- Missing integer fields in legacy BaseLib network save packets become zero. Both peers therefore use zero as the original-final-act sentinel; using a local -1 default caused a reproduced legacy-save boss-duo desynchronization and was corrected.
- Native MultiplayerScalingModel only accepts act indices 0..2. Inside RatDomain alone, the argument is mapped to index 2 and the original native method runs unchanged. This preserves native HP and Block scaling, including the boss factor. BaseLib 3.4.7 has no dedicated adapter for that table. Random normal combats in the bonus path are covered by the same adapter.
- OnEnded preserves the regular victory once the invitation has been reached; native history/progress closure still occurs only once. The bonus victory explicitly enters the Architect to avoid relying on other mods' replacement of the generic post-act-three transition.

## Presentation and combat

A mod-owned native ActMap contains three Unknown nodes, one Shop and a separate boss. Presentation labels attach to actual NMapPoint controls, ignore mouse input and rebuild after load. Point spacing and native scroll spacing are adjusted together. The original game resources are untouched.

The boss uses native MoveState, intents, DamageCmd, CreatureCmd and PowerCmd. Royal Decree and Exposed Crown have explicit custom intent descriptions. Exposed Crown previews Block through native Hook.ModifyBlock, including multiplayer and active power modifiers. Two native rats occupy named formation markers around the throne. A new transparent sprite uses simple Idle, Attack, Cast, Hit and Dead AnimationPlayer states. The AnimationPlayer lives under Visuals so native end-screen reparenting retains its track targets. The arena references the existing Queen background; no extracted base-game assets are shipped.

## Compatibility boundaries

All peers require the same mod files and dependency versions. Other patches targeting treasure resolution, act replacement, endings, potion initialization or reward gold can conflict. Limitless also replaces the normal post-act-three ending, so it requires a dedicated runtime check. UnifiedSavePath changes profile routing; La Rata itself does not copy, migrate, unify or unlock saves. Installed Workshop files and Steam launch options are left intact during isolated Steam-off tests.

Native disconnect handling is retained; no missing player is silently counted as consent and no timeout invents an RPS move. A pending contest restarts from the native room checkpoint after load. There is no arbitrary mid-round save. Modified-client cheating is outside the private UI choice model.

Full human Steam co-op, all regular finale variants, natural balance, controller accessibility and every third-party mod combination require separate acceptance evidence.

## Native UI update, 0.3.1

Treasure choices instantiate NGenericPopup/NVerticalPopup and duplicate the original NPopupYesNoButton scene, retaining the native art, MegaLabel fonts, hover/press animation and sounds. Three buttons use focused-control selection instead of the yes/no global shortcuts. Waiting and Well messages use the same native modal. No game textures or fonts are extracted into the release pack.

Each synchronized round calls native NHandImage.DoFightMove with the actual selected enum for every remaining contender, awaits those tweens, then calls native DoLoseShake for eliminated hands. Hands are frozen and hidden during private choices. Native cosmetic jitter does not choose a winner or consume a run RNG stream.

Completed contests retain their authoritative FoughtOver result and full round history. Only AnimateRelicAwards receives a presentation copy requesting its existing direct-grab branch, so neither a replay nor the enclosing native Fight banner runs a second time. Native relic ownership, consolation/fallback handling and completion remain in the original method. Other contests are passed through unchanged.

The benefits panel references the existing MapLegend texture and copies its header/body label styling. Bonus-map word labels use the same native heading style. The invitation already used native shared-event UI and remains unchanged.
