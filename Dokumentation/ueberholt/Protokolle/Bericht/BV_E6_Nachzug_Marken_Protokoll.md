# BV-E6 Nachzug — Platzhaltermarken an den fehlenden Stellen (Protokoll)

Nachzug zur Kennzeichnung in der App ([`BV_E6_Kennzeichnung_Protokoll.md`](BV_E6_Kennzeichnung_Protokoll.md)) nach dem
Abschluss der Berichtsvorlagen ([`BV_E9_Abschluss_Protokoll.md`](BV_E9_Abschluss_Protokoll.md)). Auftrag #581,
Anwenderauftrag vom 26.09.2026 „eigener auftrag“ auf die Frage, ob die fehlenden Stellen nachgezogen werden sollen. Der
gültige Stand steht in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md) und in der Wiki-Quelle
„Berichtsvorlagen“; hier steht, wie es geworden ist. Zweig `claude/intelligent-bohr-hthrk8`, ein Opus-5.5-Agent im
Worktree ab `805add7e` (4 Commits), Zusammenführung, Gate und Papiere durch Opus 5.5. Kein Schemaschritt, kein
Rechenweg berührt (Referenzlauf GESAMT: PASS), keine Referenzbasis neu eingefroren, `EPOS.iOS/` nicht berührt.

| Teil | Gegenstand | Commits |
|---|---|---|
| Katalog v10 | zehn Ergebnisbilder `stand.bild.*` samt `hat.bild.*`, Speicherlauf `stand.speicher.*` und `stand.hat_speicherlauf`, `stand.solarthermie.deckung` (30 Schlüssel) | `ef888e71` |
| Marken | Ergebnisreiter, Autarkie, Stromspeicher, Positionsform, „nur Excel“, Ortstabelle, Wachen | `080d8190` |
| Vorlagen | zehn mitgelieferte Vorlagen mit `alle` auf Fassung 10 neu erzeugt | `c04045bc` |
| Wiki-Quelle | Abschnitt Platzhalter in der App | `762b0635` |
| Bildhöhe | feste Höhe der Fußnotenzeile der Verlaufsbilder | `d75bab47` |
| Zusammenführung | Merge des Agentenzweigs und von `origin` | `3eff2d4f` |

## 1 Stellen

- **Simulation › Ergebnis:** Bedarf (Wärme, Strom, Kälte), Wärmepumpe (Produktion, Stromverbrauch, Streuwolke),
  Heizkessel, Solarthermie, BHKW, Photovoltaik tragen neue Bildschlüssel `stand.bild.*`; Wärme- und Stromgang die
  vorhandenen. Die Bilder entstehen im Bericht aus dem Zeitreihensatz mit denselben Aufrufen, Titeln und Farbrollen wie
  in der App (`Berichtsbilder.Ergebnis.cs`), in Excel als Diagramm — die Streuwolke nur in Word, Excel kennt keine
  Punktwolke. Die Marken heißen „ähnlich im Bericht“: Der Bericht zeigt alle Reihen ohne Schalter und Sortierung.
- **Autarkie:** PV-Autarkie, solare Deckung und Monatsstapel; **Stromspeicher:** acht Kacheln des Speicherlaufs.
  Kapazität, Leistung und Ladebereich bleiben ohne Marke (Kenndaten, kein Laufergebnis).
- **Positionsform:** Die Aufklappung einer Marke an einem Standwert nennt zusätzlich `{{stand.<n>.…}}` bzw.
  `{{variante.<n>.…}}` zum Kopieren; die App zählt alle Varianten der Gruppe, ein Bericht mit Teilauswahl nur die
  gewählten — der Hinweis sagt es.
- **Reine Excel-Tabellen:** fünf Marken mit dem Hinweis „nur Excel“ (Parameternachweis, Kapitalwertverlauf,
  Gegenüberstellung, Kennzahlenliste, Monatswerte).
- **Benannte Ausnahmen** der Abdeckungswache: CO₂-/Speichernutzen-Kachel, Wärme-Autarkie-Monatsbild, Kältering und zwei
  ältere der Übersicht.

## 2 Katalog und Vorlagen

`KATALOGFASSUNG` und `KatalogfassungWord` stehen auf 10 (neue Liste `Vorlagenfeldkatalog_v10.txt`). Die ausführliche
Vorlage trägt im Block je Stand die zehn Bilder unter `#wenn hat.bild.<name>`, die Speicherkennwerte unter
`#wenn stand.hat_speicherlauf` und die solare Deckung; die Bausteinvorlage alle neuen Schlüssel und die Beispiele der
Positionsform. Beispiel- und Standardvorlage tragen nur die neue Fassung, nicht die neuen Schlüssel — sie sind reine
Kapitelvorlagen, und neue Bilder dort würden den Standardbericht aller Anwender verändern.

## 3 Bildhöhe der Verlaufsbilder

Befund der Sitzungen „Dialoge und Korrekturen“ (#580) und „Zapfprofil“: Unter Windows waren drei Berichtstests der
Gruppe rot, Bild 323 statt 322 px. Die Höhe der umbrochenen Fußnote hing an der gemessenen Zeilenhöhe der Schrift
(Calibri unter Windows, Carlito unter Linux). Mit der festen Höhe `FUSS_ZEILE_VERLAUF` = 24 ist das Bild auf beiden
Plattformen gleich hoch; die Linux-Messlatten bleiben unverändert. Der Nachweis unter Windows steht beim Anwender.

## 4 Abnahme

Gate auf `3eff2d4f`: Kern-Filter und Windows-Schale 0 Fehler, Designer wiederholbar, SQL-Dialekt-Prüfer 2.025 Texte
ohne Fundstelle, ChartProben 220/0, Referenzlauf 1030, 1007, 1017, 1045, 1046, 1047, 1049 gegen R22 GESAMT: PASS
(2.497.845 Werte), voller Lauf 16.400 bestanden, 1 Fehler (`TwwKatalogWacheTests.Das_Einspielskript_ist_wiederholbar`,
Python 3.11 im Container), 2 übersprungen.

## 5 Offen

Siehe „Nach #581“ in der Statusdatei.
