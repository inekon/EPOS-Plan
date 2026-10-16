# KD-8 Teil B und Welle KD-238 — Rohwert-Reihen, Schritt 238 in der Kette, Testdatenbank 238

Sitzung Dialoge und Korrekturen (Klimadaten), 16.10.2026, Zweig `kd238` (Merge von `kd5` mit `kd4` und von `kd8a`).
Konzept:
[`2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md`](../../../aktuell/Dialoge/2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md),
Abschnitte 3, 4a.3 und 7; Vorgänger:
[`2026-10-16_KD-4_KD-5_Rohwerte_Klimaexport.md`](2026-10-16_KD-4_KD-5_Rohwerte_Klimaexport.md) und
[`2026-10-16_KD-8A_Weitere_Klimadatenreihen.md`](2026-10-16_KD-8A_Weitere_Klimadatenreihen.md). `ChartRenderer` und
`DiagrammSvg` unverändert, Basis R57 unverändert.

## Zusammenführung

- `kd5` (`2a0c7b186`, enthält `kd4`) und `kd8a` (`460394e25`) mit `--no-ff` auf den Stand von
  `origin/ios_migration_september` (`e706d4c61`, Schritt 237, Basis R57) gemergt.
- Konflikte: die fünf Schemaregister (`TestDatenbank`, `Paketanhebung`, `SchemaStand`, `Testdatenbankschema`,
  `SchemaMigration`) — die Blöcke 235 bis 237 von origin, dahinter der Block 238; `KlimaImportAblauf` (DirektNormal-Kopie
  aus KD-8 und `RohwerteGespeichert` aus KD-4 beide); `epos-ui.css` und `fensterprobe.mjs` (Klimaexport und Klimareihen
  beide); beide `Resource.resx` an der Naht (ein fehlendes `</data>` nach dem Dreiwegemerge), 18 980 Schlüssel ohne
  Dubletten, `Resource.Designer.cs` neu erzeugt. Konfliktmarker repoweit: keine.

## Schritt 238 in der Kette

- `KlimaRohwerteSchema.SCHRITT = BauteilStrahlungSchema.SCHRITT + 1` (238 hängt an 237); Register
  `SchemaMigration.SCHRITTE_SQLITE` (`SCHRITT_KLIMA_ROHWERTE`, Methode `Schritt_KlimaRohwerte`), `Paketanhebung.STUFEN`
  (Art Ddl), `SchemaStand.Zielversion = 238`, Testvorrichtung und `Werkzeuge/Testdatenbankschema`; Parkvermerke entfernt,
  die Park-Zusicherung in `KlimaRohwerteTests` gegen die Kettenzusicherung getauscht.
- Testdatenbank mit `Werkzeuge/Testdatenbankschema` aus der origin-Fassung 237 (`75fb0a61…`) gehoben: 18 Spalten,
  1 Tabelle; die Rohtabelle leer, alle 32 Katalogregionen `Rohdaten = 0` (kein Nachladen, KD-Q7); `integrity_check` ok,
  `foreign_key_check` leer; 109 604 864 Byte, LFS-SHA-256 `20c0a3fe4d7a399fe18ed20fc11725639c50b3eba10eb0ba704b8900399ebe35`.
  Nachtrag in `Referenzlaeufe/LIESMICH.md`; Auslieferungsvorlage 198 `STRICT`-Tabellen (`VorlageTests` P6).

## KD-8 Teil B

| Teil | Inhalt | Nachweis |
|---|---|---|
| Freischaltung | `KlimaReihenkatalog.RohwerteFreigeschaltet()` kommt aus dem Datenstand (`KlimaRohwerteSchema.SchemaVollstaendig`, Schritt 238); `Sichtbar(reihe, frei)` ist die Regel, der Schalter mit internem Setzer ist fort. Die Vorschau trägt dieselbe Auskunft als `KlimaVorschauErgebnis.RohwerteGespeichert`, der Jahresverlauf über den Reihenbestand der Hülle | `KlimaReihenkatalogTests` (Sichtbarkeit), `KlimaRohwerteTests` |
| Vorschau | `KlimaKennzahlen.Rechnen(stunden, direktNormal, rohreihe)`: die Monatsreihen der fünf Rohwert-Größen aus derselben Rohreihe, die der Import schreibt (Stunde 1 … 8760 im Raster der Quelle, Monat aus `KlimaReihenkatalog.MonatVonStunde`, Mittel oder Summe wie die Rechenspalten); eine Größe ohne Stunde steht nicht darin | `KlimaReihenkatalogTests` (Monatsreihen der Rohwerte, Gemeinjahr) |
| Jahresverlauf | `KlimaExportCtrl.RohwerteLesen(id)` liest allein die Rohwerte einer Katalogregion (fester SQL-Text, `?`-Parameter), `KlimaReihenkatalog.AusRohwerten` macht daraus die fünf Reihen; `KlimadatenHuelle` hängt sie hinter die Rechenspalten, sobald der Schritt steht. Eine Region ohne Rohwerte (`Rohdaten = 0`) oder eine Spalte nur aus NULL: „führt nicht“, der Chip weich gesperrt mit `KLIMA_RW_FUEHRT_NICHT` | `KlimaRohwerteTests` (`RohwerteLesen`, Altbestand und Region mit Rohwerten), `KlimaReihenkatalogTests` (`AusRohwerten`) |
| Dialog | Die fünf Rohwert-Chips in Vorschau und Reiter „Weitere Reihen“ mit Marke „R“ (Titel `KLIMA_RW_ROHWERT_MARKE`, beide Sprachen); `KlimaReihe.IstRohwert` | `KlimadatenDialogTests.KD8B_Rohwert_Chips_folgen_dem_Datenstand_mit_Marke_R` |
| Eine Quelle | `KlimaExportOptionen` nimmt Schlüssel (`R_*`, `F_*`, `ROH_PREFIX`) als Aliasse des `KlimaReihenkatalog`, Spalte und Einheit der Rechenspalten aus dem Katalog; die Rohwert-Reihen des Katalogs nennen ihre Spalten über `KlimaRohwerteSchema.SPALTE_*` | `KlimaReihenkatalogTests` (Export-Aliasse), `KlimaCsvTests` |
| Wirt | `Fensterprobe.razor`: die Vorschau speichert Rohwerte (fünf Chips), der Reihenbestand führt drei geführte und zwei nicht geführte Rohwert-Reihen | Fensterprobe `klimaimport`, `klimareihen` |

## Abnahme (lokal, Linux)

(ABNAHME)

## Logbuch-Entwurf (nicht veröffentlicht; Versionsnummer beim Anwender erfragen)

- Der Klimadatenimport speichert alle Stundenwerte der Quelle (TRY, PVGIS) als Rohwerte; der Katalog der Klimaregionen
  nennt Höhe, Bezugspunkt und Ausführung des Datensatzes.
- Klimadaten lassen sich als CSV exportieren („Exportieren…“ in der Klimadatenverwaltung): Rechenwerte, Fassadenwerte,
  Rohwerte und Taupunkt, mit wählbarem Trennzeichen, Dezimalzeichen, Zeitspalte und Zeitbezug.
- Vorschau und Reiter „Weitere Reihen“ der Klimadatenverwaltung zeigen Luftdruck, Wind, Wasserdampfgehalt, Ausstrahlung
  und Direktstrahlung horizontal (Marke „R“), sobald die Region Rohwerte führt.

## Offen

- Auslieferung der Rohwerte in der Vorlage (KD-Q7: nur Neuimporte; die Vorlage führt die Rohtabelle leer).
- KD-6 (CSV-Import als vierte Quelle) und KD-7 (Dokumentation, Wiki-Quelle, Logbuch) folgen.
