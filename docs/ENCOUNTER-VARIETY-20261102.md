# Encounter Variety — 2026-11-02

K4 erweitert den bestehenden Director um zehn Twistfamilien und acht native Verstärkertypen. Grundlage ist der gemeinsame 0.9.6-Stand einschließlich La Ratas Energie 5/7. Es gibt keine neue Save-, Auswahl- oder Effektarchitektur.

## Neue Twists

Kosten sind die vorhandenen Sechzehntel-Budgeteinheiten. Die neuen Familien besitzen keine zusätzliche Lesser-Variante und keine Akt- oder Party-Multiplikation ihrer angegebenen Dosen. Alle zehn haben das normale Auswahlgewicht 4. Die bisherigen Thorns-Gewichte bleiben unverändert.

| ID | Familie | Kosten | Effekt und Trigger | Native Umsetzung / Begrenzung |
|---:|---|---:|---|---|
| 48 | Sheltered | 48 | Höchstens 18 Schaden pro Treffer | `HardToKillPower`; nur bei fehlender Power gewähren. Ein vorhandenes natives Schadenslimit bleibt erhalten. Keine Summierung von Grenzwerten. |
| 49 | Reactive | 32 | Nach jeder von einem Spieler gespielten Angriffskarte 2 Block | `SneakyPower`; unpowered Block, keine zusätzliche Party-/Dexterity-Skalierung. Auf mindestens 2 ergänzen, vorhandene höhere Werte unverändert lassen. |
| 50 | Drifting | 48 | Eingehender Angriffsschaden zunächst halbiert; jede ausgespielte Karte steigert den nativen Slow-Multiplikator um 10 % bis zum nächsten gegnerischen Zugbeginn | `SoarPower` + `SlowPower`. Beispiel ohne weitere Modifikatoren: 20 → 10; nach einer Karte 11, nach zwei Karten 12. Kein Nerf des eigenen Angriffsschadens. |
| 51 | Overcommitted | 32 | Einmal 4 Vigor; vollständig geblockter Schaden dieses Gegners betäubt ihn | Native `VigorPower` + `ImbalancedPower`. Vigor gilt für den nächsten vollständigen Angriff, Imbalanced bleibt bestehen. Die native Imbalanced-Prüfung verarbeitet `WasFullyBlocked`, ohne zusätzlichen eigenen Hit-Filter. |
| 52 | Sundering | 32 | Nach dem markierten Zug jedem lebenden Spielerziel bis zu 6 Block entfernen | `CreatureCmd.LoseBlock`, begrenzt durch vorhandenen Block; kein Schaden und kein negativer Block. |
| 53 | Purifying | 32 | Nach dem markierten Zug einen eigenen negativen Effekt entfernen | Aufsteigende native Model-ID bestimmt die Auswahl. Temporäre Strength-/Dexterity-Powers werden über ihre native Mengenänderung aufgehoben, damit die Gegenbuchung erhalten bleibt. |
| 54 | Mending | 32 | Nach dem markierten Zug bis zu 3 LP heilen | `CreatureCmd.Heal`; zusätzlich höchstens `max(1, floor(MaxHP / 20))`, natürlich durch fehlende LP begrenzt. Keine erneute Party-Multiplikation. |
| 55 | Entangling | 64 | Nach dem markierten Zug kosten Angriffskarten der Ziele im nächsten Spielerzug 1 Energie mehr | Native `TangledPower`, Cap 1; native Affliction-/Kostenbehandlung und Entfernung am Ende des betroffenen Spielerzugs. |
| 56 | Withering | 64 | Nach dem markierten Zug ziehen die Ziele im nächsten eigenen Zug 1 Karte weniger; danach endet der Effekt | `WitheringPower` als eigener nicht stapelbarer Debuff im vorhandenen Power-System. Native Turn-Start-Menge, Handzieh-Hook und Cleanup nach dem tatsächlichen Handziehen des Besitzers; **erst ab Akt 2, auch im Koop**. Native MindRot-Effekte bleiben getrennt. |
| 57 | Shriveling | 48 | Nach dem markierten Zug verursachen Ziele im nächsten Spielerzug 30 % weniger Angriffsschaden | Native `ShrinkPower`, Cap 1; native Dauer und Aufhebung beim Tod des Appliers. |

Die Kosten priorisieren frühe Vielfalt: Reactive, Overcommitted, Sundering, Purifying und Mending passen mit je 32 Einheiten genau in die 32 verbleibenden Einheiten nach einem Solo-Akt-1-Scout (60 − 28). Die teureren Familien konkurrieren mit den vorhandenen großen Effekten. Withering bleibt auch nach dem temporären Nerf ausdrücklich auf Akt 2 und später beschränkt; Kosten und Auswahlgewicht bleiben unverändert.

## Konflikte, Skalierung und Darstellung

- `Crossfire` bleibt ID 47. Die neuen IDs 48–57 sind angehängt: 58 Enumwerte, 53 aktiv. Armored, Slippery, Spiteful, SkillHunter und Doomweaver bleiben retired.
- Overcommitted zählt zur bestehenden globalen Offense-Gruppe (höchstens eine solche Familie pro Plan). Reactive zählt zu Block; Sheltered und Drifting zu Shield. Die bestehenden lokalen Gruppenlimits bleiben bestehen.
- Die sechs neuen Move-Effekte nutzen den bestehenden Hex-Platz: höchstens eine Hex-Familie pro Gegner, insgesamt eine bei 1–2 bzw. zwei bei 3–4 Spielern. Purifying und Mending erhalten dafür einen nativen Buff-Intent statt eines Debuff-Intents.
- Purifying wird nicht mit den Tradeoff-Familien auf demselben Gegner kombiniert. Drifting/Overcommitted werden auch nicht mit Warded kombiniert. Mending und Regenerating schließen sich auf demselben Ziel aus.
- Startgaben haben wie bisher keinen Applier und verstärken sich nicht über Empowered/Echoing. Neue Hex-Debuffs gehören nicht zu den drei durch Echoing verstärkten Powerklassen. Wiederholungen überschreiten ihre Caps nicht.
- Native Artifact- und Power-Regeln bleiben wirksam, auch für die Nachteile eines Tradeoffs: ein von einem Monster selbst mitgebrachtes Artifact kann Slow/Imbalanced abfangen. Der Director umgeht dieses native Verhalten nicht. Zusätzliche Warded-Kombinationen werden ausgeschlossen.
- Anhaltende Startwirkungen sind als echte native Powers sichtbar. Sheltered/Reactive/Drifting erhalten ausschließlich in ihrem Twistkontext kurze EN/deu/zhs-Beschreibungen, da die Originaltexte Exoskeleton, einen anderen Spieler oder eine Landung erwähnen. Native Amounts, Icons und Effekte bleiben unverändert.
- Neue markierte Effekte benutzen `HexTwistPower`, den vorhandenen deterministisch ausgewählten Move und konkrete lokalisierte Intent-Tooltips. Das Entfernen dieses Statusmarkers schaltet keine Mechanik ab. Die Texte nennen ihre Trigger; es gibt keine zusätzlichen Rundungstechnik-Sätze.
- EN/deu/zhs enthalten jeweils zwölf neue Hex-Titel-/Beschreibungsschlüssel und drei kontextspezifische native Powerbeschreibungen. Die übrigen nativen Powers liefern ihre vorhandenen Spielübersetzungen.

## Verstärkungen

| Neu | Frühester Akt | Native Mechanik / geprüfte Abhängigkeiten |
|---|---:|---|
| SneakyGremlin | 1 | Eigener Spawn-/Aufwachzug, danach Tackle. Benötigt keinen GremlinMerc und keinen benannten Slot; der native Aufwachzug bleibt erhalten. |
| SnappingJaxfruit | 1 | Energiegeschoss, danach 2 Strength; eigener Soundloop mit nativem Cleanup. Optionale Visuals gehören zum Monster, nicht zu einer Encounter-Szene. |
| TwigSlimeM | 1 | Sticky Shot fügt Slimed hinzu; zufälliger Wechsel mit Angriff. Eigene, nullgeprüfte SpitTarget-Visuals, keine Slot-/Encounterabhängigkeit. |
| SlitheringStrangler | 2 | Constrict, anschließend zufälliger Angriff bzw. Angriff plus Block. Keine besonderen Szenen, Slots, Beschwörungen oder externen Lebenszyklen. |
| Chomper | 2 | 2 Artifact beim Eintritt; abwechselnd Doppelangriff und 3 Dazed. Screech besitzt eine native Sprachblase; für die Console-Fixture nur deren UI-Ausgabe ausgelassen. |
| SewerClam | 2 | Native Plating beim Eintritt, abwechselnd Jet und Strength-Aufbau. Native Power-Skalierung bleibt erhalten. |
| SludgeSpinner | 2 | Zufälliger Wechsel zwischen Angriff/Weak, normalem Angriff und Angriff/Strength. Keine unmittelbaren Move-Wiederholungen laut nativer Zustandsmaschine. |
| SpinyToad | 3 | Eigenständiger Zyklus aus 5 Thorns, Explosion mit Thorns-Abbau und Zungenschlag. Der größere Körper kommt erst spät hinzu. |

Akt 1 hat damit **6 statt 3** Kandidaten; Akt 2 **13 statt 6**; Akt 3 und Bonusakt **14 statt 6**. LeafSlimeS, TwigSlimeS, CorpseSlug sowie die bisherigen späteren Nibbit/Flyconid/FuzzyWurmCrawler bleiben enthalten. Inklet kommt nicht in den zufälligen Pool.

Die Zahl vorhandener Gegner, Verstärkungswahrscheinlichkeit, Budgets, Mengenlimits und 4-Gegner-Obergrenze bleiben unverändert. Weiterhin 35 % HP / 50 % Angriff für Scouts und 60 % / 65 % für reguläre Verstärkungen; die bestehende Inklet-Ausnahme im Schadenspfad bleibt erhalten. RatKing, Kingsguard, Bossduo-Regeln und der feste TwoTailedRat-Slot `second` werden nicht verändert.

### Native Audit-Ausschlüsse

Wriggler benötigt `wriggler1`–`wriggler4`; Myte benötigt `first`/`second` für den Startzweig. Stabbot/Zapbot verwenden Fabricator-spezifische Fallpositionen. Axebot hat einen zusätzlichen Stock-/Wiederbelebungslebenszyklus. Parafright hängt über Illusion an anderen Primärgegnern. Diese Typen werden nicht injiziert. ScrollOfBiting ist technisch weitgehend autonom, verursacht aber über PaperCuts dauerhaften MaxHP-Verlust und wurde für diesen normalen Verstärkungspool nicht gewählt. LeafSlimeM wäre dem bereits vorhandenen Slime-Verhalten zu ähnlich; stattdessen erweitert SnappingJaxfruit die Auswahl.

Auch bei Powers wurde die konkrete native Implementierung gelesen: Flutter erzwingt einen ThievingHopper-Cast; Juggernaut/Countdown/Furnace greifen auf Player-Daten zu; NoxiousFumes/TheBomb adressieren spielerseitig `HittableEnemies`; Adaptable/Surprise haben spezielle Wiederbelebungs-/Beschwörungswege. Diese werden nicht als generische Twists eingesetzt.

## Anti-Wiederholung und gespeicherter Zustand

Die Auswahl liest die letzten drei gespeicherten Kampfräume der nativen `MapPointHistory`, ohne den aktuellen Raum. Vorhandene Typen werden vorrangig ausgeschlossen; danach werden Typen bevorzugt, die in diesem Fenster nicht vorkommen. Wenn alle vorkamen, wird die früheste letzte Fundstelle in der geordneten Historienliste bevorzugt. Sortierung erfolgt weiterhin ordinal nach Model-ID; die Ziehung nutzt einen dedizierten Run-/Floor-/Act-/Party-seeded Stream `reinforcements/v3`.

Es gibt keine prozesslokale Bag, neue Savefelder oder persistente Nebenablage. Gleiche Eingaben ergeben gleiche Auswahl. Ein Update der Mod verändert bewusst die mögliche Auswahl und damit die Rekonstruktion eines alten Kampfstarts; dies ist keine Zusage byteidentischer Kampfrekonstruktion über Modversionen hinweg.

## Technische Prüfung und Grenzen

Der bestehende ContentChecks-Runner erhält `--encounter-variety`. Er verwendet die bestehende Desktop-Fixture und optional die bereits extrahierten nativen Lokalisierungstabellen. Prüfergebnisse und Quellenhashes liegen unter `artifacts/encounter-variety-20261102/k4/`.

Die Prüfung umfasst gerichtete Act-/Tier-/Party-Pläne, Replay/RNG, Kosten und Konfliktgruppen, tatsächliche Startgaben, konkrete markierte Intents, native Befehle ohne Statusmarker, Power-Caps, Kosten-/Draw-/Schadenswirkung, native Artifact-/Dauergrenzen sowie autonome null-slot Monsterläufe einschließlich Eintritt, ausgeführten MoveStates und Cleanup. Die Fixture ersetzt Desktopzugriffe und Chompers Sprachblasen-UI, nicht seine Dazed-/Angriffslogik.

Keine Vollsuite, kein Spielstart, kein Export und keine Installation durch K4. Keine gerenderte Szenen-/Animations-/UI-Abnahme, keine reale Netzwerkpartie, kein Disk-Save/Load-Nachweis und keine vollständige Balanceabnahme. K6 übernimmt nach dem Quellen-Freeze die unabhängige Integration; technische Simulation ist keine menschliche Spielabnahme.

## Abschluss der Integration

K6 hat die Erweiterung als **0.9.6 lokal installiert**. Release-Build, 2.517 native EN/deu/zhs-Formatierungen, die drei kontextbezogenen Beschreibung-Getter, 93 Patch-Methoden und der offizielle Ressourcenexport bestehen. Im PCK ändern sich ausschließlich die drei `powers.json`, jeweils um 15 neue Schlüssel; die übrigen 350 gepackten Dateien bleiben byteidentisch. Die vorherige Installation wurde außerhalb des Mod-Ordners gesichert. Kein Spiel wurde gestartet.

Kandidat und Nachweise: `artifacts/encounter-variety-20261102/final-candidate/LaRata` und `artifacts/encounter-variety-20261102/qa/REPORT.md`. Live-Spiel, Darstellung, Balance, echtes Koop und vollständiges Speichern/Laden bleiben durch den Nutzer zu prüfen. Die oben genannten technischen Grenzen gelten weiterhin.

### K4-Abschluss / Quellen-Freeze

- PASS: isolierter Debug-Build, 0 Fehler. Bestehende Warnungen: NU1900 (NuGet-Audit nicht erreichbar), CS8600 in DisconnectRegressionChecks.cs:219.
- PASS: 9.216 gerichtete Pläne und deterministisches Replay; alle zehn neuen Familien erreichbar, fünf im Solo-Akt-1-Scout-Restbudget.
- PASS: 160 native Twist-Fixtures über vier Akte und 1–4 modellierte Spieler; zusätzliche Artifact-, temporäre-Statistik- und bestehende-Power-Grenzen.
- PASS: 3.008 ausgeführte native Monsteraktionen einschließlich Replay über alle aktabhängigen Pools; 14 unterschiedliche Typen, Eintritt/Status/Null-Slots/Cleanup und Verstärkungsfaktoren.
- PASS: je Sprache genau 15 zusätzliche Powers-Schlüssel, identische Schlüssel-/Platzhaltermenge, sämtliche vorherigen Werte erhalten. RatKing.cs und alle drei monsters.json stimmen mit dem Energie-5/7-Übergabehash überein.
- NOT TESTED: gerenderte Darstellung, echte Netzwerkclients, Disk-Save/Load und menschliche Balance-/Spielabnahme.

`build.log`, `variety.log`, `scope-checks.txt`, `scoped.patch`, `report.txt` und `sha256.json` enthalten die konkrete Evidenz. Der Quellen-Freeze bezieht sich auf die K4-Dateien in diesem Manifest. K6s eigene Aktualisierung von HexLocalizationChecks.cs gehört nicht zu K4s Delta.

## Withering-Nerf — 2026-11-02

**Abgeschlossen und lokal in 0.9.6 installiert.** K6 hat Release-Build, Registrierung, Status-/Iconbindungen, 2.529 native Sprachformatierungen und Paketdelta geprüft. Die Installation stimmt mit dem Kandidaten überein; der Vorgänger ist gesichert. Kein Spiel wurde gestartet. Bericht: `artifacts/withering-nerf-20261102/qa/REPORT.md`. Der vollständige Content-Katalog enthält inzwischen alle 53 aktiven Twists direkt in Abschnitt 5.

Der freigegebene Folgepatch ersetzt ausschließlich Witherings dauerhaftes MindRot durch einen eigenständigen Debuff für die nächste Hand. `DrawCardsNextTurnPower` bietet nativ nur einen Buff ohne negative Mengenunterstützung; negative Verwendung würde außerdem fremde Draw-Boni vermischen und Artifact umgehen. Deshalb verwendet `WitheringPower` die vorhandene CustomPowerModel-Integration mit nativen Hooks und Befehlen. Kein allgemeiner Draw-Patch, keine Sonderbehandlung fremder MindRot-Instanzen, keine zusätzliche Zustands-/Savearchitektur und keine neue Abhängigkeit.

- Anwendung durch den unveränderten markierten Move über `PowerCmd.Apply`, Cap 1 je Besitzer über alle Quellen. Native Artifact-Regeln gelten. Akt-2-Grenze und Kosten 64 bleiben bestehen.
- `AmountOnTurnStart` übernimmt die native Momentaufnahme beim Beginn des Besitzerzugs. Anwendung nach dieser Momentaufnahme wartet auf den nächsten Zug.
- `ModifyHandDraw` reduziert ausschließlich die Starthand des Besitzers um 1, mindestens auf 0. Gewöhnliche Kartenzieh-Befehle werden nicht beeinflusst.
- `AfterPlayerTurnStartEarly` entfernt den Effekt über `PowerCmd.Remove` unmittelbar nach dem tatsächlichen nativen Draw-Befehl. Der globale `AfterSideTurnStart`-Hook ist absichtlich kein Cleanup: Eine Spielerentscheidung kann `SetupPlayerTurn` vor dem Ziehen pausieren, während der Seitenstart und andere Spieler bereits weiterlaufen.
- Spätere erneute Anwendung wirkt wieder einmal auf die nächste Hand. Permanente MindRot-Mengen und positive DrawCardsNextTurn-Boni bleiben unabhängig; ein bereits auf 0 reduzierter Draw verbraucht Withering ebenfalls.
- Eigene Registrierung `WitheringPower`; sichtbarer Spieler-Debuff mit bestehendem MindRot-Icon. EN/deu/zhs erhalten je Titel/Beschreibung für den Spielerstatus und korrigierte markierte Intent-/Gegnerstatustexte. Alte Withering-Aussagen zur Kampfdauer sind mit diesem Abschnitt ersetzt.

Gezielte Evidenz des Folgepatchs: `artifacts/withering-nerf-20261102/k4/`. Die frühere Encounter-Variety-Evidenz bleibt historische Evidenz ihres damaligen Quellenstands. Keine neue Vollsuite oder Live-Abnahme.

K4-Folgeprüfung: **PASS** isolierter Debug-Build und **174 tatsächlich gezogene native Hände**, einschließlich 12 Akt-/Party-Szenarien, Doppelanwendung, Wiederanwendung, Artifact, getrennten Besitzern, permanentem MindRot plus positivem Draw-Bonus, Null-Hand, gewöhnlichem Draw und einem pausierten Besitzer vor dem Handziehen. Die Pausierung wird ausschließlich in der Fixture vor dem nativen Draw eingefügt; SetupPlayerTurn, Draw-Befehl und Cleanup laufen danach nativ. **PASS** eng/deu/zhs-Delta und unveränderter Director-/Budget-/Energie-5/7-Stand. Warnungen bleiben NU1900 und bestehendes CS8600. **NOT TESTED**: gerenderte UI, echte Netzwerkclients, Disk-Save/Load, menschliche Spielabnahme.
