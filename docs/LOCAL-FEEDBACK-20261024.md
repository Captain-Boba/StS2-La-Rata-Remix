# Local feedback changes — 2026-10-24

Implementation notes for the feedback changes prepared locally on 2026-10-24, subsequently authorized for publication as [0.9.2](UPDATE-0.9.2.md). Development initially left the installed 0.9.1 runtime and running game unchanged.

- Ashen Spool grants 1 Block per exhausted card instead of 3. Its first-exhaust draw remains 1 card per turn. The description matches the new value.
- Doomweaver, the random twist that grants Shroud, is excluded from new encounter plans. Its enum identity is retained; native monster mechanics and royal cards are separate.
- Inklet is excluded from the early and late random reinforcement pools. Three early and six later helper types remain. Native Inklet encounters remain available, and their existing Regen cap is unchanged.
- Potion-history hover tips remain native tooltip controls but are parented to the history popup, above its dimming backstop and outside the scroll area's clipping.
- Rat King base HP is reduced from 3,000 to 1,500. Native boss scaling produces 1,500 / 3,900 / 5,850 / 7,800 HP for one / two / three / four players. Kingsguard HP is unchanged.
- Sewer Alchemy and Plague of Crowns are removed without replacements from the active boss deck, leaving 23 cards. Stable catalog IDs and playable booster prizes are retained. Rotting Tribute remains in the boss deck.
- Sewer Alchemy is also removed from royal interventions. Stolen Hour, Royal Pardon and Nightfall Decree join Royal Flush, Stolen Aegis and Exposed Crown, for six intervention cards total. Their existing card effects, warning, cancellation on marked-enemy death and intervention frequency are retained.
- Hidden intervention state still uses the native power amount. A versioned wider card field supports the new IDs without confusing cards with round numbers; the old encoding remains readable.

Deferred at the user's request: “Relics die an die anderen erinnern”. No relic redesign or additional effect change is specified or implemented for that note.

The initial implementation did not include active-game tests, installation or publication. The user subsequently authorized a versioned release, commit, push and Workshop update. The tooltip's appearance still needs a visual check.

Validation: C# build completed without errors (NU1900 vulnerability-feed availability warning); the content checks passed. The policy suite passed 384,000 seeded encounter cases with 43 active twist families, plus its existing deck, potion and cameo checks. Reinforcement checks cover all three early/six late types, exclusion of Inklet, fallback selection and deterministic replay across simulated peers. These checks do not establish live UI or co-op acceptance. Local logs: `artifacts/local-feedback-20261024-{build,content,policy}.log`.

Follow-up validation after the HP/deck changes and intervention expansion: build, content checks and policy checks passed again. Native boss HP matches all four requested party sizes; 40,000 royal turns conserve the 23-card active deck; 120,000 cameo schedules retain their timing and goblin separation. All six intervention IDs resolve to the intended card models, remain hidden in native power state, round-trip with the wider encoding and preserve legacy decoding. No live game or multiplayer session was started for these checks.
