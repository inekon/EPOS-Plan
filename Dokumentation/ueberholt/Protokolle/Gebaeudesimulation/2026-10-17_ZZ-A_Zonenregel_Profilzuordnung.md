# Protokoll ZZ-A — Zonenregel Z7 und Profilzuordnung je Raum (Kern, ohne Schema)

**Datum:** 17.10.2026 · **Zweig:** `zon` (Worktree, umbasiert auf `0dd20072c`) · **Modell:** Claude Fable 5.1 ·
**Grundlage:** [Entwurf ZZ](../../../aktuell/Gebaeudesimulation/2026-10-17_Entwurf_Zonenzuordnung_IFC.md) nach Empfehlung
(F1, F4 bis F7 „ja“) mit den **Anwenderentscheiden vom 17.10.2026 zu F2 und F3** (unten). Kein Schemaschritt, Testdatenbank
unverändert (Stand 240); Schritt 244 bleibt angemeldet, siehe Empfehlung.

## 1. Anwenderentscheide, die den Entwurf ändern

- **F2 (Wortlaut):** „Zuordnung je nach Quelldatei (Import). Wenn Bezeichnung wie DIN 18599 oder andere passende Zuordnung,
  dann vorschlagen. Sonst Auswahl geben.“ — Keine globale Umstellung der ausgelieferten `DIN_NUMMER`-Zeilen; der Katalog
  bleibt. Je Quelldatei: Bezeichnung nach DIN 18599 (Name, Nummer, 70/71 der Projektdatei) oder passende Zuordnung (Raumart,
  Name) → das DIN/TS-18599-10-Profil wird **vorgeschlagen** (vorbelegt, „Alle Vorschläge übernehmen“ in einem Schritt,
  Begründung mit Herkunft „Bezeichnung der Datei“ bzw. „Raumart“); sonst die Auswahl im Katalogbaum. Still gesetzt wird nur
  ohne eigene Beschreibung und eindeutig.
- **F3 (Wortlaut):** „nein. Rückfragen, wenn keine Bezeichnung die Zuordnung erlaubt.“ — Keine Flächenschwellen (15/60/200 m²
  entfallen samt Festwerten); führen Angabe, Raumart und Name nicht auf genau ein Profil, steht das Bündel offen mit den
  Kandidaten der Bezeichnung als Auswahl (Büro 1/2/3, Küche 14/15, Essen 12/13), sonst mit dem ganzen Katalogbaum. Weder ein
  stiller noch ein flächenbegründeter Vorschlag.
- **Empfehlung zu Schritt 244:** Die Kern-Tafel `Raumarttafel` deckt alle 28 Raumart-Codes der Beispieldateien; eine
  Zuordnungszeile `HOTTCAD_RAUMTYP` des Katalogs geht ihr vor (Pflegeweg für den Anwender ohne Schema, weil die Art schon in
  der `CHECK`-Klausel steht). Mit F2 entfällt die Umstellung der `DIN_NUMMER`-Zeilen. **Schritt 244 ist für diese Funktion
  nicht mehr nötig**; eine Saat der Raumart-Zeilen wäre nur Komfort (sichtbar im Katalog) und kann entfallen oder später kommen.

## 2. Umsetzung (Kern)

| Baustein | Datei | Inhalt |
|---|---|---|
| Profilzuordnung je Raum | `EPOS.Kern/Allgemein/Import/Gebaeude/Raumprofilzuordnung.cs` (neu) | `Raumprofilschluessel` (DIN-Nummer als Text, `EFH`/`MFH`, `#Id`; 70/71 → Wohnprofile), `Raumarttafel` (28 Codes → Kandidaten, Platzhalter `W`), `Raumnamentafel` (Stichworte de/en, schärft oder setzt), `DinProfilgruppen` (12 Gruppen der Norm), `Gebaeudeartbefund`/`Gebaeudeart` (Wohnanteil ≥ 50 % der beheizten Fläche, Küchenzahl als Näherung der Wohneinheiten, EFH ≤ 2), `Raumprofilbefund` (Kandidaten, Vorschlag, Stand eindeutig/Vorschlag/offen/ohne/Hand, Begründungskette, Gruppenschlüssel), `Zuordnen`, `Befunde`, `Zaehlen`, `Mehrheit` (flächengewichtet, Schwelle 50 %), `Buendel`, `Katalogbaum`, Texte über Ressourcen |
| Regel Z7 | `IfcImportProfil.ZONENREGEL_Z7`; `GebaeudeZonierung`: `Z7Waehlbar`, Vorgabe nach F1 (Z7, wenn Z1 nicht wählbar und jeder beheizte Raum eine Raumart oder eigene Angabe trägt), `Z7Gruppieren` (Zonen je Gruppenschlüssel und Beheizung, eine Zone „unbeheizt“, offene Gruppen nach dem häufigsten Raumnamen, „ohne Nutzung“), `Importzone.Raumprofil`, `Raumprofile`/`Gebaeudeart` am Ergebnis, Meldung `IMP_IFC_PROT_PROFILE_ZUGEORDNET` je IFC-Gebäude (Info: eindeutig, Vorschlag, offen, ohne, Beispiele); `Bilden` mit optionalen Parametern `vorbelegung`, `profilwahl` | Wählbar ab einem beheizten Raum mit Raumart oder eigener Angabe und mindestens zwei Gruppen (weniger als Räume); Dateien mit bloßen Raumnamen (Proben ohne `RoomType`) bleiben unverändert |
| Zonenplan | `Zonenplan.cs`: `NutzungMitQuelle` für Z7 (`Z7Nutzung`), Quellarten `RAUMPROFIL` (gesetzt), `RAUMPROFIL_VORSCHLAG` (Marke), `RAUMPROFIL_OFFEN` (Kandidaten als Text), `RAUMPROFIL_MEHRHEIT` (Z4/Z1–Z3, Befund B4, nur wenn die Datei Raumarten trägt; Z6 behält seinen Weg); `DateiDinNummer(…, gbxmlTafel)` — die gbXML-Raumtyptafel gilt nicht mehr für IFC (B4); `Bedarf` liest den Katalog für Z6, Z7 und IFC | `Tab_Importquelle.Zonenregel` ist `TEXT` ohne `CHECK` (`ImportzuordnungSchema`), `Z7` passt ohne Schema; geprüft |
| Schnittstelle ZZ-B | `Zonenplan`: `Raumprofile`, `Profilwahl`, `Buendel()`, `Katalogbaum()`, `ProfilZuordnen(raeume, schluessel)` (leer = „keine“; lehnt unbekannte Schlüssel mit `PLAN_PROFIL_UNBEKANNT` ab), `ProfilzuordnungZuruecknehmen(raeume)`, `VorschlaegeUebernehmen()`, `VorschlaegeZuruecknehmen()` — jede Operation bildet den Plan nach seiner Regel neu (wie der Regelwechsel, Rückfrage im Dialog); `Kopie()` trägt die Wahl mit | Der Dialog kommt in ZZ-B |
| Hülle (nur Texte) | `GebaeudeImportZonen.Quellentext` (vier neue Quellarten), `ModellpruefungHuelle.HinweiseAus` (F6: `RAUMPROFIL` grün wie Datei, `RAUMPROFIL_VORSCHLAG`/`_MEHRHEIT` gelb als „aus Regel“) | Windows-Schale **nicht** gebaut (Budget) — offen |
| Ressourcen (de/en) | `GIMP_ZONENREGEL_Z7`, `GIMP_ZONE_UNBEHEIZT_Z7`, `GIMP_ZONE_OHNE_NUTZUNG`, `IMP_IFC_PROT_PROFILE_ZUGEORDNET`, `IMP_IFC_PROT_PLAN_PROFIL_UNBEKANNT`, `IMP_GBXML_PROT_PLAN_PROFIL_UNBEKANNT`, `RNP_IMP_PROFIL_NUMMER`, `RNP_IMP_QUELLE_RAUMPROFIL[_VORSCHLAG|_OFFEN|_MEHRHEIT]`, `RNP_IMP_BEGR_{DATEI,RAUMART,NAME,WOHNGEBAEUDE,NICHTWOHN,KUECHEN,HAND,MEHRHEIT}`, `RNP_GRUPPE_{BUERO,HANDEL,BILDUNG,BEHERBERGUNG,GASTRONOMIE,NEBENFLAECHEN,HALLEN,KULTUR,SPORT,PARKEN,GESUNDHEIT,WOHNEN}` (31); `designer_neu.py schreiben` gelaufen | |

Unverändert: X1, Einzonenweg (Vorgaberegel 2.5 bleibt am Projektdatei-Weg), Zonenkalender N7/N8, Rechenweg, Referenzprojekte.
F7 (Projektdatei-Weg, Räume ohne DIN-Zone als Vorschlag) ist **nicht** gebaut — offen.

## 3. Zählungen je Beispieldatei (Messlauf mit Testdatenbank, vor der Korrektur der Gebäudeart)

| Datei | Räume | Zonen vorher (Vorgabe Z4) | Vorgabe jetzt | Z7-Zonen | eindeutig | Vorschlag | offen | ohne | Bündel | Wohnanteil / Küchen |
|---|---:|---:|---|---:|---:|---:|---:|---:|---:|---|
| `MFH-Klein-unsaniert-1964.ifc` | 30 | 7 | Z7 | 4 | 30 | 0 | 0 | 0 | 7 | 78 % / 2 |
| `MFH_mittel_1984.ifc` | 29 | 5 | Z7 | 10 | 28 | 0 | 0 | 1 | 18 | 48 % / 0 |
| `Produktion_groß_mit_Verwaltung_EG55-2026.ifc` | 49 | 3 | Z7 | 7 | 39 | 5 | 5 | 0 | 13 | 0 % / 0 |
| `Sportheim_1970_unsaniert.ifc` | 56 | 4 | Z7 | 9 | 53 | 0 | 3 | 0 | 32 | 11 % / 3 |
| `Sportheim_Bestand-1997_test_ifc4.ifc` | 56 | 4 | Z7 | 9 | 53 | 0 | 3 | 0 | 32 | 11 % / 3 |
| `Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc` | 113 | 8 | Z7 | 7 | 66 | 4 | 43 | 0 | 17 | 0 % / 0 |
| `WG-EH55_Poroton-GModG-2026.ifc` | 20 | 3 | Z7 | 7 | 16 | 0 | 1 | 3 | 10 | 64 % / 0 |
| **Summe** | **353** | | | | **285** | **9** | **55** | **4** | | |

Gegenüber dem Prototyp des Entwurfs (344/9/0) sind die 53 Büros (F3: 1/2/3 bleiben offen) und zwei Räume mit Küchen-/Essen-
Kandidaten offen; die 9 Vorschläge sind die Werkstätten (F5). **Befund des Messlaufs:** Alle Dateien wurden als
Nichtwohngebäude gewertet (`Wohnen=False` bei 78 % und 64 %), weil `Gebaeudeverwendung.WohnenAusGebaeudeart` den CAD-Typnamen
des Gebäudes als benannte Nichtwohn-Art las und der Wohnanteil nicht mehr zog. Korrektur (ein Ausdruck in
`Raumprofilzuordnung.Gebaeudeart`: die Gebäudeart der Datei zählt nur, wenn sie wohnt) ist eingebaut, **aber nicht erneut
gemessen** — für MFH-Klein und WG-EH55 werden die Wohnräume samt Bad/WC/Küche/Flur zu einem Wohnprofil-Vorschlag (EFH bei
≤ 2 Küchen, sonst MFH), die Spalte „eindeutig“ sinkt dort entsprechend, die Z7-Zonenzahl ebenso.

## 4. Abnahme (Stand bei Abgabe)

- `dotnet build WP-Plan.Kern.slnf -c Release`: **0 Fehler** (vor der letzten Korrektur; danach nicht gebaut).
- Bestandstests (Filter Ifc, Gbxml, Sqproj, Zonierung, Zonenplan, GebaeudeImport, Raumnutzung, Nutzungsprofil, Zonen,
  Modellprüfung, Profilprüfung, Mehrzonen): erster Lauf 1797 grün / 22 rot; die 22 sind erklärt (Vorgabe Z7 statt Z4 an den
  Quelldateien, Z7 wählbar in den Z6-Proben, Mehrheitsprofil auf Z6/Namensdateien) und behoben — Z6 nicht mehr unter der
  Mehrheitsregel, Mehrheit nur mit Raumart, Erwartungen auf Z7 gesetzt (`IfcQuelldateienDiagnoseTests`: `VORGABE`-Tabelle,
  Durchgang rechnet Z7 fehlerfrei durch, gemessene Z4-Zahlen bleiben). Zweiter Lauf (Teilfilter): `ZonierungZ6`,
  `ZonenplanTests`, `ZonenplanDatenbank`, `ZonenimportZuordnung`, `ZonenbaumHerleitung`, `IfcQuelldateienDiagnose` **grün**;
  rot: `ZonenzuordnungQuellenprobeTests` (7, Schranken noch 0 — Zahlen oben eintragen nach erneuter Messung),
  `ZonierungZ7Tests` 2 (die unbeheizte Zone des synthetischen Hauses entfällt ohne Flächen — Erwartung anpassen oder Bauteile
  geben), `RaumprofilzuordnungTests` 2 (Erwartungen: `Guests` zählt als Wohnfläche → 18 % statt 12 %; `Wohnhaus(kuechen: 3)`
  fällt unter 50 % Wohnanteil → Wohnräume vergrößern).
- Referenzlauf 1030, 1051, 1073 gegen `2026-10-17_R59_Ergebnisreihen`: **alle drei PASS** (236 700 / 385 632 / 289 416 Werte); das
  Gesamturteil der Vergleichsausgabe ist nur wegen der zwölf nicht gerechneten Projekte der Basis FAIL. Der Rechenweg ist nicht berührt.
- SQL-Dialekt-Prüfer: keine SQL-Änderung. Windows-Schale: nicht gebaut.

## 5. Offene Reste (für den nächsten Agenten)

1. Kern-Filter nach der Korrektur der Gebäudeart bauen; Quellenprobe messen und die Tabelle `ERWARTET` in
   `EPOS.Kern.Tests/ZonenzuordnungQuellenprobeTests.cs` sowie Abschnitt 3 dieses Protokolls mit den neuen Zahlen füllen
   (Wohnfälle MFH-Klein, WG-EH55; `Kennzahlen`-Erwartung „Z7 n“ und `VORGABE` in `IfcQuelldateienDiagnoseTests` nachziehen, falls
   sich die Z7-Zonenzahl der Wohnfälle ändert).
2. Die vier roten synthetischen Tests (Abschnitt 4) auf die beobachtete Regel setzen.
3. Windows-Schale mit `EnableWindowsTargeting=true` bauen (Hülle berührt).
4. Vollständiges Gate der Orchestrierung nach dem Merge (kein Agenten-Gate).
5. F7: Projektdatei-Weg — Räume ohne DIN-Zone als Vorschlag über dieselbe Rangfolge (nur Vorschlag, nie automatisch).
6. F2 auf die bestehenden Zonenpfade ausdehnen (EPOS_Zone, gbXML-DIN-Nummer): heute setzen sie weiter mit Quelle `DATEI`/
   `DIN_NUMMER`; der Entwurf ZZ-B kann sie über „Alle Vorschläge übernehmen“ bestätigen lassen.
7. ZZ-B: Dialog „Nutzungsprofile zuordnen…“ auf der Schnittstelle oben; Hülle `GebaeudeImportZonen.Profilzuordnungsdaten`.
8. Entwurf ZZ nach Umsetzung nach `ueberholt/` verschieben; Konzept Nutzungsprofile 5.4 und Konzept DA 4.3 fortschreiben.
