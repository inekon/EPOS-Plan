# Dokumentation — Index und Regeln

Hier liegen **alle** Markdown-Papiere von EPOS-Plan, seit Auftrag **#241** (Anwender,
12.09.2026: „Verschiebe alle .MD Dateien in ein Verzeichnis und strukturiere nach aktuell und
überholt. Sie sollen nach wie vor für Claude genutzt werden."). Vorher lagen 73 in der Wurzel
und 168 zwischen den Quelldateien unter `WindowsFormsApplication1/Allgemein/`.

**Die Regel.** [`aktuell/`](aktuell/) ist die **Arbeitsgrundlage**: Wer eine Thematik
bearbeitet, liest das zugehörige Papier hier — und schreibt es hier fort.
[`ueberholt/`](ueberholt/) ist **Geschichte**: abgeschlossene Pakete, ersetzte Konzepte,
Nachweise erledigter Abnahmen, dazu in [`ueberholt/Protokolle/`](ueberholt/Protokolle/) die
Etappen- und Wellenprotokolle. Ein Papier aus `ueberholt/` erklärt, **wie** etwas geworden
ist; es ist **nie** die Regelquelle. Wer dort einen Widerspruch zu `aktuell/` findet, folgt
`aktuell/`.

**Die Pflegeregel.** Ein neues Konzept entsteht in `aktuell/` und bekommt hier eine Zeile.
Ist sein Gegenstand umgesetzt, verworfen oder von einem anderen Papier abgelöst, wandert es
im selben Schritt mit `git mv` nach `ueberholt/`, und seine Zeile zieht in die zweite Tabelle
um — mit dem Grund oder dem Nachfolger in der letzten Spalte. Ein Protokoll entsteht gleich
unter `ueberholt/Protokolle/<Ordner>/`; es zählt zur Sammelzeile seines Ordners und braucht
keine eigene. Die Wache `EPOS.Kern.Tests/DokumentationLinkWacheTests` hält beides: kein toter
relativer Verweis, kein Papier ohne Indexzeile, keine `.md` in der Wurzel außer `CLAUDE.md`
und `README.md`.

**Was hier nicht liegt**, steht unten unter „Bleibt am Ort".

---

## aktuell — gültige Arbeitsgrundlage

| Datei | Gegenstand | letzter Stand |
|---|---|---|
| [`aktuell/ADR-001_Schema-Ausrollung.md`](aktuell/ADR-001_Schema-Ausrollung.md) | ADR-001: Ausrollung von Schemaänderungen und Datenmigration — die geltende Entscheidung hinter `SchemaMigration` | 2026-08-29 |
| [`aktuell/BETRIEB_SQLITE.md`](aktuell/BETRIEB_SQLITE.md) | Betrieb der SQLite-Datenbank: Ablage, Sicherung, Migration, SQL-Dialektregeln (§ 6) | 2026-09-11 |
| [`aktuell/Doku_Mehrspeicher_Konzept_und_Umsetzung.md`](aktuell/Doku_Mehrspeicher_Konzept_und_Umsetzung.md) | Mehrspeicher: Konzept, Umsetzung und Prüfumfang — der implementierte Flottenstand | 2026-09-12 |
| [`aktuell/Doku_PV_Strangauslegung_EPOS-Plan.md`](aktuell/Doku_PV_Strangauslegung_EPOS-Plan.md) | Modul, Strang und Wechselrichter — Regeln, Meldungen, Auslegung | 2026-09-09 |
| [`aktuell/Doku_Simulationsergebnis_Darstellung.md`](aktuell/Doku_Simulationsergebnis_Darstellung.md) | Darstellung der Reiter „Detaillierte Simulation" — das Muster für jeden neuen Reiter | 2026-09-11 |
| [`aktuell/EPOS-Plan_Konzept_Lizenzierung.md`](aktuell/EPOS-Plan_Konzept_Lizenzierung.md) | Zeitlich beschränkte Lizenzierung: Modell, Server, Token, Offline-Betrieb | 2026-09-06 |
| [`aktuell/Entscheidungsregister_iOS_EPOS-Plan.md`](aktuell/Entscheidungsregister_iOS_EPOS-Plan.md) | Entscheidungsregister iOS (iF1–iF29): welche Frage ist offen, wer hat wann wie entschieden | 2026-09-03 |
| [`aktuell/Glossar_Lokalisierung.md`](aktuell/Glossar_Lokalisierung.md) | Glossar DE → EN des Simulationsbereichs — verbindliche Übersetzungen | 2026-08-29 |
| [`aktuell/Grundlagen_1_Normen_Regelwerke_TWW-Zapfprofile.md`](aktuell/Grundlagen_1_Normen_Regelwerke_TWW-Zapfprofile.md) | TWW-Zapfprofile: normative Grundlagen (DIN, VDI, SIA) für D/A/CH | 2026-08-29 |
| [`aktuell/Grundlagen_2_Modelle_Generatoren_Daten_TWW.md`](aktuell/Grundlagen_2_Modelle_Generatoren_Daten_TWW.md) | TWW-Zapfprofile: Modelle, Generatoren, Bibliotheken und Messdaten | 2026-08-29 |
| [`aktuell/Grundlagen_3_DIN-EN-12831-3_A1_A100_Auswertung.md`](aktuell/Grundlagen_3_DIN-EN-12831-3_A1_A100_Auswertung.md) | Auswertung DIN EN 12831-3 samt A1 und A100 | 2026-08-29 |
| [`aktuell/Grundlagen_5_VDI-4655_Auswertung.md`](aktuell/Grundlagen_5_VDI-4655_Auswertung.md) | Auswertung VDI 4655 (Juli 2021) für das TWW-Zapfprofil-Modul | 2026-08-29 |
| [`aktuell/Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](aktuell/Grundlagen_KWKG_Energiesteuer_Stromsteuer.md) | KWK-Gesetz, Energiesteuer, Stromsteuer — Rechtsstand mit Quellen, Faktenbasis der BHKW-Erlöse | 2026-08-30 |
| [`aktuell/KONTEXT_Brauchwassertypen_VDI6002.md`](aktuell/KONTEXT_Brauchwassertypen_VDI6002.md) | Brauchwasserkatalog nach VDI 6002: Datenmodell, sämtliche Zahlenwerte, Herleitung | 2026-08-29 |
| [`aktuell/KONTEXT_Importkodierung_ANSI.md`](aktuell/KONTEXT_Importkodierung_ANSI.md) | ANSI-Kodierung der Herstellerdaten-Importe — Ursache und stehende Regel | 2026-08-29 |
| [`aktuell/Konzept_Einheiten_EPOS-Plan.md`](aktuell/Konzept_Einheiten_EPOS-Plan.md) | Energieeinheiten: Inventar, kWh gegen MWh, Stufenplan — Herleitung der Einheitenregel | 2026-09-07 |
| [`aktuell/Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md`](aktuell/Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md) | Emissionsarten-Katalog und CO₂-Äquivalent — die geltende Lesekette der Faktoren | 2026-09-07 |
| [`aktuell/Konzept_Hilfesystem_Wikidokumentation.md`](aktuell/Konzept_Hilfesystem_Wikidokumentation.md) | Hilfesystem auf die Wiki-Dokumentation — Katalog, Zuordnung, Wiki-Vermerke (wird fortgeschrieben) | 2026-09-12 |
| [`aktuell/Konzept_KI-Assistent_Aufgabensteuerung.md`](aktuell/Konzept_KI-Assistent_Aufgabensteuerung.md) | KI-Assistent mit Aufgabensteuerung: Schutzstufen, Aktionen, Grenzen | 2026-09-11 |
| [`aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md) | Der Hilfe-Assistent im Dialog: Aufruf, Kontext, Feldzustand, Steuerung | 2026-09-11 |
| [`aktuell/Konzept_Katalogfilter_EPOS-Plan.md`](aktuell/Konzept_Katalogfilter_EPOS-Plan.md) | Katalogfilter: Spaltenfilter, Suche, Sortierung — Stufen S1–S3 | 2026-09-09 |
| [`aktuell/Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md`](aktuell/Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md) | PV-Ertragsmodell aus den Modulstammdaten (T_NOCT, Temperaturkoeffizienten) | 2026-09-07 |
| [`aktuell/Konzept_Projektbeispiele_Dokumentation.md`](aktuell/Konzept_Projektbeispiele_Dokumentation.md) | Projektbeispiele als mitwachsende Online-Dokumentation — Grundlage des Beispiele-Gerüsts | 2026-08-29 |
| [`aktuell/Konzept_Projektstammdaten_EPOS-Plan.md`](aktuell/Konzept_Projektstammdaten_EPOS-Plan.md) | Projektstammdaten: Datumspflege, Kunde und Bearbeiter | 2026-09-02 |
| [`aktuell/Konzept_Projekttransfer_EPOS-Plan.md`](aktuell/Konzept_Projekttransfer_EPOS-Plan.md) | Projekttransfer: Export und Import zwischen Rechnern | 2026-08-29 |
| [`aktuell/Konzept_Repository_Aufraeumen_EPOS-Plan.md`](aktuell/Konzept_Repository_Aufraeumen_EPOS-Plan.md) | Repository aufräumen: Regel, Inventar, Stufenplan — Stufe 1 mit #242, Stufe 3 mit #243, Stufe 4 (Git-Geschichte umschreiben, AUF‑Q1) mit #244 | 2026-09-12 |
| [`aktuell/Konzept_Setup_InnoSetup_EPOS-Plan.md`](aktuell/Konzept_Setup_InnoSetup_EPOS-Plan.md) | Installationsprogramm mit Inno Setup — Freigabekette, Auslieferungsdatenbank, Versionsquelle | 2026-09-11 |
| [`aktuell/Konzept_Simulationsablauf_EPOS-Plan.md`](aktuell/Konzept_Simulationsablauf_EPOS-Plan.md) | Simulationsablauf ohne Dialog — eine Ansicht, ein Rückweg (Stufen S1–S3) | 2026-09-12 |
| [`aktuell/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md`](aktuell/Konzept_Stromspeicher_Dialoge_EPOS-Plan.md) | Stromspeicher-Dialoge: eine Ansicht statt Fenster in Fenster (Pakete P1–P7) | 2026-09-12 |
| [`aktuell/Konzept_Stromspeicher_EPOS-Plan.md`](aktuell/Konzept_Stromspeicher_EPOS-Plan.md) | Fach- und Umsetzungskonzept Stromspeicher-Modul, Rev. 5 (Lastspitzenkappung) | 2026-09-10 |
| [`aktuell/Konzept_Stromspeicherimport_EPOS-Plan.md`](aktuell/Konzept_Stromspeicherimport_EPOS-Plan.md) | Stromspeicherimport: Quellenprüfung, Abbildung, Stufenplan (CEC, bslib) | 2026-09-12 |
| [`aktuell/Konzept_TWW-Zapfprofile_WP-Plan_1.md`](aktuell/Konzept_TWW-Zapfprofile_WP-Plan_1.md) | TWW-Zapfprofile: Methodik und Umsetzungsplan, Fassung V1.2 (die jüngere der zwei) | 2026-08-29 |
| [`aktuell/Konzept_Wechselrichter_EPOS-Plan.md`](aktuell/Konzept_Wechselrichter_EPOS-Plan.md) | Wechselrichter: Katalog, Strangzuordnung, Rechenweg (Rev. 5) | 2026-09-09 |
| [`aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`](aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md) | Szenarioparameter und VALERI-Abgleich nach DIN EN 17463 | 2026-09-09 |
| [`aktuell/Lokalisierung_Katalog.md`](aktuell/Lokalisierung_Katalog.md) | Ressourcenkatalog des Simulationsbereichs — Schlüssel, Texte, Fundstellen | 2026-08-29 |
| [`aktuell/Lokalisierung_Pruefung.md`](aktuell/Lokalisierung_Pruefung.md) | Prüfrezeptur Lokalisierung — angewandt, von `WindowsFormsApplication1/CLAUDE.md` genannt | 2026-08-29 |
| [`aktuell/Spezifikation_Stromspeicher_Optimierung.md`](aktuell/Spezifikation_Stromspeicher_Optimierung.md) | Simulation mehrerer Stromspeicher — die fachliche Spezifikation (14 Kapitel) | 2026-09-11 |
| [`aktuell/Umsetzung_iU10_Nachweise.md`](aktuell/Umsetzung_iU10_Nachweise.md) | Nachweisliste iU10 (iOS-Hülle): was ohne Mac nachweisbar ist und was nicht — wird fortgeschrieben | 2026-09-11 |
| [`aktuell/Umsetzungskonzept_iOS_EPOS-Plan.md`](aktuell/Umsetzungskonzept_iOS_EPOS-Plan.md) | Umsetzungskonzept iOS mit den **Statusblöcken** der laufenden Wellen | 2026-09-12 |
| [`aktuell/Wirtschaftlichkeit_Kosten/LIESMICH.md`](aktuell/Wirtschaftlichkeit_Kosten/LIESMICH.md) | Wegweiser des Ordners: Mockups, Rechenwege, Beispielprojekt | 2026-09-02 |
| [`aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md) | **Führende Fassung** des Wirtschaftlichkeitskonzepts (Dialoge, Felder, Formeln) | 2026-09-02 |
| [`aktuell/Wirtschaftlichkeit_Kosten/Beispielprojekt.md`](aktuell/Wirtschaftlichkeit_Kosten/Beispielprojekt.md) | „Musterprojekt Gewerbepark" — die eine Zahlenquelle aller Rechenwege | 2026-09-02 |
| [`aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/01_Investitionskosten_BHKW.md`](aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/01_Investitionskosten_BHKW.md) | Rechenweg: Investitionskosten BHKW | 2026-09-02 |
| [`aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/02_Betriebskosten_BHKW.md`](aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/02_Betriebskosten_BHKW.md) | Rechenweg: Betriebskosten BHKW (VDI 2067) | 2026-09-02 |
| [`aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/03_Kosten_Photovoltaik.md`](aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/03_Kosten_Photovoltaik.md) | Rechenweg: Kosten der Photovoltaik | 2026-09-02 |
| [`aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/04_Energiekosten.md`](aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/04_Energiekosten.md) | Rechenweg: Energiekosten (BEHG, EU-ETS 2, GEG) | 2026-09-02 |
| [`aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/05_Verguetungen_BHKW.md`](aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/05_Verguetungen_BHKW.md) | Rechenweg: Vergütungen BHKW (KWKG, EnergieStG, StromStG) | 2026-09-02 |
| [`aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/06_Verguetungen_PV.md`](aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/06_Verguetungen_PV.md) | Rechenweg: Vergütungen Photovoltaik (EEG) | 2026-09-02 |
| [`aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/07_Erloesrubrik.md`](aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/07_Erloesrubrik.md) | Rechenweg: Erlösrubrik und Differenzmethode | 2026-09-02 |
| [`aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md`](aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md) | Rechenweg: Kapitalwert über die Nutzungsdauer (DIN EN 17463) | 2026-09-02 |

Im Ordner `aktuell/Wirtschaftlichkeit_Kosten/` liegt neben diesen Papieren der Mockup
`Mockups/Dialog_Formel_Zahlenprobe.html`, auf den die Rechenwege verweisen.

---

## ueberholt — abgeschlossen, ersetzt oder nur noch Geschichte

| Datei | Gegenstand | letzter Stand | warum / ersetzt durch |
|---|---|---|---|
| [`ueberholt/BETRIEB_Installer_Hinweise.md`](ueberholt/BETRIEB_Installer_Hinweise.md) | Betriebshinweise für Installer und Auslieferung | 2026-08-29 | Stand vor `#157‑E‑1`; die geltende Freigabekette steht in `aktuell/Konzept_Setup_InnoSetup_EPOS-Plan.md` |
| [`ueberholt/BETRIEB_Mehrbenutzer_Datenbank.md`](ueberholt/BETRIEB_Mehrbenutzer_Datenbank.md) | `Kenndaten.accdb` mit mehreren Windows-Konten | 2026-08-29 | Access; die Engine kommt im Programm nicht mehr vor (`#157‑E‑1`, 09.09.2026) |
| [`ueberholt/Bericht_Stromspeicher_Optimierung.md`](ueberholt/Bericht_Stromspeicher_Optimierung.md) | Prüfbericht der realen Projektintegration auf einer SQLite-Arbeitskopie | 2026-09-11 | einmaliger Prüflauf, abgeschlossen |
| [`ueberholt/Bestandsaufnahme_Kosten-Energie-Dialogstruktur.md`](ueberholt/Bestandsaufnahme_Kosten-Energie-Dialogstruktur.md) | Ist-Analyse Kosten, Energiedaten, Dialogstruktur (19.08.2026) | 2026-08-29 | Archiv; Nachfolger `aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` |
| [`ueberholt/CECModuleImporter.md`](ueberholt/CECModuleImporter.md) | eigenständiges Vorläuferwerkzeug „CEC PV-Modul Import" | 2026-09-06 | Vorläufer; in EPOS-Plan ist daraus `EPOS.Kern/Allgemein/Import/CEC/` geworden |
| [`ueberholt/Doku_Speicherauslegung_Kosten_Zeitreihen.md`](ueberholt/Doku_Speicherauslegung_Kosten_Zeitreihen.md) | Einzelspeicher-Auslegung mit Kostenprofilen und Zeitreihen | 2026-09-11 | „bleibt datiert liegen" (Wurzel-`CLAUDE.md`); Nachfolger `aktuell/Doku_Mehrspeicher_Konzept_und_Umsetzung.md` |
| [`ueberholt/Doku_Speicherauslegung_Pruefnachweis.md`](ueberholt/Doku_Speicherauslegung_Pruefnachweis.md) | Prüfnachweis der Einzelspeicher-Auslegung | 2026-09-11 | Selbstauskunft im Kopf: ersetzt durch den Mehrspeicher-Nachweis |
| [`ueberholt/EPOS-Plan_Lizenzierung_Umsetzungsstand.md`](ueberholt/EPOS-Plan_Lizenzierung_Umsetzungsstand.md) | Umsetzungsstand der Lizenzierung vom 01.08.2026 | 2026-08-29 | Tagesstand, abgearbeitet; das Konzept steht in `aktuell/EPOS-Plan_Konzept_Lizenzierung.md` |
| [`ueberholt/EPOSPlan_Dokumentation_DesignSkizze.md`](ueberholt/EPOSPlan_Dokumentation_DesignSkizze.md) | Design-Skizze für wiki.epos-plan.de | 2026-08-29 | Entwurf zur Abstimmung; das Wiki steht, Fortschreibung in `aktuell/Konzept_Hilfesystem_Wikidokumentation.md` |
| [`ueberholt/Erreichbarkeit_2026-09-03.md`](ueberholt/Erreichbarkeit_2026-09-03.md) | K6-Liste: welche WinForms-Maske ist noch erreichbar (03.09.2026) | 2026-09-11 | Momentaufnahme vor iU9; `WindowsFormsApplication1` führt seit M9 keine Fachmaske mehr |
| [`ueberholt/Grundlagen_4_WP-Plan_Repo-Analyse.md`](ueberholt/Grundlagen_4_WP-Plan_Repo-Analyse.md) | Repo-Analyse vom 29.07.2026 (.NET Framework 4.8, x86) | 2026-08-29 | Technik-Steckbrief überholt (net10.0, x64, SQLite) |
| [`ueberholt/Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md`](ueberholt/Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md) | Etappenplan S0–S8 der Access→SQLite-Migration | 2026-09-11 | umgesetzt (Cutover 02.09.2026); es gilt `aktuell/BETRIEB_SQLITE.md` |
| [`ueberholt/KONTEXT_Designer_Migration_Dialoge.md`](ueberholt/KONTEXT_Designer_Migration_Dialoge.md) | Dialoge ohne WinForms-Designer, Bewertung der Migration | 2026-08-29 | Gegenstand mit M9 erledigt; neue Dialoge entstehen in `EPOS.UI` |
| [`ueberholt/KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md`](ueberholt/KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md) | konsolidierter Stand Kosten/Energieträger/Wirtschaftlichkeit (29.08.2026) | 2026-08-29 | Quelldokument; bei Widerspruch gilt `aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` |
| [`ueberholt/KONTEXT_Lizenz_Erststartzustimmung.md`](ueberholt/KONTEXT_Lizenz_Erststartzustimmung.md) | Lizenz-Zustimmung beim ersten Start | 2026-08-29 | am 29.08.2026 verdrahtet — abgeschlossene Einzelmaßnahme |
| [`ueberholt/KONTEXT_Stammdaten_Aenderbarkeit.md`](ueberholt/KONTEXT_Stammdaten_Aenderbarkeit.md) | Stammdatensätze änderbar machen — Analyse und Vorgehen | 2026-08-29 | Analyse am WinForms-Bestand, der mit M9 gefallen ist |
| [`ueberholt/KONTEXT_Waermespeicher-Tool.md`](ueberholt/KONTEXT_Waermespeicher-Tool.md) | Streamlit-Auslegungstool des Vorprojekts | 2026-08-29 | anderes Werkzeug, nicht Teil von EPOS-Plan |
| [`ueberholt/KOORDINATION_Parallelsession_2026-08-30.md`](ueberholt/KOORDINATION_Parallelsession_2026-08-30.md) | Koordinationsübergabe der B5-Vorbereitungssession | 2026-08-30 | ausdrücklich temporäres Dokument, Etappe beendet |
| [`ueberholt/Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md`](ueberholt/Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md) | BHKW-Wirtschaftlichkeit: Kostentransparenz, Steuerlast, Abgrenzung | 2026-09-07 | eine der drei Quellen der konsolidierten Fassung; bei Widerspruch gilt jene |
| [`ueberholt/Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md`](ueberholt/Konzept_CO2-Faktoren_Energietraeger_EPOS-Plan.md) | CO₂-Faktoren der Energieträger | 2026-08-29 | Rev. 1.1 **umgesetzt** (Schemaschritt 56); aufgegangen in `aktuell/Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md` |
| [`ueberholt/Konzept_DB-Migration_SQL_EPOS-Plan.md`](ueberholt/Konzept_DB-Migration_SQL_EPOS-Plan.md) | Migration Access → SQL Server | 2026-08-29 | Weg verworfen; entschieden wurde SQLite |
| [`ueberholt/Konzept_DB-Migration_SQLite_EPOS-Plan.md`](ueberholt/Konzept_DB-Migration_SQLite_EPOS-Plan.md) | Konzept der Datenhaltung Access → SQLite (Rev. 2) | 2026-09-11 | umgesetzt; es gilt `aktuell/BETRIEB_SQLITE.md` |
| [`ueberholt/Konzept_Dublettenpruefung_Import_EPOS-Plan.md`](ueberholt/Konzept_Dublettenpruefung_Import_EPOS-Plan.md) | Dublettenprüfung beim Import und Katalog-Dublettensuche | 2026-08-29 | umgesetzt in `EPOS.Kern/Allgemein/Katalog/`; Text noch auf Access und WinForms bezogen |
| [`ueberholt/Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md`](ueberholt/Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md) | Einheitenbruch Brennstoff-Stamm ↔ Umrechnungstabelle | 2026-08-30 | entschieden als Weg (a)/Schemaschritt 62; die Restfragen führt die konsolidierte Fassung (§ U-1) |
| [`ueberholt/Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md`](ueberholt/Konzept_Emissionsfaktoren_Quellenwahl_EPOS-Plan.md) | mehrere Emissionsfaktoren je Träger mit Quellenwahl | 2026-08-29 | aufgegangen in `aktuell/Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md` (Tabellen `emissionsart`/`emissionswert`) |
| [`ueberholt/Konzept_Etappe3b_Formularsteuerung.md`](ueberholt/Konzept_Etappe3b_Formularsteuerung.md) | Etappe 3b: Formularsteuerung des KI-Assistenten | 2026-08-29 | F1–F5 umgesetzt (21.08.2026); Fachkonzept ist `aktuell/Konzept_KI-Assistent_Aufgabensteuerung.md` |
| [`ueberholt/Konzept_Hilfesystem_Infobutton_EPOS-Plan.md`](ueberholt/Konzept_Hilfesystem_Infobutton_EPOS-Plan.md) | Hilfesystem und Infobutton (Rev. 1) | 2026-08-29 | ersetzt durch `aktuell/Konzept_Hilfesystem_Wikidokumentation.md` |
| [`ueberholt/Konzept_Klimazonenkarte_EPOS-Plan.md`](ueberholt/Konzept_Klimazonenkarte_EPOS-Plan.md) | Klimazonen-Karte für die DIN-4710-Auswahl | 2026-08-29 | Selbstauskunft § 7: **umgesetzt** am Tag des Konzepts |
| [`ueberholt/Konzept_Kosten_Energietraeger_EPOS-Plan.md`](ueberholt/Konzept_Kosten_Energietraeger_EPOS-Plan.md) | Kosten- und Energieträgerstruktur (Rev. 2) | 2026-08-29 | „erledigt — Etappen K1–K6 umgesetzt" (KONTEXT-Tabelle 29.08.2026) |
| [`ueberholt/Konzept_Kostendialoge_EPOS-Plan.md`](ueberholt/Konzept_Kostendialoge_EPOS-Plan.md) | Design der Kosten- und Energieträger-Dialoge | 2026-09-11 | „überwiegend erledigt — KD1–KD6 umgesetzt"; die Dialoge sind inzwischen Razor |
| [`ueberholt/Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md`](ueberholt/Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md) | PV-Wirtschaftlichkeit: Eingabedialog und Rechenmodell | 2026-08-29 | „erledigt — P1–P6 umgesetzt" |
| [`ueberholt/Konzept_Projektdialoge_Vereinheitlichung.md`](ueberholt/Konzept_Projektdialoge_Vereinheitlichung.md) | Projektdialoge vereinheitlichen (P1–P6) | 2026-08-29 | umgesetzt (vier Protokolle unter `ueberholt/Protokolle/Views/`); die Dialoge sind seither Razor-Seiten |
| [`ueberholt/Konzept_TWW-Zapfprofile_WP-Plan.md`](ueberholt/Konzept_TWW-Zapfprofile_WP-Plan.md) | TWW-Zapfprofile: Methodik und Umsetzungsplan, erste Fassung | 2026-08-29 | ältere der zwei Fassungen; es gilt `aktuell/Konzept_TWW-Zapfprofile_WP-Plan_1.md` (V1.2) |
| [`ueberholt/Konzept_Umstellung_64Bit_EPOS-Plan.md`](ueberholt/Konzept_Umstellung_64Bit_EPOS-Plan.md) | Umstellung auf x64 (P0–P5) | 2026-08-29 | abgeschlossen am 22.08.2026; bleibt als Rückweg-Beleg (Tag `letzter-x86-stand`) |
| [`ueberholt/Konzept_iOS-Portierung_EPOS-Plan.md`](ueberholt/Konzept_iOS-Portierung_EPOS-Plan.md) | iOS-Portierung, Rev. 1 (Machbarkeit) | 2026-09-04 | ersetzt durch `aktuell/Umsetzungskonzept_iOS_EPOS-Plan.md` |
| [`ueberholt/Projekt-ExportImport.md`](ueberholt/Projekt-ExportImport.md) | Handover-Notiz zum `.wpx`-Export/Import | 2026-08-29 | beschreibt den Access/x86-Stand; Nachfolger `aktuell/Konzept_Projekttransfer_EPOS-Plan.md` |
| [`ueberholt/Referenzbasen/LIESMICH.md`](ueberholt/Referenzbasen/LIESMICH.md) | Wegweiser des Ordners: die Protokolle der 24 entfernten Referenzbasen, Tabelle Basis → Datum → Zweck → Protokoll | 2026-09-12 | vor dem Umschreiben der Git-Geschichte gesichert (AUF‑Q1, #244); die Messdaten der Basen sind endgültig weg |
| [`ueberholt/STAND.md`](ueberholt/STAND.md) | datierte Stände des Anwendungsprojekts (03.09.2026) | 2026-09-06 | Momentaufnahme; die laufenden Zahlen stehen in den `CLAUDE.md` und im Umsetzungskonzept |
| [`ueberholt/Umsetzung_iU0_iU1_Nachweise.md`](ueberholt/Umsetzung_iU0_iU1_Nachweise.md) | Nachweisliste iU0/iU1/iU4–iU7 | 2026-09-04 | Abnahme abgeschlossen |
| [`ueberholt/Umsetzung_iU8_Nachweise.md`](ueberholt/Umsetzung_iU8_Nachweise.md) | Nachweisliste iU8 (erster Blazor-Dialog) | 2026-09-05 | Abnahme abgeschlossen |
| [`ueberholt/Umsetzung_iU9_Nachweise.md`](ueberholt/Umsetzung_iU9_Nachweise.md) | Nachweisliste iU9, Wellen W0–W10a | 2026-09-07 | Abnahme abgeschlossen; fortgeschrieben wird `aktuell/Umsetzung_iU10_Nachweise.md` |
| [`ueberholt/Umsetzungskonzept_Stromspeicher_EPOS-Plan.md`](ueberholt/Umsetzungskonzept_Stromspeicher_EPOS-Plan.md) | Umsetzungskonzept Stromspeicher V1.1 (16.08.2026) | 2026-09-11 | ergänzte Rev. 4 des Fachkonzepts; es gilt `aktuell/Konzept_Stromspeicher_EPOS-Plan.md` (Rev. 5) und `aktuell/Doku_Mehrspeicher_Konzept_und_Umsetzung.md` |
| [`ueberholt/vdi_3805_importer_README.md`](ueberholt/vdi_3805_importer_README.md) | Anleitung des VDI-3805-Scrapers | 2026-08-29 | Werkzeug nicht mehr im Repository (#242); Überrest aus dem ehemaligen Werkzeugordner `WindowsFormsApplication1/Allgemein/vdi_3805_importer/` |
| [`ueberholt/WP-Plan_Doku_Waermebedarf_Deckung_Pufferspeicher.md`](ueberholt/WP-Plan_Doku_Waermebedarf_Deckung_Pufferspeicher.md) | Wärmebedarf, Bedarfsdeckung, Pufferspeicher, **Zweikanal**-Logik | 2026-08-29 | Codestand Juni 2026; die Engine rechnet seit Paket K1 die Dreikanalbilanz (Quellen/Senken) |
| [`ueberholt/WPPlan_Code_Befunde.md`](ueberholt/WPPlan_Code_Befunde.md) | Code-Prüfung gegen das Stromspeicher-Konzept Rev. 2 | 2026-08-29 | Prüfstand vom 16.08.2026, Befunde abgearbeitet |

### ueberholt/Protokolle — die Etappen- und Wellenprotokolle

Je Ordner eine Zeile; die Unterstruktur ist die des früheren Ablageorts, damit die
Querverweise der Protokolle untereinander kurz bleiben.

| Ordner | Anzahl | Inhalt |
|---|---|---|
| [`ueberholt/Protokolle/Reporting/`](ueberholt/Protokolle/Reporting/) | 82 | Bericht, Kosten und Wirtschaftlichkeit: Etappen B*, H*, K*, KD*, FX*, PV*, W4_* sowie die **Blazor-Port-Protokolle iU9‑W1…W16c**; dazu die Konzepte des Ordners (`Konzept_Berichtserstellung_EPOS-Plan.md`, `Konzept_Wirtschaftlichkeit.md`, `Konzept_BHKW_Kosten_Erloese.md`), `UMSETZUNGSSTAND.md` und die Prüfberichte |
| [`ueberholt/Protokolle/Simulation/`](ueberholt/Protokolle/Simulation/) | 61 | Simulationskern: Pakete 1–9, A/B, Quellen/Senken, Dreikanal, Kaskade, Booster, Schichtspeicher, die Merge-Protokolle und die Bestandsbefunde; dazu `Konzept_Simulation_QuellenSenken.md` und `Konzept_Brauchwasser_Heizung_Pufferspeicher.md` (beide umgesetzt) |
| [`ueberholt/Protokolle/sql/`](ueberholt/Protokolle/sql/) | 10 | Access→SQLite: Protokolle S0, S2, S5, S7, S8, `S1_Feinmessung`, `S3_Migrationsbericht`, `MIGRATION_Pruefrezepte` und die zwei Messungen des PV-Modulkatalogs |
| [`ueberholt/Protokolle/Hilfe/`](ueberholt/Protokolle/Hilfe/) | 6 | Hilfesystem: H1/H2, H7 Infobuttons, H11 Sammelpaket, H12 Feldhilfe, H13 Berechnungshilfe |
| [`ueberholt/Protokolle/KI/`](ueberholt/Protokolle/KI/) | 5 | KI-Assistent: H4/H5, H8, H10 Semantikindex, Klarnamenschutz |
| [`ueberholt/Protokolle/Views/`](ueberholt/Protokolle/Views/) | 4 | Projektdialoge P1–P6 und das Redesign „Berichte & Kosten" |
| [`ueberholt/Protokolle/Update/`](ueberholt/Protokolle/Update/) | 2 | Anlagenzeilen-Eindeutigkeit (Schritt 17) und die E6-Quellensaat |
| [`ueberholt/Protokolle/Bericht/`](ueberholt/Protokolle/Bericht/) | 1 | `LIESMICH_Phase1.md` — Phasen-Historie des Berichtsmoduls |
| [`ueberholt/Protokolle/EPOS.Kern_Import/`](ueberholt/Protokolle/EPOS.Kern_Import/) | 1 | `PvKatalog_Koeffizienten_Protokoll.md` — Befund und Reparatur der PV-Modulkoeffizienten (Schemaschritt 69) |

### ueberholt/Referenzbasen — die Protokolle der 24 entfernten Referenzbasen

Mit dem Anwenderentscheid **AUF‑Q1** vom 12.09.2026 („ausführen", Auftrag #244) ist die
Git-Geschichte umgeschrieben worden; die 24 historischen Referenzbasen unter `Referenzlaeufe/`
sind seither weder im Arbeitsbaum noch in der Geschichte. **Vorher gesichert** wurde je Basis ihr
Protokoll — byte-gleich, **25 Dateien, 603 913 Byte**. Der Wegweiser des Ordners ist
[`ueberholt/Referenzbasen/LIESMICH.md`](ueberholt/Referenzbasen/LIESMICH.md) mit der Tabelle
Basis → Datum → Zweck; hier stehen die Dateien selbst. Sie sind **byte-gleiche Kopien** — auch
in ihren Verweisen, von denen fünf ins Leere zeigen (die Wache führt sie als vorbestehende
Lücken).

| Basis | gesichertes Protokoll |
|---|---|
| `2026-08-27_V0` | [`ueberholt/Referenzbasen/2026-08-27_V0/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-08-27_V0/lauf_protokoll.md) |
| `2026-08-27_K1` | [`ueberholt/Referenzbasen/2026-08-27_K1/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-08-27_K1/lauf_protokoll.md) |
| `2026-08-27_A1` | [`ueberholt/Referenzbasen/2026-08-27_A1/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-08-27_A1/lauf_protokoll.md) |
| `2026-08-27_E1` | [`ueberholt/Referenzbasen/2026-08-27_E1/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-08-27_E1/lauf_protokoll.md) |
| `2026-08-28_P1` | [`ueberholt/Referenzbasen/2026-08-28_P1/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-08-28_P1/lauf_protokoll.md) |
| `2026-08-28_B2` | [`ueberholt/Referenzbasen/2026-08-28_B2/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-08-28_B2/lauf_protokoll.md) |
| `2026-08-28_E2` | [`ueberholt/Referenzbasen/2026-08-28_E2/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-08-28_E2/lauf_protokoll.md) |
| `2026-08-29_Booster` | [`ueberholt/Referenzbasen/2026-08-29_Booster/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-08-29_Booster/lauf_protokoll.md) |
| `2026-08-29_E1E2` | [`ueberholt/Referenzbasen/2026-08-29_E1E2/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-08-29_E1E2/lauf_protokoll.md) |
| `2026-08-30_B3-Kaskade` | [`ueberholt/Referenzbasen/2026-08-30_B3-Kaskade/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-08-30_B3-Kaskade/lauf_protokoll.md) |
| `2026-09-02_PA0_vor-PaketA` | [`ueberholt/Referenzbasen/2026-09-02_PA0_vor-PaketA/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-09-02_PA0_vor-PaketA/lauf_protokoll.md) |
| `2026-09-02_PA1_nach-PaketA` | [`ueberholt/Referenzbasen/2026-09-02_PA1_nach-PaketA/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-09-02_PA1_nach-PaketA/lauf_protokoll.md) |
| `2026-09-03_M1_nach-Merge` | [`ueberholt/Referenzbasen/2026-09-03_M1_nach-Merge/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-09-03_M1_nach-Merge/lauf_protokoll.md) |
| `2026-09-03_M2_nach-Merge2` | [`ueberholt/Referenzbasen/2026-09-03_M2_nach-Merge2/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-09-03_M2_nach-Merge2/lauf_protokoll.md) |
| `2026-09-03_M3_nach-Merge3` | [`ueberholt/Referenzbasen/2026-09-03_M3_nach-Merge3/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-09-03_M3_nach-Merge3/lauf_protokoll.md) |
| `2026-09-03_M4_nach-Merge4` | [`ueberholt/Referenzbasen/2026-09-03_M4_nach-Merge4/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-09-03_M4_nach-Merge4/lauf_protokoll.md) |
| `2026-09-03_PB1_nach-PaketB` | [`ueberholt/Referenzbasen/2026-09-03_PB1_nach-PaketB/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-09-03_PB1_nach-PaketB/lauf_protokoll.md) |
| `2026-09-05_M5_nach-Merge5` | [`ueberholt/Referenzbasen/2026-09-05_M5_nach-Merge5/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-09-05_M5_nach-Merge5/lauf_protokoll.md) |
| `2026-09-05_R2_Zeitbasis` | [`ueberholt/Referenzbasen/2026-09-05_R2_Zeitbasis/protokoll.txt`](ueberholt/Referenzbasen/2026-09-05_R2_Zeitbasis/protokoll.txt) |
| `2026-09-06_R3_Straenge` | [`ueberholt/Referenzbasen/2026-09-06_R3_Straenge/protokoll.txt`](ueberholt/Referenzbasen/2026-09-06_R3_Straenge/protokoll.txt) |
| `2026-09-07_M7_nach-Merge7` | [`ueberholt/Referenzbasen/2026-09-07_M7_nach-Merge7/lauf_protokoll.md`](ueberholt/Referenzbasen/2026-09-07_M7_nach-Merge7/lauf_protokoll.md), dazu [`vergleich_M5_zu_M7.txt`](ueberholt/Referenzbasen/2026-09-07_M7_nach-Merge7/vergleich_M5_zu_M7.txt) |
| `2026-09-07_R4_Double` | [`ueberholt/Referenzbasen/2026-09-07_R4_Double/protokoll.txt`](ueberholt/Referenzbasen/2026-09-07_R4_Double/protokoll.txt) |
| `2026-09-07_R5_Zahlenrand` | [`ueberholt/Referenzbasen/2026-09-07_R5_Zahlenrand/protokoll.txt`](ueberholt/Referenzbasen/2026-09-07_R5_Zahlenrand/protokoll.txt) |
| `2026-09-07_R6_PvKoeffizienten` | [`ueberholt/Referenzbasen/2026-09-07_R6_PvKoeffizienten/protokoll.txt`](ueberholt/Referenzbasen/2026-09-07_R6_PvKoeffizienten/protokoll.txt) |

### ueberholt/Geschichte — die Commit-Karte des Umschreibens

Der Ordner `ueberholt/Geschichte/` führt **keine** Markdown-Papiere, sondern die eine Textdatei
`commit-map_2026-09-12.txt`: die Zuordnung **alte → neue Commit-Kennung** aus dem Umschreiben der
Git-Geschichte (AUF‑Q1, #244). Sie ist der Schlüssel zu jeder Kennung, die in einem Statusblock,
Protokoll oder Konzept von **vor dem 12.09.2026** steht — solche Kennungen treffen im heutigen
Repository nichts. Eine Indexzeile braucht die Datei nicht: Der Index führt Papiere (`.md`), und
die Wache prüft nur diese.

---

## Bleibt am Ort

Diese Papiere sind **nicht** hierher gezogen, weil sie an ihrer Datei, ihrem Werkzeug oder
ihrem Ladeort hängen:

| Ort | Warum |
|---|---|
| [`../CLAUDE.md`](../CLAUDE.md), [`../EPOS.Kern/CLAUDE.md`](../EPOS.Kern/CLAUDE.md), [`../EPOS.UI/CLAUDE.md`](../EPOS.UI/CLAUDE.md), [`../EPOS.iOS/CLAUDE.md`](../EPOS.iOS/CLAUDE.md), [`../WindowsFormsApplication1/CLAUDE.md`](../WindowsFormsApplication1/CLAUDE.md) | Claude Code lädt sie **an ihrem Ort** — das ist die Voraussetzung dafür, dass die Dokumentation „nach wie vor für Claude genutzt" wird |
| [`../README.md`](../README.md) | Startseite des Repositoriums auf GitHub |
| [`../Referenzlaeufe/LIESMICH.md`](../Referenzlaeufe/LIESMICH.md) | beschreibt die Regressionsbasen im selben Ordner und wird bei jedem Einfrieren fortgeschrieben |
| [`../Proben/Rasterprobe/LIESMICH.md`](../Proben/Rasterprobe/LIESMICH.md), [`../Werkzeuge/Formularkarte/LIESMICH.md`](../Werkzeuge/Formularkarte/LIESMICH.md), [`../Werkzeuge/SqlDialektPruefer/LIESMICH.md`](../Werkzeuge/SqlDialektPruefer/LIESMICH.md) | Bedienungsanleitung des Werkzeugs daneben; `Formularkarte.Tests` liest ihre Datei sogar als Prüfmuster |
| [`../sql/LIESMICH.md`](../sql/LIESMICH.md), [`../sql/pv_katalog/LIESMICH.md`](../sql/pv_katalog/LIESMICH.md) | erklären die Skripte und Schemadateien ihres Ordners |
| [`../Setup/Vorlage/LIESMICH.md`](../Setup/Vorlage/LIESMICH.md) | einziger versionierter Inhalt des Vorlagenordners, auf den die `.gitignore` verweist |
| [`../EPOS-Plan_Beispiele_Geruest/Beispiele/README.md`](../EPOS-Plan_Beispiele_Geruest/Beispiele/README.md), [`../EPOS-Plan_Beispiele_Geruest/Beispiele/_vorlage/text.md`](../EPOS-Plan_Beispiele_Geruest/Beispiele/_vorlage/text.md) | Kurzfassung und **Vorlage** des Beispiel-Gerüsts; `neues-beispiel.ps1` kopiert `text.md` |
| [`../Lizenzserver/EINBAU-Lizenzserver.md`](../Lizenzserver/EINBAU-Lizenzserver.md) | Einbauanleitung neben der PHP-Quelle des WordPress-Plugins |
| [`../VDI-3805-Daten/PV/LIESMICH_CEC_Inverters.md`](../VDI-3805-Daten/PV/LIESMICH_CEC_Inverters.md), [`../VDI-3805-Daten/Stromspeicher/LIESMICH_bslib.md`](../VDI-3805-Daten/Stromspeicher/LIESMICH_bslib.md) | Beipackzettel der ausgelieferten Katalogdateien — `LIESMICH_bslib.md` wird von einem Test gelesen |
| [`../Projekte/Speichersimulation/`](../Projekte/Speichersimulation/) (11 Dateien) | das Python-**Referenzpaket** als Ganzes: Referenzkern, Spezifikationsteile, Beispieldaten, docx. „Referenz, kein Werkzeug" (Wurzel-`CLAUDE.md`); die fünf Verweis-Stummel darin zeigen auf die maßgeblichen Fassungen hier |

## Was mit #241 weggefallen ist

Acht byte-gleiche Dubletten sind entfernt worden, nicht bewegt — je einmal blieb die Fassung
am maßgeblichen Ort: `Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` (Wurzelkopie; die
Fassung im Ordner `Wirtschaftlichkeit_Kosten/` gilt), `vdi3805_importer.md` in der Wurzel und
im Werkzeugordner (die dortige `README.md` ist Zeichen für Zeichen dieselbe Datei) und die
fünf Kopien unter `WindowsFormsApplication1/Allgemein/Waermespeicher/`
(`Grundlagen_3`, `Grundlagen_4`, `Grundlagen_5`, `KONTEXT_Waermespeicher-Tool`,
`Konzept_TWW-Zapfprofile_WP-Plan_1` — sämtlich byte-gleich zu ihren Wurzelfassungen).
