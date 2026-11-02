# Eight original artworks: content expansion, 2026-11-02

Latest selection: the [four-image redesign](redesign/README.md) supersedes the initial Static Tribute, Gilded Coffin, Bloodsmith and Amber Hourglass motifs/palettes described below. Current manifest, prompts and previews include the newer originals; prior selections remain archived. The revised cards use black/green, only Bloodsmith depicts a rat, and Amber Hourglass explicitly uses lavender glass and honey amber instead.

K5 supplies only the 5 new card illustrations, 3 potion icons and their provenance/previews. K2 owns card models/powers, bindings and cards.json; K3 owns potion models, bindings and potions.json. No C# file, localization, card style, existing artwork or release version is changed by this art delivery. K6 owns the engine import/export after the asset freeze.

| New original | Runtime target |
| --- | --- |
| Static Tribute | `assets/LaRata/images/cards/static_tribute.png` |
| Gilded Coffin | `assets/LaRata/images/cards/gilded_coffin.png` |
| Bonfire Sale | `assets/LaRata/images/cards/bonfire_sale.png` |
| Bloodsmith | `assets/LaRata/images/cards/bloodsmith.png` |
| Under the Table | `assets/LaRata/images/cards/under_the_table.png` |
| Brass Polish | `assets/LaRata/images/potions/brass_polish.png` |
| Grave Distillate | `assets/LaRata/images/potions/grave_distillate.png` |
| Amber Hourglass | `assets/LaRata/images/potions/amber_hourglass.png` |

Every original is produced with a separate built-in ImageGen call and copied unchanged from its generated source to the agreed workspace path. `prompts.json` records the exact prompt set. `manifest.json` records selected source paths, runtime SHA-256 identities, actual dimensions/modes/alpha and source binding occurrences. No sheet cutouts, title lettering, UI or complete card-frame graphic is embedded in the artwork. Author visual review is not human acceptance.

The card palette is deliberately varied: Static Tribute uses copper and electric blue; Gilded Coffin gold/violet and clear geometric shield plates; Bonfire Sale warm copper/orange energy; Bloodsmith molten red and steel blue; Under the Table burgundy/cream. Rat poses use simple readable arm/hand chains. The 3 potion silhouettes are simplified for native inventory sizes: angular brass flask/red polish/ochre cloth, intertwined green/violet glass chambers, and turquoise hourglass flask with suspended amber drops. No opaque white lacquer or milky liquid is requested; restrained painted glass highlights remain ordinary highlights.

The existing RoyalPotion getters bind the agreed potion artkeys to Packed, Outline and Large image paths. New cards inherit the existing ComboCard portrait-path contract. Source binding checks identify the new keys in K2/K3-owned implementation files; they do not exercise native texture loading or potion/card effects. No importer metadata is hand-forged: K6 creates the actual engine imports after freeze.

`verify-and-preview.py` checks 5 opaque 4:3 card canvases, 3 square potion images with genuine alpha/visible silhouette margins, exact source/runtime identity, source artkey occurrences, preserved prior image/import hashes, and unchanged accepted normal-frame/Royal-Prize-banner source files. It creates `cards.png` and `potions.png` under `artifacts/content-expansion-20261102/k5`; the potion sheet includes 48/32px samples on dark and light surfaces. These are offline previews, not game captures.

Final audit: all 8 source/runtime identities and source bindings found; all 296 pre-existing image/import files and both accepted style-binding files unchanged. Cards are 1448 x 1086; potions are 1254 x 1254 RGBA. All potion corners are alpha 0, with alpha ranging from 0 to 255. Brass Polish and Amber Hourglass contain isolated generated edge specks below alpha 8; visible silhouettes at alpha 8 and above have clear margins. Exact originals remain unmodified. Native outline/sampling remains for K6. `handoff.json` records the completed art freeze.

Rustwater is explicitly protected, including its existing source/import files. All other prior images and the accepted normal La-Rata frame and 10-prize boss-banner binding remain unchanged. Version stays 0.9.6. Prior approvals are preserved; all 8 new originals remain pending human review.

Remaining acceptance belongs to the coordinated K6 cycle: actual import/resource loading, native card crops and small potion visibility/outline/alpha on both bright and dark game scenes. Model behavior, balance, Save/Load/network and human visual approval are not established by this art audit. K5 performs no engine export/import, game start, installation, build, Git or release action for this image-only delivery.
