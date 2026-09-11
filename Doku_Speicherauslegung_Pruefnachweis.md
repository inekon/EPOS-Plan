# Umsetzung und Prüfung der EPOS-Speicherauslegung

> **Aktueller Mehrspeicherstand (11.09.2026):** Der neue Nachweis für Flottenphysik, Prognose-/MILP-Planung, Projektjahre, SoC-Mitnahme, NPV und gemeinsame Auslegung steht in [Mehrspeicher: Konzept, Umsetzung und Prüfumfang](Doku_Mehrspeicher_Konzept_und_Umsetzung.md). Die nachfolgenden Zahlen der Dokumentversion 1.2 bleiben unverändert als datierter Prüfstand der vorherigen Einzelspeicherauslegung erhalten und sind keine neu ausgeführte Mehrspeicher-Gesamtsuite.

Stand: 11.09.2026 · Dokumentversion 1.2

Die Kosten-, Auslegungs- und Importfunktionen sind im lokalen EPOS-Plan-Projekt unter `C:/Users/DirkEngelmann/Documents/WP-Plan` implementiert. Der Release-Build liegt unter `WindowsFormsApplication1/bin/Release/net10.0-windows/EPOS_Plan.exe`. Die bestehende installierte Anwendung erhält den neuen Dialog beim Start dieses neu gebauten Programmstands; die HTML-Auswertung im Speichersimulationsordner gehört weiterhin zum Python-Beispiel.

## Einstieg

Im neuen EPOS-Plan-Build: **Simulation → Detaillierte Simulation → Parameter → Stromspeicher → Auslegung optimieren**. Dort Quellen und Kosten wählen, Bereiche eingeben, bei Bedarf CSV-Dateien über ihre Spaltenvorschau importieren, Einstellungen oder ein benanntes Profil speichern und neu berechnen.

## Prüfergebnisse

| Prüfung | Ergebnis |
|---|---|
| Gesamte Testsammlung im Solution-Filter, Release | 6.622 von 6.622 bestanden |
| EPOS.Kern.Tests | 2.379 bestanden |
| EPOS.UI.Tests | 3.414 bestanden |
| SpeicherEngine.Tests | 360 bestanden |
| KiKern.Tests | 469 bestanden |
| Windows-Anwendung, finaler Release-Build | 0 Fehler; 3 bestehende Warnungen (zweimal NU1510, einmal WFO0003) |
| Referenzlauf nach dem finalen Build | 12 von 12 Projekten erfolgreich; 312 Dateien |
| Vergleich mit 2026-09-07_R6_PvKoeffizienten | 3.313.072 Werte innerhalb der Toleranz; keine überschrittene Toleranz |
| Plausibilitätsprüfung | Alle zwölf Projekte plausibel |
| SQL-Dialektprüfung gegen die Testdatenbank | 1.319 SQL-Texte geprüft; 0 Fundstellen |
| SQLite-Testdatenbank, Schema 73 | integrity_check: ok; foreign_key_check: 0 Verletzungen |
| Word-Dokument | 30 Seiten gerendert und visuell geprüft |

Die Referenzbasis wurde nicht geändert. Vor der Schemaanhebung wurde eine konsistente SQLite-Sicherung der Testdatenbank erstellt. Die produktive Datenbank wurde für diese Prüfungen nicht verändert; der neue Programmstand führt beim Start den regulären Migrationsschritt 73 aus.

Der ursprüngliche Python-Prüfbericht und die Prüfung der HTML-Auswertung stammen vom 10.09.2026. Der Python-Code wurde für die EPOS-Erweiterung nicht geändert; deren alte Testergebnisse sind nicht als neu durchgeführter C#-Nachweis zu lesen.

## Dokumentation und Wiki

Die Bedienung und Datenverträge stehen in `EPOS_Dialog_Kosten_und_Zeitreihen.md`; `Spezifikation.md` und das Word-Dokument enthalten den Fachinhalt einschließlich der tatsächlichen Umsetzung in Kapitel 14. Im EPOS-Repository wurde zusätzlich `Doku_Speicherauslegung_Kosten_Zeitreihen.md` angelegt.

Vier Wiki-Seiten wurden über HTTPS aktualisiert und anschließend gegen die freigegebenen Seitentexte geprüft. Vorhandene Inhalte und Anker wurden erhalten:

- [Programm Dokumentation/Kosten](https://wiki.epos-plan.de/wiki/Programm_Dokumentation/Kosten) – Revision 531
- [Programm Dokumentation/Simulationsergebnisse](https://wiki.epos-plan.de/wiki/Programm_Dokumentation/Simulationsergebnisse) – Revision 532
- [Programm Dokumentation/Stromspeicher](https://wiki.epos-plan.de/wiki/Programm_Dokumentation/Stromspeicher) – Revision 533
- [Programm Dokumentation/Berechnung/Stromspeicher](https://wiki.epos-plan.de/wiki/Programm_Dokumentation/Berechnung/Stromspeicher) – Revision 534

## Grenzen der Umsetzung

Die Größenoptimierung untersucht die aktive einzelne Speichervariante. Sie berücksichtigt spezifische Kosten aus der aktiven Anlage oder aus der Direkteingabe. Nichtlineare Investitionskurven und eine gemeinsame Auslegung mehrerer physisch parallel betriebener Speicher sind weiterführende Konzepte.

Die wirtschaftliche Jahresauslegung verlangt vollständige, zusammenhängende Jahresreihen. Beim Mischen mit EPOS-Modellwerten wird die Kalenderzuordnung ausdrücklich gewählt; CSV-Werte und tatsächliche Zeitstempel bleiben erhalten. Reine Dateiauslegungen enthalten die importierte Last und PV sowie die Projektvergütung, aber keine zusätzliche BHKW-Erzeugung aus einem Simulationslauf.

Der Betriebskostenbetrag des Referenzjahres wird als konstanter jährlicher Betrag bewertet. Die Übernahme des Bestpunkts übernimmt die Dimensionierung; für die vollständige Projektwirtschaftlichkeit sind die dortigen Kostenpositionen zu prüfen.

## Nachweise

Die vollständigen Konsolenprotokolle und die Referenzergebnisse liegen unter `C:/Users/DirkEngelmann/Documents/Büro Optimierung/.work`. Maßgeblich sind `dotnet_test_WP-Plan.Kern_Release_after_pv_fix.log`, `build_WindowsFormsApplication1_Release_final.log`, `epos_referenz_73_final2_vergleich.log`, `sql73_final.log` und `wiki_publish_report.json`.
