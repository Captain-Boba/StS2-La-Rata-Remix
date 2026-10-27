# La Rata Remix localization

## Ownership and integration

K5 owns English source tables and UI integration. K7 owns the German (`deu`) and Simplified Chinese (`zhs`) tables under `assets/LaRata/localization/`. K2/K3/K4 own the model code. K6 exclusively owns technical tests and builds. Translation editing and source comparison are not runtime acceptance.

Use the native game language setting, `LocString`, `LocManager`, SmartFormat and BaseLib's existing table support. Do not introduce a separate language selector, translation loader or fallback implementation. Standard tables use `LARATA-<MODEL_ENTRY>.<field>`; `larata_ui` is the existing custom UI table. Keep English available as the native fallback.

The previous `ILocalizationProvider` implementation wrote English strings into the current table after `ModelDb.Init`. BaseLib's `ModelLocPatch.AddModelLoc` assigns entries unconditionally. The code owners migrated the English entries into JSON and emptied the active providers. `ModEntry` registers `larata_ui`; `UiText.Format` binds its numbered objects before native formatting. K5 also added JSON to the export include filter and applies the native locale font substitution to custom labels. K7 read these integration changes but did not edit the C# or export configuration. Source inspection does not demonstrate a working exported package or visible in-game rendering.

## Authoritative sources

- Local decompiled `MegaCrit.Sts2.Core.Localization/LocManager.cs`: `de` maps to `deu`, `zh_Hans` maps to `zhs`; culture mapping, English fallback and SmartFormat extensions.
- Local `BaseLib-StS2/Patches/Localization/ModelLocPatch.cs`, `DefaultLoc.cs`, `CustomLocTablePatches.cs`: provider writes, fallback and custom-table support.
- [BaseLib in-code localization documentation](https://alchyr.github.io/BaseLib-Wiki/docs/localization/code-loc.html): providers are loaded after the model database and do not dynamically update.
- [BaseLib localization documentation](https://alchyr.github.io/BaseLib-Wiki/docs/localization/): English is the base game's fallback.
- K6 extracted the installed v0.107.1 reference tables without starting the engine or game: `.research/native-reference-0.107.1-20261025/localization/{eng,deu,zhs}` and `.research/native-reference-0.107.1-20261025-characters/localization/{eng,deu,zhs}/characters.json`. These are local reference material, not distributable mod assets.
- K6's `artifacts/dev-wave-20261025/localization-export-01/*.json` contains the actual model-provider export: 87 providers, 212 entries, seven tables. K7 read this export for the editorial comparison of model IDs and English source text. Non-provider UI, boss adapters and regalia are additional tables/entries maintained by K5 and K4.
- Native `FontManager`, `FontControlUtils`, and `ZhsFontPathSet` identify the game's existing Chinese font substitution. Paths include `res://themes/fonts/zhs/noto_sans_mono_cjksc_regular_shared.tres`, `source_han_serif_sc_bold_shared.tres`, and `source_han_serif_sc_medium_shared.tres`. Custom labels must use the existing locale font substitution rather than fixing a Latin font.

## Terminology

Terminology below was read from the installed game's language tables, not inferred from the English words or the first game's translations.

| English | German | Simplified Chinese |
| --- | --- | --- |
| Block | Block | 格挡 |
| Strength | Stärke | 力量 |
| Dexterity | Geschicklichkeit | 敏捷 |
| Vigor | Elan | 活力 |
| Poison | Gift | 中毒 |
| Doom | Verderben | 灾厄 |
| Weak | Schwach | 虚弱 |
| Vulnerable | Verwundbar | 易伤 |
| Frail | Gebrechlich | 脆弱 |
| Thorns | Stacheln | 荆棘 |
| Plating | Panzerung | 覆甲 |
| Buffer | Puffer | 缓冲 |
| Ritual | Ritual | 仪式 |
| Forge | Schmieden | 铸造 |
| Exhaust | Erschöpft | 消耗 |
| Ethereal | Flüchtig | 虚无 |
| Dazed | Benommen | 晕眩 |
| Burn | Verbrennung | 灼伤 |
| Draw pile | Nachziehstapel | 抽牌堆 |
| Discard pile | Abwurfstapel | 弃牌堆 |
| Ironclad | Der Eiserne | 铁甲战士 |
| Silent | Die Stille | 静默猎手 |
| Defect | Das Defekte | 故障机器人 |
| Regent | Der Regent | 储君 |
| Necrobinder | Die Totenbinderin | 亡灵契约师 |

Keep La Rata Remix, La Rata and Ser Kev recognizable as proper names. German possessives may use “Ser Kevs”. Boss/player variants of the same card family use the same translated title; the reference UI distinguishes their roles. German descriptions use LP; Chinese descriptions use 生命. Use digits for numbers greater than three. Preserve the identifiers and formatter syntax of dynamic values; translate prose around them. Do not translate resource paths, IDs, serialized enum values or diagnostic-only code messages.

Identical strings are intentional for proper names such as `La Rata`, the German poker term `Royal Flush`, native German terms such as `Block` and `Gold`, and placeholder-only templates such as `{Plan}` and `{0}  {1}`. An equality check against English must distinguish these from missing translations. Localized German decimal prose uses commas, while Simplified Chinese uses decimal points; numeric mechanics remain unchanged.

Cold Circuit draws one card before upgrading and two afterward. German uses the native `{Cards:plural:Karte|Karten}` expression alongside the unchanged `{Cards:diff()}`, following the installed `COOLHEADED.description`. Chinese has no corresponding noun inflection. Token checking must permit this language-dependent repetition of the existing `Cards` variable and test both amounts; it must not require identical placeholder counts across languages.

## Inventory and handoff

### 2026-10-27 tooltip feedback delta

K2 froze two English card descriptions, `LARATA-CINDER_RECEIPT.description` and `LARATA-SCRAP_CORONATION.description`, without the "If you do" lead-in. German and Simplified Chinese now mirror that short form; the actual Exhaust success condition remains in the mechanics. No selection-related "up to" remains in these card tables. Blood Contract's nonlethal HP limit and unrelated numerical caps remain intact.

K4 froze `LARATA-STURDY_TWIST_POWER.description` as a direct `+{Amount}%` Max HP statement and removed the complete rounding/minimum-heal parenthesis from `LARATA-RAT_KING.royal_adapter.10`, `.16` and `.23`. Both locales mirror these edits. Rounding and minimum healing are unchanged in code.

Both `powers.json` tables add 34 keys under `LARATA-HEX_TWIST_POWER`: base title/description, `unbound.description`, `echoing_unbound.description`, and title/description pairs for `echoing`, `hexed`, `venomous`, `ominous`, `cinders`, `disorienting`, `lacerating`, `shackling`, `sapping`, `constricting`, `eroding`, `doomweaver`, `leaden`, `statichex` and `crossfire`. `{Dose}` and `{Debuff}` are preserved; `{Debuff}` is already the native localized power title. "Marked move" is translated as "markierte Aktion" / "标记行动", with explicit repetition and no hit requirement. These are displays of existing encounter rules, not dispellable switches. Unbound text means no compatible move exists. Doomweaver remains a legacy fallback outside the active selection pool.

Native reference terms include Umwickelt / 紧缠, Zerfall / 瓦解, Leichentuch / 厄运之衣, Wunde / 伤口, Verschleimt / 黏液 and Leere / 虚空. Existing Vigor and Envenom use their native displays: Vigor lasts for the next complete attack, including multiple hits and fully blocked attacks; Envenom persists and triggers on unblocked attack damage. No new next-attack OnHit mechanic or potion, trade or damage-ramp text was introduced by localization. K6 owns targeted technical verification; this entry records source editing and handoff only.

### Corrected A Thousand Cuts price contract

The subsequent user correction supersedes the former increasing-price contract: the owner may remove any number of cards at the Merchant, and every removal uses the native first removal's base price with current discounts and price modifiers. This is neither a fixed Gold amount nor the possibly elevated price at acquisition. Successful native removal history remains intact; losing the relic restores native pricing from that history. K3 owns the implementation; K5 owns the English description. K7 synchronized only `LARATA-A_THOUSAND_CUTS.description` in German and Simplified Chinese; titles and flavor text remain unchanged.

Earlier PASS results for increasing removal prices are historical checks of the wrong contract, not validation of this corrected requirement. Preserve those reports. The corrected descriptions and mechanics require K6's targeted verification; editing or reading the localization sources is not build, installation or live-game acceptance.

### Subsequent compact-card delta in the same 0.9.4 wave

The user subsequently requested concise native-style card text and another coordinated mechanics delta. This supersedes earlier descriptions of Thorn Buster, Graveyard Shift, Court of Cinders and Second Shadow below; historical formatter results retain their original meaning. K7 follows K2/K4's final variables and K5's English source, without changing model costs or balance through translation. The night-wave minimal-stat templates, relics and twist labels remain intact.

Thorn Buster keeps its ID/name and costs 1 in both states: remove Thorns, apply `{VulnerablePower:diff()}` Vulnerable only to enemies that lost Thorns, and show the `{Cards:diff()}` draw line only through native `{IfUpgraded:show:...|}` after upgrading. Graveyard Shift exchanges all Vulnerable removed from a target for `{CalculationExtra:diff()}` Block per removed stack, replacing its old HP-loss/draw text. Court of Cinders uses `{PoisonPower:diff()}` (5/8) on the first owner-card Exhaust each turn, with no Energy grant or owner-turn-only wording; its power uses `{Amount}`. Second Shadow uses `{AfterimagePower:diff()}` (1/2) with unchanged costs. The playable Sewer Alchemy keeps its existing Poison upgrade and gains the model's cost reduction; no fixed cost is duplicated in its description. Optional Exhaust costs and their success conditions must remain explicit when shortening Cinder Receipt or Scrap Coronation.

Boss overrides are separate. Grave Levy's `royal_adapter.10` says 1% Max HP, rounded down with minimum 1, without another quartering; its ordinary card/cameo healing amount remains 24 HP. Royal Thorn Buster removes Thorns and applies 3 Vulnerable without draw. Royal Graveyard Shift removes Vulnerable from the announced player for 3 base Block per removed stack. Royal Court of Cinders applies 5 Poison per first royal Exhaust of a boss turn, without Energy. The repeated boss-adaptation/hand-limit paragraphs are removed from the compact adapter texts; global limits remain in the deck hover text. Player Exhaust keywords are appended natively, whereas boss overrides replace the whole description and include their own single Exhaust keyword. Fuel actions such as “Exhaust 1 spare card” remain distinct from that self-Exhaust keyword.

The additional fourteenth new card is `LARATA-LORD_TIBOS_RESET`: `Lord Tibos Reset` / `Lord Tibo的重置`. Its short description gives each player their own unrefunded Energy spent this turn and excludes costs of every copy of the named Reset card. It promises neither full Energy nor Stars. There are no text DynamicVars or literal Exhaust lines: the model costs 2 in both states, appends native Exhaust only in the base state, and removes that keyword on upgrade. Its boss override `.royal_adapter.47` instead reserves only royal Energy for the next hand under the shared +2 card-Energy cap. The `reset_no_spend`, `reset_cap` and `reset_refund` status strings preserve `{Energy}` and `{Remaining}`. These player/boss differences remain explicit in both locales.

K7 read K5's final `eng/cards.json`, `eng/monsters.json` and `eng/powers.json` and completed the editorial alignment of all 60 card descriptions, 48 royal adapters, the six changed/new status texts and the two Court of Cinders power descriptions. Existing names remain stable; the Reset name and self-reference are localized consistently. Cold Circuit and Grave Spark use native German card-count plurals. The final delta affects only `cards.json`, `monsters.json`, and `powers.json` in each of `deu` and `zhs`, plus this document; the minimal-stat UI tables are preserved. K6 receives the stable delta for actual base/upgrade/preview formatting, keyword, key and variable checks. Earlier 861-call and historical results do not cover this later card/Reset delta; technical verification and actual-client evidence remain pending until K6 reports them.

### Current follow-up after the first local 0.9.4 test

The latest user instruction supersedes the verbose in-game statistics presentation described in the earlier handoffs. Relic tooltips must keep the normal relic description and show only concise, reliably attributable results for the current run: amount, result type and a natural equivalent of “this run”. Multiple supported result types may each receive a short line. Owner, acquisition, coverage, gaps, attribution/contribution explanations and diagnostic headings are excluded from the in-game statistics tooltip. Unsupported or ambiguous measurements are omitted, never presented as zero. Existing diagnostic localization keys may remain for internal use; their presence does not authorize showing them in the tooltip.

K7 owns German and Simplified Chinese additions for this minimal presentation, five new player relics and visible monster twist statuses, following the final K3/K4/K5 model and English contracts. Prefer existing native power names, localized units and dynamic placeholders. Routine translation choices need no additional approval. K6 alone owns the newly authorized build, backup, local installation and targeted actual-client testing; K7 does not run tests or access the shared game/desktop. Earlier no-live-test results below retain their original scope and do not imply acceptance of this new follow-up.

Minimal-stat result templates use `stats.result.<result>` with `{0}` for the locale-formatted amount, `{1}` for the actual numeric value used only by native plural selection, and `{2}` where needed for the native localized power title. German uses the natural singular/plural forms. Chinese uses its classifiers without an artificial plural operand. K6's parity rule permits Chinese to omit `{1}` only when English uses it exclusively in `{1:plural:...}` under these result keys; visible amount/power-name variables remain mandatory. Verify actual `UiText.Format` output for values 1 and 2, not only placeholder sets. No additional formatter implementation is needed.

The implemented follow-up delta is 35 new keys per locale across `relics.json` (15), `powers.json` (8), and `larata_ui.json` (12). The 12 minimal result kinds are Block gained, HP restored, maximum HP gained, Gold, Energy, Stars, cards drawn, cards removed, cards recovered, Energy saved, Stars saved, and native named powers. Drawn/removed/recovered cards remain distinct. German example: `12 Block in diesem Lauf`; Chinese example: `本局12点格挡`. Chinese named-power output places the native title before the amount, avoiding an incorrect universal classifier for effects whose native units differ. Five German templates use native plural selection; Chinese omits the grammar-only operand. Initial targeted verification is recorded below; the historical candidate-04 results do not cover this follow-up.

K6's first night-wave [candidate-01-localization.log](../artifacts/night-wave-20261025/candidate-01-localization.log), read by K7, records **PASS with 861 native formatter calls**. It includes all five new relic descriptions, the four canonical twist templates and minimal-stat amounts 1/2/12 in EN/DE/ZHS. K6 also reports successful native tooltip integration preserving the normal description and `CanonicalModel`, with only positive Exact results from the whitelist; metadata, unknown/mismatched metrics and unsupported shop measurements produce no diagnostic or invented-zero lines. The canonical twist samples establish text formatting only: their supplied example percentages do not establish the actual owner-bound reinforcement mapping. K6 is separately checking 35/60% HP with 50/65% attack and the Inklet 100% exception. That mapping remains pending in this initial report. No PCK loading, font rendering or live UI acceptance is established by this formatter PASS.

The new relic keys are `LARATA-SOOTBOUND_TICKET`, `LARATA-STAIRWELL_KEY`, `LARATA-MOCKING_BELL`, `LARATA-BRASS_SCAVENGER`, and `LARATA-MOONLIT_STOPPER`, each with `title`, `description`, and `flavor`. Both locales preserve K3's distinct triggers and limits: base-cost-2-or-more Exhaust excluding Ethereal; strictly descending paid Energy on manual plays; a different enemy for the Weak follow-up; actual Block-absorbed damage capped at 12 Gold; and a discarded Potion affecting the next non-X manual play in the same turn. K3's final Stopper contract excludes both Energy-X and Stars-X cards, which do not consume the armed effect. `Flüchtig` / `虚无` was read directly from the installed `card_keywords.json`.

Four new twist status models have translated titles/descriptions in both locales: `STURDY_TWIST_POWER`, `KEEN_TWIST_POWER`, `EMPOWERED_TWIST_POWER`, and `REINFORCEMENT_TWIST_POWER` (all `LARATA-` prefixed). `{Amount}` and `{AttackPercent}` are preserved. Text distinguishes increased maximum HP applied on entry, percentage attack damage already included in intents, strengthened small Strength grants, and a reinforcement's percentages of normal HP/damage. These passive display powers do not apply a second stat modification; additive Strength and opening Vigor retain their native power displays.

The locale tables cover cards and their native upgrade variables, relics and booster selection, powers, monsters and moves, royal adapters and status text, encounters, the bonus invitation and act, and UI labels. The owner-supplied English files remain the comparison baseline as the development wave adds strings. Workshop text is outside this work.

K7's deliverables are eight tables in each of `deu` and `zhs`: `acts.json`, `cards.json`, `encounters.json`, `events.json`, `larata_ui.json`, `monsters.json`, `powers.json`, and `relics.json`, plus this document. The final UI handoff includes pack opening, secret contest choices and results, combat journals, the bonus-ending message and map words, and contest statistics. The relic-remover text describes immediate selection on pickup, cancellation leaving it unused, and reopening outside combat until a successful use.

The second statistics handoff adds card/relic/potion/orb acquisition and removal, Osty's summoned HP, and contribution units. Labels for damage, HP loss, rest healing, Block, Gold, Energy and power calculations explicitly describe adjustments before subsequent modifiers. Requested draws are distinguished from actual cards drawn and later limits. Power metrics describe increased/decreased amounts rather than counting effect applications. `Osty` / `奥斯提` follows the installed monster table.

The user's revised scope for 0.9.4 prioritizes obvious, reliably attributable actual contributions. Complex or ambiguous relic measurements are deferred; complete coverage of every vanilla relic is no longer a completion requirement for this wave. Preserve working, verified simple measurements. Missing or deferred measurements must remain unavailable or unmeasured, never inferred zeroes or hypothetical totals. Existing German and Chinese coverage/gap labels already state that data was tracked for the run or is unavailable; they do not promise universal relic-effect coverage. The identifier `stats.coverage.Complete` must not be translated as complete coverage of all effects. No additional UI status keys are introduced solely for this scope change. Existing contribution labels continue to identify intermediate calculations rather than actual final outcomes.

K6's targeted verification should cover JSON parsing, key parity against the final English tables, native SmartFormat syntax and placeholder parity, native model-key resolution after provider initialization, native English fallback, and the custom UI table registration. Exercise both base and upgraded card variables, energy icons, nested localized names, and the royal hand/status variables. Check that UI placeholders are bound before native formatting. Do not treat a regex-only placeholder comparison as proof that SmartFormat renders correctly.

## Recorded technical evidence, 2026-10-25

K6 ran the localization fixture against candidate 02, DLL SHA-256 `4E773623F4AC6A07F1939636A0BCCCA0101DCB856D8F638480F15812FC177B24`. K7 read the resulting [localization log](../artifacts/dev-wave-20261025/candidate-02-localization-01.log) and [fixture](../tests/LaRata.ContentChecks/LocalizationWaveChecks.cs); K7 did not execute them.

The recorded result is **PASS** for strict UTF-8/JSON parsing including duplicate/empty-value rejection, EN/DE/ZHS table/key/variable-set parity, native format parsing of all strings, and empty active model providers. The fixture made 708 native formatter calls across the three languages: 57 base cards and 32 upgraded cards per language, all indexed/custom UI entries, and production relic-stat tooltip generation including contribution labels. Native energy icons are checked for the relevant cards. The fixture also exercised native `LocTable` English fallback. Language-specific repetition of a variable for plural grammar is permitted.

The fixture injects workspace tables into the native localization manager. It does **not** establish BaseLib/engine asset discovery, custom-table registration during actual startup, PCK loading, or exported-package completeness. Non-card royal status and regalia strings receive syntax/variable checks, but this result does not claim full combat-driven rendering of every nested value. These remain distinct from the successful formatter checks.

After that recorded candidate-02 pass, K5 supplied six further statistics keys: completed rest-site actions, automatic plays, actual retained cards, the Sovereign Blade's base-damage change, and the `Plays`/`ForgeDamage` units. K7 added all six to German and Chinese and prefixed the library hint with `La Rata:` in both languages. `Hoheitsklinge` / `君王之剑` follows the installed `SOVEREIGN_BLADE.title`; the statistic explicitly excludes damage dealt. These additions were outside the earlier 708-call result.

The subsequent `stats.orb_passives_completed` key is also translated in both languages. It counts completed direct native Orb passive activations attributed to a relic, using the existing `Activations` unit; it does not describe resulting damage or Block.

K6 subsequently reported **PASS** for those statistics additions on candidate 03b, with 729 native formatter calls. K7 read [candidate-03b-localization-01.log](../artifacts/dev-wave-20261025/candidate-03b-localization-01.log). This includes the rest-action, automatic-play, Forge/base-damage, retained-card and direct Orb-passive labels and units, alongside table/key/variable parity and UTF-8/JSON checks. The same workspace-injection and no-live-rendering limitations apply. This result predates the following tester-card delta.

## Candidate-04 payment statistics delta

Candidate 04's additional payment-statistics text delta adds seven UI keys in German and Chinese: actual Energy/Star savings, actual additional Energy/Stars paid, signed Energy/Star cost adjustments used before later modifiers, and the `StarValue` unit. Negative adjustments are explicitly reductions. The contribution labels do not claim final savings through overlapping modifiers, clamps or alternative currencies. These additions are outside the recorded candidate-03b result and covered by the later candidate-04 verification below.

## Tester-card localization delta

K2 supplied the stable model IDs `LARATA-WRATH_OF_THE_BIG_O` and `LARATA-THE_KIKILYST`; K5 owns their final English title/description keys. K7 added both title/description pairs in German and Simplified Chinese. `Big O` and `Kikilyst` remain recognizable personal names. Strength and Poison use the native terms documented above.

Kikilyst preserves `{Spread:diff()}` for the model's 25-to-50 upgrade. Both translations specify the resulting doubled Poison amount as the basis and all **other** enemies as recipients. Costs, native character pools and floor rounding belong to K2's model, not duplicated localization constants. The existing Thorn Buster entry was updated in place: all Thorns removed, a line break, and a fixed draw count of 2 matching the explicit final English wording; no duplicate card was introduced.

None of these three JSON descriptions includes an explicit Exhaust line. Native `CardModel.GetDescriptionForPile` appends `CardKeywordOrder.afterDescription`, including Exhaust, using the game's localized keyword (`Erschöpft` / `消耗`). These additions are outside candidate 03b's earlier PASS; the subsequent candidate-04 result checks their actual composed text. Model costs and combat behavior remain separate from this localization result.

## Candidate-04 verification

K6's [candidate-04-localization-02.log](../artifacts/dev-wave-20261025/candidate-04-localization-02.log), read by K7, records **PASS** with 780 native formatter calls, strict UTF-8/JSON and duplicate checks, EN/DE/ZHS table/key/variable parity, empty model providers and native English fallback. K6 additionally reports that the seven payment-statistics keys and signed contribution samples (`-1 EnergyValue`, `-2 StarValue`) are included.

The log contains 18 complete `GetDescriptionForPile` outputs: three tester cards, base/upgraded, in three languages. Each contains exactly one native Exhaust line. Thorn Buster draws 2 in both states; Kikilyst displays 25% before upgrading and 50% afterward. The German space before `%` is valid localization and is preserved. This supersedes the pending localization status of the payment-statistics and tester-card deltas, without claiming validation of their gameplay effects or costs.

Tables were supplied from workspace files. Engine/BaseLib asset discovery, PCK loading, fonts and live layout remain untested.

After this PASS, K3/K5 supplied two concrete deferred-measurement gap keys: `stats.gap.shop_savings_deferred` for shop-discount savings and `stats.gap.complex_payments_deferred` for payments involving overlapping modifiers, limits or alternative currencies. Both are translated into German and Chinese as not yet measured/deferred, without zero-value claims. Existing exact measurements and observation-period labels are unchanged. These two later labels await K6's targeted verification and are not covered by the 780-call result.

No game, test client, engine export or build is run by K7. Text overflow, wrapping, actual Chinese glyph rendering, hover layout and readability need a later permitted in-game review. Long royal adapter descriptions, the hand summary, relic selection and statistics tooltips are specific review surfaces. A source-level font path does not establish visible CJK rendering.
