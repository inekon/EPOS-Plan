# Protokoll G5-0 — Schemaschritt 197 `FlaechenherkunftSchema`: Herkunft der Bauteilfläche in `Tab_Bauteil` (07.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#805**. Commits: `d18b9f163` (Merge origin mit Schritt 196), `907afcb8c` (Schritt, Schreib- und Leseweg), `25948fa64` (Testdatenbank 197), `87d9c7e1e` (Tests), `b10315524` (Spaltenliste in `GebaeudeG3SchemaTests`); zusammengeführt in `e25217ad8`.
**Entscheid:** E101; Abstimmung [G5 IFC](../../../aktuell/Gebaeudesimulation/2026-10-07_Abstimmung_G5_IFC.md).

## 1 Auftrag

Die Herkunft der Bauteilfläche (Mengensatz, Raumgrenze, Körper, schematisch), die der IFC-Import seit G5-1 trägt, wird in der Datenbank gespeichert: Schemaschritt 197 legt `Tab_Bauteil.Flaechenherkunft` an. Kein Rechenweg.

## 2 Gebaut

- **Schritt:** `EPOS.Kern/Allgemein/Update/FlaechenherkunftSchema.cs`, `SCHRITT = StandardlastprofilPvSchema.SCHRITT + 1` = 197. `ALTER TABLE "Tab_Bauteil" ADD COLUMN "Flaechenherkunft" TEXT CHECK (… IS NULL OR … IN ('MENGENSATZ','RAUMGRENZE','KOERPER','SCHEMATISCH'))`; die Wertliste kommt aus dem Enum über `FlaechenherkunftWerte.Wert` (eine Quelle). Reines ADD COLUMN, wiederholbar.
- **Eintragungen:** `SchemaStand.Zielversion`, `Paketanhebung` (Art Ddl), `SchemaMigration` der Schale (`SCHRITT_FLAECHENHERKUNFT`), `Werkzeuge/Testdatenbankschema`, `EPOS.Kern.Tests/TestDatenbank.cs`. `Tab_Bauteil` hat keine `_STAMM`-Spiegelung und gehört nicht zum Katalogpaket.
- **Lesen:** `BauteilModel.Flaechenherkunft` (nullbar), gefüllt in `GebaeudeZonenCtrl.Zusammenfuehren`. Die Spaltenprüfung `GebaeudeZonenanschluss.FlaechenherkunftVorhanden()` gilt je Datenbankpfad; ohne Spalte (etwa iOS ohne Nachmigration) wird sie weggelassen. Die Spaltenzählprüfung `bauteilspalten.Count > ZonenSchema.Bauteilspalten.Count` ist durch `MitKopplung` und `MitFlaechenherkunft` ersetzt.
- **Schreiben:** Import über `GebaeudeBauteilzeile.Flaechenherkunft` und `VorschlagSchreiben` (Übernahme in `WizardCtrl`).
- **Pflege:** `SpeichernJeGebaeude` (Hülle `GebaeudeKatalogHuelle`): Die Herkunft bleibt, solange die Fläche gleich ist (relativ 1e-9), sonst NULL (`FlaechenherkunftNachPflege`); neu angelegte Zeilen und Kopien tragen NULL. „Datei erneut lesen“ schreibt keine Bauteilfläche, daher keine Anpassung.
- **Kopierwege:** `ProjektDuplizierenCtrl` und `ProjektExportImportCtrl` kopieren generisch (mit Tests belegt); Katalogabgleich entfällt; die Variantenkopie läuft über Duplizieren.
- **Testdatenbank** 196 auf 197 mit `Werkzeuge/Testdatenbankschema`: LFS-oid 78b62ac3…, Größe 89 698 304 Byte vorher und nachher; Schema nur bei `Tab_Bauteil` geändert, Daten nur `SchemaVersion` und `sqlite_sequence`; alle 62 Bauteilzeilen NULL; `integrity_check` ok, `foreign_key_check` leer.

## 3 Prüfung

- `FlaechenherkunftSchemaTests` 15/15: Nummer, Ziel, Paketanhebung, Prüfklausel, Spalte, Wiederholbarkeit, Werkzeug-Wache, `ifc4_ohne_mengen` ergibt KOERPER, `ifc4_haus` ergibt MENGENSATZ, Handänderung und Handanlage ergeben NULL, Duplikat und Paket behalten die Werte.
- Gefilterter Lauf 2 437 bestanden, 4 übersprungen; Auslieferungsvorlage 61/61; Kern-Filter und Windows-Schale 0 Fehler; SQL-Dialekt-Prüfer 2 483 Texte, 0 Fundstellen.
- Referenzlauf der acht CI-Projekte gegen `2026-10-07_R40_Erdreichquellen` PASS. Kein Rechenweg geändert, keine Basis und keine Einfrierregel berührt.

## 4 Gate 805

Hauptbaum `e25217ad8`, 93 min: 20 610 Tests, 20 602 grün, 6 übersprungen, 2 rot (KiKern 549, SpeicherEngine 397, SpeicherPlanung 28 mit 1 übersprungen, EPOS.UI 7 775, EPOS.Kern 11 861 mit 5 übersprungen); rot waren zwei Folgen der neuen Spalte — die Verlustliste des Gebäudeexports kannte `BauteilModel.Flaechenherkunft` nicht, die Spaltenprobe der Zonenkopplung las die letzten zwei Spalten —, behoben in `dafcac611`, die betroffenen Klassen 223/223 nachgeprüft; Dokumentationswachen 35/35; ChartProben 211 Hashes gleich `Messlatte_2026-10-05`; Referenzlauf 22/22 gegen `2026-10-07_R40_Erdreichquellen` PASS, 677/677 CSV byte-gleich; Störlauf ulp PASS; SQL-Dialekt-Prüfer 2 483 Texte, 0 Fundstellen; Windows-Schale 0 Fehler; Auslieferungsvorlage-Tests 61/61.

## 5 Offen

- **G5-3:** Herkunft in Zuordnungsdialog und Bauteilsteckbrief zeigen (DTO `EPOS.UI/Dialoge/Bedarf/GebaeudeZonenDaten.cs` und `EPOS.UI.Daten/Bedarf/GebaeudeKatalogHuelle.cs` führen sie noch nicht; Anzeigetexte der vier Werte in beiden Sprachen).
- Die offenen Punkte aus [#801 und #802](2026-10-07_G5-1_G5-2_Bauteilkoerper_Oeffnungen.md).
