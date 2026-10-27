# Akt-4-Kampfabbrüche — read-only Forensik, 27.10.2026

Owner K6, Auftrag und Übergabe über K1. Keine Spielstarts, Tests, Installation oder Produktänderungen; ausschließlich vorhandene Quellen gelesen und diesen Bericht erstellt. Der ENet-Abbaufix ist keine Lösung oder Abnahme der hier untersuchten Steam-Kampfabbrüche.

## Ergebnis und Priorität

**Die jüngsten gemeldeten Kampfabbrüche sind lokalisiert. Der erste Trennungsgrund ist auf diesem Rechner nicht belegt.** Im letzten echten Steam-Run `AYPN8UGULG` fällt in drei Akt-4-Kämpfen zuerst derselbe Silent-Spieler aus. Der lokale Necrobinder-Client erhält jeweils `PlayerLeftMessage`; anschließend meldet er die Schließung durch den Host mit Code 1001 / `Quit`. Der Nutzer bestätigt über K1 ausdrücklich: „immer der selbe mitspieler der Silent danach restart vom Host“.

Damit ist der lokale `Quit` die Folge des Host-Neustarts, kein Beleg gegen einen vorherigen Desync auf dem Silent-Client. Die Ursache darf weder dem letzten lokal gespielten Karteneffekt noch einem Transportproblem oder dem Rastplatzfehler zugeschrieben werden. **Priorität 1: Silent-Originalsitzung mit dem ersten Fehler und gegebenenfalls lokalem/remote Zustandsdump. Priorität 2: dazugehöriges Hostlog mit Trennungsgrund und Checksum-ID.**

## Quellen, Versionen und Zeitbasis

`U` = `C:/Users/lukas/AppData/Roaming/SlayTheSpire2/`.

`A` = `E:/Spieleentwicklung/StS2 - La Rata Mod/artifacts/night-wave-20261025/baseline-backup/userdata/logs/`.

Alle Zeilen sind 1-basiert, beziehen sich auf die vollständige Originaldatei und sind keine Uhrzeiten. Rotationsnamen sind nicht die Sitzungsstartzeit. Netzwerk- und Action-Zeitangaben verwenden laut `.research/game/decompiled/MegaCrit.Sts2.Core.Logging/Log.cs:9` ausdrücklich `DateTime.UtcNow.ToString("HH:mm:ss")`; `ActionQueueSynchronizer.cs:142` und `NetQualityTracker.cs:154` verwenden diesen Wert. Am 26.10. entspricht Berlin UTC+1 (CET); am 24.10. und vor der Zeitumstellung am 25.10. UTC+2. Angegebene Zeitintervalle begrenzen den Empfang anhand benachbarter expliziter Zeitstempel, nicht die millisekundengenaue Ursache auf einem anderen Rechner.

| Quelle | Sitzungsheader, lokale Zeit | Rolle / Einordnung | SHA256 |
| --- | --- | --- | --- |
| `U/logs/godot.log` | 26.10.2026 18:13:44 | Jüngste Steam-Sitzung, lokaler Client; letzte Dateischreibung 21:03:13 | `30EF6402B2F78055C0DFE44AC5195053FAEA9BF87BA91703EDAAD0C5131890B4` |
| `U/logs/godot2026-10-26T18.13.43.log` | 26.10.2026 14:47:01 | Lokaler Steam-Host, zwei Runs; kein Akt-4-Kampf | `776A476C7D6731E8F73B2294CD9A8B76668763769AA1948660E2248E0716A841` |
| `U/logs/godot2026-10-26T14.47.00.log` | 26.10.2026 13:12:09 | Steam-Run R3SK7BYNES, später im obigen Hostlog fortgesetzt | `BAF97D5B5E96A9FF1688A5960E139C1A5B565F246D2927698B101F2D3E80EA17` |
| `U/logs/godot2026-10-26T13.12.06.log` | 26.10.2026 01:15:40 | Kein Multiplayer-Embark/Load und kein Akt-4-Kampf gefunden | `A541889441EDB11F1DDFDA3E1A78512DEF82211B73573A439269A472FA4C298A` |
| `U/logs/godot2026-10-26T01.15.36.log` | 25.10.2026 17:13:41 | Steam-Client, zwei Spieler, QCWZ41Y4F4; kein Akt-4-Kampf | `FFACDEF93E0D4659193718C34A395F5B46D38E4996E34E7A7DADA135237AA685` |
| `A/godot2026-10-25T05.41.42.log` | 25.10.2026 00:43:47 | Steam-Client, drei Spieler, FG5TMLF1F2, LaRata 0.9.3 | `368580FBF27D6966EA27940A1BE71C4767B2A8601FA7A99C18B9122BBC569F8E` |
| `A/godot2026-10-25T00.43.45.log` | 24.10.2026 21:12:05 | Steam-Client, vier Spieler, V1PEGDFUMY, LaRata 0.9.2 | `532EAEEDC62B1953B296302ACE534579535038B9BBD053604E5A4ADC892CEE41` |

Jüngste Sitzung: Header `godot.log:22–48` nennt Spiel v0.107.1, Release-Commit `59260271`, MegaDot 4.5.1-m.12, protokollierten Main-assembly-Hash `-1555940892`. BaseLib-Initialisierung Zeile 230: 3.4.7.0; LaRata Zeile 257: 0.9.4.0. Dieselbe Spiel-/BaseLib-Version in den beiden älteren Akt-4-Sitzungen; dort LaRata 0.9.2.0 / 0.9.3.0. Die protokollierten historischen Versionen sind keine Hashbestätigung einer heute installierten DLL. Die fünf aktuellen Rotationen nennen jeweils LaRata 0.9.4.0 und BaseLib 3.4.7.0.

### Run und Spieler der jüngsten Abbrüche

`godot.log:365/372/377` identifiziert den Steam-Host. Zeile 418 nennt Ascension 1, Seed `AYPN8UGULG` und die vollständige Gruppe:

| Rolle | Steam-NetId, exakt aus Logtext | Name / Charakter |
| --- | --- | --- |
| Host | `76561198218050768` | earlS / Ironclad |
| Lokaler Nutzerclient | `76561198011203038` | Boba / Necrobinder |
| Zuerst ausfallender Client | `76561198044899582` | 4zzk1ck3r / Silent |

Namen bestätigt durch `U/WhoCarried/events.log:1–4`, SHA256 `5A9F9BCDE45C7358E732BFB51C6544FBA72214185A09CEE43884F5E445E0822D`. Dort lautet der ergänzende Run-Schlüssel `AYPN8UGULG:1793035056`, Start 26.10. 18:17. `current_run.dat:2–4` nennt dagegen `AYPN8UGULG:1793035054`, `Finished=false`, `Victory=null`, SHA256 `27489B17E4D08D77A037D46AA72A4839D94204DAE77595AA807AA074B586FE8D`. Die zwei Sekunden Differenz bleiben sichtbar: dies sind Hilfsmod-Schlüssel, keine übereinstimmend bestätigte native Run-ID. Für Gegenlogs sind Seed, Gruppe, Lobby und Kampf maßgeblich. WhoCarried ist ergänzende lokale Spielverlaufsquelle, kein unabhängiger Netzwerkzustandsvergleich.

## P1: Rat King — jüngster erster Peer-Abgang, 26.10. gegen 21:03 CET

Quelle für alle folgenden Zeilen: `U/logs/godot.log`, Hash oben. Lobby `109775242435126427`, Join Zeile 71940; geladen mit derselben Gruppe Zeile 71971. Aktindex 3 = Akt 4, Koordinate `(3,5)`, Raum 0. WhoCarried Zeile 4475 bestätigt Floor 51 / A4 / La Rata.

1. **77110:** echter aktiver Kampf `LARATA-RAT_KING_ENCOUNTER`. Plan Runde 1 in 77115.
2. **79311/79335/79336:** lokale Checksum-IDs 258, 259, 260 an den Phasengrenzen. **79337–79344:** `ROYAL_HAND` der zweiten Gegnerphase; Royal Flush, Umbral Stitch, Soul Foreclosure, danach Crown Bulwark STRIKE und Crown Headsman OATH.
3. **79345:** Checksum-ID 261, After enemy turn end. **79348:** `ROYAL PLAN turn=3`, Energie 6/6, planned `47,18,9`. **79349:** Checksum-ID 262, After player turn start.
4. **79359–79372:** Host spielt `UNMOVABLE`, Action-ID 309, Kartenindex 28; expliziter Action-Zeitstempel 20:02:57 UTC = **21:02:57 CET**.
5. **79373:** `Received PlayerLeftMessage for 76561198044899582`. Empfang liegt zwischen 21:02:57 und 21:03:06 CET. Dies ist der erste sichtbare Ausfall, während UNMOVABLE lokal noch ausgeführt wird. Die vorherige lokale Prüfsummengrenze ist 262.
6. **79375–79377:** Aktion endet dennoch, lokale Checksum-ID 263 wird erzeugt. **79383–79384:** letzte Verbindungsstatistik 20:03:06 UTC = 21:03:06 CET, Host-Ping 35,219 ms, gemeldeter Paketverlust 0.
7. **79386–79389:** danach `ClosedByPeer`, Host schließt mit 1001 / `Application closed connection`; lokaler RunLobby-Grund `Quit`. Keine weitere Gameplayaktion danach; Rückkehr ins Hauptmenü und reguläres lokales Programmende.

**Belegt:** Silent-Ausfall vor Host-Neustart, genauer Encounter und lokale Zustandsgrenzen. **Nicht belegt:** welcher Checksum/Fehler den Silent-Ausfall ausgelöst hat. Kein lokaler StateDivergence-Dump, keine Exception in diesem Rat-King-Abschnitt und kein numerischer lokaler/remote Checksumvergleich. Das Erzeugen der IDs 262/263 ist allein keine Bestätigung gleicher Zustände. Royal-Karten und UNMOVABLE sind zeitliche Anker, keine nachgewiesenen Fehlerverursacher. Die Host-Verbindungsstatistik beurteilt nicht die Verbindung zum Silent.

## P1: Kingsguard — erster Peer-Abgang 20:46:32–35 CET

Gleiche Quelldatei/Hash und Rungruppe. Lobby `109775242434527297` ab **68388**, tatsächlicher Reload **68419**. Aktindex 3, Koordinate `(3,3)`, Raum 0. WhoCarried **3841**: Floor 49 / A4 / Kingsguard.

1. **68691:** aktiver `LARATA-KINGSGUARD_ENCOUNTER`.
2. **71685–71703:** Ende der dritten Spielerrunde. **71777–71781:** Bulwark/Headsman/Armorer RENEW, Herald OATH, Checksum-ID 117 nach Gegnerende.
3. **71786:** Checksum-ID 118 nach Beginn der nächsten Spielerrunde. **71789–71810:** lokales `DELAY` (Action 143) ab 19:46:31 UTC = 20:46:31 CET. **71811–71815:** Hostaktion Emerald Ward+ wird eingereiht, Zeitstempel 20:46:32 CET.
4. **71816:** derselbe Silent wird durch `PlayerLeftMessage` als getrennt gemeldet. Folgender Zeitanker **71836** ist 20:46:35 CET. Der erste sichtbare Ausfall liegt daher **20:46:32–35 CET**, vor Abschluss von DELAY und vor der lokalen Ausführung von Emerald Ward.
5. **71818/71829:** Checksum-IDs 119 nach DELAY und 120 nach Emerald Ward. Danach drei RIGHT_HAND_HAND-Aktionen; letzte **71898** mit Checksum-ID 123, Action-ID 147 / Kartenindex 150 gegen Crown Herald. Letzter zugehöriger Action-Zeitstempel **71885**: 20:46:41 CET.
6. **71905–71911:** Hoststatistik 20:46:45 CET, anschließend Host-`ClosedByPeer` 1001 / `Quit`. **71940/71971/72020:** neue Lobby, gemeinsames Laden und erneuter Kingsguard-Kampf.

**Belegt:** derselbe erste Silent-Ausfall, danach mehrere lokal weiter ausgeführte Aktionen und der vom Nutzer bestätigte Host-Neustart. RIGHT_HAND_HAND geschieht nach dem ersten Ausfall und ist deshalb kein belastbarer Ausgangspunkt für einen Ursachenfix. Kein lokaler Akt-4-Divergenzdump; ursprünglicher Silent-Grund fehlt.

## P2: Vorheriges identisches Muster im Akt-4-Vorkampf

Gleiche Sitzung, Lobby `109775242433718335` ab **66081**, Load **66111**, Kampf **66345** `DEVOTED_SCULPTOR_WEAK`, Aktindex 3, `(3,2)`, Raum 0. WhoCarried **3719**: Floor 48 / A4.

- **68224:** lokale Checksum-ID 70 nach Spielerturnstart. Host-THUNDERCLAP wird ausgeführt; lokales SQUEEZE ist eingereiht.
- **68243:** erneut Silent `76561198044899582` weg; benachbarte Zeitanker **68240/68286** begrenzen dies auf **20:39:32–37 CET**.
- **68245:** Checksum-ID 71 nach THUNDERCLAP; **68256:** Kampf gewonnen. Rewardverarbeitung folgt; **68323–68326:** Host schließt / lokaler Quit.
- **68334:** `PlayerChoiceMessage ... is not connected` tritt erst nach der Trennung auf. Dies ist ein separater Folgefehler, kein Beweis für die Ursache des ersten Silent-Ausfalls.

Das Muster beginnt somit bereits vor Kingsguard und Rat King. Keine pauschale Schlussfolgerung auf Bosskarten oder Royal-RNG zulässig.

## Separate Exceptions im später erfolgreich beendeten Kingsguard-Versuch

Nach Reload **72020** folgt in **75767–75772** `ArgumentException: '0' cannot be greater than -1` aus `NInspectCardScreen.SetCard/Open` und `NCardGridSelectionScreen.ShowCardDetail`. **76554** dokumentiert anschließend den Kingsguard-Sieg. Dieser Stack betrifft Kartenansicht, belegt hier keinen Netzwerkabbruch.

**76775/76822/76869:** `Unexpected act` aus `NRestSiteCharacter._Ready` nach dem Sieg beim Rastplatz. Rat-King-Kampf startet danach in **77110**. Diese Meldungen dürfen nicht als Ursache der zuvor beobachteten Silent-Kampfabbrüche angegeben werden.

## Vorhandene echte Divergenzdumps: geprüft und abgegrenzt

Alle unten genannten Paare wurden erneut vollständig zeilenweise verglichen; Leerzeilen am Dumpende ausgenommen. Keine pauschale RNG-Vermutung: innerhalb der jeweiligen Paare stimmen die übrigen protokollierten Felder einschließlich RNG-Zähler überein. Die Dumps betreffen frühere Kontexte, nicht die drei beschriebenen ersten Silent-Abgänge.

| Quelle / Meldung | Kontext | Tatsächliche Unterschiede lokal / remote |
| --- | --- | --- |
| `U/logs/godot.log:26450`, ID 862 | After player turn start, BOWLBUGS_NORMAL, Aktindex 1 | 236 Zeilen; nur Choice IDs `22,53,60` / `22,52,60` in 26453/26691 |
| gleiche Datei:43649, ID 561 | Exiting VAKUU | 112 Zeilen; nur MUSIC_BOX acquisition 16 / 15 in 43711/43825 |
| gleiche Datei:65743, ID 706 | Exiting PAEL | 134 Zeilen; nur PAELS_TOOTH acquisition 23 / 22 in 65822/65958 |
| `U/logs/godot2026-10-26T18.13.43.log:16447`, ID 684 | Exiting VAKUU, lokaler Host | 77 Zeilen; nur MUSIC_BOX acquisition 14 / 17 in 16506/16585 |
| gleiche Datei:34156, ID 526 | Exiting PAEL, lokaler Host | 66 Zeilen; PAELS_BLOOD 12 / 11 in 34180/34248 und PAELS_GROWTH 11 / 12 in 34204/34272 |
| `U/logs/godot2026-10-26T01.15.36.log:13153`, ID 485 | Exiting PAEL | 64 Zeilen; nur PAELS_FLESH acquisition 11 / 12 in 13176/13242 |
| `A/godot2026-10-25T00.43.45.log:54957`, ID 1716 | Exiting REFLECTIONS | 154 Zeilen; nur Choice IDs `36,53,48,37` / `37,53,48,37` in 54960/55116 |
| gleiche Datei:72934, ID 533 | Exiting SLIPPERY_BRIDGE, vor Kingsguard | 189 Zeilen; nur Choice IDs `11,16,15,6` / `12,16,15,6` in 72937/73128 |

Ergänzung gegenüber dem früheren Bericht: Auch die zwei Host-Fälle in der Rotation von 14:47 Uhr enthalten auswertbare lokale/remote Dumps; deren Feldunterschiede sind hier konkretisiert. Historischen Bericht nicht verändert.

Der ältere Akt-4-nahe Fall ist eindeutig ein **Event-Exit vor Kampfstart**: SLIPPERY_BRIDGE-Prüfsumme **72929–72935**, StateDivergence, erst **73342** Erstellung des Kingsguard-Raums, **73346–73347** Disconnect; nach Reload **73510/73561** Fortsetzung. Kein bereits laufender Kingsguard-Zug als ursprünglicher Divergenzkontext. Andere Peer-Abgänge dieser Vierergruppe in **55291/73332** begleiten die Event-Divergenzen; spätere **80829/80875** liegen im Game-over-/Badge-Kontext und sind keine neuen belegten Midfight-Desyncs.

Historie `U/steam/76561198011203038/profile1/saves/history/1792869496.run`, SHA256 `7EC0C8A64C7A58B2761316C4EAC8DB63BF99023EB6798D72B02A51E7F107C57F`: Seed V1PEGDFUMY, Start 24.10. 21:18:16 Berlin, vier Spieler, `win=true`; letzter Akt enthält Kingsguard mit zwei und Rat King mit fünf Zügen. Die Folgesitzung `A/godot2026-10-25T05.41.42.log` enthält Kingsguard **53485**, Sieg **57147**, Rat King **57706**, spätere Game-over-/Badgefolge und Quit **62798**, ohne StateDivergence-Meldung. Historie `1792881881.run`, SHA256 `5747F4299EF189ACE88CD543B8491EE41A3DE7A5C547B1F8BD0B213F501B1CF2`: Seed FG5TMLF1F2, Start 25.10. 00:44:41 Berlin, drei Spieler, `win=true`, Kingsguard fünf / Rat King neun Züge. Das widerlegt keine späteren Abbrüche mit anderer Version.

## Umfang der ausgeschöpften lokalen Quellen

- Alle fünf aktuellen `U/logs/godot*.log` samt Headern, Steam-Rollen, Divergenzen, PlayerLeft-, Disconnect-, Kampf- und Exceptionkontexten geprüft. Keine neuere echte Nutzersitzung als 26.10. 18:13:44 vorhanden.
- Archivierte Originale in `disconnect-live-20261027/pre-live`, `feedback-wave-20261027/pre-live`, `enet-teardown-20261027/pre-live` sowie `night-wave-20261025/{baseline-backup,pre-live,pre-menu-exit,post-live-userdata}` nach Dateihash dedupliziert. Die aktuelle `godot.log` liegt mit identischem Hash in drei frischen QA-Backups. Kein Gegenlog des betreffenden Host-/Silent-Rechners darunter.
- Alle Akt-4-Kampftreffer unter `artifacts` mit `rg -uuu --glob '*.log'` geprüft: ältere benannte QA-Logs sind lokale ENet-/Solo-Szenarien, keine neuen echten Steam-Gegenlogs. Beispiel `artifacts/logs/20261025-094730-Client1.log:135/186/202`: `--force-steam off`, Client-ID 2101, ENet-Join. Diese Tests und deren Kopien/gekürzte Vergleichslogs nicht als Nutzerfälle gezählt.
- Aktuelle Steam-Runhistorien und archivierte `.run`/`.save`/`.backup` nach den Seeds durchsucht. Für AYPN8UGULG keine native abgeschlossene Runhistorie oder aktuelle native Multiplayer-Savedatei auf diesem Client gefunden; WhoCarried enthält den oben genannten unvollendeten Verlauf. Vorhandene `.VAL.corrupt`-Dateien stammen vom 15.10., nicht aus dieser Sitzung.
- Sentry-/Crashdateien aktuell und in den jüngsten Backups geprüft: `last_crash` nennt 09.03.2026; sechs vorhandene `__sentry-event`-Dateien haben Schreibzeiten aus den QA-Prozessen am 27.10. (02:16/02:21, 03:55/03:56, 15:24/15:25 Berlin) und keine Klartextmarker für diesen Seed oder StateDivergence. Keine passende separate `.dmp`-/Desyncdatei in den untersuchten Spiel-/QA-Verzeichnissen. Die binären Sentry-Dateien wurden nicht als vollständige Crashstacks dekodiert; aus ihrer bloßen Existenz wird kein Crash am 26.10. abgeleitet.
- Spielinstallationsunterordner `.larata-dev`, relevante Documents-/My-Games-Verzeichnisse sowie direkte Dateien in Desktop/Downloads auf Spiel-/Logfundstellen geprüft; kein zusätzliches passendes Gegenlog. Native `ChecksumTracker.cs:224–264` schreibt lokale/remote Dumps in das normale Log und meldet zusätzlich an Sentry; ein zusätzlicher lokaler Dump wird deshalb nicht vorausgesetzt.

## Genau fehlende Nachweise und nächster Schritt über K1

**Zuerst 4zzk1ck3r / Silent, Steam-ID `76561198044899582`:** vollständige `godot*.log` aus `%APPDATA%/SlayTheSpire2/logs/` für den 26.10.2026, Runseed AYPN8UGULG. Insbesondere die Zeitfenster **20:39:32–37, 20:46:32–35 und 21:02:57–21:03:06 Berlin**, mit vorherigem Kampf-/Turnkontext. Benötigt werden Header, erster Fehler, Checksum-ID/-Kontext und beide State-Dumps, sofern protokolliert. Keine bloßen letzten zehn Zeilen: Ursache und spätere Trennung können auseinanderliegen.

**Danach bzw. ergänzend earlS / Host, Steam-ID `76561198218050768`:** dieselben Sitzungen/Neustarts, besonders Lobbies `109775242433718335` (Vorkampf), `109775242434527297` (Kingsguard), `109775242435126427` (Kingsguard-Reload und Rat King). Erwarteter relevanter Eintrag: `Player 76561198044899582 disconnected ... Reason: ...`, gegebenenfalls erste `State divergence detected`-Meldung mit Kontext und Checksum. Native `RunLobby.cs:202–216` protokolliert den Grund auf dem Host, sendet den anderen Clients aber nur die ID. `PlayerLeftMessage.cs:10–35` enthält kein Reason-Feld; deshalb kann der lokale Client diesen fehlenden Grund nicht rekonstruieren.

K1 fragt den Nutzer nach diesen beiden vorhandenen Logordnern. Keine direkte Nachricht an Mitspieler und kein neuer Testlauf. Bis Gegenbelege eintreffen: **Kampfabbrüche und erster betroffener Spieler bestätigt; StateDivergence als Ursache dieser drei Ausfälle noch nicht bestätigt; verursachende Karte, Relikt, RNG-Feld oder Exception unbestimmt.** Keine produktive Korrektur oder pauschale K4-Sourceprüfung aus zeitlicher Nachbarschaft beauftragt. Die bereits korrigierten Choice-/Relikt-ID-Fälle und der ENet-Abbau bleiben getrennt.

## Nachtrag: Host-Originale eingetroffen — drei Statistik-Desyncs bestätigt

Am 27.10. stellte der Nutzer `C:/Users/lukas/Downloads/host-logs.zip` bereit. Ohne weitere Agenten, Spielstarts oder Produktänderungen ausgewertet. Fünf Originaldateien unter `artifacts/act4-host-logs-20261027/` gesichert; `source-hashes.json` enthält SHA256 aller Logs und des ZIPs. Archivpfade vor dem Entpacken auf einfache `godot*.log`-Dateinamen begrenzt.

Die zunächst einzeln zugesandte `godot2026-10-26T18.13.34.log` enthält laut Header die Sitzung vom **25.10. 18:08:33**, nur Host und lokalen Nutzer in Akt 2. Der Archivname bezeichnet nicht die gesuchte Sitzungsstartzeit. Im ZIP liegt die passende Sitzung in **`godot2026-10-26T21.04.17.log`**, Header 26.10. 18:13:36, Seed AYPN8UGULG und alle drei richtigen Spieler; SHA256 `CB52C463AF27DA8170716C2617986EB6FC694E09CC53B76595D6A4C753DC11C2`.

Der Host enthält sowohl seinen eigenen als auch den vom Silent übermittelten Zustandsdump. Die vollständigen Paare wurden zeilenweise verglichen, lediglich Leerzeilen am Dumpende entfernt. Ergebnisdatei: `state-differences.json`. **In jedem der drei Paare unterscheidet sich genau eine Zeile: der gespeicherte La-Rata-Nutzenzähler von Snecko Skull des Silent-Spielers.** Feld: `RelicModel_LaRataRelicValuesV1`, Metrik `power_value_adjustment`, Einheit `PowerValue`, Attribution `Contribution`. Erwerbs-ID 18 und alle übrigen protokollierten Felder einschließlich tatsächlicher Powers, HP, Karten, Choice-IDs und RNG-Zähler stimmen jeweils überein.

| Kampf / Berlin-Zeit aus vorheriger Zuordnung | Checksum | Host / Silent Zähler | Dump-Meldung | Explizite Trennung |
| --- | --- | --- | --- | --- |
| Devoted Sculptor, 20:39 | 70 | 32 / 33 | 66779; 378 Zeilen je Dump | 67555–67556: StateDivergence |
| Kingsguard, 20:46 | 118 | 42 / 44 | 70973; 438 Zeilen je Dump | 71870: StateDivergence |
| Rat King, 21:03 | 262 | 50 / 53 | 79115; 441 Zeilen je Dump | 80018: StateDivergence |

Alle drei Checksumkontexte lauten **After player turn start**. Der Host trennt gezielt Silent-ID `76561198044899582` wegen `StateDivergence`. Damit sind diese drei Fälle keine bloß vermuteten Netzwerkabbrüche mehr: **Die auseinanderlaufenden La-Rata-Statistiken lösen den Zustandsvergleich aus.** Das erklärt auch, weshalb die anderen Clients zunächst weiterlaufen und erst nach dem Host-Neustart aussteigen. Die späteren Heartbeat-Exceptions nach Peerentfernung sind Folgefehler und nicht die erste Ursache.

### Source-Eingrenzung, noch kein Fix oder Reproduktionsnachweis

`src/LaRata/RelicResolutionMetrics.cs` zeichnet `ModifyPowerAmountGivenAdditive/Multiplicative` als `power_value_adjustment` auf. Native `SneckoSkull.ModifyPowerAmountGivenAdditive` liefert den Poison-Zuschlag. Derselbe native Hook wird sowohl von `PowerCmd.Apply/ModifyAmount` als auch von `PowerVar<T>.UpdateCardPreview` aufgerufen. Der Preview-Aufruf übergibt keinen `previewMode` an `Hook.ModifyPowerAmountGiven`; der bisherige Schutz in `CalculationBoundary` erkennt Vorschauen über einen gleichnamigen Hookparameter und einen aktiven Command-Kontext.

Eine verschachtelte lokale Vorschau innerhalb eines aktiven Power-Kommandos ist daher ein konkreter zu prüfender Zählpfad. **Noch nicht bewiesen ist, welcher Aufruf die zusätzlichen 1/2/3 Beiträge erzeugte.** Die Dumps belegen das falsche Statistikfeld, nicht den vollständigen Ausführungsweg. `RelicResolutionMetrics.cs` ist gegenüber HEAD unverändert; die bisherigen ID-Fixes beseitigen diesen separaten Pfad nicht. Keine Prüfsummenfilterung, kein Entfernen der Desync-Prüfung und keine pauschale Transportänderung vorgenommen.

Die Silent-Originalsitzung bleibt für die erste zusätzliche Zählung hilfreich, ist aber für die Bestätigung dieser drei Statistik-Desyncs nicht mehr Voraussetzung. Nächster technischer Schritt: gezielte Regression für reale Power-Anwendung versus lokale Kartenpreview, einschließlich verschachtelter UI-Callbacks; darauf basierend eng begrenzte Zählkorrektur und passende Koop-Prüfung. Produktfix und Live-Abnahme dieses neuen Befundes stehen aus.
