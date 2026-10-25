# Approved PNG identity and Godot alpha-border processing

The approved generated original, frozen draft and runtime **PNG files remain SHA-256 identical** for each of the six relics. This does **not** mean that every decoded RGBA byte in the imported CTEX is identical to the PNG. The import is a separate normal processing step, not a new artwork revision.

The existing importer uses `compress/mode=0`, `process/fix_alpha_border=true`, `process/premult_alpha=false` and no mipmap generation. The enabled alpha-border option is a documented Godot default that extends neighboring colors into nearly transparent borders to reduce filtering outlines: [ResourceImporterTexture documentation](https://docs.godotengine.org/en/4.5/classes/class_resourceimportertexture.html#class-resourceimportertexture-property-process-fix-alpha-border).

The official [Godot 4.5.1 texture importer](https://github.com/godotengine/godot/blob/4.5.1-stable/editor/import/resource_importer_texture.cpp#L793-L800) calls `Image.fix_alpha_edges()`. Its [RGBA8 implementation](https://github.com/godotengine/godot/blob/4.5.1-stable/core/io/image.cpp#L3678-L3739) skips source alpha values of 20 or more. Below that threshold it searches a four-pixel neighborhood for the nearest eligible source color and changes only RGB, preserving alpha. These fixed-tag sources match the Godot 4.5.1 version recorded in K6's existing export log. No import option was changed for this audit.

K5 decoded the existing GST2 v1 single-mip lossless WebP payload using the already installed Pillow decoder, then compared all six PNG/imported pixel arrays. This is a read-only artifact audit requested by K6; no engine, import, build, game or installation was invoked. The [native compressed texture loader](https://github.com/godotengine/godot/blob/4.5.1-stable/scene/resources/compressed_texture.cpp) documents the embedded WebP container. No additional decoder or runtime dependency was introduced.

| Relic | Changed RGB pixels at alpha 0 | At alpha 1–19 | At alpha 20–255 | Changed alpha pixels |
| --- | ---: | ---: | ---: | ---: |
| The Relic Remover | 12949 | 6332 | 0 | 0 |
| Sootbound Ticket | 11631 | 5880 | 0 | 0 |
| Stairwell Key | 12839 | 7002 | 0 | 0 |
| Mocking Bell | 13273 | 7332 | 0 | 0 |
| Brass Scavenger | 14493 | 6981 | 0 | 0 |
| Moonlit Stopper | 15150 | 7468 | 0 | 0 |

All six are 1254 x 1254. Original PNG hashes still match the approved selection. Each local CTEX is byte-identical to its corresponding CTEX extracted from K6's prepared PCK. All observed RGB changes are confined to source alpha 0–19; every alpha byte and all RGB values at alpha 20–255 are unchanged.

Detailed CTEX/PNG/packed hashes, exact import parameters and RGB-change counts per alpha value are recorded in `artifacts/relic-redesign-20261025/k5/alpha-border-audit.json`. K6's independent native Godot loader plus `Image.fix_alpha_edges()` comparison is recorded in `artifacts/rarity-art-pending-20261025/native-import-audit.json`: all six `nativeImportedPixelsEqualExpected` results are true. The K5 report records the hash of that K6 result. The actual six-image differences and native expected-result evidence therefore confirm ordinary alpha-border preprocessing rather than an unexplained artwork change.

Acceptance remains separated: human approval covers the exact selected PNG images; technical evidence covers the observed native import result. Neither establishes live UI appearance, filtering quality on all backgrounds, installation, or gameplay acceptance. No claim of full raw PNG-to-CTEX RGBA equality is made. The approved PNGs, import flags and active selection/approval manifests remain unchanged by this audit.
