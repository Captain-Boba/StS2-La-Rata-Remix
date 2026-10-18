# Workshop publication: 0.8.2

Published on 2026-10-18: [StS2 - La Rata Remix](https://steamcommunity.com/sharedfiles/filedetails/?id=3822192203), item **3822192203**, using Mega Crit's official uploader v0.2.0. [BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127) is a required Workshop dependency. The content is the unchanged 0.8.2 runtime from GitHub release commit `045a71442e063ee59b77030795731e488dad1423`.

## Installation verification

- Initial restricted upload remained **Friends Only**, as explicitly requested for the test phase. After successful installation checks, the same item was updated to **Public**.
- Steam subscription downloaded the item successfully. All three downloaded runtime files matched the GitHub release and staged upload byte for byte.
- Manual LaRata and BaseLib directories were moved to a local backup outside the game's mod directory before launch. This prevents the native loader from prioritizing them over Workshop copies.
- Game v0.107.1 started through Steam. Its log confirms BaseLib 3.4.7 and La Rata 0.8.2 loaded from their Workshop paths and initialized successfully.
- Main menu, card library and relic collection opened. Custom card artwork and text rendered; Phantom Parade's upgraded preview changed its damage correctly. Undiscovered relics retained the game's normal dark presentation.
- Other previously enabled mods remained enabled. Two unrelated subscribed packages emitted invalid extra-JSON manifest errors; La Rata initialized successfully despite those messages. This is not a full compatibility certification.
- The existing run was never opened or abandoned. Before/after comparison found no changed progress or run files. The only changed pre-existing profile file was `settings.save`: the game updated LaRata/BaseLib installation sources from local directories to Workshop.
- The game was closed after the check. Workshop subscriptions remain active; manual copies are retained in the local artifact backup.
- Final uploader response succeeded for the existing item. Steam's visibility menu showed **Public**, and an unauthenticated HTTP request returned the item page with its title and description (200, no access-error page).

## Runtime checksums

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| LaRata.dll | 240128 | `1D2C75E906793E76FFD978C76C1D702A35D7E64260DEEA6BDD76310CD632B002` |
| LaRata.pck | 78339352 | `6102A7546FE6C98FAF0632951224FE680C4CD68417511715C6898EC6DAEA7C76` |
| LaRata.json | 528 | `75CAF34DF20B2A1E9A0FE5773A69FB9C2F91370F92F15FB3F0A930962EB1E239` |

## Scope and evidence

This was a Workshop delivery, startup and resource-loading test. No additional combat run or Internet co-op session was performed for publication. Prior targeted gameplay checks and their limitations remain documented in [UPDATE-0.8.2.md](UPDATE-0.8.2.md); the runtime bytes are unchanged.

Local evidence is retained under ignored `artifacts/workshop-0.8.2/`: private/public uploader logs, Steam game log, unauthenticated public-page HTML, profile backup/hash baseline and `manual-install-backup/`. Backups and personal logs are not included in the Workshop upload or source control.
