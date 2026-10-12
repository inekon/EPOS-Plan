# Protokoll KWN‑0 — Prozesskälte als Unterposten des Kühlkanals (Schemaschritt 218)

**11.10.2026** · Sitzung Kälteanlagen · Grundlage: [Kühlkonzept](../../../aktuell/Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md),
Abschnitte 5.6.9 und 5.6.13 (Stufenplan KWN‑0 bis KWN‑7, Entscheide E127).

## Was gebaut ist

- **Schemaschritt 218 `KaelteArtSchema`** (`SCHRITT = SchnellaufheizungSchema.SCHRITT + 1`): Spalte
  `Kaelte_Art TEXT DEFAULT 'RAUM' CHECK (Kaelte_Art IS NULL OR Kaelte_Art IN ('RAUM','PROZESS'))` an
  `Z_Projekt_Kaeltebedarf` und `Z_ProjektWaermebedarf`; Werte als `DbWerte`-Konstanten. Reines `ADD COLUMN`,
  wiederholbar, kein DML. Leser: `SchemaMigration`, `Werkzeuge/Testdatenbankschema`, `TestDatenbank`;
  `SchemaStand.Zielversion` 218, Paketanhebung Stufe 218 als DDL.
- **Testdatenbank auf 218** mit dem Werkzeug gehoben: `integrity_check` ok, Zellvergleich nur Schemastand und die neue
  Spalte (2 Lastgangzeilen `RAUM`); LFS‑SHA `1fa07ff1…`. origin hatte die Testdatenbank seit dem Abzweig nicht geändert.
- **Unterposten:** `Kaelte_Art` an Modell, Leser und beiden Schreibwegen (spaltentolerant; im Lastgang nur für den Kanal
  Kühlung, sonst `RAUM`). `Kaeltedeckung` trägt den Prozessanteil; `SimulationKaeltebedarf` führt
  `Kaeltebedarf_Prozess` [kWh je Stunde] und `Kaeltebedarf_Prozess_Mwh`:
  `Raum = Kanal − Prozess`. Der Kühlkanal bleibt Zeichen für Zeichen, wie er ist.
- **Naht Prozesskälte belegt:** Der zentral gedeckte Prozessanteil belegt `Kaelteschranke.Prozesskaelte` (AK3‑Weg); ohne
  `PROZESS`-Zeile bleibt die Naht null. Der Assistentenabdruck nennt Prozess nur bei Prozesszeilen.
- **Oberfläche:** Auswahlfeld „Art“ (Raum/Prozess) in der Projekt‑Kopfleiste von `WaermebedarfExternDialog` (nur Kanal
  Kühlung) und eigene Gruppe in `BedarfsProfileDialog` (nur Kälte, Parameter `KaelteArten` der Hülle); Texte
  `KAELTE_ART_*` in beiden Sprachen; Hüllen tragen `KaelteArt` hin und zurück, der Satzmerker stellt sie bei Abbrechen
  zurück; KI‑Feld `kaelte_art` in beiden Masken, Eingabebilanz 7. Rahmen, Kopf und Schlussleiste der Dialoge unverändert.
- Kühlkonzept 5.6.9/5.6.13 auf den gebauten Stand nachgezogen, Querverweis am Kältebedarf.

## Vorgaben

- Fehlt der Wert (NULL oder Spalte fehlt), gilt `RAUM`; Basis unverändert, weil keine Referenzzeile `PROZESS` trägt.

## Nachweise

- `KaelteArtTests`: Fassade, Schranke, Rundreise, Lauf 1017 mit Prozesslastgang; Schematests des Schritts 218.
- Gate (`gate_rest_linux.sh` auf `29b9c9cc2`, Integration mit K‑F3 und KWN‑0): Referenzlauf 30/30 `GESAMT: PASS` gegen `2026-10-11_R52_Vorheizen`, CSV byte-gleich 979/979, SQL-Dialekt 0 Fundstellen, EPOS.UI.Tests 9087 (1 Zeitprobe unter Last rot, allein grün), KiKern 553, SpeicherEngine 397, SpeicherPlanung 27, Dokumentationswachen 35, Kern-Filter und Windows-Schale 0 Fehler; EPOS.Kern.Tests voll 13 760 grün (7 übersprungen); nach der Zusammenführung mit origin EPOS.UI.Tests voll 9174 grün.

## Befunde

- `WaermebedarfExternHuelle.NachModell` verlor die acht Deckungsfelder aus Schema 213 (Deckungsart, EER‑Weg, `Eer1`,
  `Taussen1`, `Eer2`, `Taussen2`, Kühlträger, eigener Zähler) — **bestätigt und behoben** mit #1007: plattformfreie
  Abbildung `EPOS.UI.Daten/Bedarf/WaermebedarfExternAbbildung.cs`, `WaermebedarfExternRueckwegTests` (5).
- Die Quellenwahl der Wärmepumpe listet auch Kältespeicher (gehört zu KWN‑2S).

## Offen

- **KWN‑1:** Kopplungsvorstufe.
- `ParameterVerwendung` kennt die Bedarfstabellen nicht.
