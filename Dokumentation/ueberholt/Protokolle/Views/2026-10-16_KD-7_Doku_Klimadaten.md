# KD-7 — Dokumentation der Klimadaten nachgezogen

Sitzung Dialoge und Korrekturen (Klimadaten), 16.10.2026, Zweig `kd7`, Statuszeile #1151.
Konzept:
[`2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md`](../../2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md),
Abschnitt 7, Zeile KD-7; Vorgänger:
[`2026-10-16_KL_Klimadatenimport_Vorschau.md`](2026-10-16_KL_Klimadatenimport_Vorschau.md),
[`2026-10-16_KD-4_KD-5_Rohwerte_Klimaexport.md`](2026-10-16_KD-4_KD-5_Rohwerte_Klimaexport.md),
[`2026-10-16_KD-8A_Weitere_Klimadatenreihen.md`](2026-10-16_KD-8A_Weitere_Klimadatenreihen.md),
[`2026-10-16_KD-8B_Rohwertreihen_Schritt_238.md`](2026-10-16_KD-8B_Rohwertreihen_Schritt_238.md).
Nur Papiere und Wiki-Quellen; kein Code, kein Schemaschritt, Basis unberührt.

## Konzept Klimadatenquellen

[`Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md`](../../../aktuell/Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md)
beschreibt den gültigen Stand nach KD-1 bis KD-5 und KD-8:

| Abschnitt | Inhalt |
|---|---|
| Kopf | Gegenstand um Vorschau, Rohwerte und Export erweitert; Verweis auf das überholte Konzept Klimadatenimport |
| § 1 | was ein Import speichert (Rechenspalten, Rohwerte im Katalog, Bezugspunkt, Höhe, Ausführung, Zeitbezug, `Rohdaten`); weitere Quellen nicht bekannt, CSV-Import vorgemerkt |
| § 2 | Bedienweg als Verwaltung (Fußleiste `Import…`, Auswahlleiste mit „Exportieren…“), Überlagerung mit Höhe des Ortes, „Datensatz suchen“ für alle drei Quellen, Übernahme ohne zweiten Abruf, weiche Sperre, haftende Schlussleiste |
| § 2.3 (neu) | Vorschaukarte: Treffer, Standort und Bezugspunkt, Hinweise (50 km/200 m, Regionalweg immer), Kennzahlen, Monatsbild, Parameterbestand, Verfall |
| § 2.4 (neu) | Weitere Reihen: Baustein `Reihenwahl`, `KlimaReihenkatalog`, Regeln Monatsbild (zwei Einheiten) und Jahresverlauf (eine Einheit, drei Reihen), Rohwert-Chips mit Marke „R“ |
| § 2.5 (neu) | CSV-Export: Gruppen, Formatwahl, Vorlagen, Vorschau, Leseweg, Zeitdrehung, Kernklassen, Speicherweg |
| § 3 | Zeile `p WR WG x E IL`: Rohwert statt „benannt verworfen“ |
| § 6 | neu „Rechenspalten und Rohwerte“: `Tab_Klimastunde_STAMM` (Schritt 238) mit Belegung je Quelle, nur Katalog, kein Leser im Rechenweg, Altbestand, Katalog und Auslieferung (vorerst ohne Rohwerte) |
| § 7 | „Regionsvorschau“ ersetzt durch „Datensatzsuche“ und „Ehrlich benannt“ (nächste Repräsentanzstation, Hinweis) |
| § 8 | Herkunftsvermerk „alle Felder als Rohwerte gespeichert“, Herkunftsspalten an der Region |
| § 10 | KL-Q3 auf den gültigen Stand, Zeile „Anwender 19.09.2026 (b)“ entfällt (abgelöst durch KD-Q3), KD-Q1 bis KD-Q14 aufgenommen |
| § 11 | Nachweise der KD-Schritte |
| § 12 | Punkt 2 neu gefasst (Rohwerte ohne Leser); neu 4 CSV-Import KD-6 vorgemerkt (KD-Q5 „später“), 5 Rohwerte in der Auslieferung später, 6 Zuordnung nach der DWD-Regionenkarte unbelegt, 7 Mehrfachexport und Windrose auf Wunsch |

## Verschoben

- Konzept Klimadatenimport `aktuell/Dialoge/` → `ueberholt/` (Gegenstand umgesetzt), KD-7-Zeile
  dort als umgesetzt vermerkt, Verweise im Papier angepasst.
- Mockups `Klimadatenimport_Mockup.html` und `Klimadatenreihen_Mockup.html` → `ueberholt/Mockups/`;
  kein Papier unter `aktuell/` verweist mehr auf sie. Verweise nachgezogen in den Mockups selbst,
  in `Energieflussbild_Mockup.html` (Kommentar), in vier Protokollen (KL, KD-4/KD-5, KD-8A, KD-8B)
  und im Index `Dokumentation/LIESMICH.md` (Zeile von `aktuell` nach `ueberholt`, Indexzeile des
  Konzepts Klimadatenquellen, Mockup-Absatz).

## Wiki-Quellen

- `Projekte/Wiki/Programm Dokumentation - Klimadaten.wiki`: Einleitung, Auswahlleiste mit
  „Exportieren…“, „Die Diagramme“ mit drittem Reiter und Unterabschnitt „Weitere Reihen“ (Anker
  `weitere-reihen`), Höhe des Ortes unter „Standort“, neuer Abschnitt „Datensatz suchen und
  Vorschau“ (Anker `vorschau`, `datensatz-suchen`), „Einlesen“ mit Übernahme des Vorschausatzes,
  „Was gespeichert wird“ mit Rohwerten, neuer Abschnitt „Exportieren“ (Anker `export`),
  Näherungshinweis. Die Anker `klimaquelle`, `standort`, `einlesen`, `uebernahme` bleiben.
- `Projekte/Wiki/Klimadaten festlegen.wiki`: „Region ermitteln“ → „Datensatz suchen“ mit Vorschau.
- `Projekte/Wiki/Datenexport.wiki`: ein Satz mit Verweis auf `Klimadaten#export`.
- Gegenlesemuster aus `CLAUDE.md` auf alle neuen Zeilen: 0 Treffer. Keine Hersteller- oder
  Produktdaten; genannt sind nur Datenquellen (PVGIS, DWD).
- Upload ausstehend (Sammel-Upload, Statusdatei 4.2): Programm Dokumentation/Klimadaten,
  Klimadaten festlegen, Datenexport.

## Logbuch-Entwurf (Version offen, nicht veröffentlicht)

Regel Konzept Hilfesystem 13.4. Fasst die Klimadaten-Aufträge KD-1 bis KD-8 zusammen und ersetzt
beim Upload den Abschnitt „Klimadaten-Import, Einlesen-Dialoge“ in
[`Wiki_Update_2026-09-26.md`](../../../aktuell/Wiki_Update_2026-09-26.md) (dessen zwei Sätze stehen
unverändert darin):

- Seit 16.10.2026: Der Klimadaten-Import zeigt vor dem Einlesen den gefundenen Datensatz mit Treffer, Standort und Kennzahlen.
- Seit 16.10.2026: Der Klimadaten-Import speichert alle Größen der Quelle, auch Wind, Luftdruck und Wasserdampfgehalt.
- Seit 16.10.2026: Der Dialog Klimadaten zeigt im Reiter „Weitere Reihen“ und im Monatsbild der Vorschau wählbare weitere Größen.
- Seit 16.10.2026: Die Stundenwerte einer Klimaregion lassen sich mit Wahl der Größen und des Formats als CSV-Datei exportieren.
- Seit 16.10.2026: Die Knöpfe der Einlesen-Dialoge bleiben am unteren Fensterrand sichtbar.

## Abnahme

- `dotnet build EPOS.Kern.Tests -c Release`: 0 Fehler.
- `DokumentationLinkWache`, `RepositoryOrdnungWache`, `WikiProduktdatenWache`: 35/35 grün. Der erste Lauf fand den
  Mockup-Verweis der Statuszeile #1121 auf den alten Ort; nachgezogen auf `../ueberholt/Mockups/`.
- Markdown UTF-8 ohne BOM, Wiki-Quellen UTF-8 ohne BOM mit LF wie die Nachbardateien.
