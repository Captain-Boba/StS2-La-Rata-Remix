# ENet-Verbindungsabbau — Untersuchung und Abnahme 2026-10-27

Owner QA/Tests/Belege: K6. Produktkorrektur: K3. Koordination: K1. Nutzerauftrag über K1: den nachgewiesenen Abbaufehler untersuchen, belastbar korrigieren, lokal installieren und gezielt mit zwei Clients prüfen. Exklusives Desktop-/Testfenster bei K6; vor Starts reale Nutzung prüfen. Keine Git-/Release-/Artänderung, historische Feedbackwelle unverändert.

## Ausgangsbefund und Quellen

- Vollständige Originallogs: `artifacts/logs/20261027-035544-Host.log`, `20261027-035605-Client1.log`. Nach 13 identischen Gameplaychecksums Client zuerst Alt+F4: NGame.Quit ab Clientzeile721. Host ab725 insgesamt61 `Peer not connected`, beim späteren Hostquit1367 `Parameter "peer" is null`. Keine Gameplay-Divergenz zuvor. Beide Logs zusätzlich12 Engine-Ressourcenfehler beim Exit, gesondert.
- Native Fehlerketten: NetQualityTracker.Update → NetHostGameService.SendMessageToClientInternal → ENetPeer.Send; daneben PeerInputSynchronizer.SendSyncMessage → NetHostGameService.SendMessage. Hostquit: StopHost → DisconnectClientInternal(now=true) → PeerDisconnectNow. Kein vorheriges Host-OnPeerDisconnected im Log.
- Aktuelle installierte `sts2.dll` frisch mit vorhandenem ILSpy dekompiliert: `artifacts/enet-teardown-20261027/native/ENetHost.cs`, Assemblyhash daneben. `Update` ignoriert raw EventType.Disconnect: dekompilierter switch(type+1), case Receive entspricht ursprünglichem Disconnect. Nur das eigene Disconnection-Paket oder lokale DisconnectClientInternal entfernen den Listeneintrag über HandleClientDisconnection.
- NetHostGameService.Update verarbeitet zuerst Transport.Update, danach Quality.Update. OnPeerDisconnected entfernt sowohl Servicepeer als auch Quality-Tracker und informiert Listener. Ein ignoriertes Transportereignis erklärt daher beide Folgefehler. BaseLibs untersuchter NGame.Quit-Prefix speichert ausschließlich ModConfigs; kein eigener Transportpfad oder unterdrückter Originalaufruf gefunden. Diese Quellanalyse allein ist noch kein unveränderter Vanilla-Livenachweis.
- Native `ModManager.Initialize` unterstützt `-nomods`; damit ist ein Vergleich ohne LaRata/BaseLib und ohne Installationsumbau möglich. Historische vollständige Quellen unter `.research/game/decompiled`; die1Byte-Platzhalter unter `.research/native-ui-20261027/src` sind kein Quellbeleg.
- Offizielle Schnittstellen: [ENetPacketPeer](https://docs.godotengine.org/en/stable/classes/class_enetpacketpeer.html), [Godot ENet-Ereignisverarbeitung](https://github.com/godotengine/godot/blob/4.5.1-stable/modules/enet/enet_connection.cpp). Remote Disconnect invalidiert den nativen Peer; eventbezogene Lifecycle-Bereinigung ist gegenüber Sendfehler-Unterdrückung zu prüfen. Kein neues Transportframework oder neue externe Abhängigkeit erforderlich.

## Minimaler reproduzierbarer Prüfplan

1. Frisches Backup und Hashmanifest der eigenen Profile1/2101 sowie persistenten Userdaten und installierten Moddateien; Prozesse/Desktop prüfen. Historische Backups nicht als aktuellen Ausgangspunkt verwenden.
2. Vorher: zwei echte native ENet-Spielprozesse mit `--force-steam off`, `-nomods`, nativen Host-/Join-Argumenten. Verbindung bis laufendem Spiel herstellen, Client per Alt+F4 schließen. Host mindestens25s weiterlaufen lassen (native20s-Peer-Timeout plus Reserve), dann regulär schließen. Vollständige Logs und tatsächliche Reihenfolge sichern. Vanilla-Repro entscheidet Modunabhängigkeit; BaseLib-only nur bei abweichendem Ergebnis.
3. K3-Fix unabhängig gegen aktuellen nativen Pfad prüfen. Enger Fixturetest nur für betroffene Event-/Callback-Verträge; technische Aktivierung/Build. Keine Änderung des bestehenden Fehlerfilters. Ressourcen-PCK unverändert übernehmen, sofern ausschließlich Transportcode betroffen.
4. Nachher clientfirst: zwei echte Clients, wenigstens ein nativer gemeinsamer Gameplaychecksum; Clientquit, Hostnachlauf≥25s. Abnahme: genau eine native Peerentfernung/Disconnectbenachrichtigung, kein weiterer Sendeversuch auf toten Peer, keine Nullpeeroperation, Host bedienbar. Gleicher Client-NetId erneut beitreten, regulären Spielzustand/Checksumfortsetzung prüfen (Liste und Handler werden durch Fix berührt).
5. Nachher hostfirst: Host regulär schließen, Client muss seinen nativen Disconnectzustand erreichen und danach regulär schließen. Keine Send-/Nullpeerfehler oder unbehandelte Ausnahme, keine doppelte Benachrichtigung. Falls die Clientseite tatsächlich fehlerhaft ist, eng begründeten Befund an K3; keine unbelegte symmetrische Änderung.
6. Vollständige Logs auswerten: alle Gameplay-/Transportfehler bleiben Fehler. Bekannte Engine-Ressourcenexitmeldungen separat zählen, niemals in Transport-PASS umdeuten. Profile wiederherstellen, temporäre BaseLib zurücknehmen, Installhashes/Prozessfreiheit verifizieren und Fenster an K1 zurückgeben.

## Stand

Abgeschlossen: Vanilla-Reproduktion, unabhängige technische Prüfung, lokale Installation, gezielte echte Zwei-Client-Nachtests und Profilwiederherstellung. **ENet-Abbau clientfirst und hostfirst: PASS im beschriebenen Umfang.** Bekannte Engine-Ressourcenmeldungen beim Exit bleiben offen. Steam/Internet/3–4 Spieler werden nicht aus diesem ENet-Fall abgeleitet.

## Vorhervergleich ohne Mods

Frisches Backup915Dateien, `pre-live-manifest.json`; eigene Profile1/2101 temporär EN/1024×576. Keine aktiven Spiele/Godot vor Beginn, Desktop geprüft. Native Option `-nomods` auf beiden Prozessen, Skip-Modinitialisierung in beiden Logs ausdrücklich bestätigt. HostPID64908, ClientPID40972, gemeinsamer Neow-Raum. Client Alt+F4 um15:10:36.779 CET, Host nach mehr als25s ebenfalls geschlossen (genaue UTC-Eingabechronik `ui-timeline.jsonl`). **Reproduktion ohne LaRata und ohne BaseLib:225× Peer not connected,1× peer is null**. Dieselben nativen Callchains; einzige Host-Peerentfernung erst beim eigenen StopHost(Quit), nicht beim vorherigen rawDisconnect. Screenshots und komplette `vanilla-host.log`/`vanilla-client.log` unter `artifacts/enet-teardown-20261027`, Originalhashes gesichert. Engine-Exitressourcenmeldungen bleiben enthalten. BaseLib-only-Vergleich deshalb nicht erforderlich.

## K3-Fix und technische Abnahme

Einzige neue Produktdatei `src/LaRata/EnetHostLifecyclePatch.cs`, FreezeSHA256 `52052188E7EAFC0845B9CB14027903760F4C7095835D146FDA3E6BF1AC1C740E`. Native AssemblySHA256 `A1F9E653F1E28E4076558FEE1E60D218619CB7E057B887C6417F62C62C6D7A52`. Enger Harmony-Transpiler ersetzt ausschließlich den einzelnen TryService-Call in ENetHost.Update durch einen delegierenden Wrapper. Native Event-/Returnwerte bleiben erhalten; rawDisconnect verwendet vorhandene Peerzuordnung und HandleClientDisconnection(UnknownNetworkError,true). Kein Sendguard, Fehlerfilter, eigener Loop oder Steam-/Clientpatch. Fehlende/mehrfache Callsite führt ausdrücklich zum Fehler. Nach reentrantem Hoststop endet der nächste Poll vor Zugriff auf zerstörte Verbindung.

K6 unabhängig gelesen. Buildcode-01 PASS:0Fehler,2bekannteNU1900-Feedwarnungen. Bestehender kompletter Patchlauf4109 einschließlich Fixture-Desktopsubstitutionen PASS. Neue `EnetLifecycleChecks.cs` im bestehenden `--patches-only`:17Checks PASS für konkretes Patchziel,0/2Callsite-Drift, Connect/Receive/Error/empty unverändert, reale native Hostliste/HandlePacketReceived/Callback bei raw und App-Disconnect, Reasonerhalt/Idempotenz, unbekannter Peer, reentranter Stop und Exceptionweitergabe. Polling wird kontrolliert ersetzt, Peeridentität ist absichtlich null: **Modell-/Callbacktest, kein Godot-Handle-, Socket- oder Livebeleg**. Vollständige Logs/Runner-/Source-/Binarymanifeste in `code-01/`.

Fix-DLL `CFA23D616C49063DAE4B001D518F44B28B72BE27A0EA868BB7DF281487BCAFA1` über bestehendes Installationsskript installiert. PCK `1E164A2C8647096F77BF806BD1EED71CF06AD00235B315971CB101D626F43466` und JSON `1A2834DB1A10732A1B64E880E7D7077F4BE83BA81B1667AE49983996DF13455D` unverändert übernommen, kein Ressourcenexport. Kanonisches Paket nach Install wiederhergestellt; bekannte BaseLib3.4.7 für die Tests temporär bereitgestellt und anschließend entfernt. Alle 25 vorherigen Feedback-Sourcefreeze-Dateien unverändert; Abschlussmanifest unten.

## Echte Zwei-Client-Nachtests

Alle folgenden Belege liegen unter `artifacts/enet-teardown-20261027/`. Native ENet-Verbindung mit Host-ID 1 und Client-ID 2101, `--force-steam off`, Audit und Network/GameSync-Debuglogs. Zeitangaben CET; Eingabechronik `ui-timeline.jsonl`. Native Konsolenanweisung `fight TWO_TAILED_RATS_NORMAL` stellt einen gemeinsamen Kampf her. Sie überspringt Neow-Auswahlen; der gespeicherte Checkpoint bleibt Neow. Dies ist ausdrücklich kein Nachweis für Speichern mitten im Kampf oder für Karten-/Boosterbalance.

| Fall | Ergebnis | Messbarer Nachweis |
| --- | --- | --- |
| Client zuerst schließen, Host weiter bedienen und später schließen | PASS ENet-Abbau | Genau eine raw-Disconnect-Benachrichtigung mit UnknownNetworkError; Host länger als 43 s beobachtet, sauberer Kontrollpunkt nach 66,5 s; keine Send-/Nullpeerfehler; zwei identische Gameplaychecksums |
| Dieselbe Client-ID erneut am laufenden Host anmelden | PASS Handshake und Peer-Lifecycle; keine Gameplay-Rejoin-Abnahme | Host akzeptiert ID und ClientRejoinResponse; native Join-UI lehnt Running-Zustand mit RunInProgress ab; bekannte App-Reason bleibt erhalten, genau eine weitere Entfernung |
| Host neu starten, gespeicherten Lauf laden, Client-ID 2101 erneut beitreten | PASS gespeicherter Neow-Checkpoint und folgende Gameplayaktion | Beide Spieler in Load-Lobby bereit; tatsächliches Laden; danach Defend gespielt, 5 Block/2 Energie/4 Handkarten; drei identische Checksums |
| Erster Hostfirst-Lauf mit automatischem `-fastmp join` beim Client | FAIL vollständiger Logvergleich | Abbau ohne Send-/Nullpeerfehler; danach automatischer neuer Joinversuch gegen geschlossenen Host, zwei Managed-ERRORs wegen Timeout; vollständige Fehlerbelege erhalten |
| Hostfirst-Wiederholung mit manuell beigetretenem Client ohne `-fastmp` | PASS ENet-Abbau | Genau eine Client-Disconnect-Benachrichtigung; nativer UnknownNetworkError-Dialog im Hauptmenü nach 44 s weiterhin stabil; zwei identische Checksums; vollständiger bestehender Vergleich PASS |

### Clientfirst, App-Disconnect und spätes Hostende

`fixed-host.log` (PID 59796), `fixed-client1.log` (PID 25464): Checksums 0 = 1343443359 (ExitingNeow) und 1 = 3173487648 (AfterPlayerTurnStart) auf beiden Seiten gleich. Client Alt+F4 um 15:17:56.352. Host meldet genau einmal `Peer ID 2101 disconnected, reason: UnknownNetworkError`; RunLobby erhält dieselbe Trennung. Host bleibt bedienbar, Screenshot `fixed-host-after-client-quit.png`.

Erneuter Clientstart mit derselben ID: `fixed-client2-rejoin.log` (PID 50356). Kein ID-Konflikt; Host nimmt den Client an. Die native UI in `NJoinFriendScreen.cs` (historische vollständige Quellen, Zeilen 333–338) zeigt beim Running-Zustand jedoch RunInProgress und trennt den Client selbst. Screenshot `native-running-rejoin-rejected.png`. Der Host verarbeitet diese App-Trennung genau einmal und erhält den ursprünglichen Grund RunInProgress; nachfolgendes Transportereignis erzeugt keine Doppelbenachrichtigung. **Wiedereintritt in einen laufenden Kampf ist auf diesem UI-Pfad nicht unterstützt und daher nicht als erfolgreiches Gameplay getestet.**

Host Alt+F4 um 15:20:47.097. `fixed-clientfirst-checksums.txt`: bestehender vollständiger Checker PASS, zwei gemeinsame Checksums plus Reset-Prüfung. Keine neuen Managed-ERRORs, `Peer not connected` oder Nullpeerfehler. Bekannte Engine-Exitmeldungen siehe unten.

### Laden und Hostfirst: fehlgeschlagener Aufbau, erfolgreiche Wiederholung

`fixed-loadhost.log` (PID 63756), `fixed-loadclient.log` (PID 10572): tatsächlicher Host-Neustart mit `-fastmp load`, Clientbeitritt und gemeinsames Laden des Neow-Checkpoints. Screenshots `fixed-client-loaded.png`, `fixed-client-action-after-load.png`, `fixed-host-action-after-load.png`. Nach den gleichen Checksums 0/1 folgt auf beiden Seiten Checksum 2 = 236452079 für PlayCardAction DEFEND, Index 10.

Host Alt+F4 um 15:23:36.117. Client verarbeitet genau einen raw Disconnect und kehrt zum nativen Fehlerdialog zurück. `-fastmp join` wird jedoch beim neu erzeugten Hauptmenü erneut ausgeführt (nativer `NMainMenu.cs`-Pfad ab Zeile 896). Der nun geschlossene Host führt zum Timeout und zwei Managed-ERRORs. **`fixed-load-hostfirst-checksums.txt` bleibt FAIL.** Gleiche Gameplaychecksums und fehlende Peerfehler machen diesen vollständigen Lauf nicht nachträglich erfolgreich. Keine Filteränderung und keine Produktänderung für diese automatische Teststartfolge.

Wiederholung: `fixed-hostfirst-manual-host.log`, `fixed-hostfirst-manual-client.log`. Client ohne `-fastmp`, tatsächlicher Beitritt über Multiplayer → Join, gemeinsames Laden, anschließend nativer Kampf mit zwei gleichen Checksums. Host Alt+F4 um 15:27:10.379. Genau eine Client-Disconnect-Benachrichtigung, nativer UnknownNetworkError-Dialog; stabiler Zustand bis 15:27:54.349, danach reguläres Schließen des Clients. Screenshot `fixed-manual-hostfirst-client-after-44s.png`. **`fixed-hostfirst-manual-checksums.txt`: vollständiger bestehender Checker PASS**, zwei gemeinsame Checksums plus Reset-Prüfung, null Managed-ERRORs und null Send-/Nullpeerfehler.

## Vollständige Fehlerbilanz und Abnahmegrenzen

`log-summary.json` enthält SHA256 und getrennte Zähler aller neun vollständigen Logs. Jeder Prozess erzeugt weiterhin zwölf bekannte native Engine-Ressourcenmeldungen beim Exit, auch ohne Mods. Diese sind nicht behoben und keine saubere Engine-Shutdown-Abnahme. Das ENet-PASS beschreibt ausschließlich den belegten Verbindungsabbau; der ursprüngliche historische FAIL bleibt unverändert in `docs/FEEDBACK-WAVE-20261027.md` bestehen. `scripts/Compare-CoopChecksums.ps1` wurde nicht geändert; keine gekürzten Logpräfixe ersetzen vollständige Logs.

| Abnahmebereich | Status und Grenze |
| --- | --- |
| Quellanalyse der aktuellen Spiel-DLL | PASS: fehlender raw-Disconnect-Zweig nachgewiesen, unabhängig per IL bestätigt |
| Modell-/Callbackvertrag | PASS: 17 gezielte Checks; ersetzt Polling, kein echter Socket-/Handlebeleg |
| Patchaktivierung und Build | PASS: 4109 aktivierte Methoden einschließlich Fixture-Substitutionen; Build ohne Fehler, zwei NU1900-Warnungen |
| Zwei lokale echte ENet-Clients, beide Schließreihenfolgen | PASS für die bezeichneten vollständigen finalen Läufe |
| Native App-Reason und Peer-ID-Wiederverwendung | PASS in den beschriebenen Running-Abweisung-/Load-Lobby-Pfaden |
| Echter Save/Load | PASS für gespeicherten Neow-Checkpoint und nachfolgende synchronisierte Aktion; kein Midcombat-Persistenznachweis |
| Engine-Ressourcenfreigabe beim Beenden | OFFEN: zwölf bekannte Meldungen pro Prozess weiterhin vorhanden |
| 3/4 Spieler, Steam/Internet, künstliche Latenz/Paketverlust, gleichzeitige Mehrfachabbrüche | NOT TESTED in diesem Auftrag |
| Laufender Kampf über native Join-UI wieder betreten | NICHT UNTERSTÜTZT auf geprüftem Pfad; keine erfolgreiche Gameplay-Rejoin-Abnahme |
| Breite Inhalts-/UI-/Balance-/Releaseabnahme | NOT TESTED durch diese Transportkorrektur |

## Abschluss und Wiederherstellung

`cleanup-result.json`: 28 eigene Profildateien wiederhergestellt, sieben zusätzliche Testprofildateien archiviert, 859 persistente Ausgangsdateien hashgleich geprüft. Flüchtige Sentry-Enginediagnosen sind von der persistenten Profilgleichheit ausgenommen. Temporäre BaseLib entfernt, null verbleibende Spielprozesse. Keine Benutzerdateien gelöscht.

`final-integrity.json`: installierte DLL/PCK/JSON entsprechen den oben genannten Hashes; PCK und JSON bytegleich zum frischen Ausgangsstand. Kanonisches Paket und Workshop hashgleich zur Baseline; keine neuen persistenten Benutzerdateien. Drei ENet-Quell-/Testdateien und 25 vorherige Feedback-Sourcefreeze-Dateien unverändert gegenüber ihrem jeweiligen Prüffreeze. Die Fix-DLL bleibt lokal installiert; kein Versionswechsel, Git- oder Releasevorgang.

K6-Änderungen dieses Auftrags: `tests/LaRata.ContentChecks/EnetLifecycleChecks.cs`, dessen Aufruf in `Program.cs` und dieser Bericht. Produktdatei `EnetHostLifecyclePatch.cs` ausschließlich durch K3. Andere vorhandene Änderungen einschließlich `Ideen - La Rata.md` nicht bearbeitet. Exklusives Desktop-/Build-/Installationsfenster nach Übergabe an K1 wieder frei.
