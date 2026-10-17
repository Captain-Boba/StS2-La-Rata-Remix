# Asset provenance

The title artwork `La Rata Remix - Titelbild - V1.png` was supplied by the project owner. It is included unchanged as the branding reference.

The runtime PNGs were generated with OpenAI image generation on 2026-10-16 for this prototype:

- `assets/LaRata/images/well.png`: an isolated ancient stone well, wooden crossbar, brass bucket and emerald water, painted for a small relic icon, transparent background.
- `assets/LaRata/images/rat_king.png`: a full rat king on his throne, black robes, gold crown and jewelry, green eyes and staff, matching the supplied branding reference, transparent background.
- Map portrait and outline files reuse the generated rat portrait to satisfy the game's placeholder texture contract. They are not separately drawn outline artwork. The duplicate outline node is hidden in the map UI to avoid an offset double portrait.

The Godot scene supplies simple idle breathing, attack lunge, casting tint, hit flash and death fade animations. This is a prototype sprite presentation, not a full skeletal animation set.

Original generator outputs are retained locally by the project owner; they are not part of the public repository.

The repository's MIT license covers original code, scripts, scene definitions and documentation. Raster artwork and branding are excluded from that license; no general reuse license is granted for those files.

Native rats, encounter backgrounds, intent symbols and sound effects are referenced from the installed game. They are not copied into the distributed PCK. The archive does not contain game assemblies or decompiled source.

BaseLib 3.4.7 and the official Godot editor are MIT-licensed external tools/libraries. BaseLib is required separately at runtime and is not bundled in this mod archive. Godot is a build-time tool. See README for source links and dependency risk/effort notes.
