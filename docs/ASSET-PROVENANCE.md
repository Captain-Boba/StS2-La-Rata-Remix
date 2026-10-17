# Asset provenance

The title artwork `La Rata Remix - Titelbild - V1.png` was supplied by the project owner. It is included unchanged as the branding reference.

The runtime PNGs were generated with OpenAI image generation on 2026-10-16 for this prototype:

- `assets/LaRata/images/well.png`: an isolated ancient stone well, wooden crossbar, brass bucket and emerald water, painted for a small relic icon, transparent background.
- `assets/LaRata/images/rat_king.png`: a full rat king on his throne, black robes, gold crown and jewelry, green eyes and staff, matching the supplied branding reference, transparent background.
- Map portrait and outline files reuse the generated rat portrait to satisfy the game's placeholder texture contract. They are not separately drawn outline artwork. The duplicate outline node is hidden in the map UI to avoid an offset double portrait.

The 0.4.0 additions were generated with OpenAI image generation on 2026-10-17:

- `assets/LaRata/images/rat_king_standing.png`: a 1536 × 1024 transparent 3 × 2 sprite sheet, six 512 × 512 cells. Generation brief: preserve the supplied gray rat king, black robe, gold crown, emerald staff and green eyes; standing full-body poses for neutral idle, alternate idle, attack, cast, hit and kneeling defeat; no throne or text. A second edit removed the background while retaining the six poses. The final file was checked for an alpha channel and transparent corner pixels.
- `assets/LaRata/images/a_thousand_cuts.png`: transparent relic icon; ornate gold and emerald scissors cutting cards, readable as a small game relic.

The Godot scene uses frame tracks plus breathing, lunge, casting tint, hit flash and death fade. This is a six-pose prototype sprite presentation, not a full skeletal animation set. The old throne image remains the branding/card-art reference.

Local generator outputs for this update are retained under the task's generated_images directory: `exec-079b5674-e415-4141-9bba-04741cb934c8.png` (final standing sheet) and `exec-9b63ba37-529f-420c-9422-53be2d7870d3.png` (scissors). Source PNGs and Godot import metadata are included in the repository; raw generation history is local.

Original generator outputs are retained locally by the project owner; they are not part of the public repository.

## 0.5.0 artwork and presentation

Generated/edited through the built-in OpenAI image-generation tool on 2026-10-17, with transparent backgrounds requested explicitly. No extracted game texture was uploaded or redistributed. The two icon edits used the previous mod-owned generated icons as references.

- `assets/LaRata/images/well.png`: replaced by `exec-803d0373-f514-45ad-b80a-55c8435a1ec6.png`. Brief: a chunky hand-painted stone well, wooden beam and bucket, bold dark-brown contours, muted stone, two-step shading and moss-green water; simplify cracks, moss and rope; readable at 48 pixels; transparent padding, no scenery, particles, text, frame or photorealism.
- `assets/LaRata/images/a_thousand_cuts.png`: replaced by `exec-01964440-25f0-4d43-938c-45fc91d4d457.png`. Brief: matching hand-painted relic style, one cream card with a red slash, bronze/gold-loop scissors and emerald pivot; remove filigree, card piles and tiny debris; strong silhouette, transparent background.
- `assets/LaRata/images/potion_goblin.png`: new `exec-0f77ccc7-7a66-4dba-8e4b-0c00a8db45b0.png`. Brief: small mischievous olive-green goblin facing left in three-quarter view, huge ears, grin, orange tuft, plum/brown patched tunic, potion belt, raised throwing arm; full body, bold outline and muted cel shading, transparent background, no text, scenery or duplicate poses.

These outputs are retained locally under `C:/Users/lukas/.codex/generated_images/01a13c8e-acbb-7313-8026-b9a0bda4c75b/`. The source PNGs are included at the runtime paths above. Their generated image previews were inspected; appearance inside the game remains unverified while UI testing is paused.

Court-rat color modulation and particles are native Godot presentation applied only to the La Rata encounter. Native rat textures and skeletons remain in the installed game. Royal card frames/banners use BaseLib's established HSV material hooks. No new card-frame bitmap or third-party illustration is bundled. Idle motion uses interpolated transforms without frequent frame switching; it is not full skeletal animation.

## 0.5.1 Cleanse portrait

`assets/LaRata/images/cleanse.png` is the selected replacement, generated on 2026-10-17 using the built-in OpenAI image-generation tool. Source output: `exec-001fd7b6-330b-419a-a863-0af602c27e7b.png` in the same local generation directory above. Opaque 4:3 portrait; visually inspected as a standalone image, not yet inside the game.

Final generation brief: an ancient shallow black basalt fountain on a squat pedestal in a ruined sanctuary; a hovering luminous emerald droplet fills the bowl, whose outward streams dissolve black thorny curse sigils into harmless mist. No hands, figures, creatures or chains. Centered readable composition with margins for a rounded card crop, chunky hand-painted cartoon shapes and bold contours, near-black stone, deep teal/forest emerald and sparse pale mint highlights. No text, border, card template, UI or transparency.

The first hands/chains/party concept (`exec-678facee-c4a3-4a9f-8159-dbc938a234fa.png`) was rejected by the owner and is not included in the mod. Code-defined frame/banner colors remain shared with La Rata's cards.

## 0.5.2 goblin redesign

`assets/LaRata/images/potion_goblin_poses.png` was generated with the built-in OpenAI image-generation tool on 2026-10-17, using the previous mod-owned goblin as a reference and requesting a transparent background. Source output: `exec-1d6139ff-b7d3-4a78-8922-7611715e48dc.png` in the local generation directory above. The 1254-by-1254 RGBA image was inspected as a standalone sheet.

Final generation brief: a transparent two-by-two sheet of the same left-facing goblin at equal scale; short, squat proportions, large head and ears, olive skin, orange tuft, patched plum tunic and two belt bottles; bold brown contours and simplified two-to-three-tone hand-painted shading. Top-left idle crouch; top-right empty hand raised for wind-up; bottom-left follow-through with throwing arm extended left; bottom-right laughing with one hand on the belly. Full bodies, no held potion, projectile, scenery, text or grid. The game renders the actual selected potion separately.

Native Godot AtlasTexture regions preserve the throwing foot that crosses the nominal midpoint; measured pose offsets register the feet without editing the generated pixels. Native tweens add lean, recoil, laughter and entrance/exit movement. This is four-pose sprite animation, not a skeletal rig. The in-game appearance and timing still require owner review while assistant-driven UI tests are paused.

The repository's MIT license covers original code, scripts, scene definitions and documentation. Raster artwork and branding are excluded from that license; no general reuse license is granted for those files.

Native rats, encounter backgrounds, intent symbols and sound effects are referenced from the installed game. They are not copied into the distributed PCK. The archive does not contain game assemblies or decompiled source.

BaseLib 3.4.7 and the official Godot editor are MIT-licensed external tools/libraries. BaseLib is required separately at runtime and is not bundled in this mod archive. Godot is a build-time tool. See README for source links and dependency risk/effort notes.
