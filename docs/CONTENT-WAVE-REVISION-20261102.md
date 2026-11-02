# La Rata Remix 0.9.6 – Ergänzung der Content-Welle vom 2. November 2026

Status: Umsetzung durch K2/K3/K4/K5 und gezielte gemeinsame technische Abnahme durch K6 abgeschlossen (PASS). Bericht: CONTENT-WAVE-REVISION-QA-20261102.md. Spieltests und menschliche Bildabnahme bleiben offen. Die vorherige Abnahme in `CONTENT-WAVE-QA-20261102.md` bleibt historische Evidenz für ihren dokumentierten Kandidaten. Die lokale Installation wurde anschließend ausdrücklich beauftragt und durchgeführt; die Versionsmetadaten wurden auf ausdrückliche Nutzerkorrektur auf 0.9.6 gesetzt und lokal installiert. Spieltests übernimmt der Nutzer. Keine Veröffentlichung beauftragt.

## Karten – K2

- Cinder Receipt: Upgrade 15 Vigor.
- Cleanse: sichtbarer Name Royal Cleansing; technische ID bleibt erhalten.
- Scrap Coronation: zusätzlich 1 Draw in beiden Versionen.
- Ser Kev's Thorn Buster: Grundkarte 1 Draw und 2 Vulnerable; Upgrade 2 Draw und 3 Vulnerable. Kosten 1, Exhaust und Bedingung des verlorenen Thorns bleiben.
- Sewer Pact: Upgrade je 8 Poison und Doom.
- Smuggler's Dividend: Vigor bei jedem Discard, keine First-time-Grenze.
- Venomous Encore: Doom 5/10; sonstige Effekte bleiben.
- Dead Letter: sichtbarer Name Death Letter, kein Block, Kosten 2/1; übrige Effekte bleiben.
- Ashen Overture: zusätzlich Draw 3 in beiden Versionen.
- Last Laugh: Upgrade 5 Block und Draw 2; Grundkarte unverändert.
- Booster: tatsächliche Royal-Prize-Erzeugung, Auswahl und Übernahme prüfen; Fehler beheben, keine ungefragte Änderung der Wahrscheinlichkeiten.

## Relikte – K3

- Stairwell Key ersetzt seinen alten Effekt vollständig: “The first card you play each turn costs 1 less.”
- A Thousand Cuts: “You may remove any number of cards at the Merchant always at base price”. Nur Textänderung; bestehende Festpreisregel bleibt.
- The Relic Remover: Uncommon.

## Boss – K4 mit K2-Katalogänderung

- Black Tithe (20), Grave Spark (26), Sewer Pact (28) aus dem Bossdeck entfernen. IDs und Spielerfassungen erhalten.
- Cleanse / Royal Cleansing (25) ebenfalls aus dem Bossdeck entfernen: bereits im ursprünglichen Wellenauftrag ausdrücklich verlangt. Die reguläre Spielerkarte bleibt erhalten.
- Die bisherigen Ausschlüsse Sewer Alchemy (3) und Plague of Crowns (21) bleiben. Danach 42 aktive Bosskarten bei stabilen 48 IDs.
- Exposed Crown: nominell 25 Bossblock nach der bestehenden Bosskarten-Blockskalierung; Beschreibung und Intents stimmen überein. Normale Kampfeingriffe unverändert.
- Keine pauschale Übertragung geänderter Spielerwerte auf Bossadapter, keine allgemeine Bossbalanceanalyse.

## Artwork und Darstellung – K5

- Neue Artworks für Royal Cleansing, Scrap Coronation, Smuggler's Dividend, Pawn Promotion und The Relic Remover.
- Pawn Promotion ohne Schachbauern im Motiv.
- Nach jüngster Nutzerpräzisierung: Zehn spielbare Royal-Prize-Varianten erhalten den rötlichen Boss-Seltenheitsbanner statt ihres normalen Banners. Normaler Kartenrahmen und tatsächliche Rare-Seltenheit bleiben. Das zuvor entworfene grün-schwarze Spezialdesign ist verworfen.
- Historische veröffentlichte Motive sind für den Content-Katalog unter `docs/content-catalog-art` gesichert. Bestehende visuelle Freigaben sind keine Freigaben der neuen Bilder.

## Integration – K1/K6

- Englisch, Deutsch und vereinfachtes Chinesisch synchron halten.
- Technische IDs, Save-Kompatibilität und stabile Bossindizes erhalten.
- K1 aktualisiert den Entwicklungsabschnitt des bebilderten Content-Katalogs; veröffentlichte Abschnitte bleiben historische Referenz.
- K6 prüft nach Abschluss gezielt die neuen Verträge und Integration, exportiert gegebenenfalls einen separaten Kandidaten. Bisherige QA-Evidenz und veröffentlichte/installierte Artefakte bleiben erhalten.
- Spieltests, menschliche Bildabnahme, Git- und Release-Aktionen sind kein Bestandteil dieser technischen Ergänzung. Die lokale Installation folgte als ausdrücklicher Folgeauftrag.

## Versionskorrektur und lokale Installation

Projekt/Manifest: 0.9.6. Assembly-/FileVersion: 0.9.6.0. Nur Versionsmetadaten geändert, PCK bytegleich zum geprüften Revisionskandidaten. Lokale Installation gegen Kandidat-Hashes verifiziert; keine erneuten Spieltests. Nachweis: `artifacts/content-wave-20261102/revision/version-0.9.6/evidence/install-result.json`. Frühere QA-Berichte behalten ihre ursprünglichen Kandidathashes.

## Nachkorrektur: Seltenheitsbanner der spielbaren Bosskarten

Nutzerentscheidung: Das besondere grün-schwarze Royal-Prize-Rahmendesign wird zurückgenommen. Nach ausdrücklicher Präzisierung ersetzt die rötliche Bossdarstellung den normalen Seltenheitsbanner ausschließlich der zehn spielbaren Bossvarianten. Kein Portrait-only-Sonderpatch. Der normale Kartenrahmen und die tatsächliche Seltenheit Rare bleiben unverändert. K5 hat die native Bannerbindung umgesetzt; K2 hat den tatsächlichen Erwerb aus La-Rata-Boostern für alle zehn Varianten gegen eine bytegleiche Kopie der installierten 0.9.6-DLL erneut bestätigt. Die gezielte gemeinsame technische Prüfung und lokale Aktualisierung durch K6 sind abgeschlossen; lokaler Stand 0.9.6. Die tatsächliche Darstellung prüft der Nutzer im Spiel. Keine neuen Artworks oder Änderungen der Boosterwahrscheinlichkeit beauftragt.

Finaler Bannerumfang: `docs/art-prompts/content-wave-20261102/revision/banner-correction/handoff.json`. Booster-/Poolnachweis: `artifacts/content-wave-20261102/revision/portrait-border/k2/report.json`. Alle zehn Varianten stehen im separaten Royal-Prize-Unterpool; ein 20-%-Wurf ersetzt eine der fünf Hauptkarten. Die zwei neutralen Zusatzkarten sind davon getrennt.

Bannerkorrektur lokal installiert und Hashvergleich bestanden. Kandidat: `artifacts/content-wave-20261102/revision/boss-banner/LaRata`. Prüf-/Installationsbericht: `artifacts/content-wave-20261102/revision/boss-banner/qa/REPORT.md`. Der verworfene Portrait-only-Zwischenstand wurde nicht installiert.
