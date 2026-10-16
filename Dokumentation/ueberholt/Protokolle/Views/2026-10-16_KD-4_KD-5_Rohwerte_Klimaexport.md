# KD-4 und KD-5 — Rohwerte des Klimadatenimports und CSV-Export der Klimadaten

Sitzung Dialoge und Korrekturen (Klimadaten), 16.10.2026, Zweige `kd4` und `kd5`, zusammengeführt in der Welle
KD-238. Konzept:
[`2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md`](../../2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md),
Abschnitte 3, 5 und 7; Entscheide KD-Q2, KD-Q4 bis KD-Q9 nach Empfehlung. Schemaschritt 238 `KlimaRohwerteSchema`
(zunächst geparkt, mit der Welle an 237 gehängt), Testdatenbank 238; `ChartRenderer` und `DiagrammSvg` unverändert.

## KD-4 — Rohwerte

| Teil | Inhalt | Nachweis |
|---|---|---|
| Schema | `KlimaRohwerteSchema` (238): `Tab_Klimastunde_STAMM` (`STRICT`, Primärschlüssel Region und Stunde 1 … 8760, Fremdschlüssel auf `Tab_Klimaregion_STAMM` mit Kaskade, vierzehn nullbare Rohspalten mit der Einheit im Namen); an `Tab_Klimaregion(_STAMM)` Standorthöhe, Bezugspunkt (Art, Name, Breite, Länge, Höhe, Entfernung) und Ausführung, am Katalog `Zeitbezug` (UTC/MEZ) und `Rohdaten` (0/1, Vorgabe 0). Kein UPDATE, kein Nachladen (KD-Q7). Vier Leser: `SchemaMigration`, `Werkzeuge/Testdatenbankschema`, Testvorrichtung, `Paketanhebung` (Ddl) | `KlimaRohwerteTests` (Kette und Register, Tabelle leer und ohne Nachladen, Prüfklauseln, Eindeutigkeit, Kaskade) |
| Leser | `DwdTryLeser` liefert alle Stundenfelder der TRY-Datei als Rohreihe (MEZ, ungedreht) im `TryKopf`; der PVGIS-Leser liest `WD10m` und `SP`; `KlimaRohreihe.AusPvgis` (UTC) | `KlimaRohwerteTests` (TRY-Leser mit allen Feldern, PVGIS) |
| Schreibweg | Der Import schreibt Rohwerte und Herkunft nur, wenn der Schritt steht — sonst benannt übersprungen, die Rechenspalten entstehen gleich; der Herkunftsvermerk sagt „alle Felder als Rohwerte gespeichert“. Die Projektkopie trägt die Herkunft, nie die Rohwerte (KD-Q4). Der Ortsweg übernimmt eine eingegebene Bezeichnung als Regionsnamen | `KlimaRohwerteTests` (Import mit und ohne Rohtabelle, Projektkopie, Ortsweg), `KatalogpflegeTests` |
| Katalog | Rohtabelle von der Katalogfassung ausgenommen, Herkunftsspalten im Ausschluss des Eintrags `KLIMAREGION`; die Auslieferungsvorlage führt die Rohtabelle als Kindkatalog der Region — leer, die Auslieferung der Rohwerte ist vertagt (KD-Q7) | `KatalogabgleichTests` (57 Stammtabellen, 16 benannt ausgenommene), `VorlageTests` P6 (198 `STRICT`) |
| Vorschau | Der Parameterbestand nennt je Größe „wird gespeichert“ bzw. „wird nicht gespeichert“, sobald die Rohtabelle steht | `KlimaRohwerteTests` (Parameterbestand), `KlimadatenDialogTests` |
| Wache | Kein Rechenweg liest die Rohtabelle: Erlaubt sind `Allgemein/Update`, `Import`, `Katalog`, `Export` und `Controller/KlimaExportCtrl.cs` | `KlimaRohwerteTests.Wache_Rechenweg_liest_die_Rohtabelle_nicht` |

## KD-5 — CSV-Export der Klimadaten

| Teil | Inhalt | Nachweis |
|---|---|---|
| Kern | `KlimaExportOptionen` (Parameterwahl aus zwölf Rechenspalten, vierzehn Rohspalten und dem Taupunkt; Trennzeichen, Dezimalzeichen — Komma neben Komma benannt verweigert —, Zeitspalte Stunde/Datum/beides/ISO 8601/keine, Zeitbezug mit Vorgabe MEZ ohne Sommerzeit (KD-Q8), Kopfart, Herkunftsblock, Vorlagen Haus und International, volle Genauigkeit für den Rückweg KD-6); `KlimaCsv` (Schreiber und Vorschau auf einem Weg, Zahlformat und Entschärfung wie `ZeitreihenCsv`, Zeitbezug zyklisch im Gemeinjahr, Taupunkt nach Magnus); `KlimaExportCtrl` liest eine Katalogregion (KD-Q6) mit `?`-Parametern, Rohwerte nur bei `Rohdaten = 1` | `KlimaCsvTests` (Trennzeichen × Dezimalzeichen × Zeitspalte, Einheitenzeile, Kopf, Altbestand ohne Rohdaten, UTC ↔ MEZ, Taupunkt, Rundlauf bitgleich) |
| Dialog | `KlimaExportDialog` als Überlagerung hinter „Exportieren…“ der Auswahlleiste (genau eine Region): Parameter in vier Gruppen, gerechnete Größen gekennzeichnet, Nicht-Geführtes weich gesperrt mit Grund, eine Region ohne Rohdaten nennt den Grund; Formatfelder, Vorlagen, Vorschau der ersten fünf Zeilen; OK-Weg „Exportieren…“ bildet den Text und ruft die Naht der Hülle, ein Abbruch der Dateiwahl lässt offen | `KlimaExportDialogTests` (7) |
| Hülle und KI | `KlimadatenHuelle.ExportLesen` (`KlimaExportCtrl`) und `ExportSpeichern` über `CsvExportClass.DateiWaehlenFuerExportAsync` (gemerkter Ordner, UTF-8 mit BOM, iOS teilt); KI-Maske `KlimaExport` mit sieben Feldern | `KiDialogkatalogTests`, `KiMaskenabdeckungWacheTests` |
| Probe | `fensterprobe.mjs` Fall `klimaexport` (vier Fenster) samt Gegenprobe; der Wirt liefert eine Region mit Rohdaten | Fensterprobe |

## Abnahme (Welle KD-238, lokal, Linux)

Siehe das Protokoll der Welle:
[`2026-10-16_KD-8B_Rohwertreihen_Schritt_238.md`](2026-10-16_KD-8B_Rohwertreihen_Schritt_238.md), Abschnitt „Abnahme“ — dort
stehen Kette und Register des Schritts 238, die Hebung der Testdatenbank, das Gate und die Fensterproben.
