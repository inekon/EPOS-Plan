# Protokoll DA-5 — Profilprüfung beim Import, gbXML-Raumtyptafel, DIN-Nummer in IFC

Welle DA-5 aus dem [Konzept Datenaustausch der Simulationsdaten](../../../aktuell/Gebaeudesimulation/2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md)
(Abschnitte 4.3, 5, 7). Grundlage sind E135 (DIN/TS-18599-10-Profile, nicht lizenzpflichtig; die Werte kommen mit DA-6) und
E138 F2 (a) Datei vor Profil. Vorgänger: [Protokoll MA-1a](2026-10-13_MA-1a_Modellpruefung_Kern.md),
[Protokoll MA-2](2026-10-13_MA-2_Nutzung_und_Kalender.md), [Protokoll DA-1](2026-10-13_DA-1_gbXML_Import_Lasten_Zeitplaene.md),
[Protokoll DA-3](2026-10-13_DA-3_IFC_Standardsaetze.md). **Kein Schemaschritt, kein neues SQL.** Rechenweg, Referenzprojekte und
Testdatenbank sind unberührt.

## Befund

- Die Profilregel stand schon in `Modellvorschlagsregeln.ProfilNachRegel` (DIN-Nummer → HottCAD-Raumtyp → Nutzungsklasse →
  Gebäudeart → EPOS-Muster). Die DIN-Nummer löste sie nur über die Zuordnung `DIN_NUMMER` bzw. die Vorgabe im Code auf, nicht
  über die Nummer der Profile der Kategorie DIN/TS 18599-10. Die Zuordnung der Testdatenbank führt 18 Nummern, alle auf
  EPOS-Muster; die 43 nummerierten DIN-Profile erreichte keine Regel.
- Der Import setzte ein Profil nur unter der Zonierungsregel Z6 (Raumtyp bzw. Nutzungsklasse) und aus `EPOS_Zone.Nutzung`.
  Eine DIN-Nummer kannte nur die Projektdatei. gbXML-`spaceType` war gelesen, aber nirgends abgebildet.
- `EPOS_Zone` trug keine DIN-Nummer.
- Prüfgruppe 3 kannte für das Profil nur „Zone ohne Profil“ (gelb). Die Herkunft eines Profils war unbekannt.
- Der Katalog führt die Nummer in `Tab_Raumnutzungsprofil.Nummer` (Text, je Kategorie eindeutig) und die Normkategorie über
  `Tab_Raumnutzungskatalog.Art = 'DIN_V_18599_10'` (NP6/NP7). Das genügt, eine Spalte fehlt nicht.
- Die Spalte `Tab_Raumnutzungszuordnung.Art` hat einen `CHECK` auf die drei Arten `DIN_NUMMER`, `IFC_KLASSE`, `HOTTCAD_RAUMTYP`.
  Eine Zuordnungsart `GBXML_RAUMTYP` (Konzept 4.3: „ohne Schema“) ginge nur mit einem Tabellenneubau. Deshalb steht die Tafel im
  Kern (siehe „Offen“).

## Rangfolge je Zone (Konzept 4.3)

1. **DIN-Nummer der Datei:** IFC `EPOS_Zone.DinNummer`, sonst der gbXML-Raumtyp über die Raumtyptafel. Es zählt der
   flächengrößte Raum der Zone, der eine Nummer trägt (`Zonenplan.DateiDinNummer`). Aufgelöst wird über
   `Raumnutzungsvorbelegung.DinTreffer`: zuerst die Zeile `DIN_NUMMER` der Zuordnung (ein „keine“ dort heißt kein Treffer), dann
   das Profil der DIN-Kategorie mit dieser Nummer, ohne Katalog die Vorgabe im Code. Die Stufe geht der Regel Z6 vor. Nur ein
   Profilname aus `EPOS_Zone.Nutzung` mit Katalogtreffer bleibt, denn er ist das Profil selbst. Die Quelle der Planzone ist
   `DIN_NUMMER` mit der Nummer, auch ohne Treffer. Die Zone bleibt dann ohne Profil, und die Modellprüfung schlägt das
   nächstliegende Profil der DIN-Kategorie vor (kleinster Abstand der Nummer, bei Gleichstand die kleinere).
2. **Raumtyp der Quelle** (HottCAD-Raumtyp bzw. Nutzungsklasse) über die Zuordnung, wie bisher.
3. **Gebäudeart** (`Zonenvorbesetzung.KENNUNG_JE_GEBAEUDEART`), wie bisher.
4. **EPOS-Muster** „Wohnen“ bzw. „Sonstige“.

Die Werte der Datei gehen dem Profil vor (E138 F2 a). Die Rangfolge je Größe (`Zonenvorbesetzung.Wirksam`) ist unverändert.
Im Import setzt nur Stufe 1 neu ein Profil. Stufen 2 und 3 bleiben, wie sie waren (Z6, Vorgabe der Gebäudeart). Für jede andere
beheizte Zone ohne Profil kommt der Vorschlag aus der Modellprüfung.

## Ampel in Prüfgruppe 3 (Modellprüfung)

| Farbe | Befund | Bedingung | Vorschlag |
|---|---|---|---|
| grün | — | Profil aus der Datei mit Katalogtreffer, von Hand oder ohne Quellhinweis | — |
| gelb | `PROFIL_AUS_REGEL` | Profil aus Raumtyp, Nutzungsklasse, Gebäudeart oder einem übernommenen Vorschlag | keiner (Hinweis) |
| gelb | `DIN_OHNE_KATALOGTREFFER` | Zone ohne Profil, DIN-Nummer der Datei ohne Treffer | nächstliegendes Profil der DIN-Kategorie (`Vorschlagsherkunft.DinNaechstes`) |
| gelb | `ZONE_OHNE_PROFIL` | beheizte Zone ohne Profil und ohne Kalender — mit oder ohne Werte der Datei; sie erbt vom Gebäude und rechnet | Profil nach Regel |

Rot heißt „nicht rechenfähig“. Eine solche Lage gibt es in Gruppe 3 nicht: Eine Zone ohne Profil, ohne Kalender und ohne Werte
der Datei erbt vom Gebäude und rechnet. Deshalb ist `ZONE_OHNE_PROFIL` immer gelb (Abgleich beim Zusammenführen mit DA-4/DA-7).

Die Vorschläge laufen über den bestehenden Weg (`ModellpruefungCtrl.Uebernehmen` → `ZonenplanCtrl.NutzungUebernehmen`, mit
Rücknahme). Eine Übernahme markiert die Zone in der Sitzung als „Profil aus Regel“. Ausgenommen ist ein Profil der DIN-Nummer
mit Treffer. Die Rücknahme stellt die alte Quelle her. Die Herkunft reicht der Import über `Zonenquellhinweis.ProfilAusRegel`
herein (`ModellpruefungHuelle.HinweiseAus`). `DIN_NUMMER` und `VORGABE_DIN_NUMMER` gelten als Datei; `HOTTCAD_RAUMTYP`,
`IFC_KLASSE` und `VORGABE_GEBAEUDEART` gelten als Regel. Ein gespeichertes Gebäude ohne Hinweise bleibt bei grün, wenn es ein
Profil trägt.

## Raumtyptafel gbXML (`GbxmlRaumtyptafel`)

Die Tafel ordnet 41 Werte der Aufzählung `spaceTypeEnum` einer Nummer der Tabelle 6 zu. Sie trägt nur Namen und Nummern, keine
Kennwerte.

- Büro: geschlossenes Büro → 1 (Einzelbüro), offenes Büro → 3 (Großraumbüro), Besprechung → 4, Kundenhalle der Bank → 5.
- Handel → 6. Unterricht → 8 (auch im Strafvollzug). Auditorium → 9.
- Patientenzimmer → 10. Gästezimmer in Hotel und Motel → 11.
- Gastronomie: Cafeteria → 12, Familien- und Barbereich → 13, Speisenzubereitung → 14.
- Neben- und Verkehrsflächen: WC → 16, Aufenthalt → 17, Flur und Treppe → 19, Technik und Lager → 20.
- Halle: Feinfertigung → 24.
- Veranstaltung und Kultur: Zuschauerbereich in Theater und Kino → 25, Theaterfoyer → 26, Messe und Kongress → 28, Museum → 29.
- Bibliothek: Lesesaal → 30.
- Sport: Spielfeld der Turnhalle → 33, Fitness → 37.
- Gesundheit: Labor → 38, Untersuchung → 39, Operationsraum → 40, Flur im Pflegebereich → 41.
- Lager: Fein- und Schüttgutlager → 43.

Zwölf mehrdeutige Typen sind **benannt ohne Zuordnung**. Sie fallen auf die nächste Rangstufe:

| Typ | mögliche Nummern |
|---|---|
| Speisebereich allgemein | 12 oder 13 |
| Lobby, Atrium (zwei Typen) | 19 oder 26 |
| Umkleide | 16 oder 18 |
| Werkstatt | 23 oder 24 |
| Fertigung mit niedriger und hoher Halle (zwei Typen) | 22 bis 24 |
| Bibliotheksregale | 31 oder 32 |
| zwei Parkhaustypen | 34 oder 35 |
| Wohnheim | Wohnen, keine Nummer der Tabelle 6 |

Unbekannte Typen gelten ebenso. Die Namen sind aus dem Gedächtnis gesetzt, weil die Schemakopie nicht beiliegt (D17). Der Test
`GbxmlRaumtyptafelTests.Jeder_Name_der_Tafel_steht_in_spaceTypeEnum_der_Schemakopie` gleicht ab, sobald die Kopie lokal liegt.
Ohne die Kopie verzichtet er benannt.

## DIN-Nummer in IFC

`EPOS_Zone.DinNummer` steht als `IfcLabel` mit der Nummer als Text. Der Export schreibt sie, wenn die Zone ein Profil der
DIN-Kategorie mit Nummer trägt: Profil der Zone, sonst die Nutzung ihrer Kalender (`GebaeudeExportSatz.Zonendinnummern`). Der
Leser nimmt Text oder eine ganze Zahl von 1 bis 999 (`IfcKonditionierungssatz.DinNummerLesen`). Schreiber und Anreicherung
nutzen denselben Weg (`Zonenwerte`). Ohne DIN-Profil bleibt die Datei byte-gleich.

## Dateien

- neu: `EPOS.Kern/Allgemein/Import/Gbxml/GbxmlRaumtyptafel.cs`, `EPOS.Kern.Tests/ProfilpruefungImportTests.cs`,
  `EPOS.Kern.Tests/GbxmlRaumtyptafelTests.cs`
- geändert:
  - Kern: `Raumnutzungsvorbelegung` (`Aus`, `DinTreffer`, `AusDinKatalog`, `NaechstesDinProfil`, `DinNummer`);
    `Modellpruefung` (zwei Codes, Ampel); `Modellpruefungstypen` (`DinNaechstes`); `Modellvorschlagsregeln`;
    `ModellpruefungCtrl`; `Zonenplan` (`DinNummerAnwenden`, `DateiDinNummer`); `GebaeudeAbbild` (`AbbildKonditionierung.DinNummer`);
    `IfcKonditionierungssatz`; `IfcAbbildBauer`; `GebaeudeExportSatz`
  - Hülle: `EPOS.UI.Daten/Bedarf/ModellpruefungHuelle.cs`
  - Tests: `EPOS.Kern.Tests/IfcKonditionierungRundlaufTests.cs` (DIN-Nummer im Rundlauf)
  - Ressourcen: `Resource.resx`, `Resource.en-US.resx` (+3 je Sprache: `MP_BEFUND_DIN_OHNE_KATALOGTREFFER`,
    `MP_BEFUND_PROFIL_AUS_REGEL`, `MP_VORSCHLAG_PROFIL_DINNAECHSTES`); `Resource.Designer.cs` (Designer wiederholbar, zweiter
    Lauf +0)

## Nachweis

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Gefilterter Lauf (Profil, Raumnutzung, Modellpruefung, Gbxml, Ifc, GebaeudeImport, ZonenNutzung, Modultrennung, Dokumentation,
  Repository, Resource): 1623 Tests, 1618 bestanden, 0 fehlgeschlagen, 5 übersprungen (Probenerzeuger). Darin:
  - `ProfilpruefungImportTests`: 10 Tests, je Rangstufe und je Ampelfarbe mit Gegenprobe, dazu Importzone und Export.
  - `GbxmlRaumtyptafelTests`: 2 Tests, der Schemaabgleich verzichtet ohne Kopie.
  - `IfcKonditionierungRundlaufTests`: DIN-Nummer im IFC-Rundlauf.
  - Die Importproben sind unverändert (keine Probe trägt `spaceType` oder `EPOS_Zone.DinNummer`).
- Windows-Schale nicht gebaut: Keine Datei der Schale und kein Razor ist berührt; die Hülle liegt im Kern-Filter.
- Referenzlauf: Kein Rechenweg ist berührt. Der Lauf byte-gleich zu R53 bleibt bei der Orchestrierung.

## Offen

- **Schemaabgleich der Tafel:** Die Namen gegen `spaceTypeEnum` der lokalen Schemakopie prüfen. Der Test meldet jeden Namen, den
  das Schema nicht kennt, und listet die nicht eingestuften Werte.
- **Zuordnungsart `GBXML_RAUMTYP`** im Katalog, damit der Anwender die Tafel umstellen kann. Das braucht einen Tabellenneubau von
  `Tab_Raumnutzungszuordnung` (CHECK der Spalte `Art`), also einen Schemaschritt. Als Nachzug benannt, nicht gebaut.
- **Prüfen gegen Profilwerte** (Konzept 4.3: Dateiwerte gegen Profil, Abweichung über eine Schwelle als Hinweis): Das kommt mit
  DA-6, weil die DIN-Profile vorher keine Kennwerte tragen. Die Ampel unterscheidet bis dahin nicht „mit Werten“ und „ohne Werte“.
- **Rot heißt „nicht rechenfähig“ (erledigt beim Zusammenführen):** Eine beheizte Zone ohne Profil und ohne Dateiwerte erbt vom
  Gebäude und rechnet; `ZONE_OHNE_PROFIL` ist deshalb gelb, Gruppe 3 kennt keinen Rot-Fall (`Gelb_Zone_ohne_Profil_und_ohne_Dateiwerte_erbt_vom_Gebaeude`).
- Die Herkunft „Profil aus Regel“ gilt nur in der Sitzung bzw. aus den Hinweisen des Imports. Eine gespeicherte Herkunftsspalte
  an der Zone gibt es nicht.
- Eine Eigenschaft mit DIN-Nummer aus fremden IFC-Sätzen (Konzept 4.3: „oder eine Eigenschaft mit DIN-Nummer“) wird nicht
  gelesen. Gelesen wird nur `EPOS_Zone.DinNummer`.

## Logbuch-Vorschlag

Beim Gebäudeimport ordnet EPOS-Plan jeder Zone das Nutzungsprofil der DIN/TS 18599-10 zu, wenn die Datei eine Profilnummer oder
einen gbXML-Raumtyp trägt, und die Modellprüfung zeigt mit einer Ampel, woher das Profil jeder Zone kommt.
