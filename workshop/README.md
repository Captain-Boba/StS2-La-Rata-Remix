# Steam Workshop publishing

La Rata's existing item is [3822192203](https://steamcommunity.com/sharedfiles/filedetails/?id=3822192203). Preserve `LaRata/mod_id.txt` when updating; removing it creates a different item.

Publishing uses Mega Crit's official [sts2-mod-uploader](https://github.com/megacrit/sts2-mod-uploader), release **v0.2.0**, Windows x64. It is a development-only publishing tool, not a new runtime dependency. It uploads through the signed-in Steam account; use only the official release and keep Steam credentials out of this repository. No explicit license was found for the uploader, so its executable is not redistributed here.

The downloaded `ModUploader-win-x64.zip` SHA-256 was `2B55C19CC5932235CA9DBD663CA07D04E5D5A0018402303199F9B4EC8CA06578`.

Release target: **0.9.5 BETA**. Description and change notes use EN, Simplified Chinese and German blocks, with language pointers at the top of the description. Before each authorized upload, stage and verify the three runtime files against the current [QA report](../docs/UPDATE-0.9.5.md). The focused local ENet gameplay and orderly disconnect checks passed; wider network coverage and engine shutdown diagnostics remain open. The matching GitHub release uses tag `v0.9.5` and asset `LaRata-0.9.5.zip`.

## Files

The owner supplied the new English description for 0.9.4. Preserve its wording; remove only pasted formatting artifacts. Follow it with Simplified Chinese and German translations (EN -> CN -> DE). Use ordinary Steam BBCode headings and separators, not unsupported collapsible tags.

Patch notes use three complete language blocks in the same order. Within each block use New Content, Balancing, Bug Fixes, and Compatibility / Known Issues. Keep them concise; do not single out individual new cards. Mirror both text files into the JSON fields read by the uploader.

In Workshop descriptions and similar public-facing copy, write all numbers greater than 3 as numerals, including compound terms such as `4-player` and `7-card`.

- `LaRata/workshop.json`: official upload configuration, trilingual description and change notes, visibility, tags and BaseLib dependency (3737335127).
- `LaRata/description.txt`: editable source of the description; copy changes into `workshop.json` before publishing. The uploader reads the JSON description.
- `LaRata/changelog-0.9.5.txt`: current Steam change notes, mirrored into `workshop.json` for upload. Older change notes remain historical.
- `LaRata/mod_id.txt`: stable item identity.
- `LaRata/image.png`: 600 x 600 square Workshop thumbnail, below Steam's 1 MiB limit, recomposed to fill square tiles without letterboxing. The existing artwork license applies.
- `LaRata/artwork-square-source.png`: full-resolution square cover generated from the widescreen cover, keeping the rat king left and the large logo right. This is the current thumbnail source.
- `LaRata/artwork-source.png`: full-resolution approved cover generated with the built-in OpenAI image tool from the invitation throne room and original title logo. The king remains on the left; the enlarged gold/emerald title fills the right. See [the generation brief](artwork-prompt.txt). This is publishing artwork, not a replacement for the in-game invitation.
- `LaRata/content/LaRata/`: ignored staging folder containing only the matching release's `LaRata.dll`, `LaRata.pck` and `LaRata.json`. Do not stage game files, saves, development tools or BaseLib.

## Switching the local development installation

Double-click **`Switch La Rata.cmd`** in the repository root. Choose **1** for the local test build or **2** for the subscribed Workshop build. Close the game first. This only changes the installation source; it does not publish, build, change mod-menu settings or touch saves, BaseLib or other mods.

The local folder is parked at `<game>/.larata-dev/LaRata`, outside `mods`. Switching back restores that exact copy. On first use, if no parked copy exists, Local copies the last compiled package from `artifacts/package/LaRata`. Run `scripts/Build.ps1` first if that package is missing. Choosing Local again does not update an already active copy: use `scripts/Install.ps1` after building to install new changes. If both a parked and an active copy exist, Workshop refuses to overwrite either; restore Local before installing a newer build.

For command-line use:

```powershell
./scripts/Switch-ModVersion.ps1 -Mode Status
./scripts/Switch-ModVersion.ps1 -Mode Local
./scripts/Switch-ModVersion.ps1 -Mode Workshop
```

`-WhatIf` previews changes. `-GamePath`, `-WorkshopPath` and `-PackagePath` support other locations. A Workshop switch requires its complete downloaded package. Keep the game mod-menu switches enabled and use matching versions for co-op. The old first-publication backup under `artifacts/workshop-0.8.2/` is left untouched.

## Updating the existing item

1. Build and validate the release normally. Copy its three runtime files into `LaRata/content/LaRata/` and compare their SHA-256 hashes with the release package.
2. Update description, version references and change notes in `workshop.json`. Keep `mod_id.txt` unchanged. The checked-in configuration is public: an upload publishes immediately and requires authorization.
3. With Steam running under the item owner's account, run the official uploader from its extracted directory:

```powershell
./ModUploader.exe upload -w 'E:/Spieleentwicklung/StS2 - La Rata Mod/workshop/LaRata'
```

4. Verify the success response, the existing item ID, BaseLib dependency and actual visibility in Steam. During the first upload, `private` appeared as **Friends Only**, so do not assume that configuration value means the UI's **Hidden** setting.
5. Subscribe, let Steam download, and test with overriding manual LaRata/BaseLib copies moved outside the game's `mods` directory. Check that the game log names the Workshop paths, not local copies. Preserve profiles and avoid opening existing runs solely for installation checks.
6. Verify the public page without authentication after authorized publication. Steam content review can delay visibility independently of a successful upload.

See [the 0.8.2 publication report](../docs/WORKSHOP-0.8.2.md). This publishing task does not change the runtime or the existing GitHub release ZIP.
