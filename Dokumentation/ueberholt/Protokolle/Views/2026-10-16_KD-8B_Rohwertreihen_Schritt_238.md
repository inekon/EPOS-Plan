# KD-8 Teil B und Welle KD-238 — Rohwert-Reihen, Schritt 238 in der Kette, Testdatenbank 238

Sitzung Dialoge und Korrekturen (Klimadaten), 16.10.2026, Zweig `kd238` (Merge von `kd5` mit `kd4` und von `kd8a`).
Konzept:
[`2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md`](../../2026-10-16_Konzept_Klimadatenimport_Vorschau_und_Vollimport.md),
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

Stand `kd238` nach dem Merge von `origin/ios_migration_september` (`0a928706b`; dort lag `kd8a` schon über die andere
Integration, der doppelte Block REIHENWAHL des Stilblatts ist getilgt, elf gleichlautende Ressourcen-Dubletten ebenso).

- Kern-Filter 0 Fehler; Windows-Schale auf Linux (`EnableWindowsTargeting=true`) Debug und Release 0 Fehler.
- `gate_rest_linux.sh` gegen R57: EPOS.UI 9 653 (nach der Tilgung des doppelten Blocks `StilblattTests` 41/41), KiKern 553,
  SpeicherEngine 397, SpeicherPlanung 27 (+1 übersprungen), Dokumentationswachen 35; Werkzeugtests Formularkarte 124,
  Auslieferungsvorlage 63 (P6: 198 `STRICT`), Gebäudevergleich 24, Zapfprofilvalidierung 39; Designer unverändert,
  `SqlDialektPruefer` 3 052 Texte, 0 Fundstellen; BOM in Markdown und Konfliktmarker: keine.
- Referenzlauf der 38 Projekte gegen `2026-10-16_R57_Schnellaufheizung`: GESAMT PASS (13 025 956 Werte innerhalb der
  Toleranz), 1 233 von 1 233 CSV byte-gleich — Schritt 238 und Teil B lassen den Rechenweg unberührt.
- ChartProben: 260 Bilder, 0 Verstöße; jede der 226 Zeilen der `Messlatte_2026-10-16.sha256` unverändert in der Hashliste.
- Kern-Tests gefiltert vor dem Merge (Klima, Schema, Wache, Tww, Katalog, Paketanhebung, Testdatenbank): 1 970/1 970 auf
  dem KD-8B-Stand; auf dem Stand vor Teil B 2 118/2 119 — der eine rote Fall war die Kettenzusicherung von 237
  („ist das Ziel“), seit 238 „liegt nicht davor“. Nach dem Merge gefiltert (Klima, Reihenwahl, Strahlungsschema,
  Schemastand, Katalogabgleich, Katalogpflege, KI-Katalog, Maskenabdeckung, CSV am Bild): Kern 389/389, Oberfläche 394/394.
- Fensterprobe (Chromium headless 141, Playwright 1.56.1), vor und nach dem Merge gleich: `klimareihen` 0 Verstöße in vier
  Fenstern (15 Chips, 3 weich gesperrt — Bedeckungsgrad, Ausstrahlung, Direktstrahlung horizontal; 520 × 624 Bild zuerst,
  sonst Wahl zuerst; Wahl 626/499/622/381 px hoch), Gegenproben ohne Umbruch 2 und ohne Reihenfolgeregel 1 Verstoß;
  `klimaimport` 0 Verstöße, Rollhöhe 2 147/560 px (fünf Rohwert-Chips in der Vorschau), Gegenprobe 4 je Fenster;
  `klimaexport` 0 Verstöße, Rollhöhe 1 878/560 px, Gegenprobe 4 je Fenster; `dubletten` 0 Verstöße, Gegenproben rot.

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
