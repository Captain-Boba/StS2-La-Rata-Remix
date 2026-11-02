# Four fresh artwork replacements

The user requested completely new Static Tribute, Gilded Coffin, Bloodsmith and Amber Hourglass artwork. Subsequent direction asks for occasional classic black/green cards, rats only where the subject benefits, and explicitly excludes Amber Hourglass from the black/green palette. These instructions supersede the initial four motif/palette selections.

Separate built-in ImageGen calls generated all four from scratch without reference-image editing. The 3 cards use matte black and emerald light: Static Tribute offers a lightning-conducting crown beneath awakened orbs, Gilded Coffin is a heavy sealed gold/black shield-coffin, and Bloodsmith alone depicts a rat smith in side profile with red-hot metal and a green forge. Amber Hourglass uses a new squat lavender-glass silhouette, copper collar and suspended honey-amber liquid, without green.

`prompts.json` records the exact requests; `manifest.json` records generated original paths, unchanged runtime copies, hashes, dimensions, alpha, bindings and previous-runtime archives. The prior four PNGs and initial manifest/checks/handoff remain under `artifacts/content-expansion-20261102/k5/redesign/before`. The main expansion selection, prompt set, previews and handoff now point to these newer originals. Prior records are historical, not acceptance of the revised pixels.

`checks.json` verifies exactly 4 changed runtime PNGs, identical generated-source/runtime copies and matching prior archives; all 308 other pre-existing image/import files are unchanged. Existing accepted card-style files also pass the original expansion audit. Rustwater, Royal Shuffle, the other 4 expansion images and their bindings remain unchanged. Cards are 1448 x 1086 opaque PNGs; Amber Hourglass is 1254 x 1254 RGBA with genuine alpha, empty corners and a readable silhouette at 48/32px. `preview.png` is an offline contact sheet, not an in-game capture.

K5 visually inspected the originals and preview. Human artwork approval, native import/resource loading/cropping/outline/sampling remain pending. K6 owns renewed import/export after this freeze. No code, localization, mechanics, style shader, import metadata, build, game start, installation or Git/release action is part of this revision.

Amber Hourglass retains isolated generated alpha values below 8 at canvas edges; these are recorded in the manifest rather than removed from the original. Its visible silhouette at alpha 8 and above has clear margins. K6 should verify native outline/sampling for the new source.
