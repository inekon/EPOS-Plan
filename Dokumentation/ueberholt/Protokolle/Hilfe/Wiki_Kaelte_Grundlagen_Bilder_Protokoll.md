# Wiki: Kälteerzeugung, Grundlagen, Brauchwasser und Prozesswärme, Bilder (Protokoll, 10.10.2026)

Statuszeile #933 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Logbuch-Sätze im Abschnitt „Version offen“ von
[`Wiki_Update_2026-09-26.md`](../../../aktuell/Wiki_Update_2026-09-26.md); Konzept
[`Konzept_Hilfesystem_Wikidokumentation.md`](../../../aktuell/Konzept_Hilfesystem_Wikidokumentation.md) (Abschnitt 14.3 „PNG-Bilder“). Zweige
`claude/wiki-kaelte` und `claude/wiki-sammel` (Sammelstand `49a174c8`), Vormerge mit `origin/ios_migration_september` auf `0be44a78`.

## Auftrag

Anwenderauftrag im Wortlaut: „In der Wiki ist die Kälteerzeugung nicht unter Programmdokumentation – Erzeuger und Speicher. Suche Stellen in der Wiki,
die nicht mehr mit dem aktuellen Stand übereinstimmen und aktualisiere sie – ebenfalls die Grundlagen. Versuch möglichst auch Erklärungen mit grafischen
und symbolischen Veranschaulichungen zu versehen. Nehme auch Screenshots aus EPOS-Plan wenn sinnvoll.“ Erweiterung: „Außer Energiebedarf Gebäude sollte
auch Prozesswärme und Brauchwasser besser erläutert werden, insbesondere auch die Konfiguration und die hydraulische Einbindung.“

## Befund

Die Kälte war vollständig dokumentiert, lag aber auf der Seite „Kühlung“ (Bedarf) statt unter „Erzeuger und Speicher“. Viele Seiten nannten alte Stände:
Anlagenkopplung AK2, den Schalter unter „Weitere Einstellungen“, keine Kältemaschine in den Grundlagen.

## Wellen

| Welle | Commit | Inhalt |
|---|---|---|
| W1 | `bcbc0e9a` | neue Seite „Programm Dokumentation – Kälteerzeugung“; „Kühlung“ mit elf Anker-Stummeln; Navigation; Energieerzeuger AK2 → AK1/AK3; Grundlagen Kühlung mit Kältemaschine; 2 SVG; `help_mapping.txt` |
| W2 | `2653b805` | Rechenwegseite `EPOS.Kern/Allgemein/Hilfe/Berechnung/Kühlung.wiki` mit 27 Gleichungen, `_Index`, `_Bezuege`, Simulationsablauf; vier Rechenweg- und drei Grundlagenknöpfe in den Kältedialogen; Wachen |
| W3 | `12dffe23` | Simulation, Simulationsergebnisse, Simulation konfigurieren, Ergebnisdarstellung, Ergebnisse auswerten, Datenexport; Anker herleitung-uebergabe, anlagenfahrplan, kopplungskennzahlen |
| W4 | `1b933f61` | Gerätekataloge (Anker katalogauswahl), Heizkessel, BHKW, Wärmepumpe, Pufferspeicher, Stromspeicher, Solarthermie, Photovoltaik |
| W5 | bis `274ceaf8` | Gebäude, Mehrzonenmodell, Nutzungsprofile, Betriebskalender, Wärmebedarf erfassen, Gebäude und Gebäudetypen |
| W7 | `12d01daa` | fünf neue Grundlagenseiten (Gebäudemodell VDI 6007, Bauteile am Erdreich, Brauchwasser-Zapfprofile, Gemeinjahr und Feiertage, Anlagenkopplung und Regelung) mit 5 SVG; Grundlagen.wiki, Navigationsvorlage |
| W8 | `d396fdd5` | elf Leitseiten: Über EPOS-Plan, FAQ, Hilfe-Assistent, Strombedarf erfassen, Stammdaten, Erste Schritte, Wirtschaftlichkeit, Kosten und Energiepreise, Varianten und Bericht, Installation und Update, Beispiele |
| W9 | bis `d28972fd` | Brauchwasser, Prozesswärme, Brauchwasser-Zapfprofil, Grundlagen Hydraulikschemata; neue Grundlagenseiten Brauchwasser und Prozesswärme; 4 SVG |

Sammelmerge `b2d7751c` bis `49a174c8` mit drei Nachzügen: `888f55cc` (Zapfprofil-Verweise, Navigation Brauchwasser/Prozesswärme), `b14d9ae4`
(iPad-Sätze in Über EPOS-Plan und FAQ: genannt, noch nicht allgemein verfügbar), `3a6ee45f` (20 Kälteanker auf die Seite Kälteerzeugung umgestellt).

## Querbefunde

- Die Kältemaschine ist nicht an Kühltage gebunden (Befund der Rechenwegseite, W2); Hinweis für die Fachsitzung, keine Änderung hier.

## Vormerge und CI-Befund

- Vormerge mit origin `0be44a78` als `a1cab1f5`, vier Konflikte: Knopf „Berechnung: Kühlung“ im Baustein `WaermepumpeKuehlbetriebGruppe`; Grundlagen- und
  Berechnungsknopf im Bereich „Kälte“ der `SimulationKonfigSeite`; KB-D2-Text zur Kältefolge/Vorgabefolge auf die Seite Kälteerzeugung; Simulation.wiki
  Kältebahn. Dazu `Grundlagen - Kühlung.wiki` und `Simulation konfigurieren und starten.wiki` auf den Bereich Kälte nachgezogen.
- CI: Sitzungszweig-Lauf 38087580557 auf `f7d192f3` rot mit zwei fremden Tests. Behoben auf origin durch die Sitzung Kälteanlagen (`d21de06f` BOM, `055e7f63` Kältefolge); der gleichlautende Fix dieser Sitzung (`e94ed8a1`: BOM für `EPOS.Kern/Allgemein/Import/GanglinienProbe.cs`
  (Commit `7ac46da5`, IM-2). `646d4eac`: Kaskaden-Regression; Ursache `c4038da3`/`0e45d971` (KB-A): `Kaeltefolge` las die Kaskade über
  `KonfigurationCtrl.LiesProjekt`, der beim Lesen den Heizkessel nachzieht; neuer reiner Leser `LiesProjektOhneNachziehen`, `HeizkesselKaskadeTests` unverändert) entfiel beim Merge zugunsten der origin-Fassung.

## Bildwelle

- **W6a** `089b3447`: zwölf Beispieldiagramme aus `Proben/ChartProben` als `Diagramm_*.png` in zehn Seiten. `35cc5e83`: Konzept Hilfesystem 14.3, Absatz
  „PNG-Bilder“; neue Wache `EPOS.Kern.Tests/WikiBilderWacheTests.cs` mit fünf Prüfpunkten.
- **W6b** `6f3e9ceb`: Wirt-Seite `/wikibild`, `Wikibeispiele.cs`, Skript `Proben/Rasterprobe/wikibilder.mjs`. `acd5fee8`: 14 Bildschirmfotos
  `Bildschirmfoto_*.png` (Heizkessel, BHKW, Wärmepumpe, Pufferspeicher, Stromspeicher, Photovoltaik, Solarkollektoren, Brauchwasser, Prozesswärme,
  Gebäudedaten, Kältemaschine, Konditionierung, Zonen, Zonendialog). Acht Platzhalter entfernt: Anlagenschema (2), Nutzungsprofile, Ergebnis mit
  Reiterleiste, Bedarfsdialog gekoppelt, WP-Konfiguration mit Herleitung, Wärmequelle Erdreich.
- **W6c** (auf `claude/wiki-kaelte`; Commits `7e71e757` Dialogfix mit Test, `18465df2` drei Bilder neu): Die Satzzeile des Bedarfsdialogs zeigt bei gewählter Zeile den Satznamen (Fix in
  `BedarfsProfileDialog` mit bunit-Test); drei Bilder neu aufgenommen.

## Entscheide des Anwenders

- „Lade auch PNG hoch“ (Bilder gehen mit dem Upload ins Wiki).
- Die Versionsnummer für das Logbuch bleibt gleich (Abschnitt „Version offen“).
- iPad-App: noch nicht verfügbar, aber erwähnen.

## Trockenlauf des Upload-Werkzeugs

9 neue Seiten (Grundlagen Brauchwasser, Prozesswärme, Gebäudemodell VDI 6007, Bauteile am Erdreich, Brauchwasser-Zapfprofile, Gemeinjahr und
Feiertage, Anlagenkopplung und Regelung; Programm Dokumentation Kälteerzeugung; Berechnung/Kühlung), 61 geänderte Seiten, 11 neue SVG, 26 neue PNG.

## Abnahme

Gate 933: Gate 931 auf 646d4eac grün: Kern-Filter 0 Fehler, ChartProben 222 Hashes gleich Messlatte_2026-10-10, Tests KiKern 553, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen), UI 8478, Kern 13303 (7 übersprungen), Doku-Wachen 35, Referenzlauf 29 von 29 Projekten PASS (10036768 Werte innerhalb der Toleranz), CSV byte-gleich 943/943, Plattformnachweis (--stoerung ulp) PASS 29 von 29; Nachtest auf 9ce75a0b: UI 8480/8480, Kern-Auswahl 296/296, Build 0 Fehler. CI: CI-Vermerk ⟨folgt nach dem Push⟩.

## Offen

- Upload nur auf Zuruf des Anwenders; Zugangsdaten werden dann erfragt.
- Sichtabnahme der Bilder durch den Anwender.
- Nicht abgebildete Masken (acht entfernte Platzhalter, siehe Bildwelle).
- Befund „Kältemaschine nicht an Kühltage gebunden“ für die Fachsitzung.
- Schönheitsfehler des Prüfstands `Proben/ChartProben`: Diagrammproben mit „Waerme“ ohne Umlaut und Fußzeile „Paket iU7-3“.
