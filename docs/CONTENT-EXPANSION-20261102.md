# La Rata Remix 0.9.6 – zusätzliche Karten und Tränke

Vom Nutzer nach Entwurfsprüfung mit „welle los“ zur Umsetzung freigegeben. Status: umgesetzt, gezielt technisch geprüft und lokal als **0.9.6** installiert. Bestehende Agenten K2/K3/K5 haben Inhalt und Bilder geliefert; K6 hat Integration, Ressourcenexport und Installation abgeschlossen. Keine Veröffentlichung, Git-Aktion oder Spieltests. Nutzer prüft das Spiel selbst.

## Fünf neue reguläre Spielerkarten – K2

Alle fünf gehören zum regulären La-Rata-Spielerangebot, nicht zum Bossdeck oder zum besonderen Boss-Prize-Unterpool. Keine neuen Bossadapter/IDs. Die bisherigen 48 Boss-IDs und 42 aktiven Bosskarten bleiben erhalten.

| Karte | Seltenheit / Typ / Energie | Grundkarte | Upgrade |
| --- | --- | --- | --- |
| Static Tribute | Uncommon / Attack / 1 | Deal 7 damage. Trigger your Orbs’ passive abilities. | 10 damage |
| Gilded Coffin | Uncommon / Skill / 1 | Lose all Block. Apply that much Doom. | Lose all Block. Apply that much Doom to ALL enemies. Kosten weiterhin 1. |
| Bonfire Sale | Rare / Skill / 1 | Exhaust ALL Attacks in your Discard Pile. Gain 1 Energy for each card Exhausted. Exhaust. | Kosten 0 |
| Bloodsmith | Rare / Power / 2 | Whenever you lose HP, Forge 3. | Forge 5 |
| Under the Table | Rare / Skill / 1 | Play the bottom card of your Draw Pile twice. Exhaust. | Dreimal statt zweimal |

- **Gilded Coffin:** ausdrücklicher Bossausschluss auch für die Zukunft. Einmal erfasster eigener Block wird entfernt; das Upgrade wendet die vollständige Menge auf jeden Gegner an, nicht aufgeteilt.
- **Bloodsmith:** kein „on your turn“. Tatsächlicher HP-Verlust triggert auf beiden Seiten, einmal je Ereignis und nicht je HP. Blockverlust/verhinderter Schaden zählen nicht.
- **Under the Table:** dieselbe einmal bestimmte unterste Karte wird mehrfach gespielt, ohne permanente Kopien. Native AutoPlay-/Replay-Regeln anwenden.
- **Bonfire Sale:** nur tatsächlich erschöpfte eigene Angriffe aus dem Abwurfstapel geben Energie.

## Drei neue gemeinsame Kampftränke und Rostwasser – K3

| Trank | Seltenheit | Text |
| --- | --- | --- |
| Brass Polish | Uncommon | Gain Vigor equal to your Block. |
| Grave Distillate | Rare | Swap an enemy’s Poison and Doom. |
| Amber Hourglass | Uncommon | Block is not removed at the start of your next 2 turns. |
| Rustwater / Rostwasser (bestehend) | Uncommon, unverändert | Remove ALL Block and Plating from an enemy. |

Brass Polish verbraucht keinen Block. Grave Distillate erfasst beide Mengen einschließlich 0, entfernt die beiden Powers nativ und wendet zuerst Poison, dann Doom mit den vertauschten Mengen nativ an. Gleiche Mengen bleiben unverändert. Artifact, Immunitäten und Giftmodifikatoren gelten weiterhin und können das Ergebnis verändern; keine Umgehung der nativen Hooks. Amber Hourglass nutzt die native Blur-Power. Rustwater behält sein Artwork.

## Artwork und Lokalisierung

K5 erstellt acht neue Einzelartworks, Karten ohne Schrift/Rahmen und Tränke mit Transparenz und gut lesbarer Silhouette. Motive entsprechen den genehmigten Entwürfen; kein neues Frame-/Bannerdesign. Noch keine menschliche Bildabnahme.

Nach dem ersten Art-Freeze hat der Nutzer direkt bei K5 vier vollständige Neuentwürfe beauftragt: `static_tribute`, `gilded_coffin`, `bloodsmith` und `amber_hourglass`, gerne im klassischen Schwarz/Grün-Look und mit Ratten nur dort, wo sie zum Motiv passen. Die übrigen vier neuen Artworks bleiben erhalten. Finale Installation erst nach diesem begrenzten neuen Art-Freeze; Codechecks bleiben gültig, Ressourcenexport und Bildprüfung werden aktualisiert.

Dieser zweite Art-Freeze ist abgeschlossen. Static Tribute zeigt eine Blitzkrone, Gilded Coffin einen versiegelten Goldsarg, Bloodsmith einen seitlichen Rattenschmied; Amber Hourglass erhält auf den weiteren direkten Nutzerwunsch ausdrücklich Lavendelglas und Honigbernstein statt Schwarz/Grün. Herkunft und archivierte Vorgänger stehen unter `docs/art-prompts/content-expansion-20261102/redesign/`. Die menschliche Abnahme der neuen Bilder bleibt offen.

Kartenkeys: `static_tribute`, `gilded_coffin`, `bonfire_sale`, `bloodsmith`, `under_the_table` unter `assets/LaRata/images/cards/`.

Trankkeys: `brass_polish`, `grave_distillate`, `amber_hourglass` unter `assets/LaRata/images/potions/`.

K2 besitzt Kartentexte und eigene Powertexte in EN/deu/zhs, K3 Tranktexte in EN/deu/zhs. K1 aktualisiert den bebilderten Content-Katalog nach dem Freeze.

## Abnahme und Installation

Gezielte native technische Checks für die neuen Effekte, Upgrades, Owner/Ziele und relevante Mehrspielergrenzen; keine unnötigen Wiederholungen unbetroffener Suites. Danach separater Kandidat unter `artifacts/content-expansion-20261102/`, offizieller Ressourcenexport und Hashprüfung. Lokale Installation erst nach Prozessprüfung, mit reversiblem Backup. Kein aktives Spiel zwangsweise schließen.

Erwarteter Gesamtbestand: **46 Spielerkarten, 22 Relikte, 6 Tränke**. Die Versionsnummer bleibt **0.9.6**. Frühere QA-/Kandidaten, veröffentlichte Workshop-/Release-Artefakte, Einstellungen, Spielstände und `Ideen - La Rata.md` bleiben erhalten.

## Ergebnis

- PASS: gemeinsamer Release-Build; 204 gezielte Kartenassertions und 241 Trankchecks; 2.130 native Textformatierungen über EN/deu/zhs; 157 Modellpfade und 190 gepackte Ressourcen. Keine neuen Abhängigkeiten. Bestehende Nullable-Warnung und nicht erreichbare NuGet-Auditdaten im Buildbericht ausgewiesen.
- PASS: finaler offizieller Ressourcenexport einschließlich der vier neuen Ersatzmotive; alle acht ausgewählten Originale stimmen mit den Quelldateien überein. Der erste Zwischenexport wurde nicht installiert.
- PASS: Installation unter `E:/SteamLibrary/steamapps/common/Slay the Spire 2/mods/LaRata`, Version 0.9.6 / Assembly 0.9.6.0. K1 hat die Übereinstimmung aller drei Dateien zusätzlich unabhängig geprüft.
- DLL SHA-256: `e63b82969f12b09a6975bb4e9f9e6b1c3a09e54c7c33ce09ea3aaa87d7952d32`.
- PCK SHA-256: `fae2454dc3190c157dbd808dc7f6534fda548fecc96a684b08b5cbc50aa3244b`.
- Manifest SHA-256: `696eaa64a8aeb065f006038e84a05a3e4a0c07c4c48c9a90d788297d892931e4`.
- Vorherige Installation unverändert gesichert unter `E:/SteamLibrary/steamapps/common/Slay the Spire 2/.larata-dev/backups/content-expansion-20261102T044913Z/LaRata-0.9.6-before-expansion`.
- Bebilderter Katalog ergänzt in `docs/CONTENT-CATALOG-20261102.md`, Abschnitte 5.7–5.8; native Kartentexte aus der finalen DLL, 139 gültige Bildverweise, frühere Abschnitte erhalten.
- NOT TESTED: tatsächliche Spieloberfläche, menschliche Artwork-Abnahme, Live-Mehrspielerpartien und vollständige Run-Speichern/Laden-Sitzungen. Kein Spiel gestartet. Native Paket-/Zustandschecks sind kein Ersatz für diese Abnahmen.

Vollständige Evidenz und Grenzen: `artifacts/content-expansion-20261102/qa/REPORT.md`. Die Codeprüfungen wurden für den reinen Austausch der vier Bilder nicht wiederholt.
