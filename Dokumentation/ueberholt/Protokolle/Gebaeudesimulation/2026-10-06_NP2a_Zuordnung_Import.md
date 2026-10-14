# Protokoll NP2a — Zuordnung als Leser, Zonenplan mit Profil, Übernahme über den Generator (06.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle NP2a der Stufe NP. Zweig `np2a-zuordnung-import` auf `5d9a8188`: `accc33b1` NP2a-1, `5bdc1d27` NP2a-2, `b8348a01` NP2a-3, `4c03757e` NP2a-4; Merge `05e4daf4`; gegatet im gemeinsamen Stand mit NP3c auf `1a268145`.
**Festlegungen:** NP-F12, NP-F14, NP-F16, NP-F23 des Konzepts Nutzungsprofile.

## 1 Ergebnis

1. **Zuordnung als Leser mit Vorgabe im Code (NP-F12).** Neu `EPOS.Kern/Allgemein/Import/Gebaeude/Raumnutzungsvorbelegung.cs` (`Planprofil(Id, Name, NichtImKatalog)`). Eine Zeile der Zuordnung gilt vor der Vorgabe, auch „keine“. Ohne Zeile gelten `Din18599Nutzung` bzw. `NutzungAusKlasse`, und die Kennung führt auf das EPOS-Muster gleicher Nutzung. `HOTTCAD_RAUMTYP` geht der Nutzungsklasse vor (mit und ohne Vorsatz `mrt`). Ohne Datenbank gilt die Vorgabe im Code. Probe `RaumnutzungVorbelegungTests` (6).
2. **Zonenplan mit Profil (NP-F23).** `Planzone` und `Importzone` tragen `Profil`, `Nutzung` ist dessen Name. Z6 und `SqprojZonen` belegen über die Zuordnung vor. `IfcAbbildBauer` behält `EPOS_Zone.Nutzung` (Profilname → Profil, alte Kennung → Muster, sonst Text mit Befund). Das Wiederfinden beim erneuten Import läuft über `ZonenplanCtrl.Zonennutzung` (erst `Tab_Zone.Nutzungsprofil`, dann Kalendernutzung), der Export auf demselben Weg. Die Hülle `GebaeudeImportZonen` hat eine Klappliste aus dem Katalog (Schlüssel `#<Id>`). Probe `ZonenplanProfilTests` (IFC-Rundreise mit Profilname, alter Kennung, unbekanntem Text).
3. **Übernahme über den Generator (NP-F16).** `NutzungUebernehmen` geht über `ProfilUebernehmen` mit Fläche und Raumhöhe der Zone, ohne Katalog über den Vorlagenweg. `ProjektdateiUebernehmen` setzt erst das Profil, dann die Größen der Datei (Datei vor Profil). Der Einzonenweg in `WizardCtrl` bleibt ohne Profil — **Anwenderentscheid 06.10.2026:** kein Profil automatisch, Zuweisung über „Nutzungsprofil übernehmen…“ im Gebäudeeditor, der Import zeigt den Vorschlag aus der DIN-Nummer ohne Vorbelegung (NP2b).
4. **Proben.** Neu `ZonenimportZuordnungTests` (alle IFC- und gbXML-Proben unter Z6; alte Paare gleich; Sport, Gastronomie, Lager, Verkehr, Technik bekommen ein Muster statt „keine“, Q42). `LIESMICH_Importproben.md` nachgezogen. Geänderte Erwartungen, weil mit Katalog der Plan das Profil und die Kalender den Profilnamen tragen (NP-F23, NP-F14): `ZonenplanDatenbankTests`, `SqprojDatenbankTests`, `SqprojHuelleTests` (`BUERO`/`WOHNEN` → „Büro“/„Wohnen“), `ZonenplanTests` (`NutzungAus` liefert `Planprofil`).

## 2 Nachweise

Agentenabnahme: Kern 1296 grün, UI 241 grün, SQL 0 Fundstellen, Schale 0 Fehler, Referenzlauf 21/21 PASS, CSV 646 byte-gleich. Gate `gate_haupt.sh NP2a` auf `1a268145` (NP2a und NP3c auf NP3b): Kern-Filter rc=0; ChartProben grün, 211 Hashes gleich; KiKern 549/549, SpeicherEngine 397/397, SpeicherPlanung 27/28 (1 übersprungen), EPOS.UI.Tests 7583/7583, EPOS.Kern.Tests 11245/11248 (3 übersprungen); Dokumentationswachen 35/35; Referenzlauf 21/21 gegen R38: GESAMT PASS (6 872 111 Werte), CSV byte-gleich 646/646; gestörter Lauf PASS; Windows-Schale rc=0; Designer 15 285 Einträge unverändert; SqlDialektPruefer 2 434 Texte, 0 Fundstellen; Werkzeugtests 124/49/24/39; Konfliktmarker keine. Die Statuszeile ist #752.

## 3 Offenes

NP2b (Zonenbaum gruppiert, Herleitung, „(nicht im Katalog)“, Einzonenweg mit Vorschlag) in Arbeit, NP4 offen; kein Wiki-Upload vor NP4.
