# Basiswechsel 2026-08-14_Paket4 → 2026-08-14_B1-Fixes

**Anlass:** Merge der Bestandsfehler-Fixes B1-F1/B1-F2 (`2f88186`, stille
RecordSet-SQL-Fehler) und der Ganglinien-Controller-Umstellung (`52c279a`,
Alt-Spaltenname `ID_GanglinieDaten`), dazu B0-12 (`d1c6ca0`) und der
B0-13-Protokollmerge (`b625c35`). Gerechnet auf dem Merge-Stand `52c279a`,
Feature-Flag `Kaskade_Zweikanalig` = AUS, Modus `lauf` mit fester Projektliste
(Quelle: produktive `Kenndaten.accdb` aus `%ProgramData%`, migrierte
Arbeitskopie; siehe `lauf_protokoll.md`).

## Vergleich gegen 2026-08-14_Paket4

Sechs Projekte PASS (1007, 1010, 1017, 1018, 1021, 1023). Drei Projekte
weichen ab — vollständig zugeordnet:

| Projekt | Abweichungen | Ursache |
|---|---|---|
| **1008** | 70.062 Werte: `strombedarf_viertelstunde`, `reststrom_viertelstunde`, Strom-Skalare | **B1-F1 (gewollt):** `SimulationStrombedarf` sortierte über den Alt-Spaltennamen `ID_GanglinieDaten`; der Access-Fehler wurde von `RecordSet.Open` verschluckt, projektzugeordnete Stromganglinien gingen still mit 0 ein. Jetzt fließen sie ein — Strombedarf 365 → 9.945 MWh/a (dokumentiert im B0-Protokoll, Nachtrag B1-F1/B1-F2) |
| **1011** | 167.240 Werte: Strom (wie 1008), `waermebedarf_prozess` 0 → 365 MWh/a, `restwaerme`, `solar_ueberschuss` 127,76 → 0, WP-/Kessel-Folgegrößen | **B1-F1 + B1-F2 (gewollt):** zusätzlich las die Prozesswärme die nicht mehr existierende Spalte `Prozessname` (jetzt `Bezeichner`); die `IndexOutOfRangeException` wurde verschluckt, Prozesswärme war still 0. Folgewirkungen über den gesamten Wärmepfad |
| **1024** | 27.778 Werte: `bSimulationKessel` False → True, `Heizkessel.*` erstmals vorhanden (+ Folgen in Reststrom) | **Geänderte Projektdaten, kein Code-Effekt:** Der Heizkessel (eloBLOCK VE 10) wurde nach dem Einfrieren der Paket4-Basis (Snapshot „Etappe 4a") in die Werkzeug-Kaskade des Projekts aufgenommen. Beweis: Gegenlauf mit dem Alt-Code des Paket4-Commits (`6285460`) auf derselben heutigen DB-Kopie liefert dasselbe Ergebnis; Alt- vs. Neu-Code auf identischer DB ist für 1024 vollständig PASS (271.686 Werte) |

## Toleranzregeln

Unverändert (relativ 1e-4 ab Betrag 1, sonst absolut 0,01).
