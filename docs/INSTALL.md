# Install La Rata Remix 0.6.0

For Slay the Spire 2 **v0.107.1**, with **BaseLib 3.4.7**. All mod-added text is English. Supports solo and 2–4 players; this prototype's latest gameplay and four-player synchronization still need playtesting.

## Download

- [LaRata-0.6.0.zip](https://github.com/Captain-Boba/StS2-La-Rata-Remix/releases/download/v0.6.0/LaRata-0.6.0.zip): the compiled mod, ready to install.
- [BaseLib 3.4.7](https://github.com/Alchyr/BaseLib-StS2/releases/tag/v3.4.7): download the author's `BaseLib.3.4.7.zip`. BaseLib is a separate required dependency.

Do not download **Code → Download ZIP** or the **Source code** assets for installation. Those contain development files, not the compiled mod. You do not need .NET SDK, Godot or GitHub Desktop to install the compiled package.

## Install on Windows

1. Close the game.
2. In Steam, open Slay the Spire 2's **Browse local files** action. Use the directory containing `SlayTheSpire2.exe`, not a save folder or your GitHub checkout.
3. Create a folder named **mods** in that directory if it is missing. The name is plural.
4. Extract `LaRata-0.6.0.zip`. Copy its **LaRata** folder into **mods**.
5. Install BaseLib from its own compiled package according to the author's instructions. If installing its three runtime files manually, place them directly inside **mods/BaseLib**.
6. Check the final layout:

```text
Slay the Spire 2/
├── SlayTheSpire2.exe
└── mods/
    ├── BaseLib/
    │   ├── BaseLib.dll
    │   ├── BaseLib.json
    │   └── BaseLib.pck
    └── LaRata/
        ├── LaRata.dll
        ├── LaRata.json
        └── LaRata.pck
```

The La Rata folder also contains documentation and artwork; those extra files are expected. Avoid an extra nested folder such as `mods/LaRata/LaRata/`.

7. In Steam's launch options, remove **--nomods** if present.
8. Start the game, enable **BaseLib** and **La Rata Remix** in the mod menu, and follow any restart prompt.
9. Start a **new run**. Do not upgrade an active run from an earlier La Rata version.

## Co-op

Every player installs the same package and BaseLib version on the same supported game version. Keep the installed mod set consistent across the party. Do not update mods in the middle of a run.

## Updates and troubleshooting

- To update, close the game and replace the three files in `mods/LaRata` together. Keep BaseLib installed.
- If La Rata is missing from the mod menu, check folder nesting, the three runtime files and the `--nomods` launch option.
- If loading fails, confirm game v0.107.1 and BaseLib 3.4.7. This release explicitly rejects other game versions.
- Test a new run with only BaseLib and La Rata when investigating compatibility issues. The Hero Expansion 1.7.0 was observed to fail on the supported public game branch; its author requires the beta branch.
- The game uses separate modded profiles. La Rata does not erase, migrate or unlock progress. Keep La Rata and BaseLib installed while loading a save that contains their custom content.
- The release's `SHA256SUMS.txt` allows optional download-integrity verification with PowerShell's `Get-FileHash -Algorithm SHA256`.

See the [release page](https://github.com/Captain-Boba/StS2-La-Rata-Remix/releases/tag/v0.6.0) for changes and the repository's test reports for validation limits.
