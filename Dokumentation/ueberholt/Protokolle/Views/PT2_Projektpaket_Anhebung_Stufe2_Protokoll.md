# PT‑2 — Projektpaket-Anhebung Stufe 2: Register 62–92, untere Grenze 61 (Protokoll, 27.09.2026)

Statuszeile #587 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).
Zweig `ios_migration_september` im Hauptbaum; Commits `d17c669dd` (Umformzugriff,
Paketarbeitsdatenbank), `7c27e1386` (Register 62–92, untere Grenze 61), Merge `3299fbada`
(Konflikt in `Paketanhebung.cs` zwischen dem Text-Helfer aus #585 und den Schritten 62–92 aus
diesem Auftrag vereinigt), `245595593` (Konzept nach `ueberholt/` verschoben, beide Stufen
umgesetzt). Konzept: [`Konzept_Projektpaket_Migration_EPOS-Plan.md`](../../Konzept_Projektpaket_Migration_EPOS-Plan.md),
Abschnitte 3, 5 und 7. Kein neuer Schemaschritt, Testdatenbank unberührt, kein Rechenweg berührt.
Vorgängerstufe: [`PT1_Projektpaket_Anhebung_Protokoll.md`](PT1_Projektpaket_Anhebung_Protokoll.md) (#580).

## 1. Register 62–92

`Paketanhebung.UNTERE_GRENZE = 61` (vorher 93). Schritte nach Art:

| Art | Schritte | Zahl |
|---|---|---|
| DDL | 63–66, 70–74, 81, 82, 85, 86, 88, 91, 92 | 16 |
| Import | 80 (Wärmepumpen-Katalogverweis über Bezeichner) | 1 |
| Umformung | 67, 69, 76, 79, 83, 84, 87, 89, 90 | 9 |

Katalogteile der Schritte 62, 68, 75, 77, 78 sind ausgeschlossen — das Ziel führt sie schon aus
seiner eigenen Auslieferung.

## 2. Die neun Umformungen

- **67** BHKW-Leistungsgrenze: eine leere Grenze wird mit 30 % der Nennleistung befüllt.
- **69** PV-Koeffizienten der Projektkopie: ein Koeffizient, der als Kopie des Kurzschlussstroms
  oder außerhalb des T_NOCT-Fensters steht, wird repariert — aus der Wertequelle der Migration
  (Auslieferungsmodule, CEC-Liste) oder dem mitgereisten Stammsatz; ohne Treffer bleibt er leer
  wie in der Datenbank.
- **76** doppelte Trägersätze werden entdoppelt.
- **79** Heizstab-Schalter wandert vom Projekt auf die einzelne Wärmepumpe.
- **83** Strompreis-Aufschlag: `StrompreisZerlegung.Falten` faltet den Aufschlagsmodus mit
  aktivem Anteil in die Preiszeilen des Projekts.
- **84** Einspeisevergütung: `VerguetungUmzug.Umziehen` zieht die Vergütung von der Karte in die
  Wirtschaftlichkeitsparameter um.
- **87** von zwei aktiven Speichervarianten bleibt eine aktiv.
- **89** KWK-Zuschlag wandert vom Projekt an die Anlage.
- **90** Nullzeilen der Erfassungsgruppen: Erfassungsgruppe und Hauptkomponente werden in den
  mitgereisten Kostenkatalogen (`catalogs/Tab_KostenKomponente`, `fill/Tab_Kostenfaktor`)
  nachgeschlagen; fehlen sie im Paket, bleibt die Nullzeile ohne Wert stehen.

## 3. Kern-Bausteine

- Neu `EPOS.Kern/Allgemein/Update/Umformzugriff.cs`: `StrompreisZerlegung.Falten` und
  `VerguetungUmzug.Umziehen` (Schritte 83/84) laufen darüber — an der Datenbank für die Migration
  unverändert, an der `Paketarbeitsdatenbank` für das Paket; ein Regelwerk für beide Wege.
- `Paketarbeitsdatenbank`: eine leere Zelle, die das Paket nicht mitträgt (Spalte oder Zeile einer
  Stufe), reist nicht — am Ziel gilt die Spaltenvorgabe, auch bei `NOT NULL`.

## 4. Grenzen

- **Untere Grenze 61:** Pakete mit Stand 1–60 werden weiterhin tolerant eingespielt, alle Stufen
  ab 62 laufen; das Ergebnis nennt, dass Umformungen bis Stand 61 nicht nachgefahren wurden. Kein
  Paket wird abgelehnt, weil es zu alt ist.
- **Schritt 90:** ohne die mitgereisten Kostenkataloge bleibt die Nullzeile ohne Wert — kein
  Fehler, aber keine Nachbildung des Zielstands.
- **Katalogteile 62, 68, 75, 77, 78:** nicht umgeformt, das Ziel führt sie selbst (wie schon bei
  Stufe 1, Konzept § 5).

## 5. Tests und Gate

`ProjektpaketAnhebungTests`, erweitert um vier Paketgruppen, die je ein Paket auf Stand 61
zurückbauen und bis zum Zielstand laufen lassen: **Anlagen** (Leistungsgrenze leer, Heizstab am
Projekt, zwei aktive Speichervarianten, KWK-Zuschlag am Projekt — 67, 79, 87, 89),
**Strompreis** (doppelter Trägersatz, Aufschlagsmodus mit aktivem Anteil, Vergütung an der Karte
— 76, 83, 84), **Kosten** (Nullzeile einer Erfassungsgruppe — 90) und **PV** (Koeffizient als
Kopie des Kurzschlussstroms, T_NOCT außerhalb des Fensters — 69); dazu die Vorschau an der neuen
Grenze (Stand 61). Gate der Restwelle #584–#587 auf `3299fbada`: ausstehend (wird nachgetragen).

## 6. Offen

- Die Detailzeilen der Stufe 2 sind wie die der Stufe 1 vor #585 noch deutsche Literale — die
  Ressourcenumstellung `TRANSFER_ANHEBUNG_S<Nr>` fehlt hier noch.
- Sichtprüfung in der App unter Windows mit einem echten Altpaket (aus #580 weiterhin offen).
- Wiki-Upload der Seite Projekttransfer mit dem nächsten Sammel-Upload; Logbuch-Satz unter
  Version 1.2.0.5 schon vorbereitet.
- Kein iOS-Lauf nötig — die iOS-Hülle ist nicht berührt.
