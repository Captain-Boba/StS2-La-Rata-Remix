# La Rata native-style map boss — 0.8.6

Date: 2026-10-20. Generated with the built-in image tool, then mechanically prepared as native tint masks with the owner's explicit permission. No game assets were edited or redistributed.

## Files and integration

- Source drawing: [map-rat-king-source.png](map-rat-king-source.png).
- Runtime drawing: `assets/LaRata/images/map_rat_king.png`.
- Runtime background silhouette: `assets/LaRata/images/map_rat_king_outline.png`.
- Reproduction: `python docs/art-prompts/prepare-map-icon.py` (existing local Pillow 12.3.0, offline art tooling only; no game/runtime dependency).
- The drawing's original alpha is preserved byte-for-byte; only RGB becomes white. The silhouette fills enclosed holes and expands by one source pixel, retaining the same canvas/registration.
- Both textures use the existing `RatKingEncounter.BossNodePath` and the original `NBossMapPoint` scene. Native map tint, hover, selection, controller focus and multiplayer voting remain responsible for interaction.
- Godot's standard [channel-remap importer](https://docs.godotengine.org/en/4.5/classes/class_resourceimportertexture.html) was also verified as a working alternative. Final PNGs already contain white masks and use ordinary import settings, so non-Godot tooling sees the same masks.

## Generation prompt

Reference 1 was a local comparison of the installed game's Kaiser Crab and Knowledge Demon map icons, with an earlier simplified rat sketch in the middle. Reference 2 was the mod-owned seated La Rata illustration. Native reference images remain in ignored local inspection artifacts.

```text
Create ONE production boss-map illustration for Slay the Spire 2. Image 1 shows style references: match the LEFT and RIGHT native boss drawings with thin irregular pen outlines, simple hatched details and large ink shadow shapes. Do NOT use the middle cartoon-logo style. Image 2 is the CHARACTER reference: La Rata, a sinister crowned rat king with long rat muzzle, round ears, ragged fur-trimmed royal cloak, spiked crown and diamond jewel. Draw his HEAD AND SHOULDERS only, a small bit of crownlike throne spikes behind him, scheming grin with incisors, shoulder cloak framing the bust, compact silhouette. Exactly like the native boss-map drawings in image 1, an expressive hand-inked storybook creature portrait, not an esports logo, not a flat pictogram, not photorealistic. Fine but readable pen strokes, restrained short hatching, broad dark negative shadow masses under cloak, transparent cutouts in face and crown. Single DARK BROWN INK color only, on real transparent alpha, no paper surface. No circle, no frame, no text, no color accents, no glow, no gradients, no stippling noise, no splatter, no background. Compact roughly square bust occupies central 84% of square canvas with clear margin all around. It will display at 300px tall as a native map boss symbol.
```

The earlier flat rat logo and failed white recolor generations are not shipped. The selected output is `exec-9bb9992a-1fc6-4081-8d7f-e319538747e2.png`; its unchanged source is retained above. The local native-color comparison is `artifacts/map-fix-0.8.6/map-icon-preview.png`. It is an asset preview, not an in-game screenshot.
