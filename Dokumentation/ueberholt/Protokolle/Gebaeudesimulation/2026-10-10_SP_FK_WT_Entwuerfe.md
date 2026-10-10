# Protokoll SP, FK, WT, Entwürfe AH/KB/KK und Gate (10.10.2026)

**Sitzung:** Gebäudesimulation, Zweig `claude/magical-bohr-ff5ufj`, Statuszeilen **#911** bis **#914**. Rechenweg unverändert; Basis R51 eingefroren mit #912.

## 1 SP — Statuspflege Gebäudesimulation (#911)

Anwenderauftrag 10.10.2026 „Status und Papiere“. In `Status_Gebaeudesimulation_VDI6007.md` sind 16 Zeilen mit Beleg nachgeführt (E9, E12, E50, E58, E59, E60, E67, E109, E110, E116; Stufen NP, KP3, G6c, KM3, UB), der Kopf trägt „Stand 10.10.2026“. E76 bleibt offen, G4 bleibt „teilweise“ (offen nur die Windows-Sichtabnahme). Sieben Papiere liegen per `git mv` unter `Dokumentation/ueberholt/`: Entwurf AK3, Entwurf AK3-K, Entwurf KK, Entwurf KP3, Übergabe KP3, Übergabe 22.09. und Übergabe 26.09. (Anlagenkopplung/Mehrzonen). 103 Verweise nachgezogen, Index nachgeführt. Commits `06ff2165`, `82635e47`.

## 2 FK — Regressionsnetz der freien Kühlung (#912)

Referenzprojekt 1064 als Kopie von 1017 (einzige einfache Wärmepumpe im Kühlbetrieb an der Erdsonde 5 × 120 m, Kühlvorlauf 18 °C) mit freier Kühlung an der Anlagenzeile 24180: `Kuehl_Frei` 1, `Kuehl_Frei_Graedigkeit_K` 4,0, `Kuehl_Frei_Leistung_kW` 4,0. Saat-Skript `Referenzlaeufe/Skripte/referenzprojekt_1064_freie_kuehlung.py` (9 390 Zeilen in 25 Tabellen; Gegenprobe gegen `ProjektDuplizierenCtrl` zellgleich bis auf die Saat). Testdatenbank 95 629 312 Byte, SHA-256 `3b9097b6…`, Schemastand 210.

Jahreswerte 1064 gegen 1017: Kälte 4,05 MWh (100 %), freie Kühlung 125 h und 0,277 MWh (35 h an der Grenze), Kältestrom 0,740 statt 0,773 MWh, EER-Jahreswert 5,48 statt 5,24.

Wache `EPOS.Kern.Tests/FreieKuehlungReferenzprojektWacheTests`; zwölf Zähltests nachgezogen (u. a. `PreisbasisSchrittTests`, `ReferenzprojektKaelteerzeugerTests`, `GebaeudeKatalogverweisTests`). Basis **R51** `Referenzlaeufe/2026-10-10_R51_FreieKuehlung`: 29 Projekte, 943 CSV, 6 568 Skalare; gegen R50 28/28 PASS, 911/911 CSV byte-gleich; zweiter Lauf byte-gleich; gestörter Lauf `--stoerung ulp` GESAMT PASS (899/943 byte-gleich). R50 liegt unter `Dokumentation/ueberholt/Referenzbasen/`. Neue Einfrierregel „gesäte Daten der freien Kühlung“ in `CLAUDE.md` und `Referenzlaeufe/LIESMICH.md`; 1064 steht nicht in der CI-Auswahl. Commits `b8e20e1b`, `d054fb87`, `082f15c1`.

## 3 WT — wackelnde Tests reihenfest (#913)

Nur Tests geändert, kein Produkt- und kein Importcode.

- **`TestdatenbankSchemastandWacheTests`** (Commit `9255e69d`): Ursache war die gepoolte Verbindung `?mode=ro&immutable=1`; rund dreißig Nachbarklassen nutzen dieselbe Zeichenfolge, die Wache las den Stand des ersten Öffnens. Behebung `Pooling = false`, neuer Fall. Nachweis: mit alter Lesart rot (185 erwartet, 183 gelesen), behoben grün, 20/20 Läufe.
- **`KalenderkarteInhaltTests`** und Schwesterfall in `KalenderkarteTests` (Commit `699cbee0`): Die Entprellung von 150 ms feuerte unter Last zwischen den Eingaben (3 von 24 Läufen rot in drei parallelen Schleifen). Muster `VorpruefungEntprelltTests`, auf das Bild warten statt `Thread.Sleep`; 24/24 grün.
- **`GebaeudeImportZonenDialogTests`**, fünf Teildateien (Commit `7151d094`): bunits synchrones `Click()` wirkte bei belegtem Verteiler erst nach dem Assert (5 von 24 rot). Neue Hilfe `EPOS.UI.Tests/AbgewarteteHandlungen.cs`, 72 Handlungen umgestellt, Wachfall; 24/24 grün.

## 4 Entwürfe AH, KB, KK (#914)

Vorgelegt, Entscheide offen. Anlass: Anwenderaufträge 10.10.2026 mit Bildschirmfotos „Weitere Einstellungen“ und „Kältemaschinen im Projekt“ sowie „Konzepte zu Kälteanlagen prüfen“.

- **AH** `Dokumentation/aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Vorheizrampe.md` (Commit `44f5c703`): Bestand ist die Sollwerttreppe vor dem Kalendersprung; Projekt 1051: Jahresspitze 30,0 kW statt 42,8 kW ohne Optimierung, 43 Tage ohne Rampe. Option 1 vorgegebene Vorheizzeit mit Ankunftsprüfung, Option 2 berechnete Vorheizzeit; Fragen F1–F10; 10,5–15 PT.
- **KB** `Dokumentation/aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Kaeltebereich.md` (Commit `a21476c9`): Kälte-Kachel als Katalogauswahl-Dialog, Bereich „Kälte“ in der Simulationskonfiguration. Befunde: Kältefolge nicht pflegbar, zwei Anlagen teilen eine Projektkopie. Abstimmung Stufe 5 mit der Sitzung „Dialoge und Korrekturen“; Fragen F1–F6 und KB-1; 3,5–4,25 PT.
- **KK** `Dokumentation/aktuell/Kälteanlagen/2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md` (Commits `ded4f3c1`, `e43191c0`): kein VDI-3805-Blatt für Kälteerzeuger; Blatt 22 liefert Kühlblöcke reversibler Wärmepumpen in 7 von 12 Herstellerdateien. Stufen K-A bis K-H; Fragen KKP-Q1–Q8.

Offen beim Anwender aus derselben Anfrage: Einheitenwahl in der Zeile des Werts, thermische Desinfektion am Warmwasserpuffer, eigener Kältebereich (Varianten vorgelegt, nicht entschieden).

## 5 Gate

Lokal `Werkzeuge/Gate/gate_rest_linux.sh` auf `dbbd51dc` (SP + FK) grün: Kern-Filter, `EPOS.UI.Tests` 8 264/8 264, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (+1 übersprungen), Dokumentationswachen 35/35, Windows-Schale auf Linux, Designer, SQL-Dialekt, Werkzeugtests (124, 61, 24, 39), keine BOM, keine Konfliktmarker, Referenzlauf 29/29 PASS gegen R51, 943/943 CSV byte-gleich. CI-Kern-Lauf: ausstehend (Vermerk folgt).
