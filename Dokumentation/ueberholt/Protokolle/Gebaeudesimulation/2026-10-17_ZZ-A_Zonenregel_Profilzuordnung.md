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
| Hülle (nur Texte) | `GebaeudeImportZonen.Quellentext` (vier neue Quellarten), `ModellpruefungHuelle.HinweiseAus` (F6: `RAUMPROFIL` grün wie Datei, `RAUMPROFIL_VORSCHLAG`/`_MEHRHEIT` gelb als „aus Regel“) | Windows-Schale gebaut (Abschnitt 5) |
| Ressourcen (de/en) | `GIMP_ZONENREGEL_Z7`, `GIMP_ZONE_UNBEHEIZT_Z7`, `GIMP_ZONE_OHNE_NUTZUNG`, `IMP_IFC_PROT_PROFILE_ZUGEORDNET`, `IMP_IFC_PROT_PLAN_PROFIL_UNBEKANNT`, `IMP_GBXML_PROT_PLAN_PROFIL_UNBEKANNT`, `RNP_IMP_PROFIL_NUMMER`, `RNP_IMP_QUELLE_RAUMPROFIL[_VORSCHLAG|_OFFEN|_MEHRHEIT]`, `RNP_IMP_BEGR_{DATEI,RAUMART,NAME,WOHNGEBAEUDE,NICHTWOHN,KUECHEN,HAND,MEHRHEIT}`, `RNP_GRUPPE_{BUERO,HANDEL,BILDUNG,BEHERBERGUNG,GASTRONOMIE,NEBENFLAECHEN,HALLEN,KULTUR,SPORT,PARKEN,GESUNDHEIT,WOHNEN}` (31); `designer_neu.py schreiben` gelaufen | |

Unverändert: X1, Einzonenweg (Vorgaberegel 2.5 bleibt am Projektdatei-Weg), Zonenkalender N7/N8, Rechenweg, Referenzprojekte.
F7 und F2 auf den bestehenden Zonenpfaden sind im Rest-Auftrag gebaut (Abschnitt 4).

## 3. Zählungen je Beispieldatei (Messlauf mit Testdatenbank, nach der Korrektur der Gebäudeart)

| Datei | Räume | Zonen vorher (Vorgabe Z4) | Vorgabe jetzt | Z7-Zonen | eindeutig | Vorschlag | offen | ohne | Bündel | Wohnanteil / Küchen |
|---|---:|---:|---|---:|---:|---:|---:|---:|---:|---|
| `MFH-Klein-unsaniert-1964.ifc` | 30 | 7 | Z7 | 2 | 6 | 24 | 0 | 0 | 7 | 78 % / 2 → Wohngebäude, EFH |
| `MFH_mittel_1984.ifc` | 29 | 5 | Z7 | 10 | 28 | 0 | 0 | 1 | 18 | 48 % / 0 → Nichtwohngebäude |
| `Produktion_groß_mit_Verwaltung_EG55-2026.ifc` | 49 | 3 | Z7 | 7 | 39 | 5 | 5 | 0 | 13 | 0 % / 0 |
| `Sportheim_1970_unsaniert.ifc` | 56 | 4 | Z7 | 9 | 53 | 0 | 3 | 0 | 32 | 11 % / 3 |
| `Sportheim_Bestand-1997_test_ifc4.ifc` | 56 | 4 | Z7 | 9 | 53 | 0 | 3 | 0 | 32 | 11 % / 3 |
| `Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc` | 113 | 8 | Z7 | 7 | 66 | 4 | 43 | 0 | 17 | 0 % / 0 |
| `WG-EH55_Poroton-GModG-2026.ifc` | 20 | 3 | Z7 | 4 | 4 | 15 | 1 | 0 | 10 | 64 % / 0 → Wohngebäude, MFH |
| **Summe** | **353** | | | | **249** | **48** | **55** | **1** | | |

Die Korrektur der Gebäudeart wirkt an den zwei Wohnfällen: MFH-Klein fasst 23 Wohnräume samt Flur, Küche und WC in die Zone
„Wohngebäude – Einfamilienhaus“ (Vorschlag, 2 Küchen ≤ 2), daneben „unbeheizt“; WG-EH55 fasst 15 Räume in „Wohngebäude –
Mehrfamilienhaus“ (Vorschlag; ohne Küchenraum bleibt die Näherung MFH), daneben Sport, Lager und das offene Büro. Gegenüber dem
ersten Messlauf: eindeutig 285 → 249, Vorschlag 9 → 48, ohne 4 → 1 (der Rückgang „ohne“ sind die drei Wohnräume des WG im
Nichtwohnfall). **Befund MFH_mittel:** Der Wohnanteil liegt mit 48 % knapp unter der Schwelle 50 % (Gast-, Kinder-, Behandlungs-
und Fitnessräume); die Datei wird als Nichtwohngebäude gewertet, der Schlafraum bleibt ohne Profil — die Regel F4 gilt wie
entschieden. Die Zahlen stehen als Schranken in `ZonenzuordnungQuellenprobeTests.ERWARTET`; `IfcQuelldateienDiagnoseTests`
(`Vorgabe` „Z7 2“/„Z7 4“, `VORGABE`) ist nachgezogen.

**Anwenderdatei mit Projektdatei (F7):** `Sportheim_1970_unsaniert.ifc` + `.sqproj` — 12 Zonen der Projektdatei übernommen,
0 Räume ohne Zone, also 0 Vorschläge (die Projektdatei deckt jeden Raum).

## 4. Rest-Auftrag: rote Fälle, F7, F2

**Rote synthetische Erwartungen** (erst geprüft, ob Erwartung oder Code falsch ist):

- `ZonierungZ7Tests` „Zonen_je_Profil_und_Beheizung…“ und Folgefehler „Der_Zonenplan_setzt_Profil…“: **Erwartung falsch.** Das
  synthetische Haus trägt keine Bauteile; die Zone „unbeheizt“ entfällt ohne jede Fläche (`OhneFlaechenEntfallen`,
  Mehrzonenkonzept 6.5, gilt für jede Regel) und meldet `ZONE_OHNE_FLAECHEN`. Erwartung: fünf Zonen, der Keller außerhalb.
- `RaumprofilzuordnungTests` „Im_Nichtwohngebaeude…“: **Erwartung falsch.** `Guests` ist ein Wohncode (Gästezimmer im Haus,
  Kandidaten Wohnprofil/11); der Wohnanteil ist 45 von 251 m² = 18 %.
- `RaumprofilzuordnungTests` „Mehr_als_zwei_Kuechen…“: **Erwartung falsch.** Drei Küchen drücken den Wohnanteil unter 50 %;
  mit 60 m² Wohnzimmer (55 %) prüft der Fall wieder, was er soll (MFH; Gebäudeart der Datei macht EFH eindeutig).
- Dabei aufgefallen und **im Code behoben:** `Raumprofilschluessel.Profil` löste eine DIN-Nummer über `DinTreffer` auf, also
  über die ausgelieferte Zuordnung `DIN_NUMMER` auf das EPOS-Muster; F2 verlangt das Profil der Kategorie DIN/TS 18599-10. Jetzt
  `AusDinKatalog` vor `DinTreffer` (ohne Katalog wie bisher die alte Kennung). Die Quellenprobe prüft das an jeder Zone.

**F7 (Projektdatei-Weg, Regel X1):** `Zonenplan.Restvorschlaege()` liefert je beheiztem, nicht zugeordnetem Raum den Befund der
Rangfolge (Gebäudeart aus allen Räumen); `SqprojZonen.Uebernehmen` legt ihn in `SqprojZonenergebnis.Raumvorschlaege` und meldet
`IMP_SQ_PROT_RAUM_OHNE_ZONE_VORSCHLAG` (Info: Zahl, davon mit Vorschlag, Beispiele). Nie automatisch: kein Raum wird zugeordnet,
die Zonen der Projektdatei bleiben. Test `SqprojRaumvorschlagTests` (synthetisch: Besprechung 4, Werkstatt 23 als Vorschlag,
Büro offen 1/2/3, ein Raum ohne; Gegenprobe ohne offene Räume; Zählung an der Anwenderdatei, ohne Datei übersprungen).

**F2 auf den bestehenden Zonenpfaden:** Heute setzten `EPOS_Zone.Nutzung` (Quelle `DATEI`) und die DIN-Nummer aus
`EPOS_Zone.DinNummer` bzw. dem gbXML-Raumtyp (Quelle `DIN_NUMMER`) das Profil still. Umgestellt in
`Zonenplan.DateibezeichnungAlsVorschlag`: Das Profil bleibt vorbelegt (Rechenweg unverändert), die Quelle wird
`RAUMPROFIL_VORSCHLAG` mit der Begründung „Bezeichnung der Datei „DIN n““ bzw. dem Nutzungstext (Ampel gelb, F6);
`Planzone.Dateivorschlag` trägt den Schlüssel. `VorschlaegeUebernehmen` bestätigt auch diese Zonen (Wahl von Hand je beheiztem
Raum, auch für gbXML ohne Befunde je Raum) → Quelle `RAUMPROFIL`; `VorschlaegeZuruecknehmen` stellt den Vorschlag wieder her. Eine
DIN-Nummer ohne Katalogtreffer behält `DIN_NUMMER` (die Modellprüfung schlägt das nächstliegende DIN-Profil vor). Die Zonen der
Projektdatei (`SqprojZonen`, „aus DIN-Nr. n der Projektdatei“) und gespeicherte Pläne sind nicht berührt. Test
`ZonenplanDateibezeichnungTests` (IFC Z6 und gbXML). Die öffentliche Schnittstelle des `Zonenplan` für ZZ-B ist unverändert;
neu sind `Planzone.Dateivorschlag` und `Zonenplan.Restvorschlaege()`.

Neue Ressource (de/en): `IMP_SQ_PROT_RAUM_OHNE_ZONE_VORSCHLAG`; `designer_neu.py schreiben` gelaufen.

## 5. Abnahme (Rest-Auftrag)

- `dotnet build WP-Plan.Kern.slnf -c Release`: **0 Fehler**.
- Windows-Schale `WindowsFormsApplication1.csproj -c Debug -p:Platform=x64 -p:EnableWindowsTargeting=true`: **0 Fehler**.
- `EPOS.Kern.Tests`, Filter Ifc, Gbxml, Sqproj, Zonierung, Zonenplan, Zonenimport, Zonenbaum, Raumprofil, Quellenprobe,
  GebaeudeImport, Modellpruefung, DokumentationLinkWache, RepositoryOrdnungWache: **1197 bestanden, 5 übersprungen
  (Probenerzeuger), 0 rot** (1202 Fälle, davon 6 neu).
- `EPOS.UI.Tests`, Filter GebaeudeImport, Modellpruefung: **128 bestanden**.
- Referenzlauf 1030, 1051, 1073 gegen `2026-10-17_R59_Ergebnisreihen`: **alle drei PASS** (236 700 / 385 632 / 289 416 Werte);
  das Gesamturteil FAIL nur wegen der nicht gerechneten übrigen Projekte der Basis.
- SQL-Dialekt-Prüfer: keine SQL-Änderung.

## 6. Offene Reste

1. Vollständiges Gate der Orchestrierung nach dem Merge (kein Agenten-Gate).
2. ZZ-B: Dialog „Nutzungsprofile zuordnen…“ und Hülle; die Hülle zeigt die umgestellten Dateizonen über die bestehende Quellart
   `RAUMPROFIL_VORSCHLAG` (Text „Vorschlag …“, Ampel gelb). Der Dialog kann `Planzone.Dateivorschlag` und für den
   Projektdatei-Weg `SqprojZonenergebnis.Raumvorschlaege` anzeigen.
3. MFH_mittel (48 % Wohnanteil) als Nichtwohngebäude: so nach F4; ändert der Anwender die Schwelle, ändern sich die Schranken.
4. Entwurf ZZ nach Umsetzung nach `ueberholt/` verschieben; Konzept Nutzungsprofile 5.4 und Konzept DA 4.3 fortschreiben.
