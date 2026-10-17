# La Rata Remix 0.5.0

2026-10-17. Source implementation and automated verification only. UI testing remains paused at the owner's request; no game window was started for this update.

## Changes

- Restyled Well and A Thousand Cuts into simpler hand-painted relic silhouettes, with transparent backgrounds, heavier outlines and fewer tiny details.
- Replaced frequent idle pose switches with an 18-second continuously interpolated breathing, weight-shift, subtle sway and green-light cycle. Attack/cast/hit transitions use a short blend. This remains sprite animation, not a new skeletal rig.
- Black royal card frames, dark emerald banners and green native energy icons, through BaseLib's supported card-material hooks. The persistent hand display and detailed plan tooltip are removed. Only the currently played card enters the native player play-pile animation.
- Skills and powers resolve before attacks. Blood Contract's planned Strength is included in native attack intents, then replaced by actual native Strength without double counting. A spell-only hand finishes with an announced 12-base-damage monster attack. Execution's Vulnerable remains deferred until all attacks finish.
- Five different combat relics per La Rata fight, selected deterministically from nine native identities. Their enemy adaptations apply Strength, Dexterity, starting/timed/conditional Block, Thorns, turn-start damage or first-hand Energy. Icons and a Royal Regalia power tooltip expose the effects. The selected bit mask is carried in a native power amount for combat checksums.
- The Act 3 finale's extra boss now has 100% native HP, including native multiplayer scaling. La Rata has 1,080 base HP, up from 540. His two rats have double their native multiplayer-scaled HP; other rat encounters are unaffected. The court rats keep native animations with a dark emerald tint and green particles.
- A wandering goblin schedules one visit per seeded three-combat block. It arrives at the transition after enemy turn 2 or 3, before the following player action phase. The native hook is after block clearing, keeping Block Potion useful. Fights that end earlier skip the visit; therefore actual visit frequency can be lower than one in three during fast runs.
- Each living co-op player receives a different native potion, selected in stable NetId order. Pool: Strength, Dexterity, Block, Regen, Liquid Bronze, Flex, Speed, Ghost in a Jar, Weak, Vulnerable, Poison and Fire. No slot is consumed. Damaging potions can kill; no hidden damage cap or guaranteed positive result is added.

## Integration choices

No new external dependency, save format or network transport. BaseLib 3.4.7 supplies custom models and frame/banner shaders; native Godot handles sprites, interpolation and particles. Native monster commands, powers, card-play animation and potion throw VFX remain authoritative.

Native relic models require a Player owner. Royal Regalia therefore implements nine explicitly described enemy adaptations through native commands rather than fabricating a Player or executing player relic hooks on a monster. Native potion OnUseWrapper consumes an inventory item and records a player drink. The goblin instead invokes the supported pool's existing protected OnUse implementations through a cached, v0.107.1-pinned Harmony adapter, retaining the native effect context while bypassing inventory consumption and Before/AfterPotionUsed hooks. There is no official NPC potion-splash entry point in the inspected installed API. Mods relying on player-drinking hooks intentionally do not treat this NPC attack as voluntary potion use.

Room history and run seed reconstruct goblin timing; separate native RNG streams select arrivals, bottles, relics and cards. Cosmetic particles do not affect decisions. Native saves restart combat, so selections replay from the checkpoint rather than rerolling. Cross-version active-run upgrades are unsupported: start a new run and use identical files on every peer.

## Technical evidence

`artifacts/update-0.5.0/policy-checks.log` records passing checks using the game's native RNG:

- 120,000 encounter policies: replay, budgets, caps and offense exclusions.
- 40,000 royal turns: draw/discard conservation, affordability, first-hand bonus energy and spell-before-attack ordering.
- 40,000 goblin blocks and 160,000 co-op assignments: exactly one scheduled combat per block, four distinct in-range potions and repeatable selections.
- 1,000 five-relic samples: unique selection, replay, five-bit checksum encoding and reachability of all nine options.

Release compilation passed with zero warnings and zero errors; official Godot 4.5.1 resource export completed successfully (`build.log`). The DLL, manifest and PCK were installed while the game was closed; all three SHA-256 hashes match the packaged files (`release-verification.json`). The original executable, base PCK and game DLL still match the previous baseline (`original-files-verification.json`). All three generated icons/sprites have 32-bit alpha and transparent corner pixels (`asset-checks.json`). These checks do not execute the new combat hooks or verify their displayed results.

## Runtime acceptance still required

1. Natural solo combat: goblin appears between rounds, once at most, never at battle entry; inspect short-fight skips, helpful/harmful effects, Block persistence and a lethal Fire splash.
2. Four identical clients: compare native checksums plus GOBLIN and ROYAL logs, different bottles per living player, dead-player handling, transition timing and checkpoint reload replay. Repeat with actual remote Steam peers.
3. La Rata: five correct relic icons/effects, Lantern first hand, Anchor opening defense, Horn Cleat/Wheel turns, Orichalcum, Mercury and Thorns; inspect modifiers and native combat interactions.
4. Compare announced damage before and after Blood Contract; exercise every card and no-attack hands. Inspect card contrast, animation lifetime, hidden hand, smooth idle, spirit particles and layouts in 1–4 player fights.
5. Verify 1,080 base boss HP, doubled court rats and full-HP earlier-boss reinforcement across party sizes.
6. Retain the outstanding actual bonus-route, Architect, save/load and optional-mod coverage from the 0.4.0 report. No earlier release's successful scenario validates this update.

No commit or push is part of this update. Game originals, profiles and launch options are outside the implementation changes.
