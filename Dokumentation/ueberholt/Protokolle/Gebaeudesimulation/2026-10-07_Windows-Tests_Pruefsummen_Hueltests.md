# Protokoll — zehn Kern-Tests, die nur unter Windows rot waren (07.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#795**. Commits `7596f5eb`, `31c36886`, `13e8aa50`.
**Anlass:** Meldung der Sitzung Wirtschaftlichkeit (Nach #787, Punkt h): zehn Kern-Tests unter Windows rot, unter Linux grün. Die Windows-Ausgabe des Anwenders (47 Fälle, 10 rot) nannte die Fehlermeldungen.

## 1 Befund und Behebung

| Test | Ursache | Behebung |
|---|---|---|
| `GebaeudeEinzonennetzTests.Die_Einzonenreihen_des_Bauteilwegs_bleiben_bitgleich`, 7 Fälle (ideal, AK1 Heizseite, AK1 Kälteseite, Kühlung ideal, Sommerlüftung, Leistungsgrenze, Volumen und Raumhöhe) | Die Tafel `Erwartet` trug für diese Fälle unter Linux erfasste Zeilen (RP2a, `94e5d4d9`). Unter Windows x64 außerhalb der CI gilt die strenge Regel (SHA-256 je Reihe); Linux und CI prüfen die Momente. Die Spanne HC-5/BA-1/BA-2 ändert an den Reihen kein Bit (110 von 110 Tafelzeilen gleich). | 97 Zeilen der Windows-Ausgabe übernommen, 39 davon geändert; alle acht Fälle tragen Windows-Prüfsummen. Unter Linux bleiben die Momente innerhalb relativ 1e-9. |
| `ZonenuebergabeRechenwegTests.Ideal_an_einer_Zone_rechnet_ohne_Heizkreis` | Vergleich des Gebäuderücklaufs mit dem der einzigen gekoppelten Zone auf 9 Nachkommastellen; der Wert 53,3547015325 liegt auf der Rundungsgrenze, eine Abweichung im letzten Bit kippt die Rundung. | Absolute Toleranz 1e-9. |
| `GebaeudeImportHuelleTests.Ohne_Projekt_fragt_die_Huelle_keine_Datenbank`, `GebaeudeImportBaustoffeHuelleTests.Ohne_Projekt_bildet_die_Huelle_den_Abschnitt_ohne_Datenbank` | Die Fälle setzen keinen Datenbankpfad. Unter Windows sieht der Kern die Anwenderdatenbank unter `%ProgramData%\EPOS_PLAN`; die Raumnutzungsvorbelegung prüft dann den Nutzungskatalog (`RaumnutzungCtrl.Lesbar` → `TabelleVorhanden`), ein gezählter Zugriff. Das Lesen des Katalogs mit vorhandener Datenbank ist gewollt (Katalogmodus der Gebäudehülle). | Neue Probe `EPOS.Kern.Tests/OhneDatenbankprobe.cs` biegt `DataRepository.PfadUeberschreibung` wie der Wirt der Rasterprobe auf eine nicht vorhandene Datei und stellt ihn zurück; beide Fälle legen sie ein. Am Code der Hülle ändert sich nichts. |

## 2 Prüfung

Rote Probe unter Linux für Teil C mit eingelegter Datenbank (2 von 2 rot, je 1 Zugriff). Nachher: Kern-Filter 0 Fehler, Kern-Tests der betroffenen Klassen 503/503, UI-Tests `GebaeudeAufbau`/`GebaeudeImport` 96/96. Kein SQL, kein Rechenweg geändert.

## 3 Gate 794

Hauptbaum `6119b3c7`, 87 min: 20 455 Tests, 20 451 grün, 4 übersprungen, 0 rot (KiKern 549, SpeicherEngine 397, SpeicherPlanung 28 mit 1 übersprungen, EPOS.UI 7 772, EPOS.Kern 11 709 mit 3 übersprungen); Dokumentationswachen 35/35; ChartProben 211 Hashes gleich `Messlatte_2026-10-05`; Referenzlauf 21/21 gegen `2026-10-06_R39_Auslegungsheizlast` PASS, 646/646 CSV byte-gleich; Störlauf ulp PASS; SQL-Dialekt-Prüfer 2 480 Texte, 0 Fundstellen; Windows-Schale 0 Fehler. Nach dem Merge mit KP3-A1d (#794, nur Bericht und Variantenvergleich) gezielt nachgeprüft (siehe Statuszeile).

## 4 Offen

- Nachweis unter Windows mit demselben Filter (`GebaeudeEinzonennetzTests`, `ZonenuebergabeRechenwegTests`, `GebaeudeImportHuelleTests`, `GebaeudeImportBaustoffeHuelleTests`).
- `ZonenuebergabeRechenwegTests.Mehrere_Zonen_mischen_den_Ruecklauf_massenstromgewichtet` vergleicht ebenfalls auf Stellen und könnte an einer Rundungsgrenze kippen; unter Windows derzeit grün.
