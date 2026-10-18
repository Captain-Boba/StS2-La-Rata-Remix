# Steam Workshop publishing

La Rata's existing item is [3822192203](https://steamcommunity.com/sharedfiles/filedetails/?id=3822192203). Preserve `LaRata/mod_id.txt` when updating; removing it creates a different item.

Publishing uses Mega Crit's official [sts2-mod-uploader](https://github.com/megacrit/sts2-mod-uploader), release **v0.2.0**, Windows x64. It is a development-only publishing tool, not a new runtime dependency. It uploads through the signed-in Steam account; use only the official release and keep Steam credentials out of this repository. No explicit license was found for the uploader, so its executable is not redistributed here.

The downloaded `ModUploader-win-x64.zip` SHA-256 was `2B55C19CC5932235CA9DBD663CA07D04E5D5A0018402303199F9B4EC8CA06578`.

## Files

- `LaRata/workshop.json`: official upload configuration, English description, visibility, change notes, tags and BaseLib dependency (3737335127).
- `LaRata/description.txt`: editable source of the description; copy changes into `workshop.json` before publishing. The uploader reads the JSON description.
- `LaRata/mod_id.txt`: stable item identity.
- `LaRata/image.png`: 512 x 611 thumbnail resized from the repository's existing title artwork, below Steam's 1 MiB thumbnail limit. The existing artwork license applies.
- `LaRata/content/LaRata/`: ignored staging folder containing only the matching release's `LaRata.dll`, `LaRata.pck` and `LaRata.json`. Do not stage game files, saves, development tools or BaseLib.

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
