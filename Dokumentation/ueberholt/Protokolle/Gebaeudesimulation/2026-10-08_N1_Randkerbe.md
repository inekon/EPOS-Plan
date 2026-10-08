# Protokoll N1 — Öffnungen am Wandrand als Kerbe, Meldungen je Format (08.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#819**. Commits: `9b6af3203`, `49bb50c72`, `50cb620c3`; Merge in die Integration `b44f1c7c5`, Merge origin `618496530`.
**Entscheid:** Anwenderwunsch, dass der Gebäudeviewer Fenster und Türen am Wandrand zeigt.

## 1 Anlass

Eine Öffnung, die bis an den Rand einer Wand reicht, ließ sich nicht als Loch aussparen; sie blieb ein eigener Körper vor der ungeschnittenen Wand. Kein Rechenweg, kein Schemaschritt.

## 2 Gebaut

- `EPOS.Kern/Allgemein/Simulation/Gebaeude/Kerbschnitt.cs`: Kerbschnitt; Laibung nur an Kerbkanten im Wandinneren; Öffnungsgruppen (Fensterband, Überlappung) werden gemeinsam ausgespart.
- `Koerperbildner.Extrusion`: neue Überladung mit Aussparungsart Loch, Kerbe oder Keine samt Grund.
- Projektdatei (`SqprojGeometrie.cs`) und gbXML (`GbxmlKoerper.cs`); gbXML hatte denselben Mangel. IFC-Körper bitgleich.
- Meldungen der Flächenklassifikation mit Präfix je Format (`IMP_GBXML_PROT_`, `IMP_SQPROJ_PROT_` je `FLAECHE_OHNE_BAUTEIL`, `FLAECHENGRUPPE_ABWEICHUNG`); IFC unverändert.

## 3 Prüfung

Tests `RandkerbeTests` (15). Anwenderdatei Projektdatei: ausgesparte Öffnungen 77,4 % → 97,6 %. IFC-Körper bitgleich.

## 4 Gate 819

auf `e3ad59f20` (64 min): KiKern 549, SpeicherEngine 397, SpeicherPlanung 28 mit 1 übersprungen, EPOS.UI 7 830, EPOS.Kern 12 272 mit 6 übersprungen, 0 rot; Dokumentationswachen 35/35; ChartProben 211 Hashes gleich `Messlatte_2026-10-05`; Referenzlauf 24/24 gegen `2026-10-07_R43_Kaelteseite_AK3K` PASS (8 073 156 Werte), 759/759 CSV byte-gleich; Störlauf ulp PASS; SQL-Dialekt-Prüfer 2 522 Texte, 0 Fundstellen; Windows-Schale 0 Fehler; Auslieferungsvorlage-Tests 61/61.

## 5 Offen

- Sichtabnahme unter Windows: Fenstertür und Fensterband in der 3D-Ansicht einer Projektdatei.
- Öffnungen, die sich nur in einem Punkt berühren (`KERBE_NICHT_EINFACH`).
- Logbuch-Eintrag (Version beim Anwender erfragen) und Wiki-Upload gebündelt.
