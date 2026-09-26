# VG1 — Wirtschaftlichkeit: „Strombedarf ohne Verwendung“ gilt je Vergleichsgruppe (Protokoll, 26.09.2026)

Statuszeile #555 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: ValERI-Anwenderentscheid
aus „Nach #550“ (g) nach der Analyse der Gruppe „Test: Wirtschaftlichkeit“ (1071–1073), siehe
[`DA1_Dialogdarstellung_Protokoll.md`](../Views/DA1_Dialogdarstellung_Protokoll.md), Nachtrag #550, Abschnitt 4. Konzept
[`Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`](../../../aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md),
Absatz „Strombedarf ohne Verwendung — die Gruppenregel“. Vorgänger: [`E30_Hilfsenergie_BhkwDeckung_R21_Protokoll.md`](E30_Hilfsenergie_BhkwDeckung_R21_Protokoll.md).
Commit `e2592cb4b`, Merge `0a07ea2e1` auf `ios_migration_september`.

## Befund

Je Stand galt: Führt ein Stand einen Netzbezug, aber keinen Erzeuger, der Strom verwendet, bleibt dieser Netzbezug in
Energiekosten und Emissionen außen vor (`ProjektEnergietraegerCtrl.StromOhneVerwendung`). Im Vergleich blieb so der
Netzbezug eines Gaskessel-Stamms unbepreist, während die PV-/Speichervariante ihren Reststrom samt Grundpreis bezahlte —
die Stromersparnis erschien als Mehrkosten.

## Regel

Verwendet irgendein Stand einer Vergleichsgruppe — Referenz oder Variante — Strom
(`ProjektEnergietraegerCtrl.GruppeVerwendetStrom`), bepreisen und bewerten im Vergleich alle Stände ihren Netzbezug, auch ein
Stand ohne eigenen stromverwendenden Erzeuger; ohne zugeordneten Stromträger mit dem Auslieferungsträger des Katalogs
(`StromTraegerImVergleich`), Kosten wie Emissionen. Verwendet kein Stand Strom, gilt die Regel je Stand unverändert.

## Umsetzung

- `WirtschaftlichkeitCtrl.StromGruppenregel` bestimmt die betroffenen Stände einmal je Lauf (`Berechne`,
  `BerechneVerlauf`); `Szenariodaten` rechnet für sie eine Kopie mit `VariantenDaten.StromImVergleichBepreisen` — alle
  Szenarien, Sensitivität, Bandbreite, Verlauf. Das Original und damit die Einzelbetrachtung (Kostenseite, Übersicht)
  bleiben je Stand.
- `KostenEmissionRechner` wertet das Feld aus und vermerkt die Menge (`StromGruppenregelMWh`); neuer Hinweis
  `WIRT_HINWEIS_STROM_GRUPPENREGEL` (de/en); `SzenarioAbdeckung` zählt den Stromträger nach derselben Regel.
- Tests: `EPOS.Kern.Tests/StromGruppenregelTests` (7 Fälle).

## Zahlen

ValERI-Gruppe 1071–1073 (Testdatenbank, keine Referenzrolle): ΔKW vorher −8 294 €, mit der Gruppenregel +5 241 € bzw.
+12 899 €.

## Einfrierprüfung

Kein Referenzstand betroffen: Alle Vergleichsgruppen der 14 Basisprojekte verwenden in jedem Stand Strom; keine
Einfrierregel berührt, keine Neueinfrierung. Referenzlauf der sechs CI-Projekte (1030, 1007, 1017, 1045, 1046, 1047) gegen
`2026-09-26_R21_BhkwDeckung` PASS, 2 208 587 Werte, alle CSV byte-gleich.

## Gate

Auf `6ac7f4f91`: Kern-Filter Release 0 Fehler; voller Lauf 0 Fehler (16 042 erfolgreich, 2 übersprungen — EPOS.Kern.Tests
8 367, EPOS.UI.Tests 6 713, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27); ChartProben 220 Bilder,
0 Verstöße; Windows-Schale 0 Fehler; Designer unverändert; SQL-Dialekt-Prüfer 0 Fundstellen. Kein iOS-Lauf.

## Logbuch

Version 1.2.0.5: „Im Variantenvergleich der Wirtschaftlichkeit wird der Netzbezug eines Standes ohne stromverwendenden
Erzeuger bepreist, sobald ein anderer Stand der Gruppe Strom verwendet.“

## Offen

- Kein Schalter: die Gruppenregel gilt immer; ein Schalter je Projekt oder Gruppe bräuchte einen Schemaschritt.
- Bericht, Kostenkapitel: zeigt die Einzelzahl je Stand, der Vergleich die Gruppenzahl — Anwenderentscheid.
- Leistungspreis am Stromträger: ein Stand mit dem Auslieferungsträger trägt dessen Leistungspreis — prüfen.
- Kohärenzprüfung Stromsteuer für die Stände der Gruppenregel nachziehen.
- Nebenbefunde aus #550: Gas-Grundpreis 100 gegen 120 €/a, Preisbasis „kWh“ gegen Nm³, absolute Szenariopreise.
