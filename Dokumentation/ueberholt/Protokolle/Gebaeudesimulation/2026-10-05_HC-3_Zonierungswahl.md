# Protokoll HC-3 — Zonierungswahl und Größengrenze der Projektdatei (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Zweig `hc3` (Worktree `../EPOS-Plan-hc3`), Bau durch einen Opus-Agenten, Papiere durch einen Sonnet-Agenten. Commits der Welle: `c3d41727` Kern, `99423740` Hülle und Dialog, `229d13f0` Proben 33 und 36, `0b5f83a5` bunit-Probe 37. Kein Schemaschritt, kein SQL, Basis unverändert.
**Entscheid:** E87 (F1, F4), Nachzug an die Projektdatei-Stufe SQ ([Protokoll SQ](2026-10-05_SQ_Projektdatei.md)); Konzept [HottCAD-Verbund](../../../aktuell/Gebaeudesimulation/2026-10-05_Konzept_HottCAD_Verbund_IFC_Projektdatei_Viewer.md) 5.2 und 6.2, Datenaustauschkonzept Kapitel 16.

## 1 Auftrag

Die Projektdatei trägt zwei Zonierungen (DIN-V-18599-Zonen, `ZoneType` 5; Simulationszonen, `ZoneType` 6). Die Räume sollen der vom Anwender gewählten Zonierung gehören, Vorgabe DIN-Zone (sie steht in jeder Projektdatei). Die Projektdatei bekommt eine eigene Größengrenze: 250 MB unter Windows, 100 MB auf iOS.

## 2 Befund

- Kern: `enum SqprojZonierung { Din18599, Simulation }`; `SqprojZonen.Uebernehmen(…, zonierung)` mit `Gehoert`, `Belegte`, `Wirksam`, `Andere`, `Zonierungsmeldung`, `GeteilteGruppe`; `SqprojStand` kennt `HatDinZonen`, `HatSimulationszonen`, `BeideZonierungen`, `Gewaehlt`; `Zonenkonditionierung.Zonierung`; `SqprojProfil.MAX_BYTES` 250 MB, `MAX_BYTES_IOS` 100 MB, `GrenzeFuerPlattform(bool)`; Protokollsätze `IMP_SQ_PROT_ZONIERUNG` und `…_ZONIERUNG_EINE` vor der Bilanz; `IMP_SQ_PROT_ZU_GROSS` nennt MB.
- Hülle und Dialog: Planschritt `PROJEKTDATEI` mit Feld `Zonierung`; Wahlfeld `select[data-zonierung]` (`din`/`sim`) nur bei beiden Zonierungen; Bilanzzeile `dd[data-wert="zonierung"]`; Herkunft je Zone `projektdatei-din` / `projektdatei-sim`; Rückfrage beim Wechsel, wenn Handschritte verloren gingen.
- Ressourcen de/en: neun neue Schlüssel (`IMP_SQ_PROT_ZONIERUNG`, `…_ZONIERUNG_EINE`, `IMP_SQ_ZONIERUNG_DIN`, `…_SIM`, `GIMP_DLG_SQ_ZONIERUNG`, `…_WERT`, `…_FRAGE`, `GIMP_DLG_SQ_HERKUNFT_DIN`, `…_SIM`).

## 3 Abweichungen vom Konzept (mit Grund)

1. Nur die beiden Prüfungen in `ProjektdateiLesen` nutzen die neue Grenze; die Prüfung der Gebäudedatei (IFC/gbXML) bleibt bei `Profil.MaxBytes`.
2. Der Wechsel der Zonierung ist kein eigener Hüllenschritt, sondern der Schritt `PROJEKTDATEI` mit Feld `Zonierung`; die Hülle baut den Plan je Anfrage aus den Schritten neu und liest die Datei nicht erneut (Abbild im Speicher).
3. Die Arbeitskopie wird weiter gelöscht; der Wechsel braucht sie nicht.
4. Eine Wahl vor der Übernahme gilt erst für die nächste Übernahme; bis dahin nennt die Bilanz die zuletzt wirksame Zonierung.
5. Leere Zonen werden nur für die wirksame Zonierung gemeldet.
6. `Gebaeudekonditionierung` (Einzonenweg) zählt nur belegte Zonen der wirksamen Zonierung.

## 4 Tests und Abnahme

Kern-Filter 0 Fehler; Windows-Schale auf Linux 0 Fehler; `EPOS.Kern.Tests` (Sqproj, GebaeudeImport, Zonenplan, Ifc) 548 bestanden, 2 übersprungen (vorher 542); `EPOS.UI.Tests` (GebaeudeImport, Texte) 132 bestanden; `designer_neu.py` 0 Abweichungen. Umgestellt: `SqprojHuelleTests`, `SqprojDatenbankTests` (ausdrücklich Simulation), `SqprojZonenTests`, `SqprojLeserTests` (statt eines IFC-Grenzen-Tests drei: 250/100 MB, Gegenprobe IFC-Grenze, `ZU_GROSS` in MB), Probe 37. Neu: Probenerzeuger `ZweiZonierungen()`, `NurDinZonen()`; `SqprojZonenTests` +4, `SqprojLeserTests` +3, `SqprojHuelleTests` +2, Probe 37 +2. Merge mit dem Zweig `filtertexte` und `origin`: keine Konflikte; Kern-Filter 0 Fehler, `GebaeudeImport|Texte` 132, `Sqproj|Zonenplan` 82 grün.

## 5 Offen

- iOS-Grenze 100 MB gesetzt, nicht gemessen (wie G4-8); kein iOS-Lauf.
- Rasterprobe und `fensterprobe.mjs` nicht gezogen (Kopf und Schlussleiste unverändert).
- Sichtabnahme unter Windows (Wahlfeld, Herkunft je Zone).
- Logbuch-Version beim Anwender erfragen.

## 6 Logbuch-Satz

Beim Dazuladen einer Projektdatei werden die Zonen jetzt standardmäßig aus den DIN-V-18599-Zonen übernommen; enthält die Datei auch Simulationszonen, lässt sich die Zonierung im Gebäudedialog umschalten, und Projektdateien bis 250 MB (iOS 100 MB) werden gelesen.
