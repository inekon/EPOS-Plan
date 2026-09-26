# PT‑1 — Projektpaket-Import: ältere Pakete werden angehoben (Protokoll, 26.09.2026)

Statuszeile #580 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).
Zweig `ios_migration_september` im Hauptbaum; Commits `f73365566` (Konzept), `16a8ab990` (Kern),
`3791a6eb8` (Dialog, Tests, Wiki), `6626717a9` (Name der Registerwache), Merge `29834fef7` (Betreff mit der vorläufigen Nummer #566; die
Statusnummer ist #580, weil origin #566 für BV-E9 vergeben hat).
Konzept: [`Konzept_Projektpaket_Migration_EPOS-Plan.md`](../../../aktuell/Konzept_Projektpaket_Migration_EPOS-Plan.md).
Kein Schemaschritt, Testdatenbank unberührt, kein Rechenweg berührt.

## Anlass

Ein Projektpaket mit Schemastand 93 wurde an einem Rechner mit Stand 150 abgelehnt. Anwender
26.09.2026: „Ältere Projekte müssen migriert werden können … Es sollte nie ein älteres Projekt
nicht importierbar sein.“

## 1. Paketformat

Ein Paket (`.wpx`) ist ein ZIP mit JSON: `manifest.json` mit `schemaVersion` (Migrationsstand der
Quelle), `data/<Tabelle>.json` je Projekttabelle im Spaltenbild der Quelle, dazu Varianten,
Kataloge, Auffüllzeilen und PV-Stamm. Kein SQLite, kein DDL. Der Import war schon
spaltentolerant (Schnittmenge aus Paket- und Zielspalten).

## 2. Abbruchstelle

Die Sperre lag in `EPOS.Kern/Controller/ProjektExportImportCtrl.cs` (Einzelimport, „Bitte beide
Rechner auf denselben Programmstand bringen“) und `EPOS.Kern/Controller/ProjektTransferSammel.cs`
(Sammellauf). Ihre Begründung — Datenmigrationen laufen datenbankweit genau einmal — trifft nur
auf Schritte zu, die Werte umrechnen.

## 3. Schrittliste 94–150 nach Art

| Art | Zahl |
|---|---|
| nur Schema (Spalte, Tabelle, Sicht, Index, Abbau) — die Schnittmenge genügt | 31 |
| nur Katalog oder Saat — das Ziel führt sie schon | 12 |
| vom Import schon abgedeckt (96, 100, 121, 124) | 4 |
| rechnet Projektwerte um — Paketumformung (98, 99, 101, 102, 104, 106, 112, 113, 127, 148) | 10 |

## 4. Wege

- **A — Paket als SQLite im Quellschema, Migration darauf:** trägt nicht. Kein Schema im Paket,
  die Migration liegt in der Windows-Schale, braucht Kataloge, Sichten und den Marker der
  Anwenderdatenbank; ein neues Format hülfe vorhandenen Paketen nicht.
- **B — Zeilen in einer Arbeitsdatenbank umformen, mit denselben SQL-Bausteinen wie die
  Migration:** umgesetzt.
- **C — Staging in einer Kopie der Anwenderdatenbank:** verworfen; die Kopie steht auf dem
  Zielstand.

## 5. Umsetzung

- `EPOS.Kern/Allgemein/Update/Paketanhebung.cs`: Register 93–150, je Schritt Art, Kurztext und bei
  den zehn Umformungen die Aktion; die Wache in `ProjektpaketAnhebungTests` verlangt einen Eintrag
  je Schemaschritt.
- `EPOS.Kern/Allgemein/Update/Paketarbeitsdatenbank.cs`: In-Memory-SQLite je Projektbaum, je
  Pakettabelle eine Tabelle mit den Paketspalten, Kataloge als Nachschlagetabellen.
- `ProjektExportImportCtrl` hebt vor der Transaktion an; scheitert eine Stufe, bricht der Import
  mit Schrittnummer ab und die Datenbank bleibt unberührt. `ProjektTransferSammel` lehnt nur noch
  neuere Pakete ab.
- Untere Grenze: Pakete mit Stand 1–92 werden eingespielt, mit Hinweis, dass Umformungen bis
  Stand 92 nicht nachgefahren wurden; Altpakete (Stand 0) wie bisher.
- Dialog: Vorschau „Das Paket (Stand 93) wird beim Import auf Stand 150 gehoben: 57 Schritte,
  davon 10 mit Umformung der Projektdaten“; Paketliste „93 → 150 — wird gehoben“ bzw. „neuer als
  dieses Programm“. Ressourcen de/en; Wiki-Quelle Projekttransfer fortgeschrieben.

## 6. Tests und Gate

`ProjektpaketAnhebungTests` (6: Registerwache, Vorschau, Paket Stand 93 trägt die Werte der
Quelle, Freitext/Preisbasis/Anlagenart, Paket auf Zielstand unberührt, scheiternde Stufe),
`ProjekttransferTests.P5`, `ProjektTransferMehrfachTests`, `ProjektTransferDialogTests`.
Gate im Hauptbaum auf `5524bc6e3` (#580 mit origin samt BV-E9; danach nur Papiere gemergt):
Kern-Filter 0 Fehler; voller Lauf 16 373 erfolgreich, 2 übersprungen, 3 rot — die drei Berichtsproben
„Gruppe“ (`BerichtWertesatzTests`, zweimal `BerichtVorlagenMesslatteTests`: Bildhöhe 322 gegen
323 px), unter Windows ebenso rot auf dem reinen origin-Stand `e483d26c8`, auf ubuntu grün — also
aus BV-E9, nicht aus #580; ChartProben 220 Bilder, 0 Verstöße; Referenzlauf der sieben
CI-Projekte gegen R22 PASS (2 497 845 Werte); Windows-Schale Debug x64 0 Fehler; Designer
0 abweichend; SQL-Dialekt-Prüfer 0 Fundstellen (2 025 Texte). Vor dem Merge mit BV-E9 auf
`5864ef802`: voller Lauf 16 244 erfolgreich, 0 rot.

## 7. Offen

- Stufe 2: Register 62–92 mit den Umformungen dieser Schritte (79, 83/84, 89/91), untere Grenze 61.
- Schritt 104 im Paket vereinfacht: die Leistungspreis-Staffel eines Zonensatzes steht nur im
  Importergebnis; die Gruppe gilt je Projektbaum.
- Kataloge im Paket werden nicht umgeformt.
- Die Detailzeilen der Stufen sind nur deutsch.
- Wiki-Upload der Seite Projekttransfer mit dem nächsten Sammel-Upload.
