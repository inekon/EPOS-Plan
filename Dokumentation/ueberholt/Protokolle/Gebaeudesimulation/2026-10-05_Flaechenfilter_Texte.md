# Protokoll Flächenfilter — Texte der Liste „Flächen je Zone“ (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Zweig `filtertexte` (Worktree `../EPOS-Plan-filtertexte`), ein Sonnet-Agent, Commit `9910de03`. Kein Schemaschritt, kein SQL, Rechenweg unberührt.

## 1 Anwenderbefund

Die drei Filter der Liste „Flächen je Zone“ im Gebäudeimport-Dialog („nur Fehler“, „nur ohne Gegenstück“, „nur ohne U-Wert“) waren nicht verständlich (Anwenderbefund 05.10.2026).

## 2 Bedeutung je Filter

- **nur Fehler** (`EPOS.UI.Daten/Bedarf/GebaeudeImportZonen.cs` Z. 358–398): jede Fläche mit nicht leerer Spalte „Befund“ — ohne Nachbarfläche, ohne U-Wert, geschätzte Fläche, Körper-Beleg einer Trennfläche —, kein Prüfstufen-Fehler.
- **nur ohne Gegenstück** (Z. 365; `EPOS.Kern/Allgemein/Import/Gebaeude/GebaeudeZonierung.cs` Z. 925/959): Trennfläche ohne Gegenfläche in der Nachbarzone; sie rechnet gegen einen unbeheizten Raum (`GebaeudeImportDaten.cs` Z. 655).
- **nur ohne U-Wert** (Z. 366): weder U-Wert noch Schichtaufbau.

## 3 Texte (Schlüssel unverändert)

| Schlüssel | alt | neu de | neu en |
|---|---|---|---|
| `GIMP_DLG_FILTER_FEHLER` | nur Fehler | Nur Flächen mit Befund | Only surfaces with a finding |
| `GIMP_DLG_FILTER_OHNE_GEGENSTUECK` | nur ohne Gegenstück | Nur Flächen ohne Nachbarfläche | Only surfaces without an adjacent surface |
| `GIMP_DLG_FILTER_OHNE_UWERT` | nur ohne U-Wert | Nur Flächen ohne U-Wert und Aufbau | Only surfaces without U-value and build-up |

Neu: `GIMP_DLG_FILTER_ERLAEUTERUNG` (Zeile unter „Flächen je Zone“: nur Anzeige, „und“, gespeichert wird alles) und je Filter ein Hinweistext (`…_FEHLER_HINWEIS`, `…_OHNE_GEGENSTUECK_HINWEIS`, `…_OHNE_UWERT_HINWEIS`), als `title` auf einem `<span>` um den `Schalter`.

## 4 Wirkung

Nur Anzeige: `FlaechenFiltern()` in `GebaeudeImportDialog.razor` füllt `_flaechenzeilen`; das Speichern bleibt unberührt. Die Filter sind UND-verknüpft. Geändert: `GebaeudeImportTexte`, `Resource.Designer.cs`, `GebaeudeImportZonenDialogTests.cs`, Wiki-Quelle des Gebäudeimports.

## 5 Tests

Kern-Filter 0 Fehler; `EPOS.UI.Tests` (GebaeudeImport, Texte) 130 bestanden; `designer_neu.py` unverändert. Nach dem Merge mit `hc3`: 132 bestanden.

## 6 Offen

- Der Hinweistext erscheint nur mit der Maus; auf iOS trägt allein die Erläuterungszeile.
- Kein eigenes CSS für die Erläuterungszeile (`epos-leisezeile`).
- Rasterprobe nicht nötig (kein Raster berührt); Sichtabnahme unter Windows.
- Logbuch-Version beim Anwender erfragen.

## 7 Logbuch-Satz

Im Gebäudeimport sind die Filter der Liste „Flächen je Zone“ verständlicher beschriftet und erklären per Hinweistext, was sie zeigen; sie wirken nur auf die Anzeige.
