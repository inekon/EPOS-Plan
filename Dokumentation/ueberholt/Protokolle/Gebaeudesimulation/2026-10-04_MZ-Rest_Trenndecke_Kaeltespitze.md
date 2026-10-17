# Protokoll MZ-Rest — Trenndecke ohne Raumgrenzen, Kältespitze je Zone (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle MZ-Rest (E67, E68), ein Opus-Auftrag, Commits `985e7c1`, `bbaed9f`, `e1ab74c`, Merge `20d822b`, Testdatenbank 185 `2481d13`. Statuszeile #722. Die Importproben 13–18 haben ein eigenes Protokoll: [`2026-10-04_MZ-Rest_Importproben.md`](2026-10-04_MZ-Rest_Importproben.md).
**Entscheid:** keiner neu beim Anwender. Schemaschritt 185, Basis R35 unverändert, Referenzlauf byte-gleich.

## 1 Auftrag

Trenndecken auch aus IFC-Dateien ohne Raumgrenzen und ohne Raumbezug am Geschosspaar ableiten; Kältespitze und Kühlstunden je Zone speichern, berichten und exportieren.

## 2 Vorgehen

Ein Opus-Auftrag in drei Commits (Trenndecke, Schritt 185 mit Lauf und Export, Tests); Merge nach `20d822b`. Die Orchestrierung hob die Testdatenbank beim Merge mit KU3-4d in zwei Commits auf 184 (OID `556d5ac6…`) und 185 (OID `905096ae15695c888a89b4a5717acfa9d08d19ffb110d2929730af0afd03513c`, je 84 844 544 Byte).

## 3 Ergebnis

- **Trenndecke:** neue Datei `EPOS.Kern/Allgemein/Import/Ifc/IfcRaumgrundriss.cs` (Grundriss aus senkrechter Extrusion: Polylinie, indizierte Polylinie ohne Bögen, Rechteck nach `AbbildRaum.GrundrissM`); `Grundrissueberlappung` (Ohrenschnitt, Sutherland–Hodgman je Dreieckspaar); Regel `IfcAbbildBauer.GrundrissTrenndecken()` nur ohne Raumgrenzen und ohne Raumbezug am Geschosspaar. Grundriss-Weg: Paare ab 1 m² Überlappung, Fläche = Überlappung, U-Wert, Aufbau und Dicke von der größten freien Innenplatte. Geschoss-Weg: nur Dateien ohne Raumbezüge, die Platte trennt den ersten beheizten Raum je Geschoss. Herkunft `AbbildBauteil.Trenndeckenherkunft` (BEZUG, GESCHOSS, GRUNDRISS); Meldung `IMP_IFC_PROT_TRENNDECKE_GRUNDRISS` (I) in beiden Sprachen; `GRENZEN_ENTKOPPELT` nur noch, wenn auch das scheitert.
- **Abweichung:** die Geschoss-Regel ist auf Dateien ohne Raumbezüge beschränkt, weil sonst zwei Anwenderdateien von Z5 auf Z4 sprangen; alle sechs Anwenderdateien rechnen unverändert.
- **Schritt 185 `ZonenKaeltespitzeSchema`** (`SCHRITT = KaeltestromabrechnungSchema.SCHRITT + 1`, `SchemaStand.Zielversion` 185): `Tab_ErgebnisZone.Kaeltespitze_kW` (REAL ≥ 0) und `Kuehlstunden` (INTEGER 0..8760), nullbar, ohne DML, 35 Spalten. Der Lauf schreibt beide Werte aus `GebaeudeKennzahlen`.
- **Export:** `AbbildErgebnis.KaeltelastW` je gekühlter Zone (Spitze × 1000) nach IFC `Kaeltelast` und gbXML `CoolingLoad`. Der Bedarfsdialog liest die Ergebniszeile, ersatzweise den Lauf.
- **Bericht:** Zonenkältetabelle mit „Kältespitze [kW]“ und „Kühlstunden [h/a]“ nur bei gespeicherten Werten.
- **Tests:** `IfcTrenndeckeGrundrissTests` (6) auf der Probe `IfcProbenErzeuger.Uebereinander`, `ZonenKaeltespitzeSchemaTests` (3), Exporttest, `KuehlungJeZoneDatenbankTests` an 1052.

## 4 Entscheide der Orchestrierung

- Konflikte in sechs Registrierungsdateien gelöst, beide Schritte registriert (184 vor 185); Testdatenbank in zwei Commits gehoben.

## 5 Nachweise

Kern-Filter und Schale 0 Fehler; Kern-Tests Abnahmefilter 1 998; Ifc/Zonierung/Import 433 grün; UI 276 grün; Referenzlauf 8 CI-Projekte + 1052 + 1054 PASS gegen R35, 315 Projektdateien byte-gleich; SQL-Dialekt 0 Fundstellen; Designer ohne Befund. Gate 722: Zahlen in Statuszeile #722.

## 6 Offenes

- Befunde der Importproben (siehe deren Protokoll): DigitalHub ohne Anreicherung ohne Raumflächen (Rückfall auf Grundrissfläche fehlt); angereicherte Fassung hält 6 von 59 Räumen für beheizt; P14 trifft Sollwerte nicht; FZK-Haus Dachfläche unter Z5 280,8 m² zu groß (gegen Befund N prüfen).
- Gate-Zahlen und CI-Kennung nachtragen.
