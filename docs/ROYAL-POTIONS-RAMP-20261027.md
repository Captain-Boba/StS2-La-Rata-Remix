# Royal potions and damage ramp — 2026-10-27

Implemented and locally installed at the user's request. No game launches, live tests, co-op trading, version bump, commit or publication. Existing relic-statistics disablement and unrelated working-tree changes retained.

## Content

- **Ashbrand / Aschebrand**, Uncommon, self: Exhaust a chosen non-X hand card and gain its base Energy cost before temporary modifiers, including upgrade cost changes. Zero-cost cards give zero Energy. Energy-X and star-X cards are excluded. Energy is granted only after a native Exhaust history event confirms the effect.
- **Rustwater / Rostwasser**, Uncommon, enemy: remove all Block with the native block-loss command. No added Vulnerable or direct damage.
- **Crown Lacquer / Kronenlack**, Rare, self: remove the chosen hand card's local Exhaust keyword for this combat. Native combat-instance mutation preserves the deck original, upgrade and other keywords. Effects that independently force exhaustion, such as Corruption, retain their normal behavior.
- Shared native potion pool registration through BaseLib; combat-only use, native selection, native potion save identities and English/German/simplified-Chinese JSON. No custom multiplayer protocol or saved state.
- New original potion PNGs, unchanged from generated sources. [Full prompts, paths and SHA-256 provenance](art-prompts/royal-potions-20261027.json). Native 60px potion TextureRects scale the sources; the existing black-modulated Outline uses the same alpha silhouette. Artwork has assistant review, not human in-game acceptance.
- La Rata and his accompanying guards deal **40/50/60/70/80/90/100%** powered attack damage in rounds 1–7, remaining at 100% afterward. Native monster multiplicative hooks run after additive bonuses and before damage caps. Royal forecasts use the same hooks. The royal hand tooltip explains the current percentage in all three languages.
- Scope is the `RatKingEncounter`. Standalone guards, other enemies, player attacks and unpowered/indirect damage are unaffected. No persisted ramp counter; native combat round number is authoritative.

## Technical evidence

Evidence directory: `artifacts/royal-potions-ramp-20261027/`.

| Check | Result |
| --- | --- |
| Debug product and ContentChecks build | PASS, zero errors. Two existing NU1900 warnings because the NuGet vulnerability feed is unavailable; no new packages. |
| `--royal-potions-ramp` | PASS, 120 assertions plus existing directed Artifact/royal-forecast checks at fully ramped damage. |
| Potion effect checks | Native hand selection via TestCardSelector; native Exhaust, Energy and Block commands; base/upgraded/discounted/zero/X/star-X cases; partner isolation; combat keyword lifetime; shared pool membership and native potion JSON roundtrip. Protected effect entry invoked directly; no potion UI/input or networking claim. |
| Damage checks | 45 native damage resolutions match intent forecasts across La Rata and all four possible guards, rounds 1–9, with Strength/Vigor. Weak/Vulnerable, native cap ordering, unpowered damage and unrelated enemies/encounters also checked. Console combat state; no game scene or live encounter test. |
| `--wave-localization` | PASS: three-language table/key/placeholder parity and 2031 native formatter calls, including new potion entries and ramp text. Updated old tooltip assertions to reflect disabled statistics. |
| `--metrics-disabled` | PASS: 94 checks, 42 inactive statistics patch classes, native poison plus 36 previews, legacy ledger preservation and working remover. |
| `--patches-only` | PASS: normal patch activation and 17 existing ENet lifecycle checks. No socket/game session. |
| Godot resource export | PASS, clean final export log. Initial sandbox export had temporary/settings path errors despite exit 0 and was rejected; rerun with regular temporary-directory access succeeded. |
| Packed resources | PASS: native Godot ResourceLoader loaded all three 1254×1254 transparent textures directly from the PCK; all 27 packed locale files match workspace bytes. Standalone resource validator only. Initial sandbox log-path failure resolved by an explicit workspace log file. |
| Generated image provenance | PASS: all three runtime PNG hashes equal their source hashes. Standard Godot alpha-edge import processing remains enabled. |
| Game/UI/live multiplayer/balance | NOT TESTED, per user instruction. |

## Local installation

Installed using the existing installer into `E:/SteamLibrary/steamapps/common/Slay the Spire 2/mods/LaRata`. Game was closed. Previous installed runtime files backed up in `installed-before/`. Canonical package temporarily staged and restored byte-for-byte from `canonical-before/`; tested candidate remains in `candidate/`.

| File | Installed SHA-256 |
| --- | --- |
| LaRata.dll | `7359D6529E37EF50622ECB831C0C0BF323BB71ED8392FCAA0BF958C3A0274426` |
| LaRata.pck | `29AAC1B9A49C7EA55DE43EB07E351753369A569228875120DF1C46223ADEF57D` |
| LaRata.json | `1A2834DB1A10732A1B64E880E7D7077F4BE83BA81B1667AE49983996DF13455D` |

Installed hashes equal the tested candidate. Local manifest remains 0.9.4; published copies are unchanged. Co-op requires all peers to use this same build, not merely the same version label. No saves edited.

### Crown Lacquer color correction

User requested replacing the white lacquer while retaining the otherwise liked design. Built-in ImageGen edited the drip to amber-gold; [prompt and source](art-prompts/crown-lacquer-gold-20261027.json). Original PNG retained in `artifacts/crown-lacquer-gold-20261027/crown_lacquer-before.png`. New PNG copied unchanged into the existing runtime path.

Re-export and standalone packed-resource checks passed (three textures, 27 identical locale tables). Locally installed corrected PCK SHA-256: `2F1317DFDB8B7540AF1623E76B0C14630797C0C0B38A8F4D0AD159E156D0A494`; DLL and manifest hashes unchanged. Backups, logs and installed hash record are in `artifacts/crown-lacquer-gold-20261027/`. No game starts or mechanics changes.
