# Native menus and RPS animation verification - 0.3.1

2026-10-17. Game v0.107.1, BaseLib 3.4.7. Isolated Steam-off local profiles only; original game resources and Steam profiles unchanged.

## Implemented

- Replaced the flat Godot choice panel with the native generic/vertical popup, native fonts and native popup buttons.
- Native modal/focus handling; private choices, explicit locked-in status and disabled duplicate submissions.
- Native character hand animation immediately after every synchronized round, native loss shake, then native winner grab. Complete chosen-hand history retained.
- Removed the second Fight banner and replay from the final relic award presentation.
- Map benefits use the existing map-legend parchment and label styles; bonus-node words use the native heading style.
- No new dependency, copied game artwork, custom transport or save format.

## Four-client interactive check

Logs: 20261017-022152-Host.log and 20261017-022212-Client1/2/3.log. This intermediate build retained the 0.3.0 version string and preceded the final spacing/map/banner corrections.

The user manually exercised the choices and reported that they worked well, then identified an extra brief native Fight banner after resolution. The cause was the enclosing native AnimateRelicAwards branch, not a newly randomized fight. The final correction redirects only its animation copy to the existing direct-grab branch.

Tiny Mailbox round 1: Rock/Paper/Paper/Rock, leaving 2101 and 2102. Round 2: Rock/Scissors, winner 2101. All four logs contain exactly the same two outcome records and six completed native hand-animation records, covering all three signs. Evidence: artifacts/ui-20261017/rps-four-client-comparison.json. This proves agreement for this scenario, not general Internet synchronization or statistical randomness.

Manual client closure afterward produced native ENet/peer and engine shutdown diagnostics, retained in the raw logs. This is not an error-free full-session claim.

## Final build and remaining checks

Release 0.3.1 compiled with zero warnings and zero errors and the official Godot export succeeded (artifacts/ui-20261017/build.log). Final layout/banner runtime observations are recorded below when available. Controller hardware, unusual display ratios, reconnect during selection and the full optional Workshop mod set have not been retested for this UI update.

The subsequent two-client 0.3.1 session loaded successfully. After viewing the map, the user requested removal of the suffix after the mod name and smaller text. The header now always reads "La Rata Remix"; font sizes are 26 for the heading, 23 for effect labels and 21 for potion capacities. Header-click collapse remains available. This final typography adjustment is compiled but has not received a further visual acceptance check. The two-client logs contain no new RPS rounds, so removal of the second Fight banner still requires runtime confirmation.
